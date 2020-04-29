using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

public class Shopper : MonoBehaviour
{
    public enum Status
    {
        Healthy,
        Infectious,
        Exposed
    }

    public enum BehaviorType
    {
        RandomWalk,
        ShoppingList,
        Billing,
        InQueue,
        Exiting
    }

    public float                Speed = 1.0f;
    public Material             HealthyMaterial;
    [FormerlySerializedAs("ContagiousMaterial")]
    public Material             InfectiousMaterial;
    public Material             ExposedMaterial;
    public float                BillingTime = 0.0f;
    public BehaviorType         Behavior = BehaviorType.ShoppingList;
    public StoreSimulationQueue Regsiter;
    
    Status                       m_InfectionStatus;
    HashSet<WaypointNode>        m_VisistedNodes = new HashSet<WaypointNode>();
    private int                  m_MaxNumberOfUniqueNodes = 0;
    private bool                 m_WantsToExit = false;
    public ParticleSystem        m_ParticleSystem;
    
    private StoreSimulationQueue m_BillingQueue;
    private WaypointNode         m_PreviousNode;
    private WaypointNode         m_NextNode;

    internal List<WaypointNode>  m_Path;

    private StoreSimulation      m_Simulation;
    private Vector3              m_PreviousPosition;

    public StoreSimulation simulation
    {
        get => m_Simulation;
        set => m_Simulation = value;
    }

    public Status InfectionStatus
    {
        get => m_InfectionStatus;
        set { SetStatus(value); }
    }

    public Vector3 PreviousPosition => m_PreviousPosition;

    void SetStatus(Status s)
    {
        Material m = null;
        switch (s)
        {
            case Status.Healthy:
                m = HealthyMaterial;
                break;
            case Status.Infectious:
                m = InfectiousMaterial;
                break;
            case Status.Exposed:
                m = ExposedMaterial;
                break;
        }

        m_InfectionStatus = s;
        var renderer = GetComponent<Renderer>();
        renderer.material = m;
    }

    public void PlayRippleEffect()
    {
        if (m_ParticleSystem != null)
        {
            m_ParticleSystem.gameObject.SetActive(true);
            m_ParticleSystem.Play();
        }

    }

    public bool IsHealthy()
    {
        return m_InfectionStatus == Status.Healthy;
    }

    public bool IsInfectious()
    {
        return m_InfectionStatus == Status.Infectious;
    }

    public bool IsExposed()
    {
        return m_InfectionStatus == Status.Exposed;
    }

    public void SetWaypoint(WaypointNode node, bool reset = false)
    {
        if (m_VisistedNodes.Count != m_MaxNumberOfUniqueNodes && !reset)
            m_WantsToExit = true;
        m_PreviousNode = node;

        if (m_WantsToExit && m_VisistedNodes.Contains(m_NextNode))
        {
            // Pick the next node randomly
            m_NextNode = node.GetRandomNeighbor(m_PreviousNode);
        }
        else
        {
            m_NextNode = node.GetRandomNeighbor();
            m_VisistedNodes.Add(m_NextNode);
        }

        var worldPos = m_PreviousNode.transform.position;
        transform.position = worldPos;
    }

    public void SetPath(List<WaypointNode> _path)
    {
        m_Path = _path;
        // TODO convert to queue
        m_PreviousNode = m_Path[0];
        m_Path.RemoveAt(0);
        m_NextNode = m_Path[0];

        var worldPos = m_PreviousNode.transform.position;
        transform.position = worldPos;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_PreviousPosition = transform.position;
        m_MaxNumberOfUniqueNodes = UnityEngine.Random.Range(3, m_Simulation.Waypoints.Length);
        m_BillingQueue = gameObject.GetComponent<StoreSimulationQueue>();
        m_ParticleSystem.gameObject.SetActive(false);
    }

