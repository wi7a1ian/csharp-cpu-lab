# csharp-cpu-lab (IN PROGRESS)

### Branch Prediction - why sorted array can be faster?

[SO: Why is it faster to process a sorted array than an unsorted array?](https://stackoverflow.com/questions/11227809/why-is-it-faster-to-process-a-sorted-array-than-an-unsorted-array?rq=1)

```
int sum = 0;
foreach (var i in array)
	if (i >= 128)
		sum += i;
return sum;
```

```
        Method |        Mean |    StdDev |      Median |
-------------- |------------ |---------- |------------ |
   SortedArray |  22.7367 us | 0.6702 us |  22.3573 us |
 UnsortedArray | 149.3315 us | 0.6229 us | 149.4772 us |
```

### ILP vs Parallel vs SIMD
- ILP option doesn't help because compiler has to do boundary checks for us.
- Parallel is not fastest because of data sharing (cache invalidation).
- Considering *single instruction, multiple data (SIMD)* does bring best performance boost when working with arrays. This can be achieved via `System.Numerics.Vectorization.Vector<T>` class in C#.

```
         Method |          Mean |     StdErr |     StdDev |        Median |
--------------- |-------------- |----------- |----------- |-------------- |
    MinMaxNaive |   603.0989 us |  5.2490 us | 20.3293 us |   597.3966 us |
      MinMaxILP | 1,025.9282 us | 10.4831 us | 54.4718 us | 1,024.1082 us |
     MinMaxSimd |   160.7848 us |  1.5912 us |  9.2784 us |   158.5755 us |
 MinMaxParallel |   416.3257 us |  2.1792 us |  8.4402 us |   416.8833 us |
```

### Cache miss
- When iterating over arrays, consider CPU cache sizes (L1=64kb, L2=2MB, L3=8MB) and process only X (= L1 size) bytes at a time for best performance gain. 

```
      Method |      Mean |    StdErr |    StdDev |    Median |
------------ |---------- |---------- |---------- |---------- |
 RotateNaive | 8.7432 ms | 0.0400 ms | 0.1385 ms | 8.7250 ms |
 RotateTiled | 2.6486 ms | 0.0265 ms | 0.1092 ms | 2.6250 ms |
```

[Numbers everyone should know](https://surana.wordpress.com/2009/01/01/numbers-everyone-should-know/)

![](https://i.stack.imgur.com/a7jWu.png)

### Cache invalidation - MESI protocol
**There are only two hard things in computer science: cache invalidation and naming things.**

When working with threads where large object is schared among them i.e: array), when one thread modifies block of cached data, all other threads that work with the same copy have to re-read the data from the memory, aka invalidate. This is speed up woth MESI protocol that requires cache to cache transfer on a miss if the block resides in another cache.

When such unintentional cache sharing happens, parallel method should use local variable to modify value, and then update shared memory when done, or let them modify/access only memory regions that are CL1 size (= 64kb) bytes away from  each other.

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

### AoS vs SoA
This is strongly connected to **Data Oriented Programming**. When working with arrays of structs, consider switching to struct of arrays instead, that can benefit from vectorization (SIMD instructions) and sequential data access. 
