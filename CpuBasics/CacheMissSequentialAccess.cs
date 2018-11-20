using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;

namespace CpuBasics
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.ColdStart, launchCount: 5)]
    [HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.LlcMisses, HardwareCounter.LlcReference)]
    public class CacheMissSequentialAccess
    {
        // 512x512 x 4 bytes x 3 matrices = 3MB - should fit in 8MB L3 cache
        // 1024x1024 x 4 bytes x 3 matrices = 12MB - should exceed L3 cache 
        [Params(512, 1024)]
        public int MatrixDimension { get; set; }

        private float[] matrixA;
        private float[] matrixB;
        private float[] matrixC;

        [GlobalSetup]
        public void Setup()
        {
            matrixA = new float[MatrixDimension * MatrixDimension];
            matrixB = new float[MatrixDimension * MatrixDimension];
            matrixC = new float[MatrixDimension * MatrixDimension];

            var random = new Random(42);
            for (int i = 0; i < matrixA.Length; ++i)
            {
                matrixA[i] = random.Next();
                matrixB[i] = random.Next();
            }
        }

        [Benchmark]
        public void MatrixMultNaive()
        {
            for (int y = 0; y < MatrixDimension; ++y)
            {
                for (int x = 0; x < MatrixDimension; ++x)
                {
                    for (int k = 0; k < MatrixDimension; ++k)
                    {
                        matrixC[y * MatrixDimension + x] += matrixA[y * MatrixDimension + k] * matrixB[k * MatrixDimension + x];
                    }
                }
            }
        }

        [Benchmark]
        public void MatrixMultSequential()
        {
            for (int y = 0; y < MatrixDimension; ++y)
            {
                for (int k = 0; k < MatrixDimension; ++k)
                {
                    for (int x = 0; x < MatrixDimension; ++x)
                    {
                        matrixC[y * MatrixDimension + x] += matrixA[y * MatrixDimension + k] * matrixB[k * MatrixDimension + x];
                    }
                }
            }
        }
    }
}
