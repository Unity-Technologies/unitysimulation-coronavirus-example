using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class StoreSimulation : MonoBehaviour
{
    static double s_TicksToSeconds = 1e-7; // 100 ns per tick

    [FormerlySerializedAs("NumShoppers")]
    [Header("Store Parameters")]
    public int DesiredNumShoppers = 10;
    public int DesiredNumContagious = 1;
    public float SpawnCooldown= 1.0f;
    public bool OneWayAisles = true;

    // Exposure probability parameters.
    // These are given as the probability of a healthy person converting to exposed over the course of one second.
    // During simulation, these probability are linearly interpolated based on distance to the contagious person
    // and modified to account for the timestep.
    [Header("Exposure Parameters")]
    public float ExposureProbabilityAtZeroDistance = 0.5f;
    public float ExposureProbabilityAtMaxDistance = 0.0f;
    public float ExposureDistanceMeters = 1.8288f; // Six feet in meters


    [Header("Graphics Parameters")]
    public GameObject ShopperPrefab;

    WaypointNode[] waypoints;
    List<WaypointNode> entrances;
    List<WaypointNode> exits;
    List<WaypointNode> regularNodes;
    HashSet<Shopper> allShoppers;
    float spawnCooldownCounter;
    int numContagious;

    // Results
    int finalHealthy;
    int finalExposed;

    void Awake()
    {
        InitWaypoints();
        allShoppers = new HashSet<Shopper>();
    }

    // Update is called once per frame
    void Update()
    {
        // Cooldown on respawns - can only respawn when the counter is 0 (or negative).
        // The counter resets to SpawnCooldown when a customer is spawned.
        spawnCooldownCounter -= Time.deltaTime;
        if (spawnCooldownCounter <= 0 && allShoppers.Count < DesiredNumShoppers)
        {
            var newShopperGameObject = Instantiate(ShopperPrefab);
            var newShopper = newShopperGameObject.GetComponent<Shopper>();
            Spawn(newShopper);
            allShoppers.Add(newShopper);
            spawnCooldownCounter = SpawnCooldown;
        }

        UpdateExposure();
    }

    void OnDisable()
    {
        // Update the final counts.
        foreach (var s in allShoppers)
        {
            if (s.IsHealthy())
            {
                finalHealthy++;
            }

            if (s.IsExposed())
            {
                finalExposed++;
            }
        }

        var exposureRate = finalExposed + finalHealthy == 0 ? 0 : finalExposed / (float)(finalExposed + finalHealthy);
        Debug.Log($"total healthy: {finalHealthy}  total exposed: {finalExposed}  exposure rate: {exposureRate}%");

    }

    /// <summary>
    /// Called by the shopper when it reaches the exit.
    /// </summary>
    /// <param name="s"></param>
    public void Spawn(Shopper s)
    {
        s.simulation = this;
        if (s.Behavior == Shopper.BehaviorType.ShoppingList)
        {
            var path = GenerateRandomPath(6);
            if(path != null)
            {
                s.SetPath(path);
            }
        }

        if (s.path == null)
        {
            // Pick a random entrance for the start position
            var startWp = entrances[UnityEngine.Random.Range(0, entrances.Count)];
            s.SetWaypoint(startWp);
        }

        // Randomize the movement speed between [.75, 1.25] of the default speed
        var speedMult = UnityEngine.Random.Range(.75f, 1.25f);
        s.Speed *= speedMult;

        if (numContagious < DesiredNumContagious)
        {
            s.InfectionStatus = Shopper.Status.Contagious;
            numContagious++;
        }
    }

    public void Despawn(Shopper s)
    {
        if (s.IsContagious())
        {
            numContagious--;
        }

        // Update running totals of healthy and exposed.
        if (s.IsHealthy())
        {
            finalHealthy++;
        }

        if (s.IsExposed())
        {
            finalExposed++;
        }

        allShoppers.Remove(s);
        Destroy(s.gameObject);
    }

    void InitWaypoints()
    {
        waypoints = GetComponentsInChildren<WaypointNode>();
        entrances = new List<WaypointNode>();
        exits = new List<WaypointNode>();
        regularNodes = new List<WaypointNode>();
        Debug.Log($"Found {waypoints.Length} waypoints");

        // Clear any existing edges
        foreach (var wp in waypoints)
        {
            wp.simulation = this;
            wp.Edges.Clear();
            if (wp.waypointType == WaypointNode.WaypointType.Entrance)
            {
                entrances.Add(wp);
            }
            else if (wp.waypointType == WaypointNode.WaypointType.Exit)
            {
                exits.Add(wp);
            }
            else
            {
                regularNodes.Add(wp);
            }
        }

        var startTicks = DateTime.Now.Ticks;
        // TODO avoid duplication with the gizmos code by passing a delegate?
        foreach (var wp in waypoints)
        {
            foreach (var otherWaypoint in waypoints)
            {
                if (wp == otherWaypoint)
                {
                    continue;
                }

                var validConnection = wp.ShouldConnect(OneWayAisles, otherWaypoint);
                if (validConnection)
                {
                    wp.Edges.Add(otherWaypoint);
                }
            }
        }
        var endTicks = DateTime.Now.Ticks;
        Debug.Log($"Raycasting between waypoints took {(endTicks-startTicks)*s_TicksToSeconds} seconds");
    }

    void OnDrawGizmos()
    {
        var tempWaypoints = GetComponentsInChildren<WaypointNode>();
        foreach (var wp in tempWaypoints)
        {
            foreach (var otherWaypoint in tempWaypoints)
            {
                if (wp == otherWaypoint)
                {
                    continue;
                }

                var validConnection = wp.ShouldConnect(OneWayAisles, otherWaypoint);
                if (validConnection)
                {
                    wp.DrawEdge(otherWaypoint);
                }
            }
        }
    }

    void UpdateExposure()
    {
        foreach (var shopper in allShoppers)
        {
            if (!shopper.IsContagious())
            {
                return;
            }

            // Find nearby shoppers
            // TODO optimize - use filter layer and non-allocating methods
            // TODO consider the "swept" positions of this Shopper and others - more robust at high framerates
            var radius = 2.0f; // roughly 6 feet
            Collider[] hitColliders = Physics.OverlapSphere(shopper.transform.position, radius);
            foreach (var coll in hitColliders)
            {
                var otherShopper = coll.GetComponent<Shopper>();
                if (otherShopper != null && otherShopper.IsHealthy())
                {
                    if (ShouldExposeHealthy(otherShopper, shopper))
                    {
                        otherShopper.InfectionStatus = Shopper.Status.Exposed;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Determine whether the healthy shopper should get exposed by the contagious one.
    /// </summary>
    /// <param name="healthy"></param>
    /// <param name="contagious"></param>
    /// <returns></returns>
    bool ShouldExposeHealthy(Shopper healthy, Shopper contagious)
    {
        // Account for motion over the last frame by taking the min distance of the "swept" positions
        // TODO - compute actual min distance between the 2 segments
        // Cheap approximation for now
        var distance = Mathf.Min(
            Vector3.Distance(healthy.transform.position, contagious.transform.position),
            Vector3.Distance(healthy.transform.position, contagious.previousPosition),
            Vector3.Distance(healthy.previousPosition, contagious.transform.position)
        );
        if (distance > ExposureDistanceMeters)
        {
            // Too far away
            return false;
        }

        // Interpolate the probability parameters based on the distance
        var t = distance / ExposureDistanceMeters;
        var prob = ExposureProbabilityAtZeroDistance * (1.0f - t) + ExposureProbabilityAtMaxDistance * t;

        // The probability is given per second; since we might apply the random choice over multiple steps of deltaTime
        // length, we need to adjust the probability.
        //   prob = 1 - (1-probPerFrame)^(1/deltaTime)
        // so
        var probPerFrame = 1.0f - Mathf.Pow(1.0f - prob, Time.deltaTime);
        return UnityEngine.Random.value < probPerFrame;
    }

    List<WaypointNode> GenerateRandomPath(int numGoals)
    {
        HashSet<WaypointNode> goals = new HashSet<WaypointNode>();

        // Select numGoals regular waypoints without replacement
        // TODO: Fisher-Yates shuffle instead?
        while (goals.Count < numGoals)
        {
            var randomIndex = UnityEngine.Random.Range(0, regularNodes.Count);
            if (regularNodes[randomIndex].Passthrough)
            {
                continue;
            }
            goals.Add(regularNodes[randomIndex]);
        }

        // Randomly pick from the available entrances and exits.
        var entrance = entrances[UnityEngine.Random.Range(0, entrances.Count)];
        var exit = exits[UnityEngine.Random.Range(0, exits.Count)];

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
                // TODO this is either a bug in the Dijkstra implementation,
                // or the graph isn't fully connected, need to debug further.
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

    static List<WaypointNode> FindPath(WaypointNode startNode, WaypointNode endNode)
    {
        Dictionary<WaypointNode, float> pathCost = new Dictionary<WaypointNode, float>();
        Dictionary<WaypointNode, WaypointNode> parents = new Dictionary<WaypointNode, WaypointNode>();
        HashSet<WaypointNode> closed = new HashSet<WaypointNode>();

        parents[startNode] = null;
        pathCost[startNode] = 0.0f;

        // TODO priority queue, we'll linear search for now
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

    // TODO replace with minheap/priority queue
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
