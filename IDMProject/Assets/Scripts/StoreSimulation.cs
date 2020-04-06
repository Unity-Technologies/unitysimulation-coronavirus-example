using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoreSimulation : MonoBehaviour
{
    static double s_TicksToSeconds = 1e-7; // 100 ns per tick

    WaypointNode[] waypoints;
    List<WaypointNode> entrances;
    List<WaypointNode> exits;
    Shopper shopper;

    void Awake()
    {
        InitWaypoints();
        shopper = GetComponentInChildren<Shopper>();
        shopper.simulation = this;

        // Pick a random waypoint for the start position
        var startWp = entrances[UnityEngine.Random.Range(0, entrances.Count - 1)];
        shopper.SetWaypoint(startWp);
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Called by the shopper when it reaches the exit.
    /// </summary>
    /// <param name="s"></param>
    public void Respawn(Shopper s)
    {
        var startWp = entrances[UnityEngine.Random.Range(0, entrances.Count - 1)];
        s.SetWaypoint(startWp);
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

        // TODO only cast in cardinal directions and 45 degrees?
        var startTicks = DateTime.Now.Ticks;
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
        var endTicks = DateTime.Now.Ticks;
        Debug.Log($"Raycasting between waypoints took {(endTicks-startTicks)*s_TicksToSeconds} seconds");
    }
}
