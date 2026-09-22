using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace BonnFireGames.CustomNativeContainers
{
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [BurstCompile]
    public unsafe struct NativeEventStream<T> : INativeDisposable where T : unmanaged
    {
        
        
        internal const int SmallBlockSize = 16 * 16;
        internal const int BlockSize = 16 * 1024;
        
        [NativeDisableUnsafePtrRestriction]
        private UnsafeEventStream<T>* _eventStream;
        

        AtomicSafetyHandle m_Safety;

        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeEventStream<T>>();



        public NativeEventStream(AllocatorManager.AllocatorHandle handle, int blockSize = SmallBlockSize,
            int initialBlockCount = 16, bool dynamicAllocations = true, bool dynamicResize = true)
        {

            m_Safety = CollectionHelper.CreateSafetyHandle(handle);

            InitNativeContainer(m_Safety);
            CollectionHelper.SetStaticSafetyId<NativeEventStream<T>>(ref m_Safety, ref s_staticSafetyId.Data);


            _eventStream = UnsafeEventStream<T>.Allocate(handle, blockSize, initialBlockCount, dynamicAllocations,
                dynamicResize);
        }
        

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", GenericTypeArguments = new[] { typeof(NativeArray<int>) })]
        internal static void InitNativeContainer(AtomicSafetyHandle handle)
        {
            if (UnsafeUtility.IsNativeContainerType<T>())
                AtomicSafetyHandle.SetNestedContainer(handle, true);
        }


        [BurstCompile]
        public void Update()
        {
            _eventStream->Update();
        }

        public int ReaderCount => _eventStream->ReaderCount;
        
        
        /// <summary>
        /// Whether this event stream has been allocated (and not yet deallocated).
        /// 
        /// </summary>
        /// <value>True if this event stream has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _eventStream != null && _eventStream->IsCreated;
        }
        

        [BurstCompile]
        public long Enqueue(T eventData)
        {
            CheckWrite();
            return _eventStream->Enqueue(eventData);
        }
        
        
        public void Dispose()
        {

            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }

            if (!IsCreated)
            {
                return;
            }

            UnsafeEventStream<T>.Free(_eventStream);
            
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            throw new System.NotImplementedException();
        }
        


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckWrite()
        {

            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

        }

        public Reader AllocateReader()
        {
            return new Reader(ref this);
        }

        [StructLayout(LayoutKind.Sequential)]
        [BurstCompile]
        public struct Reader : INativeDisposable
        {


            internal AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<Reader>();


            [NativeDisableUnsafePtrRestriction]
            internal UnsafeEventStream<T>.Reader* UnsafeReader;

            public Reader(ref NativeEventStream<T> stream)
            {

                m_Safety = stream.m_Safety;
                CollectionHelper.SetStaticSafetyId(ref m_Safety, ref s_staticSafetyId.Data, "Unity.Collections.NativeStream.Reader");

                UnsafeReader = UnsafeEventStream<T>.AllocateReader(stream._eventStream);

            }


            [BurstCompile]
            public bool TryRead(out T value)
            {
                return UnsafeEventStream<T>.Reader.TryRead(UnsafeReader, 0, out value);
            }
            
            [BurstCompile]
            public void Update()
            {
                UnsafeReader->Update();
            }

            [BurstCompile]
            public T Read()
            {
                return UnsafeReader->Read();
            }

            [BurstCompile]
            public long Read(out T message)
            {
                CheckRead();
                return UnsafeReader->Read(out message);
            }
            
            
            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly void CheckRead()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            }

            public void Dispose()
            {
                UnsafeEventStream<T>.DeallocateReader(UnsafeReader);
                UnsafeReader = null;
            }

            public JobHandle Dispose(JobHandle inputDeps)
            {
                throw new System.NotImplementedException();
            }
        }
        
        
        /// <summary>
        /// Returns a parallel writer for this queue.
        /// </summary>
        /// <returns>A parallel writer for this queue.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            
            writer.m_Safety = m_Safety;
            CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref writer.m_Safety, ref ParallelWriter.s_staticSafetyId.Data);
            writer.unsafeWriter = _eventStream->AsParallelWriter();

            return writer;
        }
        
        
        /// <summary>
        /// A parallel writer for a <see cref="NativeEventStream{T}"/>.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a NativeQueue.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        [StructLayout(LayoutKind.Sequential)]
        [BurstCompile]
        public struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeEventStream<T>.ParallelWriter unsafeWriter;
            
            internal AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();
            
            
            
            /// <summary>
            /// Adds an element at the back of the queue.
            /// </summary>
            /// <param name="value">The value to be enqueued.</param>
            [BurstCompile]
            public long Enqueue(T value)
            {
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                return unsafeWriter.Enqueue(value);
            }

            /// <summary>
            /// Adds an element at the back of the queue.
            /// </summary>
            /// <param name="value">The value to be enqueued.</param>
            /// <param name="threadIndex">The threadIndex of the writer that enqueued the message</param>
            [BurstCompile]
            public long Enqueue(T value, out int threadIndex)
            {
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                threadIndex = unsafeWriter.ThreadIndex;
                return unsafeWriter.Enqueue(value);
            }

            /// <summary>
            /// Adds an element at the back of the queue.
            /// </summary>
            /// <param name="value">The value to be enqueued.</param>
            /// <param name="threadIndexOverride">The thread index which must be set by a field from a job struct with the <see cref="NativeSetThreadIndexAttribute"/> attribute.</param>
            [BurstCompile]
            public long Enqueue(T value, int threadIndexOverride)
            {
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                return unsafeWriter.Enqueue(value, threadIndexOverride);
            }
            
        }


    }
    
    
    
}