using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace CpuBasics
{
    public class SIMD2
    {
        [Params(512)]
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

        var rand = new Random(42);

            for (int i = 0; i < matrixA.Length; ++i)
            {
                matrixA[i] = matrixB[i] = matrixC[i] = (float)rand.NextDouble();
            }
        }

        [Benchmark]
        public void AddVectors()
        {
            int length = matrixA.Length;
            for (int i = 0; i < length; ++i)
                matrixC[i] = matrixA[i] + matrixB[i];
        }

        [Benchmark]
        public void AddVectorsSimd()
        {
            int vectorLength = Vector<float>.Count;
            int remainder = matrixA.Length % vectorLength, length = matrixA.Length - remainder;
            for (int i = 0; i < length; i += vectorLength)
            {
                Vector<float> va = new Vector<float>(matrixA, i);
                Vector<float> vb = new Vector<float>(matrixB, i);
                Vector<float> vc = va + vb;
                vc.CopyTo(matrixC, i);
            }
            for (int i = 0; i < remainder; ++i)
            {
                matrixC[length + i] = matrixA[length + i] + matrixB[length + i];
            }
        }

        [Benchmark]
        public void MatrixMultNaive()
        {
            for (int i = 0; i < MatrixDimension; ++i)
            {
                for (int j = 0; j < MatrixDimension; ++j)
                {
                    for (int k = 0; k < MatrixDimension; ++k)
                    {
                        matrixC[i * MatrixDimension + j] += matrixA[i * MatrixDimension + k] * matrixB[k * MatrixDimension + j];
                    }
                }
            }
        }

        [Benchmark]
        public void MatrixMultReorg()
        {
            for (int i = 0; i < MatrixDimension; ++i)
            {
                for (int k = 0; k < MatrixDimension; ++k)
                {
                    for (int j = 0; j < MatrixDimension; ++j)
                    {
                        matrixC[i * MatrixDimension + j] += matrixA[i * MatrixDimension + k] * matrixB[k * MatrixDimension + j];
                    }
                }
            }
        }

        [Benchmark]
        public void MatrixMultSimd()
        {
            int vecSize = Vector<float>.Count;
            for (int i = 0; i < MatrixDimension; ++i)
            {
                for (int k = 0; k < MatrixDimension; ++k)
                {
                    Vector<float> va = new Vector<float>(matrixA[i * MatrixDimension + k]);
                    for (int j = 0; j < MatrixDimension; j += vecSize)
                    {
                        Vector<float> vb = new Vector<float>(matrixB, k * MatrixDimension + j);
                        Vector<float> vc = new Vector<float>(matrixC, i * MatrixDimension + j);
                        vc += va * vb;
                        vc.CopyTo(matrixC, i * MatrixDimension + j);
                    }
                }
            }
        }
    }
}
