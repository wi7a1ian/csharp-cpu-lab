using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace CpuBasics
{
    class AoSvsSoA
    {
        [GlobalSetup]
        public void Setup()
        {
        }

        [Benchmark]
        public void MinMaxAoS()
        {
            // TODO
        }

        [Benchmark]
        public void MinMaxSoA()
        {
            // TODO
        }
    }

    #region Spoiler
    /*
     * A variable is accessed most efficiently if it is stored at a memory address which is divisible by the size of the variable. 
     * For example, a doubletakes 8 bytes of storage space. It should therefore preferably be stored at an address divisible by 8. 
     * The size should always be a power of 2. Objects bigger than 16 bytes should be stored at an address divisible by 16. 
     * You can generally assume that the compiler takes care of this alignment automatically.
     * 
     * You may choose to align large objects and arrays by the cache line size, which is typically 64 bytes. 
     * This makes sure that the beginning of the object or array coincides with the beginning of a cache line. 
     * Some compilers will align large static arrays automatically
     * 
     * This approach is strongly connected to Data Oriented Programming. When working with arrays of structs, 
     * consider switching to struct of arrays instead, then we can benefit from vectorization (SIMD instructions) and sequential data access.
     * 
     * It is often more efficient to allocate one big block of memory for all the objects (memory pooling) than to allocate a small block for each object. (List vs Vector in C++)
     * 
     * When working with Structs:
     * - Look at the operations in the loop and decide if it is more beneficial to use AoS or SoA to guarantee sequentialy memory access.
     * - References are located first (by the JIT compiler). It is caused by automatic layout that places refs right after struct header and method map.
     * - Apply [StructLayout(LayoutKind.Sequential)] to fix this, just be carefull for structs used internally because they can have LayoutKind.Auto like DateTime does.
     * Consider ECS like
     * - Entitas - https://github.com/sschmid/Entitas-CSharp
     * - Unity ECS
     * 
     * Remember:
     * - TODO
     * fix: 
     */
    #endregion
}
