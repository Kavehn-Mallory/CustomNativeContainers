using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace BonnFireGames.CustomNativeContainers
{
    [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativePriorityQueueData<T> where T : unmanaged, IComparable<T>
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void* m_Buffer;

        internal unsafe int Length;

        internal unsafe int Capacity;

        internal unsafe AllocatorManager.AllocatorHandle AllocatorHandle;
        
        

        [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
        internal static unsafe void AllocateQueue(ref AllocatorManager.AllocatorHandle allocator, int initialCapacity, out NativePriorityQueueData<T>* outBuf)
        {
            var state = (NativePriorityQueueData<T>*)UnsafeUtility.MallocTracked(
                sizeof(NativePriorityQueueData<T>),
                UnsafeUtility.AlignOf<NativePriorityQueueData<T>>(),
                allocator.ToAllocator, 1);
            
            state->m_Buffer = AllocateQueueData(allocator, initialCapacity);

            state->Capacity = initialCapacity;
            state->Length = 0;
            state->AllocatorHandle = allocator;
            
            outBuf = state;
        }

        private static void* AllocateQueueData(AllocatorManager.AllocatorHandle label, int initialCapacity)
        {
            return UnsafeUtility.MallocTracked(UnsafeUtility.SizeOf<T>() * initialCapacity, UnsafeUtility.AlignOf<T>(), label.ToAllocator, 1);
        }

        internal static unsafe void DeallocateQueue(NativePriorityQueueData<T>* data,
            AllocatorManager.AllocatorHandle allocatorHandle)
        {
            //deallocate queue data and then deallocate the object itself
            if (data->m_Buffer != null)
            {
                UnsafeUtility.FreeTracked(data->m_Buffer, allocatorHandle.ToAllocator);
            }

            data->AllocatorHandle = AllocatorManager.Invalid;
            UnsafeUtility.FreeTracked(data, allocatorHandle.ToAllocator);
        }
        
        private int Parent(int i) => (i - 1) / 2;
        private int LeftChild(int i) => (2 * i) + 1;
        private int RightChild(int i) => (2 * i) + 2;


        internal void Enqueue(T value)
        {
            if (Length >= Capacity)
            {
                //Increase size of array -> allocate new memory, add elements to it, then continue.
                var newCapacity = Capacity * 2;
                
                var newBuffer = AllocateQueueData(AllocatorHandle, newCapacity);
                
                UnsafeUtility.MemCpy(newBuffer, m_Buffer, UnsafeUtility.SizeOf<T>() * Length);
                UnsafeUtility.FreeTracked(m_Buffer, AllocatorHandle.ToAllocator);

                m_Buffer = newBuffer;
                Capacity = newCapacity;
            }
            
            UnsafeUtility.WriteArrayElement(m_Buffer, Length, value);
            Length++;
            
            
            SiftUp(Length - 1);
        }
        
        internal bool TryDequeue(out T item)
        {
            if (Length > 0)
            {
                Length--;
                item = UnsafeUtility.ReadArrayElement<T>(m_Buffer, 0);

                Swap(0, Length);
                
                SiftDown(0);
                
                return true;
            }
            item = default(T);
            return false;
        }
        
        internal T Peek()
        {
            return UnsafeUtility.ReadArrayElement<T>(m_Buffer, 0);
        }
        
        internal int FindElement(T element, IComparer<T> comparer = null)
        {
            comparer ??= new NativeSortExtension.DefaultComparer<T>();
            for (var index = 0; index < Length; index++)
            {
                var currentElement = UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
                if (comparer.Compare(element, currentElement) == 0)
                {
                    return index;
                }
            }
            return -1;
        }
        
        internal void ReplaceElement(int index, T newElement)
        {
            var currentElement = UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            UnsafeUtility.WriteArrayElement(m_Buffer, index, newElement);
            if (currentElement.CompareTo(newElement) == 0)
            {
                return;
            }
            Swap(index, Length - 1);
            SiftDown(index);
        }
        
        private void SiftUp(int i)
        {
            var currentValue = UnsafeUtility.ReadArrayElement<T>(m_Buffer, i);
            var parentIndex = Parent(i);
            var parent = UnsafeUtility.ReadArrayElement<T>(m_Buffer, parentIndex);
            while (i > 0 && currentValue.CompareTo(parent) < 0)
            {
                Swap(i, parentIndex);
                i = parentIndex;
                parentIndex = Parent(i);
                parent = UnsafeUtility.ReadArrayElement<T>(m_Buffer, parentIndex);
                
            }
        }
        
        private void SiftDown(int i)
        {
            var maxIndex = i;
            while (true)
            {
                var leftChildIndex = LeftChild(i);
                var rightChildIndex = RightChild(i);
                
                if (leftChildIndex < Length && this[maxIndex].CompareTo(this[leftChildIndex]) > 0)
                {
                    maxIndex = leftChildIndex;
                }
                
                if (rightChildIndex < Length && this[maxIndex].CompareTo(this[rightChildIndex]) > 0)
                {
                    maxIndex = rightChildIndex;
                }

                if (i != maxIndex)
                {
                    Swap(i, maxIndex);
                    i = maxIndex;
                    continue;
                }

                break;
            }
        }

        private void Swap(int x, int y)
        {
            if (x != y)
            {
                var xElement = UnsafeUtility.ReadArrayElement<T>(m_Buffer, x);
                this[x] = this[y];
                this[y] = xElement;
            }
            
        }

        internal bool GetElement(int index, out T element)
        {
            if (!CheckIndex(index))
            {
                element = default;
                return false;
            }
            element = this[index];
            return true;
        }

        private bool CheckIndex(int index)
        {
            return index >= 0 && index < Length;
        }

        private T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            [WriteAccessRequired, MethodImpl(MethodImplOptions.AggressiveInlining)] set => UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
        }
        
    } 
}