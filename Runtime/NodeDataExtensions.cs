using Unity.Mathematics;

namespace BonnFireGames.CustomNativeContainers
{
    public static class NodeDataExtensions
    {
        public static bool Contains(this NodeData data, int2 point)
        {
            return math.all(data.Dimensions.xy <= point) && math.all(data.Dimensions.xy + data.Dimensions.zw > point);
        }
    }
}