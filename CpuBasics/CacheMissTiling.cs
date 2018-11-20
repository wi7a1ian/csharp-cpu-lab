using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;

namespace CpuBasics
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.ColdStart, launchCount: 5)]
    [HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.LlcMisses)]
    public class CacheMissTiling
    {
        [Params(4, 8, 16, 32)]
        public int TiledBlockSize { get; set; }

        // 1024x1024 x 4 bytes x 2 matrices = 8MB - should not fit in L3 cache 
        [Params(1024, 2048)]
        public int RowCount { get; set; }

        public int ColCount => RowCount;

        private float[] sourceImage;
        private float[] rotatedImage;

        [GlobalSetup]
        public void Setup()
        {
            sourceImage = new float[RowCount * ColCount];
            rotatedImage = new float[RowCount * ColCount];

            var random = new Random(42);
            for (int i = 0; i < sourceImage.Length; ++i)
            {
                sourceImage[i] = random.Next();
            }
        }

        [Benchmark]
        public void RotateNaive()
        {
            for (int y = 0; y < RowCount; ++y)
            {
                for (int x = 0; x < ColCount; ++x)
                {
                    int from = y * ColCount + x;
                    int to = x * RowCount + y;
                    rotatedImage[to] = sourceImage[from];
                }
            }
        }

        [Benchmark]
        public void RotateTiled()
        {
            int blockWidth = TiledBlockSize;
            int blockHeight = TiledBlockSize;

            for (int y = 0; y < RowCount; y += blockHeight)
            {
                for (int x = 0; x < ColCount; x += blockWidth)
                {
                    for (int by = 0; by < blockHeight; ++by)
                    {
                        for (int bx = 0; bx < blockWidth; ++bx)
                        {
                            int from = (y + by) * ColCount + (x + bx);
                            int to = (x + bx) * RowCount + (y + by);
                            rotatedImage[to] = sourceImage[from];
                        }
                    }
                }
            }
        }
    }
}
