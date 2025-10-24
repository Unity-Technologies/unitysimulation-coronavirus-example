using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Simulation.Games;
using UnityEngine;
using UnityEngine.Serialization;
using Object = System.Object;
using Random = UnityEngine.Random;

public class StoreSimulation : MonoBehaviour
{
    static double s_TicksToSeconds = 1e-7; // 100 ns per tick

    [FormerlySerializedAs("NumShoppers")]
    [Header("Store Parameters")]
    public int               DesiredNumShoppers = 10;
    
    [FormerlySerializedAs("DesiredNumContagious")]
    public int               DesiredNumInfectious = 1;
    public float             SpawnCooldown = 1.0f;
    public bool              OneWayAisles = true;

    [Header("Billing Queue Parameters")]
    public float             MaxPurchaseTime = 3.0f;
    public float             MinPurchaseTime = 1.0f;
    public int               NumberOfCountersOpen = 9;

    // Exposure probability parameters.
    // These are given as the probability of a healthy person converting to exposed over the course of one second.
    // During simulation, these probability are linearly interpolated based on distance to the infectious person
    // and modified to account for the timestep.
    [Header("Exposure Parameters")]
    [Range(0.0f, 1.0f)]
    public float     ExposureProbabilityAtZeroDistance = 0.5f;
    [Range(0.0f, 1.0f)]
    public float     ExposureProbabilityAtMaxDistance = 0.0f;
    [Range(0.0f, 10.0f)]
    public float     ExposureDistanceMeters = 1.8288f; // Six feet in meters


    [Header("Graphics Parameters")]
    public GameObject        ShopperPrefab;
    public GameObject[]      Registers;

    [Header("Shopper Parameters")]
    public float             ShopperSpeed = 1.0f;

    [HideInInspector]
    public WaypointNode[]    Waypoints;
    [HideInInspector]
    public int               BillingQueueCapacity = 4;
    
    private List<WaypointNode>         m_Entrances;
    private WaypointGraph              m_WaypointGraph;
    private HashSet<Shopper>           m_AllShoppers;
    private float                      m_SpawnCooldownCounter;
    private int                        m_NumInfectious;
    private List<StoreSimulationQueue> m_RegistersQueues = new List<StoreSimulationQueue>();
    private int                        m_CurrentServingQueue = 0;

    bool simulationInited = false;
    float simulationSecondsRunTime;
    public float SimulationTimeInSeconds = 60f;

    // Results
    private int m_FinalHealthy;
    private int m_FinalExposed;

    public event Action<int> NumHealthyChanged;
    public event Action<int> NumContagiousChanged;

    void Awake()
    {
        GameSimManager.Instance.FetchConfig(OnConfigFetched);
    }

    void OnConfigFetched(GameSimConfigResponse configResponse)
    {
        DesiredNumShoppers = configResponse.GetInt("DesiredNumShoppers", DesiredNumShoppers);
        DesiredNumInfectious = configResponse.GetInt("DesiredNumContagious", DesiredNumInfectious);
        SpawnCooldown = configResponse.GetFloat("SpawnCooldown", SpawnCooldown);
        OneWayAisles = configResponse.GetBool("OneWayAisles", OneWayAisles);
        SimulationTimeInSeconds = configResponse.GetFloat("SimulationTimeInSeconds", SimulationTimeInSeconds);
        ExposureProbabilityAtZeroDistance = configResponse.GetFloat("ExposureProbabilityAtZeroDistance", ExposureProbabilityAtZeroDistance);
        ExposureProbabilityAtMaxDistance = configResponse.GetFloat("ExposureProbabilityAtMaxDistance", ExposureProbabilityAtMaxDistance);
        ExposureDistanceMeters = configResponse.GetFloat("ExposureDistanceMeters", ExposureDistanceMeters);
        NumberOfCountersOpen = configResponse.GetInt("NumberOfRegistersOpen", NumberOfCountersOpen);

        InitSimulation();
    }

    void InitSimulation()
    {
        Debug.Assert(NumberOfCountersOpen <= Registers.Length, 
            "Number of counters to be left open needs to be less than equal to total number of counters");
        InitializeRegisters();
        InitWaypoints();
        m_AllShoppers = new HashSet<Shopper>();
        simulationInited = true;
        simulationSecondsRunTime = SimulationTimeInSeconds;
    }

