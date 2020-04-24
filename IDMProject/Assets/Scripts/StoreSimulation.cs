using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Object = System.Object;
using Random = UnityEngine.Random;

public class StoreSimulation : MonoBehaviour
{
    static double s_TicksToSeconds = 1e-7; // 100 ns per tick

    [FormerlySerializedAs("NumShoppers")]
    [Header("Store Parameters")]
    public int DesiredNumShoppers = 10;
    [FormerlySerializedAs("DesiredNumContagious")]
    public int DesiredNumInfectious = 1;
    public float SpawnCooldown = 1.0f;
    public bool OneWayAisles = true;

    [HideInInspector]
    public int BillingQueueCapacity = 4;

    [Header("Billing Queue Parameters")]
    public float MaxPurchaseTime = 3.0f;
    public float MinPurchaseTime = 1.0f;
    public int NumberOfCountersOpen = 9;

    // Exposure probability parameters.
    // These are given as the probability of a healthy person converting to exposed over the course of one second.
    // During simulation, these probability are linearly interpolated based on distance to the infectious person
    // and modified to account for the timestep.
    [Header("Exposure Parameters")]
    [Range(0.0f, 1.0f)]
    public float ExposureProbabilityAtZeroDistance = 0.5f;
    [Range(0.0f, 1.0f)]
    public float ExposureProbabilityAtMaxDistance = 0.0f;
    [Range(0.0f, 10.0f)]
    public float ExposureDistanceMeters = 1.8288f; // Six feet in meters


    [Header("Graphics Parameters")]
    public GameObject ShopperPrefab;
    public GameObject[] Registers;

    [Header("Shopper Parameters")]
    public float ShopperSpeed = 1.0f;

    [HideInInspector]
    public WaypointNode[] waypoints;
    List<WaypointNode> entrances;
    WaypointGraph m_WaypointGraph;
    HashSet<Shopper> allShoppers;
    float spawnCooldownCounter;
    int numInfectious;
    private List<StoreSimulationQueue> registersQueues = new List<StoreSimulationQueue>();
    private int currentServingQueue = 0;

    // Results
    int finalHealthy;
    int finalExposed;

    public event Action<int> NumHealthyChanged;
    public event Action<int> NumContagiousChanged;

    void Awake()
    {
        Debug.Assert(NumberOfCountersOpen <= Registers.Length, "Number of counters to be left open needs to be less than equal to total number of counters");
        InitializeRegisters();
        InitWaypoints();
        allShoppers = new HashSet<Shopper>();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            Time.timeScale = 1;
        else
        {
            Time.timeScale = 0;
        }
    }


    private void InitializeRegisters()
    {
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
            registersQueues.Add(queue);
        }
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

        MoveQueue();
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
                NumHealthyChanged?.Invoke(finalHealthy);
            }

            if (s.IsExposed())
            {
                finalExposed++;
                NumContagiousChanged?.Invoke(finalExposed);
            }
        }

        var exposureRate = finalExposed + finalHealthy == 0 ? 0 : finalExposed / (float)(finalExposed + finalHealthy);
        Debug.Log($"total healthy: {finalHealthy}  total exposed: {finalExposed}  exposure rate: {100.0 * exposureRate}%");

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

        if (s.path == null)
        {
            // Pick a random entrance for the start position
            var startWp = entrances[UnityEngine.Random.Range(0, entrances.Count)];
            s.SetWaypoint(startWp);
        }

        // Randomize the movement speed between [.75, 1.25] of the default speed
        var speedMult = UnityEngine.Random.Range(.5f, 1f);
        s.Speed = ShopperSpeed * speedMult;

        if (numInfectious < DesiredNumInfectious)
        {
            s.InfectionStatus = Shopper.Status.Infectious;
            numInfectious++;
        }
    }

    public void Despawn(Shopper s, bool removeShopper = true)
    {
        if (s.IsInfectious())
        {
            numInfectious--;
        }

        // Update running totals of healthy and exposed.
        if (s.IsHealthy())
        {
            finalHealthy++;
            NumHealthyChanged?.Invoke(finalHealthy);
        }

        if (s.IsExposed())
        {
            finalExposed++;
            NumContagiousChanged?.Invoke(finalExposed);
        }

        if (removeShopper)
        {
            allShoppers.Remove(s);
        }
        Destroy(s.gameObject);
    }

    void InitWaypoints()
    {
        waypoints = GetComponentsInChildren<WaypointNode>();
        entrances = new List<WaypointNode>();
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
        }
        m_WaypointGraph = new WaypointGraph(waypoints);

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
        Debug.Log($"Raycasting between waypoints took {(endTicks - startTicks) * s_TicksToSeconds} seconds");
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
            if (!shopper.IsInfectious())
            {
                continue;
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
            Vector3.Distance(healthy.transform.position, infectious.previousPosition),
            Vector3.Distance(healthy.previousPosition, infectious.transform.position)
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
        foreach (var register in registersQueues)
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

        for (int i = currentServingQueue; i < registerNodesQueue.Length;)
        {
            var queue = registerNodesQueue[i].gameObject.GetComponent<StoreSimulationQueue>();
            if (queue && queue.EnterTheQueue(shopper))
            {
                shopper.Behavior = Shopper.BehaviorType.InQueue;
                currentServingQueue = (currentServingQueue + 1) % registerNodesQueue.Length;
                return registerNodesQueue[i];
            }

            i++;
        }

        shopper.SetWaypoint(currentNode, true);
        return currentNode.GetRandomNeighbor();
    }

    public void ResetSimulation()
    {
        foreach (var shopper in allShoppers)
        {
            Despawn(shopper, false);
        }
        allShoppers.Clear();
        finalExposed = 0;
        finalHealthy = 0;
        NumHealthyChanged?.Invoke(finalHealthy);
        NumContagiousChanged?.Invoke(finalExposed);
        numInfectious = 0;
        registersQueues.Clear();
        InitializeRegisters();
        InitWaypoints();
    }
}
