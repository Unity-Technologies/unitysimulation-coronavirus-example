using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Shopper : MonoBehaviour
{
    public enum Status
    {
        Healthy,
        Contagious,
        Infected
    }

    public float Speed = 15.0f;
    public Material HealthyMaterial;
    public Material ContagiousMaterial;
    public Material InfectedMaterial;

    WaypointNode previousNode;
    WaypointNode nextNode;

    public void SetWaypoint(WaypointNode node)
    {
        Debug.Log("Setting waypoint");
        previousNode = node;
        // Pick the next node randomly
        nextNode = node.GetRandomNeighbor();

        var worldPos = previousNode.transform.position;
        transform.position = worldPos;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var reachedEnd = UpdateInterpolation();
        if (reachedEnd)
        {
            var oldPrevious = previousNode;
            previousNode = nextNode;
            // Make sure we don't backtrack
            nextNode = previousNode.GetRandomNeighbor(oldPrevious);
        }
    }

    bool UpdateInterpolation()
    {
        if (previousNode == null || nextNode == null)
        {
            return false;
        }
        bool atEnd = false;
        var prevToCur = transform.position - previousNode.transform.position;
        var prevToNext = nextNode.transform.position - previousNode.transform.position;

        var distancePrevToNext = prevToNext.magnitude;
        // Fraction from 0 to 1
        var t = Vector3.Dot(prevToCur, prevToNext) / Vector3.Dot(prevToNext, prevToNext);

        // Advance by speed * timestep units
        var distance = Speed * Time.deltaTime;

        // TODO "carry over" the leftover distance along the next segment. This will let us run at higher framerate.
        var tNew = t + distance / distancePrevToNext;
        if (tNew > 1.0f)
        {
            tNew = 1.0f;
            atEnd = true;
        }

        // Interpolate
        var newPostion = previousNode.transform.position + tNew * prevToNext;
        transform.position = newPostion;

        return atEnd;
    }
}