    private void InitializeRegisters()
    {
        if (m_RegistersQueues.Count > 0)
            m_RegistersQueues.Clear();
        
        foreach (var register in Registers)
        {
            register.gameObject.SetActive(false);
            var queueComponent = register.GetComponent<StoreSimulationQueue>();
            if (queueComponent != null)
            {
                Destroy(queueComponent);
            }
        }

        for (int i = 0; i < NumberOfCountersOpen; i++)
        {
            Registers[i].gameObject.SetActive(true);
            var queue = Registers[i].AddComponent<StoreSimulationQueue>();
            queue.MaxQueueCapacity = BillingQueueCapacity;
            queue.MaxProcessingTime = MaxPurchaseTime;
            queue.MinProcessingTime = MinPurchaseTime;
            queue.ShoppersQueue = new Queue<Shopper>(BillingQueueCapacity);
            queue.QueueState = StoreSimulationQueue.State.Idle;
            m_RegistersQueues.Add(queue);
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (!simulationInited)
        {
            return;
        }
        simulationSecondsRunTime -= Time.deltaTime;
        if (simulationSecondsRunTime <= 0)
        {
            OnSimulationFinished();
            return;
        }

        // Cooldown on respawns - can only respawn when the counter is 0 (or negative).
        // The counter resets to SpawnCooldown when a customer is spawned.
        m_SpawnCooldownCounter -= Time.deltaTime;
        if (m_SpawnCooldownCounter <= 0 && m_AllShoppers.Count < DesiredNumShoppers)
        {
            var newShopperGameObject = Instantiate(ShopperPrefab);
            var newShopper = newShopperGameObject.GetComponent<Shopper>();
            Spawn(newShopper);
            m_AllShoppers.Add(newShopper);
            m_SpawnCooldownCounter = SpawnCooldown;
        }

        MoveQueue();
        UpdateExposure();
    }

    void OnDisable()
    {
        // Update the final counts.
        foreach (var s in m_AllShoppers)
        {
            if (s.IsHealthy())
            {
                m_FinalHealthy++;
                NumHealthyChanged?.Invoke(m_FinalHealthy);
            }

            if (s.IsExposed())
            {
                m_FinalExposed++;
                NumContagiousChanged?.Invoke(m_FinalExposed);
            }
        }

        var exposureRate = m_FinalExposed + m_FinalHealthy == 0 ? 0 : m_FinalExposed / (float)(m_FinalExposed + m_FinalHealthy);
        Debug.Log($"total healthy: {m_FinalHealthy}  total exposed: {m_FinalExposed}  exposure rate: {100.0 * exposureRate}%");

    }

    void OnSimulationFinished()
    {
        // Update the final counts.
        foreach (var s in m_AllShoppers)
        {
            if (s.IsHealthy())
            {
                m_FinalHealthy++;
            }

            if (s.IsExposed())
            {
                m_FinalExposed++;
            }
        }

        var exposureRate = m_FinalExposed + m_FinalHealthy == 0 ? 0 : m_FinalExposed / (float)(m_FinalExposed + m_FinalHealthy);
        Debug.Log($"total healthy: {m_FinalHealthy}  total exposed: {m_FinalExposed}  exposure rate: {exposureRate}%");
        simulationInited = false;
        SetCounters();
    }

    void SetCounters()
    {
        //Set game sim counters
        GameSimManager.Instance.SetCounter("totalHealthy", m_FinalHealthy);
        GameSimManager.Instance.SetCounter("totalExposed", m_FinalExposed);
        //quit the simulation
        Application.Quit();
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
            var path = m_WaypointGraph.GenerateRandomPath(6);
            if (path != null)
            {
                s.SetPath(path);
            }
        }

        if (s.m_Path == null)
        {
            // Pick a random entrance for the start position
            var startWp = m_Entrances[UnityEngine.Random.Range(0, m_Entrances.Count)];
            s.SetWaypoint(startWp);
        }

        // Randomize the movement speed between [.75, 1.25] of the default speed
        var speedMult = UnityEngine.Random.Range(.5f, 1f);
        s.Speed = ShopperSpeed * speedMult;

        if (m_NumInfectious < DesiredNumInfectious)
        {
            s.InfectionStatus = Shopper.Status.Infectious;
            m_NumInfectious++;
        }
    }

    public void Despawn(Shopper s, bool removeShopper = true)
    {
        if (s.IsInfectious())
        {
            m_NumInfectious--;
        }

        // Update running totals of healthy and exposed.
        if (s.IsHealthy())
        {
            m_FinalHealthy++;
            NumHealthyChanged?.Invoke(m_FinalHealthy);
        }

        if (s.IsExposed())
        {
            m_FinalExposed++;
            NumContagiousChanged?.Invoke(m_FinalExposed);
        }

        if (removeShopper)
        {
            m_AllShoppers.Remove(s);
        }
        Destroy(s.gameObject);
    }

