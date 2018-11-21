# csharp-cpu-lab (IN PROGRESS)
### Intel i7 Nehalem
![](https://github.com/wi7a1ian/csharp-cpu-lab/blob/master/Img/CPUCache.PNG)

### Pipeline of a modern high-performance CPU
![](https://github.com/wi7a1ian/csharp-cpu-lab/blob/master/Img/CPU-front-n-backend.png)

## Branch prediction
#### Problem #1
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

#### Problem #2 - which path will be executed faster?
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
	je .bar2; 
	call bar1(); <-- this gets prefetched
	...
	```
- In case of branch misprediction a stall is taken if it should go throught bar2() (it can take 20 cycles to load instructions).
- `switch` statement order is totally random under most compilers and optimization flags. Operations that are most commonly expected should end up in separate `if` and the actual` switch` should be inside of `else` clause.

## Cache miss
Cache miss is a state where the data requested for processing by a component or application is not found in the cache memory. 
Each cache miss slows down the overall process because after a cache miss, the central processing unit (CPU) will look for 
a higher level cache, such as L1, L2, L3 and random access memory (RAM) for that data. 
Further, a new entry is created and copied in cache before it can be accessed by the processor.

#### Benchmark #1 - sequential vs random data access
```
          Method | MatrixDimension |        Mean |      Error |    StdDev |      Median |
---------------- |---------------- |------------:|-----------:|----------:|------------:|
 MatrixMultNaive |             512 |    479.1 ms |  16.272 ms |  76.47 ms |    452.4 ms |
 MatrixMultReorg |             512 |    258.3 ms |   9.272 ms |  34.98 ms |    248.8 ms |
 MatrixMultNaive |            1024 | 11,301.5 ms | 135.276 ms | 545.57 ms | 11,118.6 ms |
 MatrixMultReorg |            1024 |  2,002.1 ms |  13.517 ms |  36.08 ms |  2,000.0 ms |
```

#### Benchmark #2 - tiling
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
Usually L1 & L2 cache lines are private (not shared between threads), but enabling hyperthreads will turn on *shared mode*, where the L1 data cache is competitively shared between logical processors, which in turns cause resource contingency. Projects that strongly base on proper L1 cache utilization should turn this feature off. 

#### Important questions to answer
- How big is your cache line?
- What's the most commonly accessed data?

#### Remember
- Predictable access patterns are faster. Favor sequential access over random.
- Large benefit is in hitting faster cache levels.
- Usually smallest cache line is 64 bytes, consider structs no larger that this value in order to keep the data used for one computation close.
- Keep the data used for heavy computations close (aka Access Locality), be it structs or arrays. Consder techniques like *tiling*.
- Strided memory access - when iterating over arrays, consider small strides (steps of size of a cache line). The smaler the linear stride is, the better the performance is since the data can be prefetched.
- Remember about critical stride when working with arrays/matrices/streams/buffers . I.e: when acessing memory on Intel Core i7-8550U try not to jump by more than 128KiB / 8-ways = 16KiB to maximize L1 cache utilization.

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

When such unintentional cache sharing happens, parallel method should use private memory and then update shared memory when done, or let them modify/access only memory regions that are L1 cache line size (64) bytes away from each other.

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
                   Method | Parallelism |     Mean |    Error |    StdDev |   Median | Allocated |
------------------------- |------------ |---------:|---------:|----------:|---------:|----------:|
      IntegrateSequential |           4 | 445.4 ms | 6.122 ms | 23.097 ms | 438.3 ms |      64 B |
 IntegrateParallelSharing |           4 | 369.6 ms | 3.627 ms | 15.672 ms | 396.8 ms |    1752 B |
  IntegrateParallelSkeved |           4 | 234.0 ms | 1.328 ms |  3.403 ms | 232.7 ms |    1976 B |
 IntegrateParallelPrivate |           4 | 233.0 ms | 1.086 ms |  2.744 ms | 232.6 ms |    1752 B |
```

#### Remember
- *Design for parallelization*
- Do not let threads to work with the same cache lines. Take care to avoid data sharing problems (same shared memory locations).
- Consider lock-free solutions
- Be careful about hyperthreading which share L2 cache

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

Aos-vs-soa term is strongly connected with **Data Oriented Programming**. When working with collections of objects try to look for *hotpoints* that use several class/struct fields for calculations and then try to keep that data close. If the data is repeatively modified the same way for multiple items, then consider switching to struct-of-arrays approach instead. The latter one will be more beneficial from vectorization (SIMD instructions) and has beter chances to avoid cache misses thanks to sequential data access. 

### Diagrams
#### AoC
![](https://github.com/wi7a1ian/csharp-cpu-lab/blob/master/Img/CPU-AoS-Class.svg)
#### AoS
![](https://github.com/wi7a1ian/csharp-cpu-lab/blob/master/Img/CPU-AoS-Struct.svg)
#### SoA
![](https://github.com/wi7a1ian/csharp-cpu-lab/blob/master/Img/CPU-SoA.svg)

#### Benchmark #1
```
                      Method | ArraySize |      Mean |     Error |    StdDev |    Median |
---------------------------- |---------- |----------:|----------:|----------:|----------:|
               VectorNormAoS |    524288 | 15.320 ms | 0.9692 ms | 2.8577 ms | 13.961 ms |
      VectorNormAoSViaMember |    524288 |  6.267 ms | 0.1727 ms | 0.5091 ms |  6.038 ms |
          VectorNormAoSFitCL |    524288 |  8.832 ms | 0.3219 ms | 0.9492 ms |  8.533 ms |
 VectorNormAoSFitCLViaMember |    524288 |  6.515 ms | 0.2505 ms | 0.7386 ms |  6.240 ms |
               VectorNormSoA |    524288 |  5.497 ms | 0.1098 ms | 0.2855 ms |  5.439 ms |
              VectorNormSimd |    524288 |  1.464 ms | 0.0737 ms | 0.2172 ms |  1.432 ms |
```

#### Benchmark #2
```
        Method | ArraySize |     Mean |     Error |    StdDev |   Median |
-------------- |---------- |---------:|----------:|----------:|---------:|
 VectorNormAoC |    524288 | 6.594 ms | 0.1747 ms | 0.5152 ms | 6.419 ms |
 VectorNormAoS |    524288 | 6.019 ms | 0.1196 ms | 0.3252 ms | 5.936 ms |
```

#### Guidelines
A variable is accessed most efficiently if it is stored at a memory address which is divisible by the size of the variable. 
For example, a double takes 8 bytes of storage space. It should therefore preferably be stored at an address divisible by 8. 
The size should always be a power of 2. Objects bigger than 16 bytes should be stored at an address divisible by 16. 
You can generally assume that the compiler takes care of this alignment automatically.

You may choose to align large objects and arrays by the cache line size, which is typically 64 bytes. 
This makes sure that the beginning of the object or array coincides with the beginning of a cache line. 
Some compilers will align large static arrays automatically.

It is often more efficient to allocate one big block of memory for all the objects (memory pooling) than to allocate a small block for each object (List vs Vector in C++). Its also more reasonable to reuse objects instead of deallocating.

When working with arrays & structs:
- Look at the operations in the loop and decide if it is more beneficial to move from AoS to SoA to guarantee sequential memory access.
- In C#
  - Apply `[StructLayout(LayoutKind.Sequential)]`to a struct to stop auto alignment, just be carefull for structs used internally because they can have `LayoutKind.Auto` like DateTime does.
  - Reference types are always located first (by the JIT compiler).
  - Classes have additional 16 bytes taken by *Object Header* (8 bytes) and *Method Table Ptr* (8 bytes). Keep that in mind.

#### Struct & class memory layout samples
```csharp
public struct VectorThatDontFitCacheLine { // Contains Object Header (8 bytes) and Method Table Ptr (8 bytes)
	public float SomeField1;
	public float X; // <------------- Used for calculations
	public int SomeField2;
	public float Y; // <------------- Used for calculations
	public float Z; // <------------- Used for calculations
	public double SomeField3;
	public bool SomeField4;
	public double SomeField5;
	public double SomeField6;
	public double SomeField7;
	public double SomeField8;
	public bool SomeField9;
	DateTime UpdateTime;
	SomeSubclass ObjectByRef; // Will be moved to the beginning of the struct
}
```

```
Size: 80 bytes. Paddings: 2 bytes (%2 of empty space)
|===========================================|
|   0-7: SomeSubclass ObjectByRef (8 bytes) |
|-------------------------------------------|
|  8-15: Double SomeField3 (8 bytes)        |
|-------------------------------------------|
| 16-23: Double SomeField5 (8 bytes)        |
|-------------------------------------------|
| 24-31: Double SomeField6 (8 bytes)        |
|-------------------------------------------|
| 32-39: Double SomeField7 (8 bytes)        |
|-------------------------------------------|
| 40-47: Double SomeField8 (8 bytes)        |
|-------------------------------------------|
| 48-51: Single SomeField1 (4 bytes)        |
|-------------------------------------------|
| 52-55: Single X (4 bytes)                 |
|-------------------------------------------|
| 56-59: Int32 SomeField2 (4 bytes)         |
|-------------------------------------------|
| 60-63: Single Y (4 bytes)                 |
|-------------------------------------------|
| 64-67: Single Z (4 bytes)                 |
|-------------------------------------------|
|    68: Boolean SomeField4 (1 byte)        |
|-------------------------------------------|
|    69: Boolean SomeField9 (1 byte)        |
|-------------------------------------------|
| 70-71: padding (2 bytes)                  |
|-------------------------------------------|
| 72-79: DateTime UpdateTime (8 bytes)      |
|===========================================|
```

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct VectorThatFitCacheLine {
	public float SomeField1;
	public float X; // <------------- Used for calculations
	public int SomeField2;
	public float Y; // <------------- Used for calculations
	public float Z; // <------------- Used for calculations
	public double SomeField3;
	public bool SomeField4;
	public double SomeField5;
	public double SomeField6;
	public double SomeField7;
	public double SomeField8;
	public bool SomeField9;
	//DateTime UpdateTime;          // LayoutKind.Auto inside! will force same layout type in our struct
	//SomeSubclass ObjectByRef;     // Will force LayoutKind.Auto too
}
```

```
Size: 80 bytes. Paddings: 18 bytes (%22 of empty space)
|====================================|
|   0-3: Single SomeField1 (4 bytes) |
|------------------------------------|
|   4-7: Single X (4 bytes)          |
|------------------------------------|
|  8-11: Int32 SomeField2 (4 bytes)  |
|------------------------------------|
| 12-15: Single Y (4 bytes)          |
|------------------------------------|
| 16-19: Single Z (4 bytes)          |
|------------------------------------|
| 20-23: padding (4 bytes)           |
|------------------------------------|
| 24-31: Double SomeField3 (8 bytes) |
|------------------------------------|
|    32: Boolean SomeField4 (1 byte) |
|------------------------------------|
| 33-39: padding (7 bytes)           |
|------------------------------------|
| 40-47: Double SomeField5 (8 bytes) |
|------------------------------------|
| 48-55: Double SomeField6 (8 bytes) |
|------------------------------------|
| 56-63: Double SomeField7 (8 bytes) |
|------------------------------------|
| 64-71: Double SomeField8 (8 bytes) |
|------------------------------------|
|    72: Boolean SomeField9 (1 byte) |
|------------------------------------|
| 73-79: padding (7 bytes)           |
|====================================|
```

```csharp
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
```

```
Size: 64 bytes. Paddings: 7 bytes (%10 of empty space)
|===========================================|
| Object Header (8 bytes)                   |
|-------------------------------------------|
| Method Table Ptr (8 bytes)                |
|===========================================|
|   0-7: SomeSubclass ObjectByRef (8 bytes) |
|-------------------------------------------|
|  8-15: Double SomeField2 (8 bytes)        |
|-------------------------------------------|
| 16-23: Double SomeField3 (8 bytes)        |
|-------------------------------------------|
| 24-31: Double SomeField5 (8 bytes)        |
|-------------------------------------------|
| 32-39: Double SomeField6 (8 bytes)        |
|-------------------------------------------|
| 40-43: Single X (4 bytes)                 |
|-------------------------------------------|
| 44-47: Single Y (4 bytes)                 |
|-------------------------------------------|
| 48-51: Single Z (4 bytes)                 |
|-------------------------------------------|
| 52-55: Int32 SomeField1 (4 bytes)         |
|-------------------------------------------|
|    56: Boolean SomeField4 (1 byte)        |
|-------------------------------------------|
| 57-63: padding (7 bytes)                  |
|===========================================|
```

### ECS - TODO
TODO

Consider ECS like
- Entitas - https://github.com/sschmid/Entitas-CSharp
- Unity ECS

### Remember:
- Fit the cache line (~64b)
- Fit the highest cache level (~8MiB)
- "Just" keep most “hot data” in L1/L2/L3…
- Avoid non-sequential access
- Design for parallelization
  - Do not let threads modify cache lines from the same shared memory locations
  - Lock-free solutions
- Utilize Vector<T> or anything that utilize SSE (SIMD) instructions
- Consider moving from AOS to SOA

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
