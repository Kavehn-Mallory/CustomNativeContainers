using BonnFireGames.CustomNativeContainers;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Tests.Runtime.PerformanceTesting.Benchmark.Benchmarks
{
    public static class EventStreamPerformanceTests
    {
        
        public static class EventStreamUtil
        {
            public static void AllocInt(ref NativeEventStream<int> container, int capacity, bool addValues)
            {
                if (capacity >= 0)
                {
                    Random.InitState(0);
                    container = new NativeEventStream<int>(Allocator.Persistent, 1024 * 16, 0);
                    if (addValues)
                    {
                        for (int i = 0; i < capacity; i++)
                            container.Enqueue(i);
                    }
                }
                else
                    container.Dispose();
            }
            public static void AllocInt(ref UnsafeEventStream<int> container, int capacity, bool addValues)
            {
                if (capacity >= 0)
                {
                    Random.InitState(0);
                    container = new UnsafeEventStream<int>(Allocator.Persistent, 1024 * 16, 0, true, true);
                    if (addValues)
                    {
                        for (int i = 0; i < capacity; i++)
                            container.Enqueue(i);
                    }
                }
                else
                    container.Dispose();
            }
            public static object AllocBclContainer(int capacity, bool addValues)
            {
                if (capacity < 0)
                    return null;

                Random.InitState(0);
                var bclContainer = new System.Collections.Generic.Queue<int>();
                if (addValues)
                {
                    for (int i = 0; i < capacity; i++)
                        bclContainer.Enqueue(i);
                }
                return bclContainer;
            }

            public static void CreateRandomValues(int capacity, ref UnsafeList<int> values)
            {
                if (capacity >= 0)
                {
                    values = new UnsafeList<int>(capacity, Allocator.Persistent);
                    Random.InitState(0);
                    for (int i = 0; i < capacity; i++)
                    {
                        int randKey = Random.Range(0, capacity);
                        values.Add(randKey);
                    }
                }
                else
                    values.Dispose();
            }
        }

        private struct EventStreamEnqueueGrow : IBenchmarkContainer
        {
            private int _capacity;
            private int _workers;
            private NativeEventStream<int> _nativeContainer;
            private UnsafeEventStream<int> _unsafeContainer;

            void IBenchmarkContainer.SetParams(int capacity, params int[] args) => this._capacity = capacity;

            public void AllocNativeContainer(int capacity) => EventStreamUtil.AllocInt(ref _nativeContainer, capacity >= 0 ? 0 : -1, false);
            public void AllocUnsafeContainer(int capacity) => EventStreamUtil.AllocInt(ref _unsafeContainer, capacity >= 0 ? 0 : -1, false);
            public object AllocBclContainer(int capacity) => EventStreamUtil.AllocBclContainer(capacity >= 0 ? 0 : -1, false);

            public void MeasureNativeContainer()
            {
                for (int i = 0; i < _capacity; i++)
                    _nativeContainer.Enqueue(i);
            }
            public void MeasureUnsafeContainer()
            {
                for (int i = 0; i < _capacity; i++)
                    _unsafeContainer.Enqueue(i);
            }
            public void MeasureBclContainer(object container)
            {
                var bclContainer = (System.Collections.Generic.Queue<int>)container;
                for (int i = 0; i < _capacity; i++)
                    bclContainer.Enqueue(i);
            }
        }

        private struct EventStreamEnqueue : IBenchmarkContainer
        {
            private int _capacity;
            private int _workers;
            private NativeEventStream<int> _nativeContainer;
            private UnsafeEventStream<int> _unsafeContainer;

            void IBenchmarkContainer.SetParams(int capacity, params int[] args) => this._capacity = capacity;

            public void AllocNativeContainer(int capacity) => EventStreamUtil.AllocInt(ref _nativeContainer, capacity, false);
            public void AllocUnsafeContainer(int capacity) => EventStreamUtil.AllocInt(ref _unsafeContainer, capacity, false);
            public object AllocBclContainer(int capacity) => EventStreamUtil.AllocBclContainer(capacity, false);

            public void MeasureNativeContainer()
            {
                for (int i = 0; i < _capacity; i++)
                    _nativeContainer.Enqueue(i);
            }
            public void MeasureUnsafeContainer()
            {
                for (int i = 0; i < _capacity; i++)
                    _unsafeContainer.Enqueue(i);
            }
            public void MeasureBclContainer(object container)
            {
                var bclContainer = (System.Collections.Generic.Queue<int>)container;
                for (int i = 0; i < _capacity; i++)
                    bclContainer.Enqueue(i);
            }
        }
        
        [Benchmark(typeof(BenchmarkContainerType))]
        private class EventStream
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem(BenchmarkContainerConfig.kMenuItemIndividual + nameof(EventStream))]
        private static void RunIndividual()
            => BenchmarkContainerConfig.RunBenchmark(typeof(EventStream));
#endif

        /*[Test, Performance]
        [Category("Performance")]
        public unsafe void IsEmpty_x_100k(
            [Values(0, 100)] int capacity,
            [Values] BenchmarkContainerType type)
        {
            BenchmarkContainerRunner<QueueIsEmpty100k>.Run(capacity, type);
        }

        [Test, Performance]
        [Category("Performance")]
        public unsafe void Count_x_100k(
            [Values(0, 100)] int capacity,
            [Values] BenchmarkContainerType type)
        {
            BenchmarkContainerRunner<QueueCount100k>.Run(capacity, type);
        }

        [Test, Performance]
        [Category("Performance")]
        public unsafe void ToNativeArray(
            [Values(10000, 100000, 1000000)] int capacity,
            [Values] BenchmarkContainerType type)
        {
            BenchmarkContainerRunner<QueueToNativeArray>.Run(capacity, type);
        }*/

        [Test, Performance]
        [Category("Performance")]
        [BenchmarkTestFootnote]
        public unsafe void EnqueueGrow(
            [Values(10000, 100000, 1000000)] int insertions,
            [Values] BenchmarkContainerType type)
        {
            BenchmarkContainerRunner<EventStreamEnqueueGrow>.Run(insertions, type);
        }

        [Test, Performance]
        [Category("Performance")]
        [BenchmarkTestFootnote]
        public unsafe void Enqueue(
            [Values(10000, 100000, 1000000)] int insertions,
            [Values] BenchmarkContainerType type)
        {
            BenchmarkContainerRunner<EventStreamEnqueue>.Run(insertions, type);
        }

        /*[Test, Performance]
        [Category("Performance")]
        public unsafe void Dequeue(
            [Values(10000, 100000, 1000000)] int insertions,
            [Values] BenchmarkContainerType type)
        {
            BenchmarkContainerRunner<QueueDequeue>.Run(insertions, type);
        }

        [Test, Performance]
        [Category("Performance")]
        public unsafe void Peek(
            [Values(10000, 100000, 1000000)] int insertions,
            [Values] BenchmarkContainerType type)
        {
            BenchmarkContainerRunner<QueuePeek>.Run(insertions, type);
        }

        [Test, Performance]
        [Category("Performance")]
        public unsafe void Foreach(
            [Values(10000, 100000, 1000000)] int insertions,
            [Values] BenchmarkContainerType type)
        {
            BenchmarkContainerRunner<QueueForEach>.Run(insertions, type);
        }*/
    }
    }
}