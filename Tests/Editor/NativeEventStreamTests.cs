using System;
using System.Linq;
using BonnFireGames.CustomNativeContainers;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Tests.Editor
{
    public class NativeEventStreamTests
    {

        [Test]
        public void NativeEventStreamMainThreadWriteAndReadFromWriteBuffer()
        {
            var nativeStream = new NativeEventStream<int>(Allocator.TempJob, 128);
            
            
            nativeStream.Enqueue(2);
            nativeStream.Enqueue(5);
            

            var readHead = nativeStream.AllocateReader();
            Assert.IsTrue(readHead.TryRead(out var value));
            Assert.AreEqual(2, value);
            Assert.IsTrue(readHead.TryRead(out value));
            Assert.AreEqual(5, value);
            Assert.IsFalse(readHead.TryRead(out value));
            
            readHead.Dispose();
            nativeStream.Dispose();
        }
        

        
        [Test]
        public void NativeEventStreamMainThreadWriteAndReadFromReadBuffer()
        {
            var nativeStream = new NativeEventStream<int>(Allocator.TempJob, 128);
            
            
            nativeStream.Enqueue(2);
            nativeStream.Enqueue(5);
            
            nativeStream.Update();

            var readHead = nativeStream.AllocateReader();
            Assert.IsTrue(readHead.TryRead(out var value));
            Assert.AreEqual(2, value);
            Assert.IsTrue(readHead.TryRead(out value));
            Assert.AreEqual(5, value);
            Assert.IsFalse(readHead.TryRead(out _));
            
            readHead.Dispose();
            nativeStream.Dispose();
        }
        
        [Test]
        public void NativeEventStreamMainThreadWriteAndReadFromBothBuffers()
        {
            var nativeStream = new NativeEventStream<int>(Allocator.TempJob);
            
            
            nativeStream.Enqueue(2);
            nativeStream.Enqueue(5);
            
            nativeStream.Update();
            
            nativeStream.Enqueue(7);
            nativeStream.Enqueue(3);
            

            var readHead = nativeStream.AllocateReader();
            Assert.IsTrue(readHead.TryRead(out var value));
            Assert.AreEqual(2, value);
            Assert.IsTrue(readHead.TryRead(out value));
            Assert.AreEqual(5, value);
            Assert.IsTrue(readHead.TryRead(out value));
            Assert.AreEqual(7, value);
            Assert.IsTrue(readHead.TryRead(out value));
            Assert.AreEqual(3, value);
            Assert.IsFalse(readHead.TryRead(out _));
            
            readHead.Dispose();
            nativeStream.Dispose();
        }

        public class NativeEventStreamUpdateOrderTests
        {

            private int[] _data = new int[] { 6, 24, 7, 8 };
            private const int FrameCount = 3;


            private static void WriteData<T>(ref NativeEventStream<T> writeHead, T[] data)
                where T : unmanaged
            {
                for (int i = 0; i < data.Length; i++)
                {
                    writeHead.Enqueue(data[i]);
                }
            }
            
            private static void AssertRead<T>(ref NativeEventStream<T>.Reader readHead, T[] data)
                where T : unmanaged
            {
                for (int i = 0; i < data.Length; i++)
                {
                    Assert.IsTrue(readHead.TryRead(out var value));
                    Assert.AreEqual(data[i], value);
                }
                
                Assert.IsFalse(readHead.TryRead(out _));
            }
            
            [Test]
            public void UpdateProducerConsumerOrder()
            {
                var nativeStream = new NativeEventStream<int>(Allocator.TempJob);
                
                var readHead = nativeStream.AllocateReader();


                for (int i = 0; i < FrameCount; i++)
                {
                    nativeStream.Update();
                    WriteData(ref nativeStream, _data);
                    AssertRead(ref readHead, _data);
                }
            
                readHead.Dispose();
                nativeStream.Dispose();
            }
            
            [Test]
            public void UpdateConsumerProducerOrder()
            {
                var nativeStream = new NativeEventStream<int>(Allocator.TempJob);
                
                var readHead = nativeStream.AllocateReader();
                
                
                //do initial frame, there won't be any data 
                
                nativeStream.Update();
                Assert.IsFalse(readHead.TryRead(out var value));
                WriteData(ref nativeStream, _data);
                
                for (int i = 1; i < FrameCount; i++)
                {
                    nativeStream.Update();
                    AssertRead(ref readHead, _data);
                    WriteData(ref nativeStream, _data);
                }
                AssertRead(ref readHead, _data);
            
                readHead.Dispose();
                nativeStream.Dispose();
            }
            
            [Test]
            public void ProducerUpdateConsumerOrder()
            {
                var nativeStream = new NativeEventStream<int>(Allocator.TempJob);
                
                var readHead = nativeStream.AllocateReader();
                
                for (int i = 0; i < FrameCount; i++)
                {
                    WriteData(ref nativeStream, _data);
                    nativeStream.Update();
                    AssertRead(ref readHead, _data);
                }

            
                readHead.Dispose();
                nativeStream.Dispose();
            }
            
            
            [Test]
            public void ProducerConsumerUpdateOrder()
            {
                var nativeStream = new NativeEventStream<int>(Allocator.TempJob);
                
                var readHead = nativeStream.AllocateReader();

                for (int i = 0; i < FrameCount; i++)
                {
                    WriteData(ref nativeStream, _data);
                    AssertRead(ref readHead, _data);
                    nativeStream.Update();
                }
            
                readHead.Dispose();
                nativeStream.Dispose();
            }
            
            [Test]
            public void ConsumerUpdateProducerOrder()
            {
                var nativeStream = new NativeEventStream<int>(Allocator.TempJob);
                
                var readHead = nativeStream.AllocateReader();

                //first frame 
                Assert.IsFalse(readHead.TryRead(out var value));
                nativeStream.Update();
                WriteData(ref nativeStream, _data);
                
                for (int i = 1; i < FrameCount; i++)
                {
                    AssertRead(ref readHead, _data);
                    nativeStream.Update();
                    WriteData(ref nativeStream, _data);
                }
                AssertRead(ref readHead, _data);
            
                readHead.Dispose();
                nativeStream.Dispose();
            }
            
            [Test]
            public void ConsumerProducerUpdateOrder()
            {
                var nativeStream = new NativeEventStream<int>(Allocator.TempJob);
                
                var readHead = nativeStream.AllocateReader();

                //first frame 
                Assert.IsFalse(readHead.TryRead(out _));
                WriteData(ref nativeStream, _data);
                nativeStream.Update();
                
                for (int i = 1; i < FrameCount; i++)
                {
                    AssertRead(ref readHead, _data);
                    WriteData(ref nativeStream, _data);
                    nativeStream.Update();
                }
                AssertRead(ref readHead, _data);
            
                readHead.Dispose();
                nativeStream.Dispose();
            }
        }

        
        [Test]
        public void NativeEventStreamMainThreadWriteAndReadFromBothBuffersWithPause()
        {
            var nativeStream = new NativeEventStream<int>(Allocator.TempJob);
            
            
            nativeStream.Enqueue(2);
            nativeStream.Enqueue(5);
            
            var readHead = nativeStream.AllocateReader();
            Assert.IsTrue(readHead.TryRead(out var value));
            Assert.AreEqual(2, value);
            
            nativeStream.Update();
            
            nativeStream.Enqueue(7);
            nativeStream.Enqueue(3);
            


            Assert.IsTrue(readHead.TryRead(out value));
            Assert.AreEqual(5, value);
            Assert.IsTrue(readHead.TryRead(out value));
            Assert.AreEqual(7, value);
            Assert.IsTrue(readHead.TryRead(out  value));
            Assert.AreEqual(3, value);
            Assert.IsFalse(readHead.TryRead(out value));
            
            readHead.Dispose();
            nativeStream.Dispose();
            
        }
        
        [Test]
        public void NativeEventStreamWriteInJobReadOnMainThread()
        {
            var nativeStream = new NativeEventStream<int>(Allocator.TempJob, 64, 16);
            

            var elementCount = 512;
            var data = new NativeArray<int>(elementCount, Allocator.TempJob);
            var threadIndexes = new NativeArray<int>(elementCount, Allocator.TempJob);

            for (int i = 0; i < elementCount; i++)
            {
                data[i] = i;
            }

            var job = new NativeEventStreamWriteJobWithThreadIndex()
            {
                Values = data,
                WriteHead = nativeStream.AsParallelWriter(),
                ThreadIds = threadIndexes
            };

            var handle = job.Schedule(elementCount, 2);
            
            handle.Complete();
            
            Assert.IsTrue(threadIndexes.Count(threadIndex => threadIndex != 0) != 0);



            var readHead = nativeStream.AllocateReader();

            var result = new NativeArray<int>(elementCount, Allocator.Temp);

            for (int i = 0; i < elementCount; i++)
            {
                Assert.IsTrue(readHead.TryRead(out var value));
                result[i] = value;
            }
            CollectionAssert.AreEquivalent(data, result);
            Assert.IsFalse(readHead.TryRead(out _));
            
            
            readHead.Dispose();
            nativeStream.Dispose();
            data.Dispose();
            threadIndexes.Dispose();


        }
        
        [Test]
        public void NativeEventStreamWriteAndReadInJob()
        {
            var nativeStream = new NativeEventStream<int>(Allocator.TempJob);
            

            var elementCount = 10;
            var data = new NativeArray<int>(elementCount, Allocator.TempJob);

            for (int i = 0; i < elementCount; i++)
            {
                data[i] = i;
            }

            var writeJob = new NativeEventStreamWriteJob()
            {
                Values = data,
                WriteHead = nativeStream.AsParallelWriter()
            };
            

            var readHead = nativeStream.AllocateReader();
            
            

            var result = new NativeList<int>(elementCount, Allocator.TempJob);

            var readJob = new NativeEventStreamReadJob
            {
                ReadHead = readHead,
                Values = result
            };

            var handle = readJob.Schedule(writeJob.Schedule(elementCount, 2));
            handle.Complete();
            var resultArray = result.ToArray(Allocator.Temp);
            CollectionAssert.AreEquivalent(data, resultArray);
            CollectionAssert.AllItemsAreUnique(resultArray);
            Assert.IsFalse(readHead.TryRead(out _));
            
            Assert.AreEqual(elementCount, resultArray.Length);
            
            readHead.Dispose();
            nativeStream.Update();
            Assert.IsTrue(nativeStream.ReaderCount == 0);
            
            nativeStream.Dispose();
            data.Dispose();
            result.Dispose();
            

        }
        
        [Test]
        public void NativeEventStreamWriteAndReadInSameJob()
        {
            var nativeStream = new NativeEventStream<int>(Allocator.TempJob);

            
            var elementCount = 10;
            var data = new NativeArray<int>(elementCount, Allocator.TempJob);

            for (int i = 0; i < elementCount; i++)
            {
                data[i] = i;
            }
            
            
#if UNITY_2022_2_14F1_OR_NEWER
            int maxThreadCount = JobsUtility.ThreadIndexCount;
#else
            int maxThreadCount = JobsUtility.MaxJobThreadCount;
#endif
            
            var writeCount = new NativeArray<int>(maxThreadCount, Allocator.TempJob);

            var actualWrites = new NativeArray<int>(maxThreadCount, Allocator.TempJob);

            for (int i = 0; i < writeCount.Length; i++)
            {
                var mod = i % 3;

                switch (mod)
                {
                    case 0:
                        writeCount[i] = 1;
                        break;
                    case 1:
                        writeCount[i] = 10;
                        break;
                    default:
                        writeCount[i] = 1000;
                        break;
                }
            }
            
            var result = new NativeArray<int>(maxThreadCount, Allocator.TempJob);

            var readHead = nativeStream.AllocateReader();
            
            var writeJob = new NativeEventStreamWriteAndReadJob()
            {
                Input = data,
                WriteHead = nativeStream.AsParallelWriter(),
                ReadHead = readHead,
                Output = result,
                WriteCount = writeCount,
                ActualWrites = actualWrites
            };
            
            
            
            var handle = writeJob.Schedule(elementCount, 2);
            handle.Complete();

            var readElementCount = result.Sum();

            var writeCountSum = actualWrites.Sum();
            
            Assert.IsFalse(readHead.TryRead(out _));

            Assert.AreEqual(writeCountSum, readElementCount);
            
            readHead.Dispose();
            nativeStream.Update();
            Assert.IsTrue(nativeStream.ReaderCount == 0);
            
            nativeStream.Dispose();
            data.Dispose();
            result.Dispose();
            writeCount.Dispose();
            actualWrites.Dispose();


        }
        
        private struct NativeEventStreamWriteAndReadJob : IJobParallelFor
        {

            public NativeEventStream<int>.ParallelWriter WriteHead;
            [ReadOnly]
            public NativeArray<int> Input;

            public NativeArray<int> WriteCount;
            
            public NativeEventStream<int>.Reader ReadHead;
            [WriteOnly]
            public NativeArray<int> Output;

            public NativeArray<int> ActualWrites;


            public void Execute(int index)
            {
                
                var writeCount = WriteCount[index];
                ActualWrites[index] = writeCount;
                for (int i = 0; i < writeCount; i++)
                {
                    WriteHead.Enqueue(Input[index]);
                }
                var counter = 0;
                while (ReadHead.TryRead(out _))
                {
                    counter++;
                }

                Output[index] = counter;
            }
        }
        
        private struct NativeEventStreamWriteJob : IJobParallelFor
        {

            public NativeEventStream<int>.ParallelWriter WriteHead;
            [ReadOnly]
            public NativeArray<int> Values;
            
            
            
            public void Execute(int index)
            {
                WriteHead.Enqueue(Values[index]);
            }
        }
        
        private struct NativeEventStreamWriteJobWithThreadIndex : IJobParallelFor
        {

            public NativeEventStream<int>.ParallelWriter WriteHead;
            [ReadOnly]
            public NativeArray<int> Values;

            [WriteOnly]
            public NativeArray<int> ThreadIds;
            
            
            public void Execute(int index)
            {
                WriteHead.Enqueue(Values[index], out var threadIndex);
                ThreadIds[index] = threadIndex;
            }
        }

        private struct NativeEventStreamReadJob : IJob
        {
            public NativeEventStream<int>.Reader ReadHead;
            [WriteOnly]
            public NativeList<int> Values;
            
            public void Execute()
            {
                while (ReadHead.TryRead(out var value))
                {
                    Values.Add(value);
                }
            }
        }
        
    }
}