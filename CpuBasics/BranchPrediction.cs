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
        private int[] _sorted = new int[32768];
        private int[] _unsorted = new int[32768];

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            for (int i = 0; i < _sorted.Length; ++i)
            {
                _sorted[i] = random.Next(0, 256);
                _unsorted[i] = random.Next(0, 256);
            }
            Array.Sort(_sorted);
        }


        [Benchmark]
        public int SortedBranchless()
        {
            int sum = 0;
            foreach (var i in _sorted)
                sum += i;
            return sum;
        }

        [Benchmark]
        public int UnsortedBranchless()
        {
            int sum = 0;
            foreach (var i in _unsorted)
                sum += i;
            return sum;
        }

        [Benchmark]
        public int SortedBranch()
        {
            int sum = 0;
            foreach (var i in _sorted)
                if (i >= 128)
                    sum += i;
            return sum;
        }

        [Benchmark]
        public int UnsortedBranch()
        {
            int sum = 0;
            foreach (var i in _unsorted)
                if (i >= 128)
                    sum += i;
            return sum;
        }

 
    }
}
