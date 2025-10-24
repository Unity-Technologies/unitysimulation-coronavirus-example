using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
//using UnityEngine.TestTools;

namespace Unity.Simulation.Games.Tests
{
    public class GameSimManagerTests
    {
        [UnityTest]
        public IEnumerator VerifyResetAndFlush_ResetsCounter_AndSavesCurrentStateTo_FileSystem()
        {
            GameSimManager.Instance.IncrementCounter("test", 1);
            GameSimManager.Instance.ResetAndFlushCounterToDisk("test", FileAssert.Exists);
            yield return null;
            Assert.IsTrue(GameSimManager.Instance.GetCounter("test").Value == 0);
        }
    }
}