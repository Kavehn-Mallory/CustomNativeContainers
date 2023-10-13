using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BonnFireGames.CustomNativeContainers
{
    [NativeContainer]
    public struct NativePriorityQueueDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal unsafe void* m_Buffer;
        internal Allocator m_AllocatorLabel;
        internal AtomicSafetyHandle m_Safety;

        public unsafe void Dispose() => UnsafeUtility.FreeTracked(this.m_Buffer, this.m_AllocatorLabel);
    }
}