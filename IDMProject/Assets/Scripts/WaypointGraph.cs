using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple directed graph class.
/// </summary>
public class WaypointGraph
{
    public WaypointGraph(IList<WaypointNode> waypoints)
    {
        m_Entrances = new List<WaypointNode>();
        m_Exits = new List<WaypointNode>();
        m_RegularNodes = new List<WaypointNode>();

        foreach (var wp in waypoints)
        {
            if (wp.waypointType == WaypointNode.WaypointType.Entrance)
            {
                m_Entrances.Add(wp);
            }
            else if (wp.waypointType == WaypointNode.WaypointType.Exit)
            {
                m_Exits.Add(wp);
            }
            else
            {
                m_RegularNodes.Add(wp);
            }
        }
    }

    List<WaypointNode> m_Entrances;
    List<WaypointNode> m_Exits;
    List<WaypointNode> m_RegularNodes;

    /// <summary>
    /// Generate a random plausible path for a shopper.
    /// This picks a random start and goal node, and a few intermediate nodes, and computes the path
    /// between them.
    /// </summary>
    /// <param name="numGoals"></param>
    /// <returns>List of Waypoint nodes for the path.</returns>
    public List<WaypointNode> GenerateRandomPath(int numGoals)
    {
        HashSet<WaypointNode> goals = new HashSet<WaypointNode>();

        // Select numGoals regular waypoints without replacement
        // TODO: Fisher-Yates shuffle instead?
        while (goals.Count < numGoals)
        {
            var randomIndex = UnityEngine.Random.Range(0, m_RegularNodes.Count);
            if (m_RegularNodes[randomIndex].Passthrough)
            {
                continue;
            }
            goals.Add(m_RegularNodes[randomIndex]);
        }

        // Randomly pick from the available m_Entrances and exits.
        var entrance = m_Entrances[UnityEngine.Random.Range(0, m_Entrances.Count)];
        var exit = m_Exits[UnityEngine.Random.Range(0, m_Exits.Count)];

        // Order the goals.
        // Just greedily pick the closest one to the most recent point (at least, that's how I go shopping).
        List<WaypointNode> orderedGoals = new List<WaypointNode>();
        orderedGoals.Add(entrance);
        var current = entrance;
        while (goals.Count > 0)
        {
            WaypointNode closestNode = null;
            float closestDistance = float.MaxValue;
            foreach (var g in goals)
            {
                var distance = Vector3.Distance(current.transform.position, g.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = g;
                }
            }
            orderedGoals.Add(closestNode);
            goals.Remove(closestNode);
        }
        orderedGoals.Add(exit);

        // Dijkstra Search
        List<WaypointNode> path = new List<WaypointNode>();
        for (var i = 0; i < orderedGoals.Count-1; i++)
        {
            var subPath = FindPath(orderedGoals[i], orderedGoals[i + 1]);
            if (subPath == null)
            {
                // In theory, it's possible for pathfinding to fail.
                // That shouldn't happen with the current graph, though.
                return null;
            }

            path.AddRange(subPath);
            // The last point now will be the same as the first point in the next subpath, so pop it
            path.RemoveAt(path.Count-1);
        }
        // And add the last goal back
        path.Add(exit);
        return path;
    }

    /// <summary>
    /// Computes the shorted path between the start and end nodes.
    /// Uses the Euclidean distance for edge costs.
    /// Note that this implementation is very inefficient - it uses Dijkstra's algorithm instead of A*, and does
    /// not have a proper priority queue. For the sizes of graphs that we're dealing with, this should be OK though.
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="endNode"></param>
    /// <returns>List of nodes for the path, or null if a path can't be found.</returns>
    public static List<WaypointNode> FindPath(WaypointNode startNode, WaypointNode endNode)
    {
        // TODO implement a priority queue. For now we have to linear search to find the lowest-cost node.

        // Distance from the startNode to each open node.
        Dictionary<WaypointNode, float> pathCost = new Dictionary<WaypointNode, float>();

        // The parent of each node in the path, or null for the startNode
        Dictionary<WaypointNode, WaypointNode> parents = new Dictionary<WaypointNode, WaypointNode>();

        // Nodes that have already been explored.
        HashSet<WaypointNode> closed = new HashSet<WaypointNode>();

        parents[startNode] = null;
        pathCost[startNode] = 0.0f;

        while (!closed.Contains(endNode))
        {
            if (pathCost.Count == 0)
            {
                // Unreachable
                return null;
            }
            // "pop" the lowest cost node
            var currentNode = FindLowestValue(pathCost);
            var currentCost = pathCost[currentNode];
            foreach (var neighbor in currentNode.Edges)
            {
                if (closed.Contains(neighbor))
                {
                    continue;
                }

                var costToNeighbor = Vector3.Distance(currentNode.transform.position, neighbor.transform.position);
                if (!pathCost.ContainsKey(neighbor) || currentCost + costToNeighbor < pathCost[neighbor])
                {
                    // Update cost and parent for the neighbor
                    pathCost[neighbor] = currentCost + costToNeighbor;
                    parents[neighbor] = currentNode;
                }
            }

            pathCost.Remove(currentNode);
            closed.Add(currentNode);
        }

        // Walk backwards from the goal
        List<WaypointNode> pathOut = new List<WaypointNode>();
        var current = endNode;
        while (current != null)
        {
            pathOut.Add(current);
            current = parents[current];
        }
        pathOut.Reverse();
        return pathOut;
    }

    /// <summary>
    /// Find the key corresponding to the minimum value.
    /// TODO replace with minheap/priority queue
    /// </summary>
    /// <param name="heap"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    static T FindLowestValue<T>(Dictionary<T, float> heap)
    {
        var lowestVal = float.MaxValue;
        T lowestKey = default(T);
        foreach (var entry in heap)
        {
            if (entry.Value < lowestVal)
            {
                lowestKey = entry.Key;
                lowestVal = entry.Value;
            }
        }

        return lowestKey;
    }
}
