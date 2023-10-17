using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace BonnFireGames.CustomNativeContainers
{


    /*public unsafe struct NativePriorityQueue<T, TU> : INativeDisposable where T : struct where TU : IComparer<T>
    {
        public void Dispose()
        {
            // TODO release managed resources here
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            throw new NotImplementedException();
        }
    }*/
    
    //[NativeContainerSupportsDeallocateOnJobCompletion]
    //[NativeContainerSupportsMinMaxWriteRestriction]
    [NativeContainer]
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    public unsafe struct NativePriorityQueue<T> : INativeDisposable where T : unmanaged, IComparable<T>
    {
        // Raw pointers aren't usually allowed inside structures that are passed to jobs, but because it's protected
        // with the safety system, you can disable that restriction for it
        //[NativeDisableUnsafePtrRestriction] internal void* m_Buffer;
        [NativeDisableUnsafePtrRestriction] internal NativePriorityQueueData<T>* m_Buffer;
        internal AllocatorManager.AllocatorHandle AllocatorHandle;
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        
        // Statically register this type with the safety system, using a name derived from the type itself
        internal static int s_staticSafetyId;
#endif
        

        
        
        public NativePriorityQueue(AllocatorManager.AllocatorHandle allocator, int capacity = 16)
        {
            AllocatorHandle = allocator.ToAllocator;
            
            
            NativePriorityQueueData<T>.AllocateQueue(ref allocator, capacity, out m_Buffer);
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Create the AtomicSafetyHandle and DisposeSentinel
            m_Safety = AtomicSafetyHandle.Create();
            
            InitStaticSafetyId(ref m_Safety);
            
            // Automatically bump the secondary version any time this container is scheduled for writing in a job
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);

            // Check if this is a nested container, and if so, set the nested container flag
            if (UnsafeUtility.IsNativeContainerType<T>()) 
                AtomicSafetyHandle.SetNestedContainer(m_Safety, true);
#endif
        }
        
        [BurstDiscard]
        private static void InitStaticSafetyId(ref AtomicSafetyHandle handle)
        {
            if (s_staticSafetyId == 0)
                s_staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NativePriorityQueue<T>>();
            AtomicSafetyHandle.SetStaticSafetyId(ref handle, s_staticSafetyId);
        }
        
        public int Length
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // Check that you are allowed to read information about the container 
                // This throws InvalidOperationException if you aren't allowed to read from the native container,
                // or if the native container has been disposed
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Buffer->Length;
            }
        }
        
        public int Capacity
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // Check that you are allowed to read information about the container 
                // This throws InvalidOperationException if you aren't allowed to read from the native container,
                // or if the native container has been disposed
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Buffer->Capacity;
            }
        }
        
        public unsafe bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => this.m_Buffer != null;
        }

        public unsafe bool IsEmpty => !IsCreated || Length <= 0;

        public JobHandle Dispose(JobHandle inputDeps)
        {
            //Copied from NativeList. Might help to understand in the future
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            
            //code from NativeArray. Seems to be pretty thorough 
            if (this.AllocatorHandle.ToAllocator != Allocator.None && !AtomicSafetyHandle.IsDefaultValue(in this.m_Safety))
                AtomicSafetyHandle.CheckExistsAndThrow(in this.m_Safety);
            if (!this.IsCreated)
                return inputDeps;
            if (this.AllocatorHandle.ToAllocator >= Allocator.FirstUserIndex)
                throw new InvalidOperationException("The NativePriorityQueue can not be Disposed because it was allocated with a custom allocator, use CollectionHelper.Dispose in com.unity.collections package.");
            if (this.AllocatorHandle.ToAllocator > Allocator.None)
            {
                JobHandle jobHandle = new NativePriorityQueueDisposeJob<T>()
                {
                    Data = new NativePriorityQueueDispose<T>()
                    {
                        Data = this.m_Buffer,
                        AllocatorHandle = this.AllocatorHandle,
                        m_Safety = this.m_Safety
                    }
                }.Schedule<NativePriorityQueueDisposeJob<T>>(inputDeps);
                
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_Safety);
#endif
                this.m_Buffer = (NativePriorityQueueData<T>*) null;
                this.AllocatorHandle = Allocator.Invalid;
                return jobHandle;
            }
            this.m_Buffer = (NativePriorityQueueData<T>*) null;
            return inputDeps;
        }

        [WriteAccessRequired]
        public void Dispose()
        {
/*#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
            AtomicSafetyHandle.Release(m_Safety);
#endif
            UnsafeUtility.FreeTracked(m_Buffer, m_AllocatorLabel);
            m_Buffer = null;
            m_Capacity = 0;
            m_Length = 0;*/
            
            if (this.AllocatorHandle.ToAllocator != Allocator.None && !AtomicSafetyHandle.IsDefaultValue(in this.m_Safety))
                AtomicSafetyHandle.CheckExistsAndThrow(in this.m_Safety);
            if (!this.IsCreated)
                return;
            if (this.AllocatorHandle.ToAllocator == Allocator.Invalid)
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");
            if (this.AllocatorHandle.ToAllocator >= Allocator.FirstUserIndex)
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was allocated with a custom allocator, use CollectionHelper.Dispose in com.unity.collections package.");
            if (this.AllocatorHandle.ToAllocator > Allocator.None)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
                AtomicSafetyHandle.Release(m_Safety);
#endif
                NativePriorityQueueData<T>.DeallocateQueue(m_Buffer, AllocatorHandle);
                this.AllocatorHandle = Allocator.Invalid;
            }
            this.m_Buffer = (NativePriorityQueueData<T>*) null;

        }
        
        public void Enqueue(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Check that you can modify (write to) the native container right now, and if so, bump the secondary version so that
            // any views are invalidated, because you are going to change the size and pointer to the buffer
            CheckRead();
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Buffer->Enqueue(value);

        }

        public T Dequeue()
        {
            if (!TryDequeue(out var item))
            {
                ThrowEmpty();
            }
            return item;
        }

        public T Peek()
        {
            CheckRead();
            return m_Buffer->Peek();
        }

        public bool TryDequeue(out T item)
        {
            CheckRead();
            if (!IsEmpty)
            {
                CheckWrite();
                m_Buffer->TryDequeue(out item);
                return true;
            }
            item = default(T);
            return false;
        }

        /// <summary>
        /// Tries to find the index of a specific element using the given IComparer&lt;T&gt;. If no IComparer&amp;lt;T&amp;gt; is specified, the default comparer will be used
        /// </summary>
        /// <param name="element"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public int FindElement(T element, IComparer<T> comparer = null)
        {
            CheckRead();
            return m_Buffer->FindElement(element, comparer);
        }

        public bool Contains(T element, IComparer<T> comparer = null)
        {
            return FindElement(element, comparer) >= 0;
        }
        
        public bool Contains(T element, out T existingElement, IComparer<T> comparer = null)
        {
            var index = FindElement(element, comparer);
            if (index >= 0)
            {
                existingElement = this[index];
                return true;
            }

            existingElement = default;
            return false;
        }

        public void ReplaceElement(int index, T newElement)
        {
            CheckRead();
            CheckWrite();

            m_Buffer->ReplaceElement(index, newElement);
        }
        
        /// <summary>
        /// Gets the element at the specified index. Throws an IndexOutOfRangeException if the index is below zero or exceeds the maximum index
        /// </summary>
        /// <param name="index"></param>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if(!m_Buffer->GetElement(index, out var element))
                {
                    ThrowIndexOutOfRange(index);
                }
                return element;
            }
            [WriteAccessRequired, MethodImpl(MethodImplOptions.AggressiveInlining)] set => m_Buffer->ReplaceElement(index, value);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ThrowIndexOutOfRange(int index)
        {
            throw new IndexOutOfRangeException($"The index {index} is out of the queue's range");
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ThrowEmpty()
        {
            throw new InvalidOperationException("Trying to read from an empty queue.");
        }
        
        

        

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }
        
        
    }

    
}