    void InitWaypoints()
    {
        Waypoints = GetComponentsInChildren<WaypointNode>();
        m_Entrances = new List<WaypointNode>();
        Debug.Log($"Found {Waypoints.Length} Waypoints");

        // Clear any existing edges
        foreach (var wp in Waypoints)
        {
            wp.simulation = this;
            wp.Edges.Clear();
            if (wp.waypointType == WaypointNode.WaypointType.Entrance)
            {
                m_Entrances.Add(wp);
            }
        }
        m_WaypointGraph = new WaypointGraph(Waypoints);

        var startTicks = DateTime.Now.Ticks;
        // TODO avoid duplication with the gizmos code by passing a delegate?
        foreach (var wp in Waypoints)
        {
            foreach (var otherWaypoint in Waypoints)
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
        Debug.Log($"Raycasting between Waypoints took {(endTicks - startTicks) * s_TicksToSeconds} seconds");
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
        foreach (var shopper in m_AllShoppers)
        {
            if (!shopper.IsInfectious())
            {
                continue;
            }

            // Find nearby shoppers
            // TODO optimize - use filter layer and non-allocating methods
            // TODO consider the "swept" positions of this Shopper and others - more robust at high framerates
            var radius = ExposureDistanceMeters;
            Collider[] hitColliders = Physics.OverlapSphere(shopper.transform.position, radius);
            foreach (var coll in hitColliders)
            {
                var otherShopper = coll.GetComponent<Shopper>();
                if (otherShopper != null && otherShopper.IsHealthy())
                {
                    if (ShouldExposeHealthy(otherShopper, shopper))
                    {
                        otherShopper.PlayRippleEffect();
                        otherShopper.InfectionStatus = Shopper.Status.Exposed;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Determine whether the healthy shopper should get exposed by the infectious one.
    /// </summary>
    /// <param name="healthy"></param>
    /// <param name="infectious"></param>
    /// <returns></returns>
    bool ShouldExposeHealthy(Shopper healthy, Shopper infectious)
    {
        // Account for motion over the last frame by taking the min distance of the "swept" positions
        // TODO - compute actual min distance between the 2 segments
        // Cheap approximation for now
        var distance = Mathf.Min(
            Vector3.Distance(healthy.transform.position, infectious.transform.position),
            Vector3.Distance(healthy.transform.position, infectious.PreviousPosition),
            Vector3.Distance(healthy.PreviousPosition, infectious.transform.position)
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

    void MoveQueue()
    {
        foreach (var register in m_RegistersQueues)
        {
            if (register.ShoppersQueue.Count > 0 && register.QueueState == StoreSimulationQueue.State.Idle)
            {
                var shopper = register.ExitQueue();
                register.QueueState = StoreSimulationQueue.State.Processing;
                StartCoroutine(ProcessShopper(register, shopper.Item1, shopper.Item2));
            }
        }
    }

    IEnumerator ProcessShopper(StoreSimulationQueue register, Shopper shopper, float waitTime)
    {
        //shopper.BillingTime = waitTime;
        yield return new WaitUntil(() => shopper.Behavior == Shopper.BehaviorType.Billing);
        shopper.BillingTime = waitTime;
        shopper.Regsiter = register;
    }

    public void InformExit(Shopper shopper)// At this point shopper is not in the queue. Just Freeing the Register.
    {
        Debug.Assert(shopper.Regsiter != null, "Shopper needs to have an assigned register counter");
        shopper.Regsiter.QueueState = StoreSimulationQueue.State.Idle;
    }

    public WaypointNode EnterInAvailableQueue(Shopper shopper, WaypointNode currentNode)
    {
        var registerNodesQueue = currentNode.Edges.Where(e => e.waypointType == WaypointNode.WaypointType.Register)
            .ToArray();

        for (int i = m_CurrentServingQueue; i < registerNodesQueue.Length;)
        {
            var queue = registerNodesQueue[i].gameObject.GetComponent<StoreSimulationQueue>();
            if (queue && queue.EnterTheQueue(shopper))
            {
                shopper.Behavior = Shopper.BehaviorType.InQueue;
                m_CurrentServingQueue = (m_CurrentServingQueue + 1) % registerNodesQueue.Length;
                return registerNodesQueue[i];
            }

            i++;
        }

        shopper.SetWaypoint(currentNode, true);
        return currentNode.GetRandomNeighbor();
    }

    public void ResetSimulation()
    {
        foreach (var shopper in m_AllShoppers)
        {
            Despawn(shopper, false);
        }

        foreach (var register in Registers)
        {
            Destroy(register.GetComponent<StoreSimulationQueue>());
        }
        m_AllShoppers.Clear();
        m_FinalExposed = 0;
        m_FinalHealthy = 0;
        m_CurrentServingQueue = 0;
        NumHealthyChanged?.Invoke(m_FinalHealthy);
        NumContagiousChanged?.Invoke(m_FinalExposed);
        m_NumInfectious = 0;
        m_RegistersQueues.Clear();
        InitializeRegisters();
        InitWaypoints();
    }
}
