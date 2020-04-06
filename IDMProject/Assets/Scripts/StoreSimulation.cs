using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoreSimulation : MonoBehaviour
{
    static double s_TicksToSeconds = 1e-7; // 100 ns per tick

    public int NumShoppers = 10;
    public float SpawnCooldown= 1.0f;
    public GameObject ShopperPrefab;

    WaypointNode[] waypoints;
    List<WaypointNode> entrances;
    List<WaypointNode> exits;
    List<Shopper> allShoppers;
    float spawnCooldownCounter;

    void Awake()
    {
        InitWaypoints();
        allShoppers = new List<Shopper>();
    }

    // Update is called once per frame
    void Update()
    {
        // Cooldown on respawns - can only respawn when the counter is 0 (or negative).
        // The counter resets to SpawnCooldown when a customer is spawned.
        spawnCooldownCounter -= Time.deltaTime;
        if (spawnCooldownCounter <= 0 && allShoppers.Count < NumShoppers)
        {
            var newShopperGameObject = Instantiate(ShopperPrefab);
            var newShopper = newShopperGameObject.GetComponent<Shopper>();
            Spawn(newShopper);
            allShoppers.Add(newShopper);
            spawnCooldownCounter = SpawnCooldown;
        }
    }

    /// <summary>
    /// Called by the shopper when it reaches the exit.
    /// </summary>
    /// <param name="s"></param>
    public void Spawn(Shopper s)
    {
        s.simulation = this;
        // Pick a random entrance for the start position
        var startWp = entrances[UnityEngine.Random.Range(0, entrances.Count - 1)];
        s.SetWaypoint(startWp);
    }

    public void Despawn(Shopper s)
    {
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

                        var dir = waypoints[j].transform.position - wp.transform.position;
                        wp.CheckRaycastConnection(dir);
                    }
                }
                else
                {
                    // For other waypoints, just check preset directions
                    foreach (var dir in directions)
                    {
                        wp.CheckRaycastConnection(dir);
                    }
                }
            }
        }

        var endTicks = DateTime.Now.Ticks;
        Debug.Log($"Raycasting between waypoints took {(endTicks-startTicks)*s_TicksToSeconds} seconds");
    }
}
