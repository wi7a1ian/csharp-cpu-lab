using System;
using System.Collections.Generic;
using System.Numerics; // Vector<T>
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace CpuBasics
{
    [SimpleJob(RunStrategy.ColdStart, launchCount: 5)]
    public class SIMD
    {
        [Params(512 * 1024)]
        public int NumberCount { get; set; }

        private int[] numbers;

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            numbers = new int[NumberCount];
            for (int i = 0; i < numbers.Length; ++i)
                numbers[i] = random.Next();
        }

        [Benchmark]
        public Tuple<int, int> MinMaxNaive()
        {
            int max = int.MinValue, min = int.MaxValue;
            foreach (var i in numbers)
            {
                min = Math.Min(min, i);
                max = Math.Max(max, i);
            }
            return new Tuple<int, int>(min, max);
        }

        [Benchmark]
        public Tuple<int, int> MinMaxILP()
        {
            int max1 = int.MinValue, max2 = int.MinValue, min1 = int.MaxValue, min2 = int.MaxValue;
            for (int i = 0; i < numbers.Length; i += 2)
            {
                int d1 = numbers[i], d2 = numbers[i + 1];
                min1 = Math.Min(min1, d1);
                min2 = Math.Min(min2, d2);
                max1 = Math.Max(max1, d1);
                max2 = Math.Max(max2, d2);
            }
            return new Tuple<int, int>(Math.Min(min1, min2), Math.Max(max1, max2));
        }

        [Benchmark]
        public Tuple<int, int> MinMaxParallel()
        {
            int threads = Environment.ProcessorCount;
            int[] mins = new int[threads], maxs = new int[threads];
            int chunkSize = numbers.Length / threads;
            Parallel.For(0, threads, i =>
            {
                int min = int.MaxValue, max = int.MinValue;
                int from = chunkSize * i, to = chunkSize * (i + 1);
                for (int j = from; j < to; ++j)
                {
                    min = Math.Min(min, numbers[j]);
                    max = Math.Max(max, numbers[j]);
                }
                mins[i] = min;
                maxs[i] = max;
            });
            return new Tuple<int, int>(mins.Min(), maxs.Max());
        }

        [Benchmark]
        public Tuple<int, int> MinMaxSimd()
        {
            Vector<int> vmin = new Vector<int>(int.MaxValue), vmax = new Vector<int>(int.MinValue);
            int vecSize = Vector<int>.Count;
            for (int i = 0; i < numbers.Length; i += vecSize)
            {
                Vector<int> vdata = new Vector<int>(numbers, i);
                Vector<int> minMask = Vector.LessThan(vdata, vmin);
                Vector<int> maxMask = Vector.GreaterThan(vdata, vmax);
                vmin = Vector.ConditionalSelect(minMask, vdata, vmin);
                vmax = Vector.ConditionalSelect(maxMask, vdata, vmax);
            }
            int min = int.MaxValue, max = int.MinValue;
            for (int i = 0; i < vecSize; ++i)
            {
                min = Math.Min(min, vmin[i]);
                max = Math.Max(max, vmax[i]);
            }
            return new Tuple<int, int>(min, max);
        }

        [Benchmark]
        public unsafe Tuple<int, int> MinMaxAvx()
        {
            var vmin = Vector256.Create(int.MaxValue);
            var vmax = Vector256.Create(int.MinValue);
            int vecSize = Vector<int>.Count;

            fixed (int* pNumbers = numbers)
            {
                int i;
                for (i = 0; i < numbers.Length; i += vecSize)
                {
                    var d = Avx2.LoadVector256(pNumbers + i);
                    vmin = Avx2.Min(vmin, d);
                    vmax = Avx2.Max(vmax, d);
                }

                // tail is pretty much irrelevant
                int tailLen = (numbers.Length % vecSize);
                for (i = 0; i < tailLen; ++i)
                {
                    var d = Vector256.Create(numbers[numbers.Length - i - 1]);
                    vmin = Avx2.Min(vmin, d);
                    vmax = Avx2.Max(vmax, d);
                }
            }

            int min = int.MaxValue, max = int.MinValue;
            for (int i = 0; i < vecSize; ++i)
            {
                min = Math.Min(min, vmin.GetElement(i));
                max = Math.Max(max, vmax.GetElement(i));
            }

            return new Tuple<int, int>(min, max);
        }

        [Benchmark]
        public unsafe Tuple<int, int> MinMaxAvxStackalloc()
        {
            // We are going to pay the price of not using Vector256 even tho stack is in use
            Span<int> vmin = stackalloc int[Vector<int>.Count]; 
            Span<int> vmax = stackalloc int[Vector<int>.Count];
            int vecSize = Vector<int>.Count;

            vmin.Fill(int.MaxValue);
            vmax.Fill(int.MinValue);

            fixed (int* pNumbers = numbers)
            fixed (int* pVmin = vmin)
            fixed (int* pVmax = vmax)
            {
                int i;
                for (i = 0; i < numbers.Length; i += vecSize)
                {
                    var d = Avx2.LoadVector256(pNumbers + i);
                    Avx2.Store(pVmin, Avx2.Min(Avx2.LoadVector256(pVmin), d));
                    Avx2.Store(pVmax, Avx2.Max(Avx2.LoadVector256(pVmax), d));
                }

                int tailLen = (numbers.Length % vecSize);
                for (i = 0; i < tailLen; ++i)
                {
                    var d = Vector256.Create(numbers[numbers.Length - i - 1]);
                    Avx2.Store(pVmin, Avx2.Min(Avx2.LoadVector256(pVmin), d));
                    Avx2.Store(pVmax, Avx2.Max(Avx2.LoadVector256(pVmax), d));
                }
            }

            int min = int.MaxValue, max = int.MinValue;
            for (int i = 0; i < vecSize; ++i)
            {
                min = Math.Min(min, vmin[i]);
                max = Math.Max(max, vmax[i]);
            }

            return new Tuple<int, int>(min, max);
        }
    }
}
