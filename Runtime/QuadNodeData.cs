using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace BonnFireGames.CustomNativeContainers
{
    [StructLayout(LayoutKind.Sequential)]
    public struct QuadNodeData
    {
        
        /// <summary>
        /// Used to describe bounds of the node.
        /// 0: centerX
        /// 1: centerY
        /// 2: dimensionX
        /// 3: dimensionY
        /// </summary>
        public int4 Bounds;
        public int Index;
        public int Depth;

    }
}