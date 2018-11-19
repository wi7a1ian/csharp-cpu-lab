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
        public float X;
        public float Y;
        public float Z;
    }

    [SimpleJob(RunStrategy.ColdStart, launchCount: 5)]
    public class AoSvsSoA
    {
        // Array of structs
        private readonly Point3[] pts = new Point3[4096];

        // Reorganized as structure of arrays
        private readonly float[] xs = new float[4096];
        private readonly float[] ys = new float[4096];
        private readonly float[] zs = new float[4096];

        [GlobalSetup]
        public void Setup()
        {
            var rand = new Random(42);

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
        public void VectorNormNaive()
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
