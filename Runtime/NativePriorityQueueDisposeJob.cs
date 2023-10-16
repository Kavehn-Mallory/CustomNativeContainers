using System;
using Unity.Collections;
using Unity.Jobs;

namespace BonnFireGames.CustomNativeContainers
{
    [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
    internal struct NativePriorityQueueDisposeJob<T> : IJob where T : unmanaged, IComparable<T>
    {
        internal NativePriorityQueueDispose<T> Data;

        public void Execute() => this.Data.Dispose();
    }
}