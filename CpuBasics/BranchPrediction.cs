using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;

namespace CpuBasics
{
    [SimpleJob(RunStrategy.ColdStart, launchCount: 5)]
    [HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]
    public class BranchPrediction
    {
        private int[] sorted = new int[32768];
        private int[] unsorted = new int[32768];

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            for (int i = 0; i < sorted.Length; ++i)
            {
                sorted[i] = random.Next(0, 256);
                unsorted[i] = random.Next(0, 256);
            }
            Array.Sort(sorted);
        }


        [Benchmark]
        public int SortedBranchless()
        {
            int sum = 0;
            foreach (var i in sorted)
                sum += i;
            return sum;
        }

        [Benchmark]
        public int UnsortedBranchless()
        {
            int sum = 0;
            foreach (var i in unsorted)
                sum += i;
            return sum;
        }

        [Benchmark]
        public int SortedBranch()
        {
            int sum = 0;
            foreach (var i in sorted)
                if (i >= 128)
                    sum += i;
            return sum;
        }

        [Benchmark]
        public int UnsortedBranch()
        {
            int sum = 0;
            foreach (var i in unsorted)
                if (i >= 128)
                    sum += i;
            return sum;
        }

 
    }
}
