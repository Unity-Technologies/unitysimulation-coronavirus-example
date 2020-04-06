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

    // Whether this node supports one-way connections, or always two-way.
    // If true and an incoming edge is not in the forward direction or left/right, it is rejected.
    public bool SupportsOneWay = true;
    StoreSimulation m_Simulation;

    // Threshold for determining when to accept edges when in one-way mode.
    // The dot product between the desired and proposed directions are computed, and rejected if it's
    // less than the threshold.
    // This allows edges from the left and right, but not reverse the intended direction.
    const float k_OneWayDotProductThreshold = -0.1f;

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
            // Offset slightly to one side, so that one-way edges are clearer
            Vector3 start = transform.position;
            Vector3 end = neighbor.transform.position;
            Vector3 dir = (end - start).normalized;
            Vector3 side = Vector3.Cross(transform.up, dir);

            var drawMagnitude = .5f;
            start += drawMagnitude * side;
            end += drawMagnitude * side;

            Gizmos.DrawLine(start, end);

            // Draw an arrow head part-way along the line too (slightly closer to the end)
            var arrowStart = .6f * end + .4f * start;
            var arrowSide1 = arrowStart - drawMagnitude * dir + drawMagnitude * side;
            var arrowSide2 = arrowStart - drawMagnitude * dir - drawMagnitude * side;
            Gizmos.DrawLine(arrowStart, arrowSide1);
            Gizmos.DrawLine(arrowStart, arrowSide2);
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

    public void CheckRaycastConnection(bool simulationIsOneWay, Vector3 direction)
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

        // If we hit a one-way node, and we're enforcing one-way aisles in the simulation,
        // then filter on the direction.
        if (simulationIsOneWay && otherWaypoint.SupportsOneWay)
        {
            var dot = Vector3.Dot(direction, otherWaypoint.transform.forward);
            if (dot < k_OneWayDotProductThreshold)
            {
                return;
            }
        }

        // Don't add incoming edges to Entrances, or outgoing edges from Exits.
        if (!otherWaypoint.IsEntrance() && !IsExit())
        {
            Edges.Add(otherWaypoint);
        }

    }
}


