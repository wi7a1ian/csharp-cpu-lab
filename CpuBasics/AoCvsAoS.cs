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
    public class AoCvsAoS
    {
        public class SomeSubclass
        {
            string Name;
            int Number;
        }

        public class ClassThatDontFitCacheLine // Contains Object Header (8 bytes) and Method Table Ptr (8 bytes)
        {
            public float X; // <------------- Used for calculations
            public float Y; // <------------- Used for calculations
            public float Z; // <------------- Used for calculations
            public int SomeField1;
            public double SomeField2;
            public double SomeField3;
            public bool SomeField4;
            public double SomeField5;
            public double SomeField6;
            SomeSubclass ObjectByRef;
        }

        public struct StructThatFitCacheLine //  No Object Header and no Method Table
        {
            public float X; // <------------- Used for calculations
            public float Y; // <------------- Used for calculations
            public float Z; // <------------- Used for calculations
            public int SomeField1;
            public double SomeField2;
            public double SomeField3;
            public bool SomeField4;
            public double SomeField5;
            public double SomeField6;
            SomeSubclass ObjectByRef;
        }

        [Params(1024*512)]
        public int ArraySize { get; set; }

        private List<ClassThatDontFitCacheLine> arrayOfObjects;
        private StructThatFitCacheLine[] arrayOfStructs;

        [GlobalSetup]
        public void Setup()
        {
            var rand = new Random(42);

            arrayOfObjects = new List<ClassThatDontFitCacheLine>();
            arrayOfStructs = new StructThatFitCacheLine[ArraySize];

            for (int i = 0; i < ArraySize; ++i)
            {
                arrayOfObjects.Add(new ClassThatDontFitCacheLine
                {
                    X = (float)rand.NextDouble(),
                    Y = (float)rand.NextDouble(),
                    Z = (float)rand.NextDouble()
                });
                arrayOfStructs[i] = new StructThatFitCacheLine
                {
                    X = arrayOfObjects[i].X,
                    Y = arrayOfObjects[i].Y,
                    Z = arrayOfObjects[i].Z
                };
            }
        }

        [Benchmark]
        public void VectorNormAoC()
        {
            for (int i = 0; i < arrayOfObjects.Count; ++i)
            {
                var vect = arrayOfObjects[i];

                float norm = (float)Math.Sqrt(vect.X * vect.X + vect.Y * vect.Y + vect.Z * vect.Z);

                vect.X /= norm;
                vect.Y /= norm;
                vect.Z /= norm;
            }
        }

        [Benchmark]
        public void VectorNormAoS()
        {
            for (int i = 0; i < arrayOfObjects.Count; ++i)
            {
                ref var vect = ref arrayOfStructs[i];

                float norm = (float)Math.Sqrt(vect.X * vect.X + vect.Y * vect.Y + vect.Z * vect.Z);

                vect.X /= norm;
                vect.Y /= norm;
                vect.Z /= norm;
            }
        }
    }
}
