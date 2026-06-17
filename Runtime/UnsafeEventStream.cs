using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace BonnFireGames.CustomNativeContainers
{
    
    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile]
    public unsafe struct UnsafeEventStream<T> : INativeDisposable where T : unmanaged
    {

     
        [NativeDisableUnsafePtrRestriction]
        private UnsafeEventSequence* _buffer;

        private UnsafePtrList<Reader> _readers;
        private AllocatorManager.AllocatorHandle _allocatorHandle;
        
        public UnsafeEventStream(AllocatorManager.AllocatorHandle handle, int blockSize, int initialBlockCount, bool dynamicAllocations, bool dynamicResize)
        {
            _allocatorHandle = handle;
            _readers = new UnsafePtrList<Reader>(10, _allocatorHandle);
            UnsafeEventSequence.AllocateDoubleBufferEventSequence<T>(_allocatorHandle, out _buffer, blockSize, initialBlockCount, dynamicAllocations, dynamicResize);
        }

        [BurstCompile]
        public long Enqueue(T value)
        {
            UnsafeEventSequenceBlockHeader*
                block = UnsafeEventSequence.AllocateWriteBlock(_buffer, _allocatorHandle, 0, out var messageId);
            
            UnsafeUtility.WriteArrayElement(block + 1, block->ItemCount, value);
            block->ItemCount++;
            
            return messageId;
        }
        
        internal static UnsafeEventStream<T>* Allocate(AllocatorManager.AllocatorHandle allocator, int blockSize ,
            int initialBlockCount = 16, bool dynamicAllocations = true, bool dynamicResize = true)
        {
            UnsafeEventStream<T>* data = AllocatorManager.Allocate<UnsafeEventStream<T>>(allocator);
            *data = new UnsafeEventStream<T>(allocator, blockSize, initialBlockCount, dynamicAllocations,
                dynamicResize);
            return data;
        }


        internal static void Free(UnsafeEventStream<T>* data)
        {
            if (data == null)
            {
                throw new InvalidOperationException("UnsafeEventStream has yet to be created or has been destroyed!");
            }
            
            var allocator = data->_allocatorHandle;
            data->Dispose();
            AllocatorManager.Free(allocator, data);
            
        }
        
        public int ReaderCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _readers.Length;
        }


        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _buffer != null;
        }


        public void Update()
        {
            _buffer->SwapHeaders();
            _buffer->ResetWriteBlock();
            
            
            for (int i = _readers.Length - 1; i >= 0; i--)
            {
                var readHead = _readers[i];
                if (readHead == null || !readHead->IsCreated)
                {
                    _readers.RemoveAtSwapBack(i);
                    continue;
                }
                readHead->Update();
            }
        }
        

        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }
            
            for (var i = 0; i < _readers.Length; i++)
            {
                var reader = _readers[i];
                DeallocateReader(reader);
            }

            _readers.Dispose();
            UnsafeEventSequence.Free(_allocatorHandle, _buffer);
            _buffer = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            throw new System.NotImplementedException();
        }
        
        /// <summary>
        /// Returns a parallel writer for this queue.
        /// </summary>
        /// <returns>A parallel writer for this queue.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;

            writer.Buffer = _buffer;
            writer.AllocatorHandle = _allocatorHandle;
            writer.ThreadIndex = 0;

            return writer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeEventSequence* Buffer;
            
            internal AllocatorManager.AllocatorHandle AllocatorHandle;

            [NativeSetThreadIndex]
            internal int ThreadIndex;
            
            /// <summary>
            /// Adds an element at the back of the queue.
            /// </summary>
            /// <param name="value">The value to be enqueued.</param>
            public long Enqueue(T value)
            {

                
                UnsafeEventSequenceBlockHeader*
                    block = UnsafeEventSequence.AllocateWriteBlock(Buffer, AllocatorHandle, ThreadIndex, out var messageId);
            
            
                UnsafeUtility.WriteArrayElement(block + 1, block->ItemCount, value);
                block->ItemCount++;

                return messageId;

            }

            /// <summary>
            /// Adds an element at the back of the queue.
            /// </summary>
            /// <param name="value">The value to be enqueued.</param>
            /// <param name="threadIndexOverride">The thread index which must be set by a field from a job struct with the <see cref="NativeSetThreadIndexAttribute"/> attribute.</param>
            public long Enqueue(T value, int threadIndexOverride)
            {
                
                UnsafeEventSequenceBlockHeader*
                    block = UnsafeEventSequence.AllocateWriteBlock(Buffer, AllocatorHandle, threadIndexOverride, out var messageId);
            
            
                UnsafeUtility.WriteArrayElement(block + 1, block->ItemCount, value);
                block->ItemCount++;

                return messageId;
                
            }
        }
        
        public static int GetBlockHeaderAlignmentSize()
        {
            return CollectionHelper.Align(UnsafeUtility.SizeOf<UnsafeEventSequenceBlockHeader>(), 16);
        }
        
        
        internal static unsafe Reader* AllocateReader(UnsafeEventStream<T>* stream)
        {
#if UNITY_2022_2_14F1_OR_NEWER
            int maxThreadCount = JobsUtility.ThreadIndexCount;
#else
            int maxThreadCount = JobsUtility.MaxJobThreadCount;
#endif
            
            var readerSize = CollectionHelper.Align(UnsafeUtility.SizeOf<Reader>(), JobsUtility.CacheLineSize);
            
            //we need one long for each read header and one for each write header + the int for the thread affinity 

            var data = (Reader*)AllocatorManager.Allocate(stream->_allocatorHandle,
                readerSize + maxThreadCount * JobsUtility.CacheLineSize * 3, JobsUtility.CacheLineSize);
            /*var data = (Reader*)UnsafeUtility.MallocTracked(readerSize + maxThreadCount * JobsUtility.CacheLineSize * 3,
                JobsUtility.CacheLineSize, stream->_allocatorHandle.ToAllocator, 0);*/
            
            data->ReadBufferLastReadMessageId = (((byte*)data) + readerSize);
            data->WriteBufferLastReadMessageId = (((byte*)data) + readerSize + maxThreadCount * JobsUtility.CacheLineSize);
            data->Affinity = (ThreadAffinity*)((byte*)data + readerSize + maxThreadCount * JobsUtility.CacheLineSize * 2);
            data->Buffer = stream;
            data->ThreadCount = maxThreadCount;

            var affinityAsBytePtr = (byte*)data->Affinity;
            //setting message id for each thread to zero and setting ThreadAffinities 
            for (int threadIndex = 0; threadIndex < maxThreadCount; threadIndex++)
            {
                *(long*)(data->ReadBufferLastReadMessageId + threadIndex * JobsUtility.CacheLineSize) = 0;
                *(long*)(data->WriteBufferLastReadMessageId + threadIndex * JobsUtility.CacheLineSize) = 0;
                *(ThreadAffinity*)(affinityAsBytePtr + threadIndex * JobsUtility.CacheLineSize) = new ThreadAffinity
                {
                    Affinity = threadIndex,
                    CurrentMemoryBlock = null
                };
                
            }
            
            
            
            data->AllocatorHandle = stream->_allocatorHandle;
            stream->_readers.Add(data);
            
            return data;
        }

        internal static unsafe void DeallocateReader(Reader* reader)
        {
            if (reader == null || !reader->IsCreated)
            {
                return;
            }
            reader->Affinity = null;
            reader->ReadBufferLastReadMessageId = null;
            reader->WriteBufferLastReadMessageId = null;
            var testHandle = reader->Buffer->_allocatorHandle;
            reader->Buffer = null;
            
            AllocatorManager.Free(testHandle, reader);
        }
        
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct Reader
        {
            [NativeDisableUnsafePtrRestriction]
            internal byte* ReadBufferLastReadMessageId;
            [NativeDisableUnsafePtrRestriction]
            internal byte* WriteBufferLastReadMessageId;
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeEventStream<T>* Buffer;
            [NativeDisableUnsafePtrRestriction]
            internal ThreadAffinity* Affinity;
            internal AllocatorManager.AllocatorHandle AllocatorHandle;
            internal int ThreadCount;
            

            
            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Affinity != null;
            }

            private void SetThreadAffinity(int threadIndex, int threadAffinity, UnsafeEventSequenceBlockHeader* currentMemoryBlock = null)
            {
                var threadAffinityBytePtr = (byte*)Affinity;
                var threadAffinityPtr = (ThreadAffinity*)(threadAffinityBytePtr + threadIndex * JobsUtility.CacheLineSize);
                threadAffinityPtr->Affinity = threadAffinity;
                threadAffinityPtr->CurrentMemoryBlock = currentMemoryBlock;
            }


            private ThreadAffinity* GetThreadAffinity(int threadIndex)
            {
                var threadAffinityBytePtr = (byte*)Affinity;
                return (ThreadAffinity*)(threadAffinityBytePtr + threadIndex * JobsUtility.CacheLineSize);
            }
            



            public static bool TryRead(Reader* reader, int threadIndex, out T value)
            {
                //first get affinity 
                //try get next id for that affinity (maybe pass affinity to the id-method
                //check if we have pointer for the id (or maybe we will have pointer for the id and just go from there?) 
                //get message 
                //return message 

                var affinity = reader->GetThreadAffinity(threadIndex);

                var threadCount = reader->ThreadCount * 2;

                var startingAffinity = affinity->Affinity;
                var currentAffinity = startingAffinity;

                do
                {

                    var lastReadMessageId = GetLastReadIdPtr(reader, currentAffinity);
                    var header = UnsafeEventSequence.GetHeader(reader->Buffer->_buffer, currentAffinity);

                    if (AcquireMessageId(lastReadMessageId, header, out var id))
                    {
                        //we have a message id now 

                        var currentMemoryBlock = affinity->CurrentMemoryBlock != null
                            ? affinity->CurrentMemoryBlock
                            : header->FirstBlock;

                        
                        while (currentMemoryBlock != null)
                        {
                            var firstMessageId = currentMemoryBlock->FirstMessageId;

                            if (firstMessageId + currentMemoryBlock->ItemCount > id)
                            {
                                //it's our block 
                                reader->SetThreadAffinity(threadIndex, currentAffinity, currentMemoryBlock);
                                value = UnsafeUtility.ReadArrayElement<T>(currentMemoryBlock + 1, (int)(id - firstMessageId));
                                return true;

                            }
                            
                            currentMemoryBlock = currentMemoryBlock->NextBlock;
                        }
                    }
                    
                    currentAffinity = (currentAffinity + 1) % threadCount;
                    reader->SetThreadAffinity(threadIndex, currentAffinity);
                    
                } while (startingAffinity != currentAffinity);


                

                value = default;
                return false;


            }

            private static bool AcquireMessageId(long* lastReadId, UnsafeEventSequenceHeader* header, out long id)
            {
                long currentId;
                do
                {
                    currentId = *lastReadId;
                    if (header->Counter <= currentId)
                    {
                        //no more messages to read on this thread
                        id = -1;
                        return false;
                    }
                        
                    id = currentId + 1;
                    
                } while (Interlocked.CompareExchange(ref UnsafeUtility.AsRef<long>(lastReadId), id, currentId) != currentId);
                
                return true;
            }

            
            public static long* GetLastReadIdPtr(Reader* reader, int threadIndex)
            {
                return threadIndex < reader->ThreadCount
                    ? (long*)(reader->ReadBufferLastReadMessageId + JobsUtility.CacheLineSize * threadIndex)
                    : (long*)(reader->WriteBufferLastReadMessageId + JobsUtility.CacheLineSize *
                        (threadIndex % reader->ThreadCount));
            }

            

            

            public T Read()
            {
                throw new NotImplementedException();
            }

            public long Read(out T message)
            {
                throw new NotImplementedException();
            }
            
            
            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool IsEmpty()
            {
                throw new NotImplementedException();
            }

            public void Update()
            {
                var threadCount = ThreadCount;


                var readPtr = ReadBufferLastReadMessageId;
                ReadBufferLastReadMessageId = WriteBufferLastReadMessageId;
                WriteBufferLastReadMessageId = readPtr;
                
                
                
                
                var threadAffinityBytePtr = (byte*)Affinity;
                for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
                {
                    var threadAffinity = (ThreadAffinity*)(threadAffinityBytePtr + threadIndex * JobsUtility.CacheLineSize);
                    threadAffinity->Affinity = threadIndex;
                    threadAffinity->CurrentMemoryBlock = null;
                    
                    //set write buffer messages to the current counter 
                    *((long*)(WriteBufferLastReadMessageId + threadIndex * JobsUtility.CacheLineSize)) =
                        UnsafeEventSequence.GetWriteHeader(Buffer->_buffer, threadIndex)->Counter;
                }
            }

            
        }


        [StructLayout(LayoutKind.Sequential)]
        internal struct ThreadAffinity
        {
            internal UnsafeEventSequenceBlockHeader* CurrentMemoryBlock;
            internal int Affinity;
        }
        
    }
}