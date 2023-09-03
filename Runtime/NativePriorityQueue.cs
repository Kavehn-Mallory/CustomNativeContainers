using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace BonnFireGames.CustomNativeContainers
{
    [NativeContainerSupportsDeallocateOnJobCompletion]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [NativeContainer]
    public unsafe struct NativePriorityQueue<T> : INativeDisposable where T : unmanaged, IComparable
    {

        // Raw pointers aren't usually allowed inside structures that are passed to jobs, but because it's protected
        // with the safety system, you can disable that restriction for it
        [NativeDisableUnsafePtrRestriction]
        internal void* m_Buffer;
        internal int m_Length;
        internal int m_capacity;
        internal Allocator m_AllocatorLabel;
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
        
        // Statically register this type with the safety system, using a name derived from the type itself
        internal static readonly int s_staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NativePriorityQueue<T>>();
#endif
        

        
        public NativePriorityQueue(Allocator allocator, int capacity = 16)
        {
            m_capacity = capacity;
            m_AllocatorLabel = allocator;
            m_Length = 0;
            
            
            // Calculate the size of the initial buffer in bytes, and allocate it
            int totalSize = UnsafeUtility.SizeOf<T>() * m_capacity;
            m_Buffer = UnsafeUtility.MallocTracked(totalSize, UnsafeUtility.AlignOf<T>(), m_AllocatorLabel, 1);
            
            
            // Copy the data from the array into the buffer
            /*var handle = GCHandle.Alloc(initialItems, GCHandleType.Pinned);
            try
            {
                UnsafeUtility.MemCpy(m_Buffer, handle.AddrOfPinnedObject().ToPointer(), totalSize);
            }
            finally
            {
                handle.Free();
            }*/
            
            //lets either get a native array or lets get the custom memory the same way. that would be fine as well
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Create the AtomicSafetyHandle and DisposeSentinel
            m_Safety = AtomicSafetyHandle.Create();

            // Set the safety ID on the AtomicSafetyHandle so that error messages describe this container type properly.
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_staticSafetyId);
        
            // Automatically bump the secondary version any time this container is scheduled for writing in a job
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);

            // Check if this is a nested container, and if so, set the nested container flag
            if (UnsafeUtility.IsNativeContainerType<T>()) 
                AtomicSafetyHandle.SetNestedContainer(m_Safety, true);
#endif
        }
        
        
        private int Parent(int i) => (i - 1) / 2;
        private int LeftChild(int i) => (2 * i) + 1;
        private int RightChild(int i) => (2 * i) + 2;
        
        public int Count
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // Check that you are allowed to read information about the container 
                // This throws InvalidOperationException if you aren't allowed to read from the native container,
                // or if the native container has been disposed
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Length;
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
                return m_capacity;
            }
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
            AtomicSafetyHandle.Release(m_Safety);
#endif
            UnsafeUtility.FreeTracked(m_Buffer, m_AllocatorLabel);
            m_Buffer = null;
            m_capacity = 0;
            m_Length = 0;

        }

        public void Enqueue(T value)
        {
            if (m_Length < m_capacity)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // Check that you can write to the native container right now.
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                UnsafeUtility.WriteArrayElement(m_Buffer, m_Length, value);
            }
            else
            {
                //Increase size of array -> allocate new memory, add elements to it, then continue.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // Check that you can modify (write to) the native container right now, and if so, bump the secondary version so that
                // any views are invalidated, because you are going to change the size and pointer to the buffer
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
                //todo check if this is correct
                m_capacity *= 2;
                
                int totalSize = UnsafeUtility.SizeOf<T>() * m_capacity;
                var newBuffer = UnsafeUtility.MallocTracked(totalSize, UnsafeUtility.AlignOf<T>(), m_AllocatorLabel, 1);
                
                UnsafeUtility.MemCpy(newBuffer, m_Buffer, UnsafeUtility.SizeOf<T>() * m_Length);
                UnsafeUtility.FreeTracked(m_Buffer, m_AllocatorLabel);
                m_Buffer = newBuffer;
                UnsafeUtility.WriteArrayElement(m_Buffer, m_Length, value);

            }
            m_Length++;
            
            
            SiftUp(m_Length - 1);
            
        }

        public T Dequeue()
        {
            //todo call TryDequeue here and call an error method if it does not work!
            if (!TryDequeue(out var item))
            {
                ThrowEmpty();
            }
            return item;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ThrowEmpty()
        {
            throw new InvalidOperationException("Trying to read from an empty queue.");
        }

        public T Peek()
        {
            CheckRead();
            return UnsafeUtility.ReadArrayElement<T>(m_Buffer, 0);
        }

        public bool TryDequeue(out T item)
        {
            CheckRead();
            if (m_Length > 0)
            {
                CheckWrite();
                m_Length--;
                item = UnsafeUtility.ReadArrayElement<T>(m_Buffer, 0);

                Swap(0, m_Length);
                
                //todo check if this is correct. I think we need to perform a sift down from the top which will move the last element back all the way down through the heap
                SiftDown(0);
                
                return true;
            }
            item = default(T);
            return false;
        }

        private void SiftUp(int i)
        {
            CheckRead();
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
                CheckRead();

                var leftChildIndex = LeftChild(i);
                var rightChildIndex = RightChild(i);
                
                if (leftChildIndex < m_Length && this[maxIndex].CompareTo(this[leftChildIndex]) > 0)
                {
                    maxIndex = leftChildIndex;
                }
                
                if (rightChildIndex < m_Length && this[maxIndex].CompareTo(this[rightChildIndex]) > 0)
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
                CheckWrite();
                var xElement = UnsafeUtility.ReadArrayElement<T>(m_Buffer, x);
                this[x] = this[y];
                this[y] = xElement;
            }
            
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
        
        private T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            [WriteAccessRequired, MethodImpl(MethodImplOptions.AggressiveInlining)] set => UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
        }
        
    }

    
}
