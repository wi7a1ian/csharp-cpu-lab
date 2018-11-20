using System;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;

namespace CpuBasics
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.ColdStart, launchCount: 5)]
    [HardwareCounters(HardwareCounter.LlcMisses)]
    public class CacheInvalidation
    {
        [Params(4)]
        public int Parallelism { get; set; }

        private const int stepCount = 10000000;
        private const double fromX = 0.0;
        private const double toX = 1.0;

        private static double Function(double x)
        {
            return 4.0 / (1 + x * x);
        }

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
            IntegrateHelper(Function, fromX, toX, stepCount, out integral);
            return integral;
        }

        [Benchmark]
        public double IntegrateParallelSharing()
        {
            double[] partialIntegrals = new double[Parallelism];
            double chunkSize = (toX - fromX) / Parallelism;
            int chunkSteps = stepCount / Parallelism;

            Thread[] threads = new Thread[Parallelism];
            for (int i = 0; i < Parallelism; ++i)
            {
                double myFrom = fromX + i * chunkSize;
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
        public double IntegrateParallelSkeved()
        {
            //  8 bytes (double) * 8 * Parallelism = 64 * Parallelism
            double[] partialIntegrals = new double[Parallelism * 8];
            double chunkSize = (toX - fromX) / Parallelism;
            int chunkSteps = stepCount / Parallelism;

            Thread[] threads = new Thread[Parallelism];
            for (int i = 0; i < Parallelism; ++i)
            {
                double myFrom = fromX + i * chunkSize;
                double myTo = myFrom + chunkSize;
                int myIndex = i;
                threads[i] = new Thread(() =>
                {
                    IntegrateHelper(Function, myFrom, myTo, chunkSteps, out partialIntegrals[myIndex * 8]);
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
            double chunkSize = (toX - fromX) / Parallelism;
            int chunkSteps = stepCount / Parallelism;

            Thread[] threads = new Thread[Parallelism];
            for (int i = 0; i < Parallelism; ++i)
            {
                double myFrom = fromX + i * chunkSize;
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
}
