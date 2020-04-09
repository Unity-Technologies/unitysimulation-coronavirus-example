using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Shopper : MonoBehaviour
{
    public enum Status
    {
        Healthy,
        Contagious,
        Exposed
    }

    public enum BehaviorType
    {
        RandomWalk,
        ShoppingList
    }

    public float Speed = 15.0f;
    public Material HealthyMaterial;
    public Material ContagiousMaterial;
    public Material ExposedMaterial;
    Status m_InfectionStatus;
    public BehaviorType Behavior = BehaviorType.ShoppingList;

    WaypointNode previousNode;
    WaypointNode nextNode;

    internal List<WaypointNode> path;

    StoreSimulation m_Simulation;
    Vector3 m_PreviousPosition;

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
            case Status.Contagious:
                m = ContagiousMaterial;
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

    public bool IsContagious()
    {
        return m_InfectionStatus == Status.Contagious;
    }

    public bool IsExposed()
    {
        return m_InfectionStatus == Status.Exposed;
    }

    public void SetWaypoint(WaypointNode node)
    {
        previousNode = node;
        // Pick the next node randomly
        nextNode = node.GetRandomNeighbor();

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
    }

    // Update is called once per frame
    void Update()
    {
        m_PreviousPosition = transform.position;

        var reachedEnd = UpdateInterpolation();
        if (reachedEnd)
        {
            if (nextNode.IsExit() || nextNode.Edges.Count == 0)
            {
                // Need a respawn
                simulation.Despawn(this);
                return;
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
