using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace CpuBasics
{
    [SimpleJob(RunStrategy.ColdStart, launchCount: 5)]
    public class AoSvsSoA
    {
        public class SomeSubclass
        {
            string Name;
            int Number;
        }

        public struct VectorThatDontFitCacheLine // Contains Object Header (8 bytes) and Method Table Ptr (8 bytes)
        {
            public float SomeField1;
            public float X; // <------------- Used for calculations
            public int SomeField2;
            public float Y; // <------------- Used for calculations
            public float Z; // <------------- Used for calculations
            public double SomeField3;
            public bool SomeField4;
            public double SomeField5;
            public double SomeField6;
            public double SomeField7;
            public double SomeField8;
            public bool SomeField9;
            DateTime UpdateTime;
            SomeSubclass ObjectByRef; // Will be moved to the beginning of the struct

            public float GetNormFactor() => (float)Math.Sqrt(X * X + Y * Y + Z * Z); // Does not add MT to the header

            public void Normalize()  // Does not add MT to the header
            {
                var norm = GetNormFactor();

                X /= norm;
                Y /= norm;
                Z /= norm;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VectorThatFitCacheLine
        {
            public float SomeField1;
            public float X; // <------------- Used for calculations
            public int SomeField2;
            public float Y; // <------------- Used for calculations
            public float Z; // <------------- Used for calculations
            public double SomeField3;
            public bool SomeField4;
            public double SomeField5;
            public double SomeField6;
            public double SomeField7;
            public double SomeField8;
            public bool SomeField9;
            //DateTime UpdateTime;          // LayoutKind.Auto inside! will force same layout type in our struct
            //SomeSubclass ObjectByRef;     // Will force LayoutKind.Auto too

            public float GetNormFactor() => (float)Math.Sqrt(X * X + Y * Y + Z * Z); // Does not add MT to the header

            public void Normalize()  // Does not add MT to the header
            {
                var norm = GetNormFactor();

                X /= norm;
                Y /= norm;
                Z /= norm;
            }
        }

        public struct Vectors
        {
            public float[] arrayOfX;
            public float[] arrayOfY;
            public float[] arrayOfZ;
        }

        [Params(1024*512)]
        public int ArraySize { get; set; }

        // Array of structs that don't fit the cache line
        private VectorThatDontFitCacheLine[] arrayOfStructs;

        // Array of structs that fit the cache line
        private VectorThatFitCacheLine[] arrayOfStructsCL;

        // Reorganized as structure of arrays
        private Vectors vectors;
        

        [GlobalSetup]
        public void Setup()
        {
            var rand = new Random(42);

            arrayOfStructs = new VectorThatDontFitCacheLine[ArraySize];
            arrayOfStructsCL = new VectorThatFitCacheLine[ArraySize];
            vectors.arrayOfX = new float[ArraySize];
            vectors.arrayOfY = new float[ArraySize];
            vectors.arrayOfZ = new float[ArraySize];

            for (int i = 0; i < ArraySize; ++i)
            {
                vectors.arrayOfX[i] = (float)rand.NextDouble();
                vectors.arrayOfY[i] = (float)rand.NextDouble();
                vectors.arrayOfZ[i] = (float)rand.NextDouble();

                arrayOfStructs[i] = new VectorThatDontFitCacheLine
                {
                    X = vectors.arrayOfX[i],
                    Y = vectors.arrayOfY[i],
                    Z = vectors.arrayOfZ[i]
                };
                arrayOfStructsCL[i] = new VectorThatFitCacheLine
                {
                    X = vectors.arrayOfX[i],
                    Y = vectors.arrayOfY[i],
                    Z = vectors.arrayOfZ[i]
                };
            }
        }

        [Benchmark]
        public void VectorNormAoS()
        {
            for (int i = 0; i < arrayOfStructs.Length; ++i)
            {
                VectorThatDontFitCacheLine pt = arrayOfStructs[i];

                float norm = (float)Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y + pt.Z * pt.Z);

                pt.X /= norm;
                pt.Y /= norm;
                pt.Z /= norm;

                arrayOfStructs[i] = pt;
            }
        }

        [Benchmark]
        public void VectorNormAoSViaMember()
        {
            for (int i = 0; i < arrayOfStructs.Length; ++i)
            {
                arrayOfStructsCL[i].Normalize();
            }
        }

        [Benchmark]
        public void VectorNormAoSFitCL()
        {
            for (int i = 0; i < arrayOfStructs.Length; ++i)
            {
                VectorThatFitCacheLine pt = arrayOfStructsCL[i];

                float norm = (float)Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y + pt.Z * pt.Z);

                pt.X /= norm;
                pt.Y /= norm;
                pt.Z /= norm;

                arrayOfStructsCL[i] = pt;
            }
        }

        [Benchmark]
        public void VectorNormAoSFitCLViaMember()
        {
            for (int i = 0; i < arrayOfStructs.Length; ++i)
            {
                arrayOfStructsCL[i].Normalize();
            }
        }

        [Benchmark]
        public void VectorNormSoA()
        {
            // Note: Data reorganized as structure of arrays
            //      Sequential access + data fits in L1 cache line
            for (int i = 0; i < vectors.arrayOfX.Length; ++i)
            {
                float norm = (float)Math.Sqrt(vectors.arrayOfX[i] * vectors.arrayOfX[i] 
                    + vectors.arrayOfY[i] * vectors.arrayOfY[i] 
                    + vectors.arrayOfZ[i] * vectors.arrayOfZ[i]);

                vectors.arrayOfX[i] /= norm;
                vectors.arrayOfY[i] /= norm;
                vectors.arrayOfZ[i] /= norm;
            }
        }

        [Benchmark]
        public void VectorNormSimd()
        {
            // Note: Data reorganized as structure of arrays
            int vecSize = Vector<float>.Count;
            for (int i = 0; i < vectors.arrayOfX.Length; i += vecSize)
            {
                Vector<float> x = new Vector<float>(vectors.arrayOfX, i);
                Vector<float> y = new Vector<float>(vectors.arrayOfY, i);
                Vector<float> z = new Vector<float>(vectors.arrayOfZ, i);

                Vector<float> norm = Vector.SquareRoot(x * x + y * y + z * z);

                x /= norm;
                y /= norm;
                z /= norm;

                x.CopyTo(vectors.arrayOfX, i);
                y.CopyTo(vectors.arrayOfY, i);
                z.CopyTo(vectors.arrayOfZ, i);
            }
        }
    }
}
