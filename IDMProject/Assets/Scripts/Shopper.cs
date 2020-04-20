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

    public float          Speed = 1.0f;
    public Material       HealthyMaterial;
    [FormerlySerializedAs("ContagiousMaterial")]
    public Material       InfectiousMaterial;
    public Material       ExposedMaterial;
    Status                m_InfectionStatus;
    HashSet<WaypointNode> m_VisistedNodes = new HashSet<WaypointNode>();
    private int           m_MaxNumberOfUniqueNodes = 0;
    private bool          m_WantsToExit = false;
    public float          BillingTime = 0.0f;

    public BehaviorType Behavior = BehaviorType.ShoppingList;
    private StoreSimulationQueue m_BillingQueue;
    WaypointNode previousNode;
    WaypointNode nextNode;

    internal List<WaypointNode> path;

    StoreSimulation m_Simulation;
    Vector3 m_PreviousPosition;
    public GameObject Regsiter;

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

    public Vector3 previousPosition => m_PreviousPosition;

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
        previousNode = node;

        if (m_WantsToExit && m_VisistedNodes.Contains(nextNode))
        {
            // Pick the next node randomly
            nextNode = node.GetRandomNeighbor(previousNode);
        }
        else
        {
            nextNode = node.GetRandomNeighbor();
            m_VisistedNodes.Add(nextNode);
        }

        var worldPos = previousNode.transform.position;
        transform.position = worldPos;
    }

    public void SetPath(List<WaypointNode> _path)
    {
        path = _path;
        // TODO convert to queue
        previousNode = path[0];
        path.RemoveAt(0);
        nextNode = path[0];

        var worldPos = previousNode.transform.position;
        transform.position = worldPos;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_PreviousPosition = transform.position;
        m_MaxNumberOfUniqueNodes = UnityEngine.Random.Range(3, m_Simulation.waypoints.Length);
        m_BillingQueue = gameObject.GetComponent<StoreSimulationQueue>();
    }

    WaypointNode EnterInAvailableQueue(WaypointNode currentNode)
    {
        var regsiterNodes = currentNode.Edges.Where(e => e.waypointType == WaypointNode.WaypointType.Register);
        var nonRegsiterNodes = currentNode.Edges.Where(e => e.waypointType != WaypointNode.WaypointType.Register).ToArray();

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
            if (BillingTime >= m_Simulation.MaxPurchaseTime)
            {
                Behavior = BehaviorType.Exiting;
                if (Regsiter != null)
                    simulation.InformExit(this);
                nextNode = previousNode.Edges[0];
            }
            else
            {
                BillingTime += Time.deltaTime;
            }
        }
        else
        {

            if (Behavior == BehaviorType.InQueue && nextNode.waypointType == WaypointNode.WaypointType.Register)
            {
                RaycastHit hit;
                //var layermask = ~(1 << 3);
                Debug.DrawRay(transform.position, (nextNode.transform.position - transform.position) * 20, Color.red);
                if (Physics.Raycast(transform.position, (nextNode.transform.position - transform.position), out hit, 1.5f))
                {
                    if (hit.collider.CompareTag("Shopper"))
                    {
                        return;
                    }
                }
            }

            if (nextNode.waypointType == WaypointNode.WaypointType.Register && Behavior != BehaviorType.InQueue)
            {
                nextNode = simulation.EnterInAvailableQueue(this,previousNode);
            }

            var reachedEnd = UpdateInterpolation();
            if (reachedEnd)
            {

                if (nextNode.waypointType == WaypointNode.WaypointType.Register && Behavior == BehaviorType.InQueue)
                    Behavior = BehaviorType.Billing;

                if (nextNode.Edges.Count == 0)
                {
                    // Need a respawn
                    simulation.Despawn(this);
                }
                else
                {
                    var previousPos = previousNode.transform.position;
                    var currentPos = nextNode.transform.position;
                    previousNode = nextNode;
                    if (path != null)
                    {
                        // TODO convert to queue
                        path.RemoveAt(0);
                        nextNode = path[0];
                    }
                    else
                    {
                        nextNode = nextNode.GetRandomNeighborInDirection(previousPos, currentPos);
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

    void OnDrawGizmos()
    {
//        if (path != null)
//        {
//            DrawPath(path, Color.green);
//        }
    }

    bool UpdateInterpolation()
    {
        if (previousNode == null || nextNode == null)
        {
            return false;
        }
        bool atEnd = false;
        var prevToCur = transform.position - previousNode.transform.position;
        var prevToNext = nextNode.transform.position - previousNode.transform.position;

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
        var newPostion = previousNode.transform.position + tNew * prevToNext;
        transform.position = newPostion;

        return atEnd;
    }
}
