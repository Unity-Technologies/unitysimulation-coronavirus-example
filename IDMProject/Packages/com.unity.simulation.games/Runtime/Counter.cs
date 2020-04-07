using System;
using System.Runtime.CompilerServices;
using System.Threading;

using UnityEngine;

[assembly: InternalsVisibleTo("Unity.Simulation.Games.Tests")]
namespace Unity.Simulation.Games
{
    [Serializable]
    internal class Counter
    {
        [SerializeField]
        string _name;

        public string Name { get { return _name; } }

        [SerializeField]
        Int64 _value;
        
        [NonSerialized]
        internal Int64 _count;

        public Int64 Value { get { return _value; } }

        public Counter(string name)
        {
            _name = name;
            Reset();
        }

        internal Int64 Increment(Int64 amount)
        {
            return Interlocked.Add(ref _value, amount);
        }

        internal void Reset(Int64 value = 0)
        {
            Interlocked.Exchange(ref _value, value);
        }
    }
}
