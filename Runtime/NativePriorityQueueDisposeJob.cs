using Unity.Jobs;

namespace BonnFireGames.CustomNativeContainers
{
    internal struct NativePriorityQueueDisposeJob : IJob
    {
        internal NativePriorityQueueDispose Data;

        public void Execute() => this.Data.Dispose();
    }
}