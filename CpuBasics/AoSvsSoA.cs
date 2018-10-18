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
     * This approach is strongly connected to Data Oriented Programming. When working with arrays of structs, 
     * consider switching to struct of arrays instead, then we can benefit from vectorization (SIMD instructions) and sequential data access.
     * 
     * When working with Structs:
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
