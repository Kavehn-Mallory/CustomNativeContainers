using BonnFireGames.CustomNativeContainers;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PerformanceTesting;

namespace Tests.Runtime
{
    public class NativeStreamPerformanceTests

    {


        public const int ExtraSmallBlockSize = 128;
        public const int SmallBlockSize = 1024;
        public const int MediumBlockSize = 2048;
        public const int LargeBlockSize = 4096;
        public const int QueueBlockSize = 1024 * 16;


        private static int ElementCountForSingularBlock<T>(int blockSize) where T : unmanaged
        {
            var headerSize = UnsafeEventStream<T>.GetBlockHeaderAlignmentSize();

            var remainingSize = blockSize - headerSize;

            return remainingSize / UnsafeUtility.SizeOf<T>();
        }

        private void SingleThreadPerformanceTest<T>(int blockSize, int blockCountToWriteTo,
            int blockCountToAllocate, int warmupCount = 5, int iterationsPerMeasurement = 1,
            int measurementCount = 10) where T : unmanaged
        {
            var elementCount = ElementCountForSingularBlock<T>(blockSize) * blockCountToWriteTo;

            elementCount -= 2;
                
            T testValue = new T();
            NativeEventStream<T> nativeStream = default;
            var nativeEventStreamSampleGroup =
                new SampleGroup($"NativeEventStream Write - {elementCount}", SampleUnit.Microsecond);
            var nativeQueueSampleGroup =
                new SampleGroup($"NativeQueue Write - {elementCount}", SampleUnit.Microsecond);

            Measure.Method(() =>
                {
                    for (int i = 0; i < elementCount; i++)
                    {
                        nativeStream.Enqueue(new T());
                    }
                }).SampleGroup(nativeEventStreamSampleGroup)
                .WarmupCount(warmupCount)
                .MeasurementCount(measurementCount)
                .IterationsPerMeasurement(iterationsPerMeasurement).SetUp(() =>
                {
                    nativeStream = new NativeEventStream<T>(Allocator.TempJob, blockSize, blockCountToAllocate);
                }).CleanUp(() =>
                {
                    nativeStream.AllocateReader().TryRead(out testValue);
                    
                    nativeStream.Dispose();
                        
                })
                .Run();
                

            Assert.AreEqual(new T(), testValue);
            NativeQueue<T> nativeQueue = default;

            

            Measure.Method(() =>
                {
                    for (int i = 0; i < elementCount; i++)
                    {
                        nativeQueue.Enqueue(new T());
                    }
                }).SampleGroup(nativeQueueSampleGroup)
                .WarmupCount(warmupCount)
                .MeasurementCount(measurementCount)
                .IterationsPerMeasurement(iterationsPerMeasurement)
                .SetUp(() =>
                {
                    nativeQueue = new NativeQueue<T>(Allocator.TempJob);
                        
                }).CleanUp(() =>
                {
                    testValue = nativeQueue.Dequeue();
                    nativeQueue.Dispose();
                        
                }).Run();
                Assert.AreEqual(new T(), testValue);


        }
        
            
        [Test, Performance]
        public void NativeEventStreamVSNativeQueueEnqueuingSingleThreadSingularBlockWithPrimitive()
        {
            SingleThreadPerformanceTest<int>(ExtraSmallBlockSize, 1, 2, 5, 100);
            SingleThreadPerformanceTest<int>(SmallBlockSize, 1, 2, 5, 100);
            SingleThreadPerformanceTest<int>(MediumBlockSize, 1, 2, 5, 100);
            SingleThreadPerformanceTest<int>(LargeBlockSize, 1, 2, 5, 100);
            SingleThreadPerformanceTest<int>(QueueBlockSize, 1, 2, 5, 100);
        }
            
        [Test, Performance]
        public void NativeEventStreamVSNativeQueueEnqueuingSingleThreadNoAdditionalBlockAlloc()
        {
            SingleThreadPerformanceTest<int>(ExtraSmallBlockSize, 5, 16, 5, 100);
            SingleThreadPerformanceTest<int>(SmallBlockSize, 5, 16, 5, 100);
            SingleThreadPerformanceTest<int>(MediumBlockSize, 5, 16, 5, 100);
            SingleThreadPerformanceTest<int>(LargeBlockSize, 5, 16, 5, 100);
            SingleThreadPerformanceTest<int>(QueueBlockSize, 5, 16, 5, 100);
        }
            
        [Test, Performance]
        public void NativeEventStreamVSNativeQueueEnqueuingSingleThreadBlockAlloc()
        {
            SingleThreadPerformanceTest<int>(ExtraSmallBlockSize, 5, 2, 5, 100);
            SingleThreadPerformanceTest<int>(SmallBlockSize, 5, 2, 5, 100);
            SingleThreadPerformanceTest<int>(MediumBlockSize, 5, 2, 5, 100);
            SingleThreadPerformanceTest<int>(LargeBlockSize, 5, 2, 5, 100);
            SingleThreadPerformanceTest<int>(QueueBlockSize, 5, 2, 5, 100);
        }
            
        [Test, Performance]
        public void NativeEventStreamVSNativeQueueEnqueuingMultiThreadSingularBlock()
        {
            var headerSize = UnsafeEventStream<int>.GetBlockHeaderAlignmentSize();
                
                
                
        }
            
        [Test, Performance]
        public void NativeEventStreamVSNativeQueueEnqueuingMultiThreadNoAdditionalBlockAlloc()
        {
                
        }
            
        [Test, Performance]
        public void NativeEventStreamVSNativeQueueEnqueuingMultiThreadBlockAlloc()
        {
                
        }
            
    }
}
