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
        private const int MATRIX_SHAPE = 512;

        // Vectors...
        private readonly float[] a = new float[MATRIX_SHAPE * MATRIX_SHAPE];
        private readonly float[] b = new float[MATRIX_SHAPE * MATRIX_SHAPE];
        private readonly float[] c = new float[MATRIX_SHAPE * MATRIX_SHAPE];

        [GlobalSetup]
        public void Setup()
        {
            var rand = new Random(42);

            for (int i = 0; i < a.Length; ++i)
            {
                a[i] = b[i] = c[i] = (float)rand.NextDouble();
            }
        }

        [Benchmark]
        public void AddVectors()
        {
            int length = a.Length;
            for (int i = 0; i < length; ++i)
                c[i] = a[i] + b[i];
        }

        [Benchmark]
        public void AddVectorsSimd()
        {
            int vectorLength = Vector<float>.Count;
            int remainder = a.Length % vectorLength, length = a.Length - remainder;
            for (int i = 0; i < length; i += vectorLength)
            {
                Vector<float> va = new Vector<float>(a, i);
                Vector<float> vb = new Vector<float>(b, i);
                Vector<float> vc = va + vb;
                vc.CopyTo(c, i);
            }
            for (int i = 0; i < remainder; ++i)
            {
                c[length + i] = a[length + i] + b[length + i];
            }
        }

        [Benchmark]
        public float DotProduct()
        {
            float sum = 0.0f;
            for (int i = 0; i < a.Length; ++i)
                sum += a[i] * b[i];
            return sum;
        }

        [Benchmark]
        public float DotProductSimd()
        {
            // TODO Implement this
            return 0.0f;
        }

        [Benchmark]
        public void MatrixMultNaive()
        {
            for (int i = 0; i < MATRIX_SHAPE; ++i)
            {
                for (int j = 0; j < MATRIX_SHAPE; ++j)
                {
                    for (int k = 0; k < MATRIX_SHAPE; ++k)
                    {
                        c[i * MATRIX_SHAPE + j] += a[i * MATRIX_SHAPE + k] * b[k * MATRIX_SHAPE + j];
                    }
                }
            }
        }

        [Benchmark]
        public void MatrixMultReorg()
        {
            for (int i = 0; i < MATRIX_SHAPE; ++i)
            {
                for (int k = 0; k < MATRIX_SHAPE; ++k)
                {
                    for (int j = 0; j < MATRIX_SHAPE; ++j)
                    {
                        c[i * MATRIX_SHAPE + j] += a[i * MATRIX_SHAPE + k] * b[k * MATRIX_SHAPE + j];
                    }
                }
            }
        }

        [Benchmark]
        public void MatrixMultSimd()
        {
            int vecSize = Vector<float>.Count;
            for (int i = 0; i < MATRIX_SHAPE; ++i)
            {
                for (int k = 0; k < MATRIX_SHAPE; ++k)
                {
                    Vector<float> va = new Vector<float>(a[i * MATRIX_SHAPE + k]);
                    for (int j = 0; j < MATRIX_SHAPE; j += vecSize)
                    {
                        Vector<float> vb = new Vector<float>(b, k * MATRIX_SHAPE + j);
                        Vector<float> vc = new Vector<float>(c, i * MATRIX_SHAPE + j);
                        vc += va * vb;
                        vc.CopyTo(c, i * MATRIX_SHAPE + j);
                    }
                }
            }
        }
    }
}
