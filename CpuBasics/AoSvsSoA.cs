using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace CpuBasics
{
    struct Point3
    {
        public float X; // <------------- Used for calculations
        public int SomeField1;
        public double SomeField2;
        public float Y; // <------------- Used for calculations
        public bool SomeField3;
        public double SomeField4;
        public double SomeField5;
        public double SomeField6;
        public float Z; // <------------- Used for calculations
        public double SomeField7;
        public bool SomeField8;
        public double SomeField9;

        float Normalize() => (float)Math.Sqrt(X * X + Y * Y + Z * Z); // <-- Adds "Method Table" to struct header
    }

    [SimpleJob(RunStrategy.ColdStart, launchCount: 1)]
    public class AoSvsSoA
    {
        [Params(4096)]
        public int ArraySize { get; set; }

        // Array of structs
        private Point3[] pts;

        // Reorganized as structure of arrays
        private float[] xs;
        private float[] ys;
        private float[] zs;

        [GlobalSetup]
        public void Setup()
        {
            var rand = new Random(42);

            pts = new Point3[ArraySize];
            xs = new float[ArraySize];
            ys = new float[ArraySize];
            zs = new float[ArraySize];

            for (int i = 0; i < pts.Length; ++i)
            {
                xs[i] = (float)rand.NextDouble();
                ys[i] = (float)rand.NextDouble();
                zs[i] = (float)rand.NextDouble();
                pts[i] = new Point3
                {
                    X = xs[i],
                    Y = ys[i],
                    Z = zs[i]
                };
            }
        }

        [Benchmark]
        public void VectorNormAoS()
        {
            for (int i = 0; i < pts.Length; ++i)
            {
                Point3 pt = pts[i];

                float norm = (float)Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y + pt.Z * pt.Z);

                pt.X /= norm;
                pt.Y /= norm;
                pt.Z /= norm;

                pts[i] = pt;
            }
        }

        [Benchmark]
        public void VectorNormSoA()
        {
            // Note: Data reorganized as structure of arrays
            //      Sequential access + data fits in L1 cache line
            for (int i = 0; i < xs.Length; ++i)
            {
                float norm = (float)Math.Sqrt(xs[i] * xs[i] + ys[i] * ys[i] + zs[i] * zs[i]);

                xs[i] /= norm;
                ys[i] /= norm;
                zs[i] /= norm;
            }
        }

        [Benchmark]
        public void VectorNormSimd()
        {
            // Note: Data reorganized as structure of arrays
            int vecSize = Vector<float>.Count;
            for (int i = 0; i < xs.Length; i += vecSize)
            {
                Vector<float> x = new Vector<float>(xs, i);
                Vector<float> y = new Vector<float>(ys, i);
                Vector<float> z = new Vector<float>(zs, i);

                Vector<float> norm = Vector.SquareRoot(x * x + y * y + z * z);

                x /= norm;
                y /= norm;
                z /= norm;

                x.CopyTo(xs, i);
                y.CopyTo(ys, i);
                z.CopyTo(zs, i);
            }
        }
    }
}
