using System;
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
        Exposed
    }

    public float Speed = 15.0f;
    public Material HealthyMaterial;
    public Material ContagiousMaterial;
    public Material ExposedMaterial;
    Status m_InfectionStatus;

    WaypointNode previousNode;
    WaypointNode nextNode;

    StoreSimulation m_Simulation;
    Vector3 m_PreviousPosition;

    public StoreSimulation simulation
    {
        get => m_Simulation;
        set => m_Simulation = value;
    }

    public Status InfectionStatus
    {
        get => m_InfectionStatus;
        set { SetStatus(value); }
    }

    public Vector3 previousPosition => m_PreviousPosition;

    void SetStatus(Status s)
    {
        Material m = null;
        switch (s)
        {
            case Status.Healthy:
                m = HealthyMaterial;
                break;
            case Status.Contagious:
                m = ContagiousMaterial;
                break;
            case Status.Exposed:
                m = ExposedMaterial;
                break;
        }

        m_InfectionStatus = s;
        var renderer = GetComponent<Renderer>();
        renderer.material = m;
    }

    public bool IsHealthy()
    {
        return m_InfectionStatus == Status.Healthy;
    }

    public bool IsContagious()
    {
        return m_InfectionStatus == Status.Contagious;
    }

    public bool IsExposed()
    {
        return m_InfectionStatus == Status.Exposed;
    }

    public void SetWaypoint(WaypointNode node)
    {
        previousNode = node;
        // Pick the next node randomly
        nextNode = node.GetRandomNeighbor();

        var worldPos = previousNode.transform.position;
        transform.position = worldPos;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_PreviousPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        m_PreviousPosition = transform.position;

        var reachedEnd = UpdateInterpolation();
        if (reachedEnd)
        {
            if (nextNode.IsExit() || nextNode.Edges.Count == 0)
            {
                // Need a respawn
                simulation.Despawn(this);
                return;
            }
            else
            {
                var previousPos = previousNode.transform.position;
                var currentPos = nextNode.transform.position;
                previousNode = nextNode;
                nextNode = nextNode.GetRandomNeighborInDirection(previousPos, currentPos);
            }
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
