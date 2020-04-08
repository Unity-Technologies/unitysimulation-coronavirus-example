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
        // Pick a random entrance for the start position
        var startWp = entrances[UnityEngine.Random.Range(0, entrances.Count)];

        // Randomize the movement speed between [.75, 1.25] of the default speed
        var speedMult = UnityEngine.Random.Range(.75f, 1.25f);
        s.Speed *= speedMult;

        s.SetWaypoint(startWp);
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
        }

        var directions = new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
        };

        var startTicks = DateTime.Now.Ticks;
        if (false)
        {
            // Raycast all possible pairs of points
            for (var i = 0; i < waypoints.Length; i++)
            {

                for (var j = i + 1; j < waypoints.Length; j++)
                {
                    var wp1 = waypoints[i];
                    var wp2 = waypoints[j];
                    var rayStart = wp1.transform.position;
                    var rayEnd = wp2.transform.position;
                    RaycastHit hitInfo;
                    var didHit = Physics.Raycast(rayStart, rayEnd - rayStart, out hitInfo);
                    if (didHit && hitInfo.collider?.gameObject == wp2.gameObject)
                    {
                        // Don't add incoming edges to Entrances, or outgoing edges from Exits.
                        if (!wp2.IsEntrance() && !wp1.IsExit())
                        {
                            wp1.Edges.Add(wp2);
                        }
                        if(!wp1.IsEntrance() && !wp2.IsExit())
                        {
                            wp2.Edges.Add(wp1);
                        }
                    }
                }
            }
        }
        else
        {
            for (var i = 0; i < waypoints.Length; i++)
            {
                var wp = waypoints[i];
                if (wp.IsEntrance())
                {
                    // For entrances, check all other points
                    for (var j = 0; j < waypoints.Length; j++)
                    {
                        if (i == j)
                        {
                            continue;
                        }

                        var dir = (waypoints[j].transform.position - wp.transform.position).normalized;
                        wp.CheckRaycastConnection(OneWayAisles, dir);
                    }
                }
                else
                {
                    // For other waypoints, just check preset directions
                    foreach (var dir in directions)
                    {
                        wp.CheckRaycastConnection(OneWayAisles, dir);
                    }
                }
            }
        }

        var endTicks = DateTime.Now.Ticks;
        Debug.Log($"Raycasting between waypoints took {(endTicks-startTicks)*s_TicksToSeconds} seconds");
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
}
