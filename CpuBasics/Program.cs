using System;
using BenchmarkDotNet.Running;
using ObjectLayoutInspector;

namespace CpuBasics
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<BranchPrediction>();

            BenchmarkRunner.Run<CacheMissSequentialAccess>();
            BenchmarkRunner.Run<CacheMissTiling>();

            BenchmarkRunner.Run<CacheInvalidation>();

            BenchmarkRunner.Run<SIMD>();
            BenchmarkRunner.Run<SIMD2>();

            BenchmarkRunner.Run<AoSvsSoA>();
            TypeLayout.PrintLayout<AoSvsSoA.VectorThatDontFitCacheLine>(true);
            TypeLayout.PrintLayout<AoSvsSoA.VectorThatFitCacheLine>(true);

            BenchmarkRunner.Run<AoCvsAoS>();
            TypeLayout.PrintLayout<AoCvsAoS.ClassThatDontFitCacheLine>(true);
            TypeLayout.PrintLayout<AoCvsAoS.StructThatFitCacheLine>(true);
        }
    }
}
