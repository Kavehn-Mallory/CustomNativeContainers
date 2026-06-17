using NUnit.Framework;
using Unity.Collections;
using Unity.PerformanceTesting;
using UnityEngine;

namespace Tests.Runtime.PerformanceTesting.Benchmark.Benchmarks
{
    internal static class QueueParallelUtil
    {
        static public void AllocInt(ref NativeQueue<int> container, int capacity, bool addValues)
            => QueueUtil.AllocInt(ref container, capacity, addValues);

        static public object AllocBclContainer(int capacity, bool addValues)
        {
            if (capacity < 0)
                return null;

            Random.InitState(0);
            var bclContainer = new System.Collections.Concurrent.ConcurrentQueue<int>();
            if (addValues)
            {
                for (int i = 0; i < capacity; i++)
                    bclContainer.Enqueue(i);
            }
            return bclContainer;
        }
    }

    internal struct QueueParallelEnqueueGrow : IBenchmarkContainerParallel
    {
        private int _capacity;
        private int _workers;
        private NativeQueue<int> _nativeContainer;

        void IBenchmarkContainerParallel.SetParams(int capacity, params int[] args)
        {
            this._capacity = capacity;
            _workers = args[0];
        }

        public void AllocNativeContainer(int capacity) => QueueParallelUtil.AllocInt(ref _nativeContainer, capacity >= 0 ? 0 : -1, false);
        public void AllocUnsafeContainer(int capacity) { }
        public object AllocBclContainer(int capacity) => QueueParallelUtil.AllocBclContainer(0, false);

        public void MeasureNativeContainer(int worker, int threadId)
        {
            var writer = _nativeContainer.AsParallelWriter();
            ParallelHashMapUtil.SplitForWorkers(_capacity, worker, _workers, out int start, out int end);
            for (int i = start; i < end; i++)
                writer.Enqueue(i, threadId);
        }
        public void MeasureUnsafeContainer(int worker, int threadId) { }
        public void MeasureBclContainer(object container, int worker)
        {
            var bclContainer = (System.Collections.Concurrent.ConcurrentQueue<int>)container;
            ParallelHashMapUtil.SplitForWorkers(_capacity, worker, _workers, out int start, out int end);
            for (int i = start; i < end; i++)
                bclContainer.Enqueue(i);
        }
    }

    internal struct QueueParallelEnqueue : IBenchmarkContainerParallel
    {
        private int _capacity;
        private int _workers;
        private NativeQueue<int> _nativeContainer;

        void IBenchmarkContainerParallel.SetParams(int capacity, params int[] args)
        {
            this._capacity = capacity;
            _workers = args[0];
        }

        public void AllocNativeContainer(int capacity) => QueueParallelUtil.AllocInt(ref _nativeContainer, capacity, false);
        public void AllocUnsafeContainer(int capacity) { }
        public object AllocBclContainer(int capacity) => QueueParallelUtil.AllocBclContainer(capacity, false);

        public void MeasureNativeContainer(int worker, int threadId)
        {
            var writer = _nativeContainer.AsParallelWriter();
            ParallelHashMapUtil.SplitForWorkers(_capacity, worker, _workers, out int start, out int end);
            for (int i = start; i < end; i++)
                writer.Enqueue(i, threadId);
        }
        public void MeasureUnsafeContainer(int worker, int threadId) { }
        public void MeasureBclContainer(object container, int worker)
        {
            var bclContainer = (System.Collections.Concurrent.ConcurrentQueue<int>)container;
            ParallelHashMapUtil.SplitForWorkers(_capacity, worker, _workers, out int start, out int end);
            for (int i = start; i < end; i++)
                bclContainer.Enqueue(i);
        }
    }



    [Benchmark(typeof(BenchmarkContainerType))]
    [BenchmarkNameOverride(BenchmarkContainerConfig.BCL, "ConcurrentQueue")]
    internal class QueueParallelWriter
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem(BenchmarkContainerConfig.kMenuItemIndividual + "Queue.ParallelWriter")]
        private static void RunIndividual()
            => BenchmarkContainerConfig.RunBenchmark(typeof(QueueParallelWriter));
#endif

        [Test, Performance]
        [Category("Performance")]
        [BenchmarkTestFootnote]
        public unsafe void EnqueueGrow(
            [Values(1, 2, 4)] int workers,
            [Values(10000, 100000, 1000000)] int insertions,
            [Values(BenchmarkContainerType.Native, BenchmarkContainerType.NativeBurstSafety,
                BenchmarkContainerType.NativeBurstNoSafety)] BenchmarkContainerType type)
        {
            BenchmarkContainerRunnerParallel<QueueParallelEnqueueGrow>.Run(workers, insertions, type, workers);
        }

        [Test, Performance]
        [Category("Performance")]
        [BenchmarkTestFootnote]
        public unsafe void Enqueue(
            [Values(1, 2, 4)] int workers,
            [Values(10000, 100000, 1000000)] int insertions,
            [Values(BenchmarkContainerType.Native, BenchmarkContainerType.NativeBurstSafety,
                BenchmarkContainerType.NativeBurstNoSafety)] BenchmarkContainerType type)
        {
            BenchmarkContainerRunnerParallel<QueueParallelEnqueue>.Run(workers, insertions, type, workers);
        }
    }
}
