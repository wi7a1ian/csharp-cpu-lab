using System;
using BenchmarkDotNet.Running;

namespace CpuBasics
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<BranchPrediction>();
            BenchmarkRunner.Run<DataAccessReorder>();
            BenchmarkRunner.Run<CacheMiss>();
            BenchmarkRunner.Run<CacheInvalidation>();
            BenchmarkRunner.Run<SIMD>();
            BenchmarkRunner.Run<SIMD2>();
            BenchmarkRunner.Run<AoSvsSoA>();
        }
    }
}
