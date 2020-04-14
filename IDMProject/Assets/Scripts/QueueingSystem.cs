using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class QueueingSystem : MonoBehaviour
{
    public Queue<Shopper> m_ShoppersQueue;

    public int m_MaxQueueCapacity;

    public float m_MaxProcessingTime = 2.5f;


    public struct RegisterInfo
    {
        public int maxQueueCapacity;
        public float maxProcessingTime;
    }

    public void InitializeQueue(RegisterInfo info)
    {
        m_ShoppersQueue = new Queue<Shopper>(info.maxQueueCapacity);
        m_MaxProcessingTime = info.maxProcessingTime;
        QueueState = State.Idle;
    }

    public enum State
    {
        Idle,
        Processing
    }

    public State QueueState = State.Idle;


    // Start is called before the first frame update

    public QueueingSystem(int capacity)
    {
        m_MaxQueueCapacity = capacity;
        m_ShoppersQueue = new Queue<Shopper>(m_MaxQueueCapacity);
    }

    private void Awake()
    {
        m_ShoppersQueue = new Queue<Shopper>(m_MaxQueueCapacity);
    }


    private bool CanEnterQueue()
    {
        return m_ShoppersQueue.Count != m_MaxQueueCapacity;
    }

    public bool EnterTheQueue(Shopper shopper, Func<Queue<Shopper>, bool> func = null)
    {
        var condition = true;
        
        if (func != null)
            condition = func(m_ShoppersQueue);
        
        if (CanEnterQueue() && condition)
        {
            m_ShoppersQueue.Enqueue(shopper);
            return true;
        }

        return false;
    }
    
    public Tuple<Shopper, float> ExitQueue()
    {
        return new Tuple<Shopper, float>(m_ShoppersQueue.Dequeue(), UnityEngine.Random.Range(1.0f, m_MaxProcessingTime));
    }
}
