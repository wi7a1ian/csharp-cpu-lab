# csharp-cpu-lab (IN PROGRESS)

![](https://github.com/wi7a1ian/csharp-cpu-lab/blob/master/Img/CPUCache.PNG)

### Pipeline of a modern high-performance CPU
![](https://github.com/wi7a1ian/csharp-cpu-lab/blob/master/Img/CPU-front-n-backend.png)

## Branch prediction
#### Problem
[SO: Why is it faster to process a sorted array than an unsorted array?](https://stackoverflow.com/questions/11227809/why-is-it-faster-to-process-a-sorted-array-than-an-unsorted-array?rq=1)

```
int sum = 0;
foreach (var i in array)
	if (i >= 128)
		sum += i;
return sum;
```

#### Benchmark
```
        Method |        Mean |    StdDev |      Median |
-------------- |------------ |---------- |------------ |
   SortedArray |  22.7367 us | 0.6702 us |  22.3573 us |
 UnsortedArray | 149.3315 us | 0.6229 us | 149.4772 us |
```

#### Which comes first?
```
if(...)
	bar1();
else 
	bar2();
}
```
- https://godbolt.org/z/ky07ZI
- BTFNT - Backward-taken for loops that jump backwards, forward-not-taken for if-then-else.
- `bar1()` is a fall-through, which means:
	```
	test %x, %y;
	je .bar; 
	call foo(); <-- this gets prefetched
	...
	```
- In case of branch misprediction a stall is taken if it should go throught bar2() (it can take 20 cycles to load instructions).
- `switch` statement order is totally random under most compilers and optimization flags. Operations that are most commonly expected should end up in separate `if` and the actual` switch` should be inside of `else` clause.

## Cache miss
Cache miss is a state where the data requested for processing by a component or application is not found in the cache memory. 
Each cache miss slows down the overall process because after a cache miss, the central processing unit (CPU) will look for 
a higher level cache, such as L1, L2, L3 and random access memory (RAM) for that data. 
Further, a new entry is created and copied in cache before it can be accessed by the processor.

#### Benchmark
```
      Method |      Mean |    StdErr |    StdDev |    Median |
------------ |---------- |---------- |---------- |---------- |
 RotateNaive | 8.7432 ms | 0.0400 ms | 0.1385 ms | 8.7250 ms |
 RotateTiled | 2.6486 ms | 0.0265 ms | 0.1092 ms | 2.6250 ms |
```

#### About the cache organization
Most caches are organized into lines and sets, i.e: a cache of 8 kb size with a line size of 64 bytes. 
Each line covers 64 consecutive bytes of memory. One kilobyte is 1024 bytes, so we can calculate that the number of lines is 8*1024/64 = 128. 
These lines are organized as 32 sets x 4 ways. This means that a particular memory address cannot be loaded into an arbitrary cache line. 
Only one of the 32 sets can be used, but any of the 4 lines in the set can be used. 
We can calculate which set of cache lines to use for a particular memory address by the formula: (set) = (memory address) / (line size) % (number of sets). 
For example, if we want to read from memory address a= 10000, then we have (set) = (10000 / 64) % 32 = 28. 
This means that amust be read into one of the four cache lines in set number 28.
If the cache always chooses the least recently used cache line then the line that covered the address range from X to Y will be evicted when we read from Z.
Reading again from address X will cause a cache miss. The problem only occurs because the addresses are spaced a multiple of 0x800 apart. 
I will call this distance the critical stride.  Variables whose distance in memory is a multiple of the critical stride will contend for the same cache lines.

The critical stride can be calculated as: `(critical stride) = (number of sets) x (line size) = (total cache size) / (number of ways)`

##### Exemplary cache organization
	1024x1024 matrix, Intel Core i5 with L1 cache 128KB, 8-ways, line size of 64. 
	Each cache line can hold 8 floats of 8 bytes each.
	The critical stride is 128KiB / 8 = 16KiB = 2 rows.

#### Cache contentions in large data structures
It takes much more time to transpose the matrix when the size of the matrix is a multiple of the L1 cache size.
This is because the critical stride is a multiple of the size of a matrix line.
The effect is much more dramatic when contentions occur in the L2 cache...

A cache works most efficiently when the data are accessed sequentially. 
It works somewhat less efficiently when data are accessed backwards and much less efficiently when data are accessed in a random manner. 
This applies to reading as well as writing data. Multidimensional arrays should be accessed with the last index changing in the innermost loop. 
This reflects the order in which the elements are stored in memory. 

#### Hyperthreading
Usually L1 cache lines are private (not shared between threads), but enabling hyperthreads will make them share L1 cache (like L3 is) which in turns cause resource contingency. Projects that strongly base on proper L1 cache utilization should turn this feature off.

#### Guidelines
Try to answer two questions:
- How big is your cache line?
- What's the most commonly accessed data?

When iterating over arrays, consider CPU cache sizes (L1=64kb, L2=2MB, L3=8MB) and process only X (= L1 size) bytes at a time for best performance gain.
Remember about critical stride. I.E: when acessing memory on Intel Core i7-8550U try not to jump by more than 128KiB / 8-ways = 16KiB to maximize L1 cache.
Usually smallest cache line is 64 bytes, consider structs no larger that this value.

#### Remember
- Fit the L1 cache line (64 bytes) when working with structs
- Keep the data used for one computation close (*array-of-structs*) so that it can be accessed sequentially and loaded into one cache line.
- Remember about critical stride when working with arrays/matrices/streams/buffers 

[Numbers everyone should know](https://surana.wordpress.com/2009/01/01/numbers-everyone-should-know/)

![](https://i.stack.imgur.com/a7jWu.png)

### Last-minute decision making - a mix of branch misprediction and cache miss
Consider this code:
```
void NodesTranslateWorldEach(Node* nodes, int count, const Vector3* t) {
	for( int i = 0; i < count; ++i) {
		Node* node = &nodes[i];
		
		// last-minute decision making
		if(node->m_parent) {
			node ->m_position += node->m_parent->...
		}
		else {
			node->m_position += t[i];
		}
	}
}
```

Fix - decision making could be done by the calling function and not here. Exemplary function definitions:
```
void NodesTranslateWorldEachRoot(Node* nodes, int count, const Vector3* t);
void NodesTranslateWorldEachWithParent(Node* nodes, int count, const Vector3* t) 
```

### Cache invalidation (cache coherence)
**There are only two hard things in computer science: cache invalidation and naming things.**

When working with threads where large object is schared among them (i.e: array), when one thread modifies block of cached data, all other threads that work with the same copy have to re-read the data from the memory, aka invalidate. This is speed up with MESI protocol that requires cache to cache transfer on a miss if the block resides in another cache.

This happens on level of cache lines = 64 bytes.

It is not about two cores accessing same memory location, it is two cores accessing adjecent memory locations which happen to be on the same cache line.

When such unintentional cache sharing happens, parallel method should use private memory and then update shared memory when done, or let them modify/access only memory regions that are L1 cache line size bytes away from each other.

#### MESI protocol
- Stands for line states: Modified, Exclusive, Shared, Invalid
- Guarantees cache coherence (same data, but in different caches, will match for a single memory location)
- Complex inter-CPU messaging guarantees correct state transitions

#### Invalidation scenario
1. Data frm the same memory location is in two separate L1 caches from two separate cores.
1. Both are marked as shared.
1. One core wants to modify the value so it marks it as *exclusive*, which leads to losing the value by the other core (aka *invalidation*).
1. Value gets modified and goes to the main memory.
1. The other core has to fetch the value from the memory location once again.

#### Benchmark
```
                   Method | Parallelism |       Mean |    StdErr |    StdDev |     Median |
------------------------- |------------ |----------- |---------- |---------- |----------- |
      IntegrateSequential |           1 | 46.4865 ms | 0.0313 ms | 0.1130 ms | 46.5153 ms |
 IntegrateParallelSharing |           1 | 46.7902 ms | 0.1595 ms | 0.5752 ms | 46.7739 ms |
 IntegrateParallelPrivate |           1 | 46.3889 ms | 0.0577 ms | 0.2233 ms | 46.3770 ms |
      IntegrateSequential |           2 | 47.0677 ms | 0.2844 ms | 1.1014 ms | 46.7873 ms |
 IntegrateParallelSharing |           2 | 36.1801 ms | 0.4011 ms | 1.5533 ms | 35.7176 ms |
 IntegrateParallelPrivate |           2 | 26.1333 ms | 0.2574 ms | 1.5229 ms | 25.8960 ms |
      IntegrateSequential |           4 | 46.4568 ms | 0.0470 ms | 0.1820 ms | 46.4738 ms |
 IntegrateParallelSharing |           4 | 32.5735 ms | 0.0972 ms | 0.3638 ms | 32.5975 ms |
 IntegrateParallelPrivate |           4 | 24.0229 ms | 0.0677 ms | 0.2531 ms | 23.9731 ms |
```

#### Remember
- Design for parallelization
- Be careful about hyperthreading that share L1 cache

## SIMD
Streaming SIMD Extensions (SSE) is an SIMD instruction set extension to the x86 architecture.
- Most `C++` compiles enable it by default (`/arch:SSE` or `-march=native` and `-msse2`). Libraries that support SSE are [boost::simd](https://github.com/NumScale/boost.simd), [vc](https://github.com/VcDevel/Vc), [libsimdpp](https://github.com/p12tic/libsimdpp), [cvalarray](http://sci.tuomastonteri.fi/programming/sse), [eigen](http://eigen.tuxfamily.org/index.php?title=FAQ#Vectorization)
- In `C#` available via `Vector<T>`

#### Benchmark
```
         Method |          Mean |     StdErr |     StdDev |        Median |
--------------- |-------------- |----------- |----------- |-------------- |
    MinMaxNaive |   603.0989 us |  5.2490 us | 20.3293 us |   597.3966 us |
      MinMaxILP | 1,025.9282 us | 10.4831 us | 54.4718 us | 1,024.1082 us |
     MinMaxSimd |   160.7848 us |  1.5912 us |  9.2784 us |   158.5755 us |
 MinMaxParallel |   416.3257 us |  2.1792 us |  8.4402 us |   416.8833 us |
```

- ILP option doesn't help because of 
  - *loop stream detector*, an Intel dedicated optimization for loops that are particularly small. The moment we "optimizaed" the code, the number of uOps within the loop got bigger, leading to optimization being turned off,
  - additional boundary checks being added by the compiler the moment we started accessing i+1 elements within the arrays.
- Parallel is not fastest because of data sharing (cache invalidation).
- Considering *single instruction, multiple data (SIMD)* does bring best performance boost when working with arrays. This can be achieved via `System.Numerics.Vectorization.Vector<T>` class in C#.

#### Remember
- Avoid nonsequential access
- Consider SIMD operations (Vector<T>)
- Speed things up for one core before you move to additional cores and parallelize


### AoS vs SoA
Making programs that can use predictable memory patterns is important. It is even more important with a threaded program, so that the memory requests do not jump all over; otherwise the processing unit will be waiting for memory requests to be fulfilled.

Aos-vs-soa term is strongly connected with **Data Oriented Programming**. When working with collections try to look for *hotpoints* that use several class/struct fields for calculations and then try to keep that data close by either using arrays-of-structs approach or struct-of-arrays instead. Either of those will be more beneficial from vectorization (SIMD instructions) and avoid cahce miss thanks to sequential data access. 

#### Benchmark 
```
          Method |     Mean |    Error |    StdDev |   Median |
---------------- |---------:|---------:|----------:|---------:|
 VectorNormNaive | 55.87 us | 14.15 us |  95.62 us | 43.03 us |
  VectorNormSimd | 39.99 us | 42.36 us | 286.18 us | 11.05 us |
```

#### Guidelines
A variable is accessed most efficiently if it is stored at a memory address which is divisible by the size of the variable. 
For example, a double takes 8 bytes of storage space. It should therefore preferably be stored at an address divisible by 8. 
The size should always be a power of 2. Objects bigger than 16 bytes should be stored at an address divisible by 16. 
You can generally assume that the compiler takes care of this alignment automatically.

You may choose to align large objects and arrays by the cache line size, which is typically 64 bytes. 
This makes sure that the beginning of the object or array coincides with the beginning of a cache line. 
Some compilers will align large static arrays automatically.

It is often more efficient to allocate one big block of memory for all the objects (memory pooling) than to allocate a small block for each object. (List vs Vector in C++)

When working with arrays & structs:
- Look at the operations in the loop and decide if it is more beneficial to use AoS or SoA to guarantee sequentialy memory access.
- In C# references are located first (by the JIT compiler). It is caused by automatic layout that places refs right after struct header and method map.
- Apply `[StructLayout(LayoutKind.Sequential)]` to fix this, just be carefull for structs used internally because they can have `LayoutKind.Auto` like DateTime does.
Consider ECS like
- Entitas - https://github.com/sschmid/Entitas-CSharp
- Unity ECS

Remember:
- TODO

### ECS
TODO

### How-to troubleshoot
- Modern processors have a PMU with PMCs:
  - LLC misses, branch mispredictions, instructions retired, μops decoded, etc.
  - Fire interrupt after a PMC was incremented N times
- [Intel's Top-Down Characterization](https://software.intel.com/en-us/vtune-amplifier-help-tuning-applications-using-a-top-down-microarchitecture-analysis-method) to determine if frontend of backend is the bottleneck
- [BenchmarkDotNet](https://benchmarkdotnet.org/) for C# - has a mode where it collects PMU events for a benchmark run
- [Google Benchmark](https://github.com/google/benchmark) for C++
- Hardware Counters - i.e for diagnosing amout of L1 cache misses per operation
- Intel Parallel Studio - tools for correlating PMU events with code and issuing guidance (Intel VTune Amplifier, Intel Threading Advisor, Intel Vector Advisor)
- ETW (Event tracing for Windows) can also track PMU events
