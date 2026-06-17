using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace BonnFireGames.CustomNativeContainers
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NodeData
    {
        /// <summary>
        /// xy: minimum value of node
        /// zw: size of node
        /// </summary>
        public int4 Dimensions;
        public int Index;
        
    }
}