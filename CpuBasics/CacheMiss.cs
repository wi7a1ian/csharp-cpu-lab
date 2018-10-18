using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace CpuBasics
{
    public class CacheMiss
    {
        private const int ROWS = 1024;
        private const int COLS = 1024;
        private float[] _image = new float[ROWS * COLS];
        private float[] _rotated = new float[ROWS * COLS];

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            for (int i = 0; i < _image.Length; ++i)
            {
                _image[i] = random.Next();
            }
        }

        [Benchmark]
        public void RotateNaive()
        {
            for (int y = 0; y < ROWS; ++y)
            {
                for (int x = 0; x < COLS; ++x)
                {
                    int from = y * COLS + x;
                    int to = x * ROWS + y;
                    _rotated[to] = _image[from];
                }
            }
        }

        [Benchmark]
        public void RotateTiled()
        {
            const int blockWidth = 8, blockHeight = 8; // TODO Need to test appropriate values
            for (int y = 0; y < ROWS; y += blockHeight)
            {
                for (int x = 0; x < COLS; x += blockWidth)
                {
                    for (int by = 0; by < blockHeight; ++by)
                    {
                        for (int bx = 0; bx < blockWidth; ++bx)
                        {
                            int from = (y + by) * COLS + (x + bx);
                            int to = (x + bx) * ROWS + (y + by);
                            _rotated[to] = _image[from];
                        }
                    }
                }
            }
        }
    }

    #region Spoiler
    /*
     * Cache miss is a state where the data requested for processing by a component or application is not found in the cache memory. 
     * Each cache miss slows down the overall process because after a cache miss, the central processing unit (CPU) will look for 
     * a higher level cache, such as L1, L2, L3 and random access memory (RAM) for that data. 
     * Further, a new entry is created and copied in cache before it can be accessed by the processor.
     * 
     * When iterating over arrays, consider CPU cache sizes (L1=64kb, L2=2MB, L3=8MB) and process only X (= L1 size) bytes at a time for best performance gain.
     *
     * Use BenchmarkDotNet or Hardware Counters like L1c misses/op for diagnostics.
     * 
     * * Remember:
     * - Fit the cache line
     * 
     * https://surana.wordpress.com/2009/01/01/numbers-everyone-should-know/
     */
    #endregion
}
