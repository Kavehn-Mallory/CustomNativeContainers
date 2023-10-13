using System;
using BonnFireGames.CustomNativeContainers;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class PriorityQueueTests
    {


        


        [NUnit.Framework.Test]
        public void PriorityQueue_EnqueueWrongOrder_DequeueRightOrder()
        {
            var queue = new NativePriorityQueue<int>(Allocator.Temp, 10);
            
            queue.Enqueue(100);
            queue.Enqueue(45);
            queue.Enqueue(23);
            queue.Enqueue(10);
            queue.Enqueue(2);


            var results = new NativeArray<int>(5, Allocator.Temp);
            for (var i = 0; i < results.Length; i++)
            {
                results[i] = queue.Dequeue();
            }

            var expectedResults = new NativeArray<int>(new[] { 2, 10, 23, 45, 100 }, Allocator.Temp);
            
            CollectionAssert.AreEquivalent(expectedResults, results);
        }

        [NUnit.Framework.Test]
        public void PriorityQueue_ReachMaxCapacity_IncreasesCapacity()
        {
            var queue = new NativePriorityQueue<int>(Allocator.TempJob, 2);
            queue.Enqueue(2);
            queue.Enqueue(3);
            queue.Enqueue(4);
            
            Assert.IsTrue(queue.Capacity == 4);
            Assert.IsTrue(queue.Count == 3);
            queue.Dispose();
        }
        
        [NUnit.Framework.Test]
        public void PriorityQueue_ReachMaxCapacity_ElementCopyIsSuccessful()
        {
            var queue = new NativePriorityQueue<int>(Allocator.Temp, 2);
            queue.Enqueue(2);
            queue.Enqueue(3);
            queue.Enqueue(4);
            
            Assert.IsTrue(queue.Peek() == 2);
        }

        [NUnit.Framework.Test]
        public void PriorityQueue_ReachMaxCapacity_CheckForNewElement()
        {
            var queue = new NativePriorityQueue<int>(Allocator.Temp, 2);
            queue.Enqueue(2);
            queue.Enqueue(3);
            queue.Enqueue(1);
            
            Assert.IsTrue(queue.Peek() == 1);
        }

        [NUnit.Framework.Test]
        public void PriorityQueue_CheckDequeue()
        {
            var queue = new NativePriorityQueue<int>(Allocator.Temp, 2);
            queue.Enqueue(4);
            queue.Enqueue(25);
            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(0);
            queue.Enqueue(8);
            queue.Enqueue(17);
            queue.Enqueue(6);
            queue.Enqueue(7);
            queue.Enqueue(9);
            
            Assert.IsTrue(queue.Count == 10);
            Assert.IsTrue(queue.Capacity == 16);
            Assert.IsTrue(queue.Dequeue() == 0);
            Assert.IsTrue(queue.Dequeue() == 1);
            Assert.IsTrue(queue.Dequeue() == 2); 
            Assert.IsTrue(queue.Dequeue() == 4);
            Assert.IsTrue(queue.Dequeue() == 6);
            Assert.IsTrue(queue.Dequeue() == 7);
            Assert.IsTrue(queue.Dequeue() == 8);
            Assert.IsTrue(queue.Dequeue() == 9);
            Assert.IsTrue(queue.Dequeue() == 17);
            Assert.IsTrue(queue.Dequeue() == 25);
        }

        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        
        [NUnit.Framework.Test]
        public void PriorityQueue_ReadFromQueueWhileBeingWrittenTo_ThrowsInvalidOperationException()
        {
            var queue = new NativePriorityQueue<int>(Allocator.Temp, 2);
            queue.Enqueue(4);
            queue.Enqueue(25);

            var writeJob = new PriorityWriteJob
            {
                WriteToQueue = queue
            };
            
            var handle = writeJob.Schedule();
            Assert.Throws<InvalidOperationException>(() =>
            {
                queue.Peek();
            });
            
            
            handle.Complete();
            queue.Dispose();
        }


        [Test]
        public void PriorityQueue_DeallocatedPriorityQueueOnJobCompletion_ShouldBeDeallocated()
        {
            var queue = new NativePriorityQueue<int>(Allocator.Temp, 2);
            queue.Enqueue(4);
            queue.Enqueue(25);

            var writeJob = new PriorityWriteJob
            {
                WriteToQueue = queue
            };
            
            var handle = writeJob.Schedule();

            queue.Dispose(handle);
            
            
            handle.Complete();
            
            Assert.IsFalse(queue.IsCreated);
        }
        
        
        #endif
        
        
        private struct PriorityWriteJob : IJob
        {
            
            public NativePriorityQueue<int> WriteToQueue;
           
            [BurstCompile]
            public void Execute()
            {
                WriteToQueue.Enqueue(7);
            }
        }
    }
}