using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;

namespace CpuBasics
{
    [SimpleJob(RunStrategy.ColdStart, launchCount: 5)]
    public class DataAccessReorder
    {
        // 512x512 x 4 bytes x 3 matrices = 3MB - should fit in 8MB L3 cache
        // 1024x1024 x 4 bytes x 3 matrices = 12MB - should exceed L3 cache 
        [Params(512, 1024)]
        public int MatrixDimension { get; set; }

        private float[] _matrixA;
        private float[] _matrixB;
        private float[] _matrixC;

        [GlobalSetup]
        public void Setup()
        {
            _matrixA = new float[MatrixDimension * MatrixDimension];
            _matrixB = new float[MatrixDimension * MatrixDimension];
            _matrixC = new float[MatrixDimension * MatrixDimension];

            var random = new Random(42);
            for (int i = 0; i < _matrixA.Length; ++i)
            {
                _matrixA[i] = random.Next();
                _matrixB[i] = random.Next();
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
                        _matrixC[y * MatrixDimension + x] += _matrixA[y * MatrixDimension + k] * _matrixB[k * MatrixDimension + x];
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
                        _matrixC[y * MatrixDimension + x] += _matrixA[y * MatrixDimension + k] * _matrixB[k * MatrixDimension + x];
                    }
                }
            }
        }
    }
}
