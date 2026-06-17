using Unity.Mathematics;

namespace BonnFireGames.CustomNativeContainers
{
    public static class NativeQuadTreeExtensions
    {
        public static int4 MinMax(this QuadNodeData data)
        {
            var halfDistance = data.Bounds.zw / 2;
            return data.Bounds.xyxy + new int4(-halfDistance.xy, halfDistance.xy);
        }
        
    }
}