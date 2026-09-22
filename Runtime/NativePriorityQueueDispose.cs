using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BonnFireGames.CustomNativeContainers
{
    [NativeContainer]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
    internal struct NativePriorityQueueDispose<T> where T : unmanaged, IComparable<T>
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe NativePriorityQueueData<T>* Data;
        internal AllocatorManager.AllocatorHandle AllocatorHandle;
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
        public unsafe void Dispose() => NativePriorityQueueData<T>.DeallocateQueue(Data, AllocatorHandle);
    }
}