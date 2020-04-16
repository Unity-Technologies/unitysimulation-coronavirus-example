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
        Register
    }

    public WaypointType waypointType = WaypointType.Default;
    [Range(0, 90)]
    public float EdgeAngleThresholdDegrees = 2.5f;
    public bool Passthrough = false;
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
        var drawColor = IsEntrance()? Color.green : IsExit() ? Color.red : Color.cyan;
        drawColor.a = .75f;
        Gizmos.color = drawColor;

        Gizmos.DrawCube(transform.position, transform.localScale);
    }

    public void DrawEdge(WaypointNode neighbor)
    {
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
        const float offsetFraction = .65f;
        var arrowStart = offsetFraction * end + (1.0f - offsetFraction) * start;
        var arrowSide1 = arrowStart - drawMagnitude * dir + drawMagnitude * side;
        var arrowSide2 = arrowStart - drawMagnitude * dir - drawMagnitude * side;
        Gizmos.DrawLine(arrowStart, arrowSide1);
        Gizmos.DrawLine(arrowStart, arrowSide2);
    }

    public bool IsDefault()
    {
        return waypointType == WaypointType.Default;
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

    /// <summary>
    /// Whether or not this waypoint can make a connection with the other one.
    /// * Outgoing edges from exits, and incoming edges to entrances, are never allowed.
    /// * Angle check: the other waypoint must be within an angle threshold of the cardinal directions
    ///     (forward, left, right, backwards) relative to the transform of this waypoint.
    ///     If simulationIsOneWay is true, then backwards is excluded.
    /// * Raycast check - the raycast from this waypoint to the other must hit the other.
    /// </summary>
    /// <param name="simulationIsOneWay"></param>
    /// <param name="otherWaypoint"></param>
    /// <returns></returns>
    public bool ShouldConnect(bool simulationIsOneWay, WaypointNode otherWaypoint)
    {
        if (otherWaypoint.IsExit() && waypointType != WaypointType.Register)
            return false;

        if (IsExit() || otherWaypoint.IsEntrance())
        {
            return false;
        }

        var directions = new Vector3[]
        {
            transform.forward,
            -transform.right,
            transform.right,
            -transform.forward,
        };

        // TODO Convert from degrees to cos(radians) only when the angle threshold is set.
        var angleThreshold = EdgeAngleThresholdDegrees;
        var cosThetaThreshold = Mathf.Cos(Mathf.Deg2Rad * angleThreshold);

        var dirToWaypoint = (otherWaypoint.transform.position - transform.position).normalized;
        int shopperLayer = LayerMask.NameToLayer("Shopper");

        // Don't consider backwards edges if simulationIsOneWay
        // Only consider forward edges if Passthrough
        var numDirections = Passthrough ? 1 : simulationIsOneWay ? 3 : 4;
        for (var dirIndex = 0; dirIndex < numDirections; dirIndex++)
        {
            var dotProd = Vector3.Dot(directions[dirIndex], dirToWaypoint);
            if(dotProd < cosThetaThreshold)
            {
                continue;
            }

            // Raycast check
            // TODO clean up - we only need one raycast, not in the loop
            RaycastHit hitInfo;
            LayerMask raycastLayer = Physics.DefaultRaycastLayers & ~(1 << shopperLayer);
            var didHit = Physics.Raycast(transform.position, dirToWaypoint, out hitInfo,  Mathf.Infinity, raycastLayer);
            if (!didHit)
            {
                continue;
            }

            var hitWaypoint = hitInfo.collider?.GetComponent<WaypointNode>();
            if (hitWaypoint == otherWaypoint)
            {
                return true;
            }
        }

        return false;
    }
}


