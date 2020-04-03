using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoreSimulation : MonoBehaviour
{
    WaypointNode[] waypoints;
    Shopper shopper;

    void Awake()
    {
        waypoints = GetComponentsInChildren<WaypointNode>();
        Debug.Log($"Found {waypoints.Length} waypoints");
        shopper = GetComponentInChildren<Shopper>();

        // Pick a random waypoint for the start position
        var startWp = waypoints[Random.Range(0, waypoints.Length - 1)];
        shopper.SetWaypoint(startWp);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
