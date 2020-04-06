using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaypointNode : MonoBehaviour
{
    public enum WaypointType
    {
        Default = 0,
        Entrance = 1,
        Exit = 2,
    }

    public WaypointType waypointType = WaypointType.Default;
    public List<WaypointNode> Edges = new List<WaypointNode>();
    StoreSimulation m_Simulation;



    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public WaypointNode GetRandomNeighbor(WaypointNode exclude = null)
    {
        // TODO handle no edges
        var index = Random.Range(0, Edges.Count);
        if (Edges[index] == exclude)
        {
            // Picked the one we were trying to avoid
            // Add a random offset (mod the length) to
            // get a new node with uniform distribution
            var offset = Random.Range(1, Edges.Count);
            index = (index + offset) % Edges.Count;
        }

        return Edges[index];
    }

    /// <summary>
    /// Select a random edge from the list, biased towards edges that going in the same direction.
    /// Edges that travel backwards are still possible but less likely.
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    public WaypointNode GetRandomNeighborInDirection(Vector3 previous, Vector3 current)
    {
        if (Edges.Count == 1)
        {
            return Edges[0];
        }

        Vector3 dir = (current - previous).normalized;

        // Compute the probability of selecting an edge.
        // The probability is proportional to 1 + dot(dir, (position - current).normalized)
        // This means that edges in the same direction have weight 2
        // Left and right turns have weight 1
        // Turning around has weight 0

        // TODO reuse allocation
        var cumulativeSum = new float[Edges.Count];
        var sumWeights = 0.0f;
        for (var i = 0; i < Edges.Count; i++)
        {
            var outgoingDir = (Edges[i].transform.position - current).normalized;
            var weight = 1.0f + Vector3.Dot(dir, outgoingDir);
            sumWeights += weight;
            cumulativeSum[i] = sumWeights;
        }

        var p = Random.Range(0.0f, sumWeights);
        var edgeIndex = 0;
        while (cumulativeSum[edgeIndex] < p)
        {
            ++edgeIndex;
        }

        return Edges[edgeIndex];
    }

    void OnDrawGizmos()
    {
        DrawEdges();
    }

    void DrawEdges()
    {
        foreach (var neighbor in Edges)
        {
            if (neighbor == null)
            {
                continue;
            }
            var drawColor = Color.cyan;
            drawColor.a = .5f;
            Gizmos.color = drawColor;
            Gizmos.DrawLine(transform.position, neighbor.transform.position);
        }
    }

    public bool IsExit()
    {
        return waypointType == WaypointType.Exit;
    }

    public bool IsEntrance()
    {
        return waypointType == WaypointType.Entrance;
    }

    public StoreSimulation simulation
    {
        get => m_Simulation;
        set => m_Simulation = value;
    }

    public void CheckRaycastConnection(Vector3 direction)
    {
        var rayStart = transform.position;
        RaycastHit hitInfo;
        var didHit = Physics.Raycast(rayStart, direction, out hitInfo);
        if (!didHit)
        {
            return;
        }

        // See if we hit another waypoint
        var otherWaypoint = hitInfo.collider?.GetComponent<WaypointNode>();
        if (otherWaypoint == null)
        {
            return;
        }

        if (Edges.Contains(otherWaypoint))
        {
            // This waypoint already hit us
            return;
        }

        // Don't add incoming edges to Entrances, or outgoing edges from Exits.
        if (!otherWaypoint.IsEntrance() && !IsExit())
        {
            Edges.Add(otherWaypoint);
        }
        if(!IsEntrance() && !otherWaypoint.IsExit())
        {
            otherWaypoint.Edges.Add(this);
        }

    }
}


