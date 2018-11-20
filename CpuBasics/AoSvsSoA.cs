using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace CpuBasics
{ 
    struct Point3ThatDontFitCacheLine
    {
        public float X; // <------------- Used for calculations
        public int SomeField1;
        public double SomeField2;
        public float Y; // <------------- Used for calculations
        public bool SomeField3;
        public double SomeField4;
        public double SomeField5;
        public double SomeField6;
        public double SomeField7;
        public bool SomeField8;
        public double SomeField9;
        public double SomeField10;
        public double SomeField11;
        public double SomeField12;
        public float Z; // <------------- Used for calculations

        public float Normalize() => (float)Math.Sqrt(X * X + Y * Y + Z * Z); // <-- Adds "Method Table" to struct header
    }

    struct Point3ThatFitCacheLine
    {
        public float X; // <------------- Used for calculations
        public double SomeField1;
        public double SomeField2;
        public float Y; // <------------- Used for calculations
        public float Z; // <------------- Used for calculations
        public float SomeField3;
    }

    [SimpleJob(RunStrategy.ColdStart, launchCount: 1)]
    public class AoSvsSoA
    {
        [Params(1024*64)]
        public int ArraySize { get; set; }

        // Array of structs that don't fit the cache line
        private Point3ThatDontFitCacheLine[] arrayOfPts;

        // Array of structs that fit the cache line
        private Point3ThatFitCacheLine[] arrayOfPtsCL;

        // Reorganized as structure of arrays
        private float[] arrayOfX;
        private float[] arrayOfY;
        private float[] arrayOfZ;

        [GlobalSetup]
        public void Setup()
        {
            var rand = new Random(42);

            arrayOfPts = new Point3ThatDontFitCacheLine[ArraySize];
            arrayOfPtsCL = new Point3ThatFitCacheLine[ArraySize];
            arrayOfX = new float[ArraySize];
            arrayOfY = new float[ArraySize];
            arrayOfZ = new float[ArraySize];

            for (int i = 0; i < arrayOfPts.Length; ++i)
            {
                arrayOfX[i] = (float)rand.NextDouble();
                arrayOfY[i] = (float)rand.NextDouble();
                arrayOfZ[i] = (float)rand.NextDouble();

                arrayOfPts[i] = new Point3ThatDontFitCacheLine
                {
                    X = arrayOfX[i],
                    Y = arrayOfY[i],
                    Z = arrayOfZ[i]
                };
                arrayOfPtsCL[i] = new Point3ThatFitCacheLine
                {
                    X = arrayOfX[i],
                    Y = arrayOfY[i],
                    Z = arrayOfZ[i]
                };
            }
        }

        [Benchmark]
        public void VectorNormAoS()
        {
            for (int i = 0; i < arrayOfPts.Length; ++i)
            {
                ref Point3ThatDontFitCacheLine pt = ref arrayOfPts[i];

                float norm = pt.Normalize();

                pt.X /= norm;
                pt.Y /= norm;
                pt.Z /= norm;

                arrayOfPts[i] = pt;
            }
        }

        [Benchmark]
        public void VectorNormAoSCacheLine()
        {
            for (int i = 0; i < arrayOfPts.Length; ++i)
            {
                ref Point3ThatFitCacheLine pt = ref arrayOfPtsCL[i];

                float norm = (float)Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y + pt.Z * pt.Z);

                pt.X /= norm;
                pt.Y /= norm;
                pt.Z /= norm;

                arrayOfPtsCL[i] = pt;
            }
        }

        [Benchmark]
        public void VectorNormSoA()
        {
            // Note: Data reorganized as structure of arrays
            //      Sequential access + data fits in L1 cache line
            for (int i = 0; i < arrayOfX.Length; ++i)
            {
                float norm = (float)Math.Sqrt(arrayOfX[i] * arrayOfX[i] + arrayOfY[i] * arrayOfY[i] + arrayOfZ[i] * arrayOfZ[i]);

                arrayOfX[i] /= norm;
                arrayOfY[i] /= norm;
                arrayOfZ[i] /= norm;
            }
        }

        [Benchmark]
        public void VectorNormSimd()
        {
            // Note: Data reorganized as structure of arrays
            int vecSize = Vector<float>.Count;
            for (int i = 0; i < arrayOfX.Length; i += vecSize)
            {
                Vector<float> x = new Vector<float>(arrayOfX, i);
                Vector<float> y = new Vector<float>(arrayOfY, i);
                Vector<float> z = new Vector<float>(arrayOfZ, i);

                Vector<float> norm = Vector.SquareRoot(x * x + y * y + z * z);

                x /= norm;
                y /= norm;
                z /= norm;

                x.CopyTo(arrayOfX, i);
                y.CopyTo(arrayOfY, i);
                z.CopyTo(arrayOfZ, i);
            }
        }
    }
}
