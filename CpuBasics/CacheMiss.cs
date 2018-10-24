using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;

namespace CpuBasics
{
    [SimpleJob(RunStrategy.ColdStart, launchCount: 5)]
    //[HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.LlcMisses)]
    public class CacheMiss
    {
        [Params(1024, 2048)]
        public int RowCount { get; set; }

        public int ColCount => RowCount;

        private float[] _image;
        private float[] _rotated;

        [Params(4, 8, 16, 32)]
        public int TiledBlockSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _image = new float[RowCount * ColCount];
            _rotated = new float[RowCount * ColCount];

            var random = new Random(42);
            for (int i = 0; i < _image.Length; ++i)
            {
                _image[i] = random.Next();
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
                    _rotated[to] = _image[from];
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
                            _rotated[to] = _image[from];
                        }
                    }
                }
            }
        }
    }

    #region Spoiler
    /*
     * Problem: 1024x1024 matrix, Intel Core i5 with L1 cache 128KB, 8-ways, line size of 64. 
     *  Each cache line can hold 8 floats of 8 bytes each.
     *  The critical stride is 128KiB / 8 = 16KiB = 2 rows.
     * 
     * Cache miss is a state where the data requested for processing by a component or application is not found in the cache memory. 
     * Each cache miss slows down the overall process because after a cache miss, the central processing unit (CPU) will look for 
     * a higher level cache, such as L1, L2, L3 and random access memory (RAM) for that data. 
     * Further, a new entry is created and copied in cache before it can be accessed by the processor.
     * 
     * About the cache organization:
     *  Most caches are organized into lines and sets, i.e: a cache of 8 kb size with a line size of 64 bytes. 
     *  Each line covers 64 consecutive bytes of memory. One kilobyte is 1024 bytes, so we can calculate that the number of lines is 8*1024/64 = 128. 
     *  These lines are organized as 32 sets x 4 ways. This means that a particular memory address cannot be loaded into an arbitrary cache line. 
     *  Only one of the 32 sets can be used, but any of the 4 lines in the set can be used. 
     *  We can calculate which set of cache lines to use for a particular memory address by the formula: (set) = (memory address) / (line size) % (number of sets). 
     *  For example, if we want to read from memory address a= 10000, then we have (set) = (10000 / 64) % 32 = 28. 
     *  This means that amust be read into one of the four cache lines in set number 28.
     *  If the cache always chooses the least recently used cache line then the line that covered the address range from X to Y will be evicted when we read from Z.
     *  Reading again from address X will cause a cache miss. The problem only occurs because the addresses are spaced a multiple of 0x800 apart. 
     *  I will call this distance the critical stride.  Variables whose distance in memory is a multiple of the critical stride will contend for the same cache lines.
     *  The critical stride can be calculated as: (critical stride) = (number of sets) x (line size) = (total cache size) / (number of ways)
     * 
     * Cache contentions in large data structures:
     *  It takes much more time to transpose the matrix when the size of the matrix is a multiple of the level-1 cache size.
     *  This is because the critical stride is a multiple of the size of a matrix line.
     *  The effect is much more dramatic when contentions occur in the level-2 cache...
     * 
     * A cache works most efficiently when the data are accessed sequentially. 
     * It works somewhat less efficiently when data are accessed backwards and much less efficiently when data are accessed in a random manner. 
     * This applies to reading as well as writing data. Multidimensional arrays should be accessed with the last index changing in the innermost loop. 
     * This reflects the order in which the elements are stored in memory. 
     * 
     * When iterating over arrays, consider CPU cache sizes (L1=64kb, L2=2MB, L3=8MB) and process only X (= L1 size) bytes at a time for best performance gain.
     * Remember about critical stride. I.E: when acessing memory on Intel Core i7-8550U try not to jump by more than 128KiB / 8-ways = 16KiB to maximize L1 cache.
     * Usually smallest cache line is 64 bytes, consider structs no larger that this value.
     * 
     * Use BenchmarkDotNet or Hardware Counters like L1c misses/op for diagnostics.
     * 
     * * Remember:
     * - Fit the L1 cache line
     * - Remember about critical stride
     * 
     * https://surana.wordpress.com/2009/01/01/numbers-everyone-should-know/
     * https://en.wikipedia.org/wiki/CPU_cache#MULTILEVEL
     */
    #endregion
}