    WaypointNode EnterInAvailableQueue(WaypointNode currentNode)
    {
        var regsiterNodes = currentNode.Edges.Where(e => e.waypointType == WaypointNode.WaypointType.Register);
        var nonRegsiterNodes = currentNode.Edges.Where(e => e.waypointType != WaypointNode.WaypointType.Register && e.waypointType != WaypointNode.WaypointType.Exit).ToArray();

        foreach (var node in regsiterNodes)
        {
            var queue = node.gameObject.GetComponent<StoreSimulationQueue>();
            if (queue.EnterTheQueue(this))
            {
                Behavior = BehaviorType.InQueue;
                return node;
            }
        }

        return nonRegsiterNodes[UnityEngine.Random.Range(0, nonRegsiterNodes.Length)];
    }

    // Update is called once per frame
    void Update()
    {
        m_PreviousPosition = transform.position;

        if (Behavior == BehaviorType.Billing)
        {
            if (BillingTime <= 0)
            {
                Behavior = BehaviorType.Exiting;
                if (Regsiter != null)
                    simulation.InformExit(this);
                m_NextNode = m_PreviousNode.Edges[0];
            }
            else
            {
                BillingTime -= Time.deltaTime;
            }
        }
        else
        {

            if (Behavior == BehaviorType.InQueue && m_NextNode.waypointType == WaypointNode.WaypointType.Register)
            {
                RaycastHit hit;
                //var layermask = ~(1 << 3);
                Debug.DrawRay(transform.position, (m_NextNode.transform.position - transform.position) * 20, Color.red);
                if (Physics.Raycast(transform.position, (m_NextNode.transform.position - transform.position), out hit, 1.5f))
                {
                    if (hit.collider.CompareTag("Shopper"))
                    {
                        return;
                    }
                }
            }

            if (m_NextNode.waypointType == WaypointNode.WaypointType.Register && Behavior != BehaviorType.InQueue)
            {
                m_NextNode = simulation.EnterInAvailableQueue(this,m_PreviousNode);
            }

            var reachedEnd = UpdateInterpolation();
            if (reachedEnd)
            {

                if (m_NextNode.waypointType == WaypointNode.WaypointType.Register && Behavior == BehaviorType.InQueue)
                    Behavior = BehaviorType.Billing;

                if (m_NextNode.waypointType == WaypointNode.WaypointType.Exit)
                {
                    // Need a respawn
                    simulation.Despawn(this);
                }
                else
                {
                    var previousPos = m_PreviousNode.transform.position;
                    var currentPos = m_NextNode.transform.position;
                    m_PreviousNode = m_NextNode;
                    if (m_Path != null)
                    {
                        // TODO convert to queue
                        m_Path.RemoveAt(0);
                        m_NextNode = m_Path[0];
                    }
                    else
                    {
                        m_NextNode = m_NextNode.GetRandomNeighborInDirection(previousPos, currentPos);
                    }
                }
            }
        }


    }

    static void DrawPath(List<WaypointNode> path, Color color)
    {
        Gizmos.color = color;
        for (var i = 0; i < path.Count - 1; i++)
        {
            var p1 = path[i].transform.position;
            var p2 = path[i + 1].transform.position;
            Gizmos.DrawLine(p1, p2);
        }
    }

    bool UpdateInterpolation()
    {
        if (m_PreviousNode == null || m_NextNode == null)
        {
            return false;
        }
        bool atEnd = false;
        var prevToCur = transform.position - m_PreviousNode.transform.position;
        var prevToNext = m_NextNode.transform.position - m_PreviousNode.transform.position;

        var distancePrevToNext = prevToNext.magnitude;
        // Fraction from 0 to 1
        var t = Vector3.Dot(prevToCur, prevToNext) / Vector3.Dot(prevToNext, prevToNext);
        if (float.IsNaN(t))
        {
            // This should be considered an error (chances are, the next and previous nodes are the same)
            // But patch it up to prevent NaN transforms.
            t = 0.0f;
        }

        // Advance by speed * timestep units
        var distance = Speed * Time.deltaTime;

        // TODO "carry over" the leftover distance along the next segment. This will let us run at higher framerate.
        var tNew = t + distance / distancePrevToNext;
        if (tNew > 1.0f)
        {
            tNew = 1.0f;
            atEnd = true;
        }

        // Interpolate
        var newPostion = m_PreviousNode.transform.position + tNew * prevToNext;
        transform.position = newPostion;

        return atEnd;
    }
}
