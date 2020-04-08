using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class StoreSimulation : MonoBehaviour
{
    static double s_TicksToSeconds = 1e-7; // 100 ns per tick

    [FormerlySerializedAs("NumShoppers")]
    public int DesiredNumShoppers = 10;
    public int DesiredNumContagious = 1;
    public float SpawnCooldown= 1.0f;
    public bool OneWayAisles = true;
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
}
