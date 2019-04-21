using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            int res1 = solution(new int[] { 2, 4, 5, 6}, 10, 2, 5);
            Assert.AreEqual(-1, res1);
            int res2 = solution(new int[] { 2, 4, 5, 6 }, 10, 20, 5);
            Assert.AreEqual(8, res2);
            int res3 = solution(new int[] { 2, 4, 5, 6, 7, 8 }, 5, 40, 5);
            Assert.AreEqual(25, res3);
        }


        public class GasSlot
        {
            public int GasCount;
            public int WaitTime;
        }

        public int solution(int[] A, int X, int Y, int Z)
        {
            int result = 0;
            GasSlot[] gasSlots = { new GasSlot { GasCount = X }, new GasSlot { GasCount = Y }, new GasSlot { GasCount = Z } };

            int i = 0;

            while (i < A.Length)
            {
                int currDemand = A[i];
                if (!HasEnoughGas(currDemand, gasSlots))
                {
                    return -1;
                }

                GasSlot freeSlot = FindFree(currDemand, gasSlots);
                if (freeSlot != null)
                {
                    freeSlot.GasCount -= currDemand;
                    freeSlot.WaitTime = currDemand;
                    i++;
                }
                else
                {
                    int secs = Tick(gasSlots);
                    if (secs == int.MaxValue)
                    {
                        return -1;
                    }
                    result += secs;
                }                
            }

            int ticks = gasSlots.Max(s=>s.WaitTime);
            result += ticks;
            return result;
        }

        private int Tick(GasSlot[] gasSlots)
        {
            int minTime = int.MaxValue;
            foreach (var eachSlot in gasSlots)
            {
                if (eachSlot.WaitTime > 0 && eachSlot.WaitTime < minTime) {
                    minTime = eachSlot.WaitTime;
                }
            }
            foreach (var eachSlot in gasSlots) {
                if (minTime <= eachSlot.WaitTime) {
                    eachSlot.WaitTime = eachSlot.WaitTime - minTime;
                }
            }
            return minTime;
        }

        private GasSlot FindFree(int currDemand, GasSlot[] gasSlots)
        {
            return gasSlots.FirstOrDefault(s=> s.GasCount >= currDemand && s.WaitTime == 0);
        }

        private bool HasEnoughGas(int currDemand, GasSlot[] gasSlots)
        {
            bool any = gasSlots.Any(s=>s.GasCount >= currDemand);
            return any;
        }
    }
}
