using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BonnFireGames.CustomNativeContainers
{
    [NativeContainer]
    [BurstCompatible(GenericTypeArguments = new [] { typeof(int) })]
    internal struct NativePriorityQueueDispose<T> where T : unmanaged, IComparable<T>
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe NativePriorityQueueData<T>* Data;
        internal AllocatorManager.AllocatorHandle AllocatorHandle;
        internal AtomicSafetyHandle m_Safety;

        public unsafe void Dispose() => NativePriorityQueueData<T>.DeallocateQueue(Data, AllocatorHandle);
    }
}