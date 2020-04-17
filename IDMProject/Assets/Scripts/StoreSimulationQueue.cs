using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class StoreSimulationQueue : MonoBehaviour
{
    public Queue<Shopper> ShoppersQueue;
    public int            MaxQueueCapacity;
    public float          MaxProcessingTime = 2.5f;


    public struct RegisterInfo
    {
        public int maxQueueCapacity;
        public float maxProcessingTime;
    }

    public void InitializeQueue(int queueCapacity, float processingTime)
    {
        ShoppersQueue = new Queue<Shopper>(queueCapacity);
        MaxProcessingTime = processingTime;
        QueueState = State.Idle;
    }

    public enum State
    {
        Idle,
        Processing
    }

    public State QueueState = State.Idle;


    // Start is called before the first frame update

    public StoreSimulationQueue(int capacity)
    {
        MaxQueueCapacity = capacity;
        ShoppersQueue = new Queue<Shopper>(MaxQueueCapacity);
    }

    private void Awake()
    {
        ShoppersQueue = new Queue<Shopper>(MaxQueueCapacity);
    }


    private bool CanEnterQueue()
    {
        return ShoppersQueue.Count != MaxQueueCapacity;
    }

    public bool EnterTheQueue(Shopper shopper, Func<Queue<Shopper>, bool> func = null)
    {
        var condition = true;
        
        if (func != null)
            condition = func(ShoppersQueue);
        
        if (CanEnterQueue() && condition)
        {
            ShoppersQueue.Enqueue(shopper);
            return true;
        }

        return false;
    }
    
    public Tuple<Shopper, float> ExitQueue()
    {
        return new Tuple<Shopper, float>(ShoppersQueue.Dequeue(), UnityEngine.Random.Range(1.0f, MaxProcessingTime));
    }
}
