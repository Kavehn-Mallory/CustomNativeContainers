using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace BonnFireGames.CustomNativeContainers
{
    
    /// <summary>
    /// Memory Layout:
    /// <see cref="UnsafeEventSequence"/> | NumOfThreads * <see cref="UnsafeEventSequenceHeader"/> (each taking up a cache line) | NumOfThreads * <see cref="long"/> (each taking up a cache line) | NumOfThreads * <see cref="UnsafeEventSequenceHeader"/> (each taking up a cache line) | NumOfThreads * <see cref="long"/> (each taking up a cache line)
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    internal unsafe struct UnsafeEventSequence
    {


        public const string GetFreeBlockMethodName = nameof(GetFreeBlock);
        public const string IncreaseMessageIdMethodName = nameof(IncreaseMessageId);

        //internal UnsafeEventSequenceHeader** TempHeaderStorage;
        internal UnsafeEventSequenceBlockHeader* FreeList;
        internal byte* ReaderHeader;
        internal byte* WriteHeader;
        internal byte* InitialBlockAllocationPointer;
        internal int blockSize;
        internal int ElementCountPerBlock;
        internal int ThreadCount;
        internal bool dynamicallyAllocateMemory;
        internal bool dynamicallyResizeSequence;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SwapHeaders()
        {
            var tempHeader = ReaderHeader;
            ReaderHeader = WriteHeader;
            WriteHeader = tempHeader;
        }



        public void ResetWriteBlock()
        {
            for (int threadIndex = 0; threadIndex < ThreadCount; threadIndex++)
            {
                var firstBlock = ((UnsafeEventSequenceHeader*)(WriteHeader + threadIndex * JobsUtility.CacheLineSize))->FirstBlock;

                while (firstBlock != null)
                {
                    firstBlock->ItemCount = 0;

                    firstBlock = firstBlock->NextBlock;
                }
            }
        }

        
        
        
        internal static void Free(AllocatorManager.AllocatorHandle handle, UnsafeEventSequence* data)
        {
            FreeUsedBlocks(handle, data->ReaderHeader, data->ThreadCount);
            FreeUsedBlocks(handle, data->WriteHeader, data->ThreadCount);
            FreeUnusedBlocks(handle, data->FreeList);
            
            //free initial block allocation
            AllocatorManager.Free(handle, data->InitialBlockAllocationPointer);
            //free data structure itself 
            AllocatorManager.Free(handle, data);
        }

        private static void FreeUnusedBlocks(AllocatorManager.AllocatorHandle handle, UnsafeEventSequenceBlockHeader* freeListHeader)
        {
            while (freeListHeader != null)
            {
                var nextBlock = freeListHeader->NextBlock;
                if (freeListHeader->InitialBlockFlag == 0)
                {
                    AllocatorManager.Free(handle, freeListHeader);
                }
                freeListHeader = nextBlock;
            }
        }

        private static void FreeUsedBlocks(AllocatorManager.AllocatorHandle handle, byte* headerStart, int threadCount)
        {
            for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
            {
                var currentBlock = ((UnsafeEventSequenceHeader*)(headerStart + threadIndex * JobsUtility.CacheLineSize))->FirstBlock;
                while (currentBlock != null)
                {
                    var nextBlock = currentBlock->NextBlock;
                    if (currentBlock->InitialBlockFlag == 0)
                    {
                        AllocatorManager.Free(handle, currentBlock);
                    }
                    currentBlock = nextBlock;
                }
            }
        }

        internal static void AllocateDoubleBufferEventSequence<T>(AllocatorManager.AllocatorHandle handle, out UnsafeEventSequence* sequence, int blockSize = NativeEventStream<T>.SmallBlockSize,
            int initialBlockCount = 16, bool dynamicAllocations = true, bool dynamicResize = true) where T : unmanaged
        {

            blockSize = math.ceilpow2(blockSize);
            if (blockSize < UnsafeUtility.SizeOf<UnsafeEventSequenceBlockHeader>() + UnsafeUtility.SizeOf<T>())
            {
                throw new ArgumentException(
                    $"The given block size ({blockSize} bytes) does not have enough space for a single element + header ({UnsafeUtility.SizeOf<UnsafeEventSequenceBlockHeader>() + UnsafeUtility.SizeOf<T>()}) bytes. " +
                    $"In general it is a bad idea to allocate extremely small blocks");
            }

#if UNITY_2022_2_14F1_OR_NEWER
            int maxThreadCount = JobsUtility.ThreadIndexCount;
#else
            int maxThreadCount = JobsUtility.MaxJobThreadCount;
#endif
            var sequenceDataSize = CollectionHelper.Align(UnsafeUtility.SizeOf<UnsafeEventSequence>(), JobsUtility.CacheLineSize);
            
            var data = (UnsafeEventSequence*)AllocatorManager.Allocate(handle,
                sequenceDataSize + maxThreadCount * JobsUtility.CacheLineSize * 2, JobsUtility.CacheLineSize);

            
            
            Helpers.Assert.IsTrue(UnsafeUtility.SizeOf<UnsafeEventSequenceHeader>() <= JobsUtility.CacheLineSize);

            
            
            var elementCount = (blockSize - UnsafeUtility.SizeOf<UnsafeEventSequenceBlockHeader>()) / UnsafeUtility.SizeOf<T>();

            data->blockSize = blockSize;
            data->dynamicallyAllocateMemory = dynamicAllocations;
            data->dynamicallyResizeSequence = dynamicResize;
            data->ReaderHeader = ((byte*)data) + sequenceDataSize;
            //should advance pointer by threadCount number of headers 
            data->WriteHeader = data->ReaderHeader + maxThreadCount * JobsUtility.CacheLineSize;
            data->ThreadCount = maxThreadCount;
            data->ElementCountPerBlock = elementCount;
            
            
            //init headers 


            for (int threadIndex = 0; threadIndex < maxThreadCount; threadIndex++)
            {
                InitHeader(data->ReaderHeader, threadIndex);
                InitHeader(data->WriteHeader, threadIndex);
            }
            
            
            //allocate initial blocks 

            data->FreeList = null;
            data->InitialBlockAllocationPointer = null;
            
            if (initialBlockCount > 0)
            {
                data->FreeList = AllocateBlocks<T>(handle, initialBlockCount, blockSize);
                data->InitialBlockAllocationPointer = (byte*)data->FreeList;
            }
            
            /*UnsafeEventSequenceHeader** headerTable = (UnsafeEventSequenceHeader**)AllocatorManager.Allocate(...);
            for (int t = 0; t < threadCount; t++)
                headerTable[t] = (UnsafeEventSequenceHeader*)&writeBase[t * JobsUtility.CacheLineSize];*/
            
            sequence = data;

        }

        [BurstCompile]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        private static UnsafeEventSequenceBlockHeader* AllocateBlocks<T>(AllocatorManager.AllocatorHandle handle, int initialBlockCount, int blockSize) where T : unmanaged
        {
            
            
            
            var blockAddress = (byte*)AllocatorManager.Allocate(handle, blockSize, 16, initialBlockCount);
            var firstBlock = (UnsafeEventSequenceBlockHeader*)blockAddress;
           
            
            var currentBlockAddress = blockAddress;
            
            var currentBlock = (UnsafeEventSequenceBlockHeader*)currentBlockAddress;
            
            for (int blockId = 0; blockId < initialBlockCount - 1; blockId++)
            {
                currentBlockAddress += blockSize;
                currentBlock->ItemCount = 0;
                currentBlock->InitialBlockFlag = 1;
                currentBlock->FirstMessageId = 0L;
                currentBlock->NextBlock = (UnsafeEventSequenceBlockHeader*)currentBlockAddress;
                currentBlock = currentBlock->NextBlock;
                
            }
            //last block
            currentBlock->ItemCount = 0;
            currentBlock->InitialBlockFlag = 1;
            currentBlock->FirstMessageId = 0L;
            currentBlock->NextBlock = null;

            return firstBlock;
        }

        [BurstCompile]
        internal static void InitHeader(byte* headerPointer, int threadIndex)
        {
            var data = (UnsafeEventSequenceHeader*)(headerPointer + threadIndex * JobsUtility.CacheLineSize);
            *data = new UnsafeEventSequenceHeader
            {
                FirstBlock = null,
                LastBlock = null,
                Counter = 0L
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long IncreaseMessageId(UnsafeEventSequenceHeader* writeHeader, long increment = 1)
        {
            writeHeader->Counter += increment;

            return writeHeader->Counter;
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long IncreaseMessageIdWithoutRef(int threadIndex, long increment = 1)
        {
            var data = (long*)&WriteHeader[threadIndex * JobsUtility.CacheLineSize + 2 * IntPtr.Size];
            (*data) += increment;
            return *data;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeEventSequenceHeader* GetHeader(UnsafeEventSequence* buffer, int threadIndex)
        {
            return threadIndex < buffer->ThreadCount
                ? GetReadHeader(buffer, threadIndex)
                : GetWriteHeader(buffer,threadIndex % buffer->ThreadCount);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeEventSequenceHeader* GetWriteHeader(UnsafeEventSequence* buffer, int threadIndex)
        {
            return (UnsafeEventSequenceHeader*)&buffer->WriteHeader[threadIndex * JobsUtility.CacheLineSize];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeEventSequenceHeader* GetReadHeader(UnsafeEventSequence* buffer, int threadIndex)
        {
            return (UnsafeEventSequenceHeader*)&buffer->ReaderHeader[threadIndex * JobsUtility.CacheLineSize];
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeEventSequenceHeader* GetWriteHeader(int threadIndex)
        {
            var data = (UnsafeEventSequenceHeader*)&WriteHeader[threadIndex * JobsUtility.CacheLineSize];
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeEventSequenceBlockHeader* GetWriteBlock(int threadIndex)
        {
            var data = (UnsafeEventSequenceBlockHeader**)&WriteHeader[threadIndex * JobsUtility.CacheLineSize + IntPtr.Size];
            return *data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetCurrentWriteBlockTLS(int threadIndex, UnsafeEventSequenceBlockHeader* currentWriteBlock)
        {
            var data = (UnsafeEventSequenceHeader*)&WriteHeader[threadIndex * JobsUtility.CacheLineSize];
            data->LastBlock = currentWriteBlock;
        }
        
        public static UnsafeEventSequenceBlockHeader* AllocateWriteBlock(UnsafeEventSequence* buffer, AllocatorManager.AllocatorHandle allocatorHandle, int threadIndex, out long messageId)
        {
            
            var header = buffer->GetWriteHeader(threadIndex);
            
            messageId = IncreaseMessageId(header, 1);
            

            var block = header->LastBlock;
            while (block != null)
            {
                if (block->ItemCount != buffer->ElementCountPerBlock)
                {

                    block->FirstMessageId = block->ItemCount == 0 ? messageId : block->FirstMessageId;
                    return block;
                }

                //we need to go around if there is more than one block (potentially happens after allocations happened 
                block = block->NextBlock;
            }
            

            block = GetFreeBlock(buffer, allocatorHandle);
            block->FirstMessageId = messageId;

            
            

            var prevLast = header->LastBlock;
            header->LastBlock = block;


            if (prevLast == null)
            {
                header->FirstBlock = block;
            }
            else
            {
                prevLast->NextBlock = block;
            }

            buffer->SetCurrentWriteBlockTLS(threadIndex, block);


            return block;
        }
        

        private static UnsafeEventSequenceBlockHeader* GetFreeBlock(UnsafeEventSequence* buffer, AllocatorManager.AllocatorHandle allocatorHandle)
        {
            UnsafeEventSequenceBlockHeader* block;
            IntPtr ptr, nextPtr;
            do
            {
                block = buffer->FreeList;
                ptr = (IntPtr)block;
                
                if (ptr == IntPtr.Zero)
                {
                    block = (UnsafeEventSequenceBlockHeader*)AllocatorManager.Allocate(allocatorHandle, buffer->blockSize, 16);
                    block->ItemCount = 0;
                    block->InitialBlockFlag = 0;
                    block->NextBlock = null;
                    return block;
                }
                
                //I think we have to set it afterwards. Otherwise it might be a null ptr 
                nextPtr = (IntPtr)block->NextBlock;
            } while (Interlocked.CompareExchange(ref UnsafeUtility.AsRef<IntPtr>(&buffer->FreeList), nextPtr, ptr) != ptr);
            
            block->ItemCount = 0;
            block->NextBlock = null;
            return block;
            
        }
        

    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    internal unsafe struct UnsafeEventSequenceHeader
    {
        public UnsafeEventSequenceBlockHeader* FirstBlock;
        public UnsafeEventSequenceBlockHeader* LastBlock;
        public long Counter;


    }
    
    

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct UnsafeEventSequenceBlockHeader
    {
        public UnsafeEventSequenceBlockHeader* NextBlock;
        public long FirstMessageId; 
        public int ItemCount;
        public byte InitialBlockFlag;
    }
}