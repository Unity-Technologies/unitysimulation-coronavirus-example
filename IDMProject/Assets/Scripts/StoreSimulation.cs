using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoreSimulation : MonoBehaviour
{
    static double s_TicksToSeconds = 1e-7; // 100 ns per tick

    WaypointNode[] waypoints;
    Shopper shopper;

    void Awake()
    {
        InitWaypoints();
        shopper = GetComponentInChildren<Shopper>();

        // Pick a random waypoint for the start position
        var startWp = waypoints[UnityEngine.Random.Range(0, waypoints.Length - 1)];
        shopper.SetWaypoint(startWp);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void InitWaypoints()
    {
        waypoints = GetComponentsInChildren<WaypointNode>();
        Debug.Log($"Found {waypoints.Length} waypoints");

        // Clear any existing edges
        for (var i = 0; i < waypoints.Length; i++)
        {
            waypoints[i].Edges.Clear();
        }

        // TODO only cast in cardinal directions and 45 degrees?
        var startTicks = DateTime.Now.Ticks;
        for (var i = 0; i < waypoints.Length; i++)
        {
            for (var j = i + 1; j < waypoints.Length; j++)
            {
                var rayStart = waypoints[i].transform.position;
                var rayEnd = waypoints[j].transform.position;
                RaycastHit hitInfo;
                var didHit = Physics.Raycast(rayStart, rayEnd - rayStart, out hitInfo);
                if (didHit && hitInfo.collider?.gameObject == waypoints[j].gameObject)
                {
                    waypoints[i].Edges.Add(waypoints[j]);
                    waypoints[j].Edges.Add(waypoints[i]);
                }
            }
        }
        var endTicks = DateTime.Now.Ticks;
        Debug.Log($"Raycasting between waypoints took {(endTicks-startTicks)*s_TicksToSeconds} seconds");
    }
}
