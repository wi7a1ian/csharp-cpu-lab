using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace CpuBasics
{
    public class CacheInvalidation
    {
        private const int STEPS = 10000000;
        private const double FROM = 0.0;
        private const double TO = 1.0;

        private static double Function(double x)
        {
            return 4.0 / (1 + x * x);
        }

        [Params(1, 2, 4)]
        public int Parallelism { get; set; }

        private void IntegrateHelper(Func<double, double> f, double from, double to, int steps, out double integral)
        {
            integral = 0.0;
            double stepSize = (to - from) / steps;
            for (int i = 0; i < steps; ++i)
            {
                integral += stepSize * f(from + ((i + 0.5) * stepSize));
            }
        }

        [Benchmark]
        public double IntegrateSequential()
        {
            double integral = 0.0;
            IntegrateHelper(Function, FROM, TO, STEPS, out integral);
            return integral;
        }

        [Benchmark]
        public double IntegrateParallelSharing()
        {
            double[] partialIntegrals = new double[Parallelism];
            double chunkSize = (TO - FROM) / Parallelism;
            int chunkSteps = STEPS / Parallelism;

            Thread[] threads = new Thread[Parallelism];
            for (int i = 0; i < Parallelism; ++i)
            {
                double myFrom = FROM + i * chunkSize;
                double myTo = myFrom + chunkSize;
                int myIndex = i;
                threads[i] = new Thread(() =>
                {
                    IntegrateHelper(Function, myFrom, myTo, chunkSteps, out partialIntegrals[myIndex]);
                });
                threads[i].Start();
            }

            foreach (var thread in threads) thread.Join();
            return partialIntegrals.Sum();
        }

        [Benchmark]
        public double IntegrateParallelPrivate()
        {
            double[] partialIntegrals = new double[Parallelism];
            double chunkSize = (TO - FROM) / Parallelism;
            int chunkSteps = STEPS / Parallelism;

            Thread[] threads = new Thread[Parallelism];
            for (int i = 0; i < Parallelism; ++i)
            {
                double myFrom = FROM + i * chunkSize;
                double myTo = myFrom + chunkSize;
                int myIndex = i;
                threads[i] = new Thread(() =>
                {
                    double myIntegral = 0.0;
                    IntegrateHelper(Function, myFrom, myTo, chunkSteps, out myIntegral);
                    partialIntegrals[myIndex] = myIntegral;
                });
                threads[i].Start();
            }

            foreach (var thread in threads) thread.Join();
            return partialIntegrals.Sum();
        }
    }

    #region Spoiler
    /*
     * When working with threads where large object is schared among them i.e: array), 
     * when one thread modifies block of cached data, all other threads that work with 
     * the same copy have to re-read the data from the memory, aka invalidate. 
     * This is speed up with MESI protocol that requires cache to cache transfer on a miss if the block resides in another cache.
     * 
     * When such unintentional cache sharing happens, parallel method should use private memory and then update shared memory when done, 
     * or let them modify/access only memory regions that are CL1 size (=~ 64kb) bytes away from each other.
     * 
     * Remember:
     * - Design for parallelization
     */
    #endregion
}
