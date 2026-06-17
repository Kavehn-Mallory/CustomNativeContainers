using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Tests.Runtime.PerformanceTesting.Benchmark
{
    
    static class HashMapUtil
    {
        internal const uint K_RANDOM_SEED_1 = 2210602657;
        internal const uint K_RANDOM_SEED_2 = 2210602658;
        internal const uint K_RANDOM_SEED_3 = 2210602659;
        static public void AllocInt(ref NativeHashMap<int, int> container, int capacity, bool addValues)
        {
            if (capacity >= 0)
            {
                Unity.Mathematics.Random random = new Random(K_RANDOM_SEED_1);
                container = new NativeHashMap<int, int>(capacity, Allocator.Persistent);
                if (addValues)
                {
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (container.TryAdd(randKey, keysAdded))
                        {
                            ++keysAdded;
                        }
                    }
                }
            }
            else
                container.Dispose();
        }
        static public void AllocInt(ref UnsafeHashMap<int, int> container, int capacity, bool addValues)
        {
            if (capacity >= 0)
            {
                Unity.Mathematics.Random random = new Random(K_RANDOM_SEED_1);
                container = new UnsafeHashMap<int, int>(capacity, Allocator.Persistent);
                if (addValues)
                {
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (container.TryAdd(randKey, keysAdded))
                        {
                            ++keysAdded;
                        }
                    }
                }
            }
            else
                container.Dispose();
        }
        static public object AllocBclContainer(int capacity, bool addValues)
        {
            if (capacity < 0)
                return null;

            Unity.Mathematics.Random random = new Random(K_RANDOM_SEED_1);
            var bclContainer = new System.Collections.Generic.Dictionary<int, int>(capacity);
            if (addValues)
            {
                int keysAdded = 0;

                while (keysAdded < capacity)
                {
                    int randKey = random.NextInt();
                    if (bclContainer.TryAdd(randKey, keysAdded))
                    {
                        ++keysAdded;
                    }
                }
            }
            return bclContainer;
        }
        static public void CreateRandomKeys(int capacity, ref UnsafeList<int> keys)
        {
            if (capacity >= 0)
            {
                keys = new UnsafeList<int>(capacity, Allocator.Persistent);
                using (UnsafeHashSet<int> randomFilter = new UnsafeHashSet<int>(capacity, Allocator.Persistent))
                {
                    Unity.Mathematics.Random random = new Random(K_RANDOM_SEED_2);
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (randomFilter.Add(randKey))
                        {
                            keys.Add(randKey);
                            ++keysAdded;
                        }
                    }
                }

            }
            else
                keys.Dispose();
        }

        static public void CreateRandomKeys(int capacity, ref UnsafeList<int> keys, ref UnsafeHashMap<int, int> hashMap)
        {
            if (capacity >= 0)
            {
                keys = new UnsafeList<int>(capacity, Allocator.Persistent);
                using (UnsafeHashSet<int> randomFilter = new UnsafeHashSet<int>(capacity, Allocator.Persistent))
                {
                    Unity.Mathematics.Random random = new Random(K_RANDOM_SEED_2);
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (randomFilter.Add(randKey) && !hashMap.ContainsKey(randKey))
                        {
                            keys.Add(randKey);
                            ++keysAdded;
                        }
                    }
                }

            }
            else
                keys.Dispose();
        }

        static public void CreateRandomKeys(int capacity, ref UnsafeList<int> keys, ref System.Collections.Generic.Dictionary<int, int> hashMap)
        {
            if (capacity >= 0)
            {
                keys = new UnsafeList<int>(capacity, Allocator.Persistent);
                using (UnsafeHashSet<int> randomFilter = new UnsafeHashSet<int>(capacity, Allocator.Persistent))
                {
                    Unity.Mathematics.Random random = new Random(K_RANDOM_SEED_2);
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (randomFilter.Add(randKey) && !hashMap.ContainsKey(randKey))
                        {
                            keys.Add(randKey);
                            ++keysAdded;
                        }
                    }
                }

            }
            else
                keys.Dispose();
        }

        static public void CreateRandomKeys(int capacity, ref UnsafeList<int> keys, ref NativeHashMap<int, int> hashMap)
        {
            if (capacity >= 0)
            {
                keys = new UnsafeList<int>(capacity, Allocator.Persistent);
                using (UnsafeHashSet<int> randomFilter = new UnsafeHashSet<int>(capacity, Allocator.Persistent))
                {
                    Unity.Mathematics.Random random = new Random(K_RANDOM_SEED_2);
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (randomFilter.Add(randKey) && !hashMap.ContainsKey(randKey))
                        {
                            keys.Add(randKey);
                            ++keysAdded;
                        }
                    }
                }

            }
            else
                keys.Dispose();
        }

        static public void RandomlyShuffleKeys(int capacity, ref UnsafeList<int> keys)
        {
            if (capacity >= 0)
            {
                Unity.Mathematics.Random random = new Random(K_RANDOM_SEED_3);
                for (int i = 0; i < capacity; i++)
                {
                    int keyAt = keys[i];
                    int randomIndex = random.NextInt(0, capacity - 1);
                    keys[i] = keys[randomIndex];
                    keys[randomIndex] = keyAt;
                }
            }
        }
    }
    static class ParallelHashMapUtil
    {
        static public void AllocInt(ref NativeParallelHashMap<int, int> container, int capacity, bool addValues)
        {
            if (capacity >= 0)
            {
                Unity.Mathematics.Random random = new Unity.Mathematics.Random(HashMapUtil.K_RANDOM_SEED_1);
                container = new NativeParallelHashMap<int, int>(capacity, Allocator.Persistent);
                if (addValues)
                {
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (container.TryAdd(randKey, keysAdded))
                        {
                            ++keysAdded;
                        }
                    }
                }
            }
            else
                container.Dispose();
        }
        static public void AllocInt(ref UnsafeParallelHashMap<int, int> container, int capacity, bool addValues)
        {
            if (capacity >= 0)
            {
                Unity.Mathematics.Random random = new Unity.Mathematics.Random(HashMapUtil.K_RANDOM_SEED_1);
                container = new UnsafeParallelHashMap<int, int>(capacity, Allocator.Persistent);
                if (addValues)
                {
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (container.TryAdd(randKey, keysAdded))
                        {
                            ++keysAdded;
                        }
                    }
                }
            }
            else
                container.Dispose();
        }
        static public object AllocBclContainer(int capacity, bool addValues)
        {
            if (capacity < 0)
                return null;

            Unity.Mathematics.Random random = new Unity.Mathematics.Random(HashMapUtil.K_RANDOM_SEED_1);

            // FROM MICROSOFT DOCUMENTATION
            // The higher the concurrencyLevel, the higher the theoretical number of operations
            // that could be performed concurrently on the ConcurrentDictionary.  However, global
            // operations like resizing the dictionary take longer as the concurrencyLevel rises.
            // For the purposes of this example, we'll compromise at numCores * 2.
            var bclContainer = new System.Collections.Concurrent.ConcurrentDictionary<int, int>(System.Environment.ProcessorCount * 2, capacity);

            if (addValues)
            {
                int keysAdded = 0;

                while (keysAdded < capacity)
                {
                    int randKey = random.NextInt();
                    if (bclContainer.TryAdd(randKey, keysAdded))
                    {
                        ++keysAdded;
                    }
                }
            }
            return bclContainer;
        }
        static public void CreateRandomKeys(int capacity, ref UnsafeList<int> keys)
        {
            if (capacity >= 0)
            {
                keys = new UnsafeList<int>(capacity, Allocator.Persistent);
                using (UnsafeHashSet<int> randomFilter = new UnsafeHashSet<int>(capacity, Allocator.Persistent))
                {
                    Unity.Mathematics.Random random = new Unity.Mathematics.Random(HashMapUtil.K_RANDOM_SEED_2);
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (randomFilter.Add(randKey))
                        {
                            keys.Add(randKey);
                            ++keysAdded;
                        }
                    }
                }
            }
            else
                keys.Dispose();
        }

        static public void CreateRandomKeys(int capacity, ref UnsafeList<int> keys, ref UnsafeParallelHashMap<int, int> hashMap)
        {
            if (capacity >= 0)
            {
                keys = new UnsafeList<int>(capacity, Allocator.Persistent);
                using (UnsafeHashSet<int> randomFilter = new UnsafeHashSet<int>(capacity, Allocator.Persistent))
                {
                    Unity.Mathematics.Random random = new Unity.Mathematics.Random(HashMapUtil.K_RANDOM_SEED_2);
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (randomFilter.Add(randKey))
                        {
                            keys.Add(randKey);
                            ++keysAdded;
                        }
                    }
                }

            }
            else
                keys.Dispose();
        }

        static public void CreateRandomKeys(int capacity, ref UnsafeList<int> keys, ref System.Collections.Concurrent.ConcurrentDictionary<int, int> hashMap)
        {
            if (capacity >= 0)
            {
                keys = new UnsafeList<int>(capacity, Allocator.Persistent);
                using (UnsafeHashSet<int> randomFilter = new UnsafeHashSet<int>(capacity, Allocator.Persistent))
                {
                    Unity.Mathematics.Random random = new Unity.Mathematics.Random(HashMapUtil.K_RANDOM_SEED_2);
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (randomFilter.Add(randKey))
                        {
                            keys.Add(randKey);
                            ++keysAdded;
                        }
                    }
                }

            }
            else
                keys.Dispose();
        }

        static public void CreateRandomKeys(int capacity, ref UnsafeList<int> keys, ref NativeParallelHashMap<int, int> hashMap)
        {
            if (capacity >= 0)
            {
                keys = new UnsafeList<int>(capacity, Allocator.Persistent);
                using (UnsafeHashSet<int> randomFilter = new UnsafeHashSet<int>(capacity, Allocator.Persistent))
                {
                    Unity.Mathematics.Random random = new Unity.Mathematics.Random(HashMapUtil.K_RANDOM_SEED_2);
                    int keysAdded = 0;

                    while (keysAdded < capacity)
                    {
                        int randKey = random.NextInt();
                        if (randomFilter.Add(randKey))
                        {
                            keys.Add(randKey);
                            ++keysAdded;
                        }
                    }
                }

            }
            else
                keys.Dispose();
        }

        static public void RandomlyShuffleKeys(int capacity, ref UnsafeList<int> keys)
        {
            if (capacity >= 0)
            {
                Unity.Mathematics.Random random = new Random(HashMapUtil.K_RANDOM_SEED_3);
                for (int i = 0; i < capacity; i++)
                {
                    int keyAt = keys[i];
                    int randomIndex = random.NextInt(0, capacity - 1);
                    keys[i] = keys[randomIndex];
                    keys[randomIndex] = keyAt;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void SplitForWorkers(int count, int worker, int workers, out int startInclusive, out int endExclusive)
        {
            startInclusive = count * worker / workers;
            endExclusive = count * (worker + 1) / workers;
        }
    }
}