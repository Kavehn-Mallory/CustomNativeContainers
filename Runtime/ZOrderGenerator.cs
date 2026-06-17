using Unity.Mathematics;

namespace BonnFireGames.CustomNativeContainers
{
    /// <summary>
    /// Class handling transformation from and to z-order/morton codes. Max value for either 2d coordinate is 65535 (uses up all bits of uint)
    /// </summary>
    public static class ZOrderGenerator
    {
        private static readonly uint[] Masks = {0x55555555, 0x33333333, 0x0F0F0F0F, 0x00FF00FF};
        private static readonly int[] Masks2 = {0x55555555, 0x33333333, 0x0F0F0F0F, 0x00FF00FF};
        private static readonly int[] Shifts = { 1, 2, 4, 8 };
        private static readonly uint2 PositionShift = new uint2(0xAAAAAAAA, 0x55555555);
        
        public static uint CalculateZOrderForPosition(uint2 position)
        {

            //int position = position.x;  // Interleave lower 16 bits of x and y, so the bits of x
            //int y = position.y;  // are in the even positions and bits from y in the odd;

            position = (position | (position << Shifts[3])) & Masks[3];
            position = (position | (position << Shifts[2])) & Masks[2];
            position = (position | (position << Shifts[1])) & Masks[1];
            position = (position | (position << Shifts[0])) & Masks[0];

            /*y = (y | (y << Shifts[3])) & Masks[3];
            y = (y | (y << Shifts[2])) & Masks[2];
            y = (y | (y << Shifts[1])) & Masks[1];
            y = (y | (y << Shifts[0])) & Masks[0];*/

            return (position.x | (position.y << 1));
        }
        
        /// <summary>
        /// For position representing minX, minY, maxX, maxY it will calculate the z-order-index for the min and max
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static int2 CalculateZOrderForArea(int4 position)
        {

            //int position = position.x;  // Interleave lower 16 bits of x and y, so the bits of x
            //int y = position.y;  // are in the even positions and bits from y in the odd;

            position = ((position | (position << Shifts[3])) & Masks2[3]);
            position = ((position | (position << Shifts[2])) & Masks2[2]);
            position = (position | (position << Shifts[1])) & Masks2[1];
            position = (position | (position << Shifts[0])) & Masks2[0];

            /*y = (y | (y << Shifts[3])) & Masks[3];
            y = (y | (y << Shifts[2])) & Masks[2];
            y = (y | (y << Shifts[1])) & Masks[1];
            y = (y | (y << Shifts[0])) & Masks[0];*/

            return new int2(position.x | (position.y << 1), position.z | (position.w << 1));
        }
        
        /// <summary>
        /// For position representing minX, minY, maxX, maxY it will calculate the z-order-index for the min and max
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static uint2 CalculateZOrderForArea(uint4 position)
        {

            //int position = position.x;  // Interleave lower 16 bits of x and y, so the bits of x
            //int y = position.y;  // are in the even positions and bits from y in the odd;

            position = ((position | (position << Shifts[3])) & Masks[3]);
            position = ((position | (position << Shifts[2])) & Masks[2]);
            position = (position | (position << Shifts[1])) & Masks[1];
            position = (position | (position << Shifts[0])) & Masks[0];

            /*y = (y | (y << Shifts[3])) & Masks[3];
            y = (y | (y << Shifts[2])) & Masks[2];
            y = (y | (y << Shifts[1])) & Masks[1];
            y = (y | (y << Shifts[0])) & Masks[0];*/

            return new uint2(position.x | (position.y << 1), position.z | (position.w << 1));
        }
        
        public static uint CalculateZOrderForPosition(uint x, uint y)
        {
            return CalculateZOrderForPosition(new uint2(x, y));
        }

        public static int CalculateZOrderForPosition(int x, int y)
        {
            return CalculateZOrderForPosition(new int2(x, y));
        }
        
        public static int CalculateZOrderForPosition(int2 position)
        {

            //int position = position.x;  // Interleave lower 16 bits of x and y, so the bits of x
            //int y = position.y;  // are in the even positions and bits from y in the odd;

            position = ((position | (position << Shifts[3])) & Masks2[3]);
            position = ((position | (position << Shifts[2])) & Masks2[2]);
            position = (position | (position << Shifts[1])) & Masks2[1];
            position = (position | (position << Shifts[0])) & Masks2[0];

            /*y = (y | (y << Shifts[3])) & Masks[3];
            y = (y | (y << Shifts[2])) & Masks[2];
            y = (y | (y << Shifts[1])) & Masks[1];
            y = (y | (y << Shifts[0])) & Masks[0];*/

            return (position.x | (position.y << 1));
        }
        
        
        public static uint2 InvertZOrder(uint z)
        {
            var w = new uint2(z, z >> 1);
            w &= 0x55555555;
            w = (w | (w >> 1)) & 0x33333333; // 0011 0011 0011 0011 0011 0011 0011 0011
            w = (w | (w >> 2)) & 0x0F0F0F0F; // 0000 1111 0000 1111 0000 1111 0000 1111
            w = (w | (w >> 4)) & 0x00FF00FF; // 0000 0000 1111 1111 0000 0000 1111 1111
            w = (w | (w >> 8)) & 0x0000FFFF; // 0000 0000 0000 0000 1111 1111 1111 1111
            return w;
        }
        
        public static int2 InvertZOrder(int z)
        {
            var w = new int2(z, z >> 1);
            w &= 0x55555555;
            w = (w | (w >> 1)) & 0x33333333; // 0011 0011 0011 0011 0011 0011 0011 0011
            w = (w | (w >> 2)) & 0x0F0F0F0F; // 0000 1111 0000 1111 0000 1111 0000 1111
            w = (w | (w >> 4)) & 0x00FF00FF; // 0000 0000 1111 1111 0000 0000 1111 1111
            w = (w | (w >> 8)) & 0x0000FFFF; // 0000 0000 0000 0000 1111 1111 1111 1111
            return w;
        }
        

        public static uint4 InvertZOrderForArea(uint2 z)
        {
            var w = new uint4(z.x, z.x >> 1, z.y, z.y >> 1);
            w = (w & 0xAAAAAAAA) << 31 | (w & 0x55555555); //not entirely sure what the first part does (if it does anything at all)
            w = (w | (w >> 1)) & 0x33333333; // 0011 0011 0011 0011 0011 0011 0011 0011
            w = (w | (w >> 2)) & 0x0F0F0F0F; // 0000 1111 0000 1111 0000 1111 0000 1111
            w = (w | (w >> 4)) & 0x00FF00FF; // 0000 0000 1111 1111 0000 0000 1111 1111
            w = (w | (w >> 8)) & 0x0000FFFF; // 0000 0000 0000 0000 1111 1111 1111 1111
            return w;
        }
        
        public static int4 InvertZOrderForArea(int2 z)
        {
            var w = new int4(z.x, z.x >> 1, z.y, z.y >> 1);
            w = (w & 0xAAAAAAA) << 31 | (w & 0x55555555); //not entirely sure what the first part does (if it does anything at all)
            w = (w | (w >> 1)) & 0x33333333; // 0011 0011 0011 0011 0011 0011 0011 0011
            w = (w | (w >> 2)) & 0x0F0F0F0F; // 0000 1111 0000 1111 0000 1111 0000 1111
            w = (w | (w >> 4)) & 0x00FF00FF; // 0000 0000 1111 1111 0000 0000 1111 1111
            w = (w | (w >> 8)) & 0x0000FFFF; // 0000 0000 0000 0000 1111 1111 1111 1111
            return w;
        }

        public static uint4 InvertZOrderPacked(uint4 zValues)
        {
            zValues = (zValues & 0xAAAAAAAA) << 31 | (zValues & 0x55555555);
            zValues = (zValues | (zValues >> 1)) & 0x33333333; // 0011 0011 0011 0011 0011 0011 0011 0011
            zValues = (zValues | (zValues >> 2)) & 0x0F0F0F0F; // 0000 1111 0000 1111 0000 1111 0000 1111
            zValues = (zValues | (zValues >> 4)) & 0x00FF00FF; // 0000 0000 1111 1111 0000 0000 1111 1111
            zValues = (zValues | (zValues >> 8)) & 0x0000FFFF; // 0000 0000 0000 0000 1111 1111 1111 1111
            return zValues;
        }
        
        public static uint CalculateTop(uint z)
        {
            return (((z & PositionShift.x) - 1) & PositionShift.x) | (z & PositionShift.y);
        }
        
        public static uint CalculateBottom(uint z)
        {
            return (((z | PositionShift.y) + 1) & PositionShift.x) | (z & PositionShift.y);
        }
        
        public static uint CalculateLeft(uint z)
        {
            return (((z & PositionShift.y) - 1) & PositionShift.y) | (z & PositionShift.x);
        }
        
        public static uint CalculateRight(uint z)
        {
            return (((z | PositionShift.x) + 1) & PositionShift.y) | (z & PositionShift.x);
        }

        public static uint CalculateTopLeft(uint z)
        {
            return CalculateLeft(CalculateTop(z));
        }
        
        public static uint CalculateTopRight(uint z)
        {
            return CalculateRight(CalculateTop(z));
        }
        
        public static uint CalculateBottomLeft(uint z)
        {
            return CalculateLeft(CalculateBottom(z));
        }
        
        public static uint CalculateBottomRight(uint z)
        {
            return CalculateRight(CalculateBottom(z));
        }

        public static int SumZValues(int z, int w)
        {
            return ((z | 0b10101010) + (w & 0b01010101) & 0b01010101) |
                   ((z | 0b01010101) + (w & 0b10101010) & 0b10101010);
        }
        
        static uint morton1(uint x)
        {
            x = x & 0x55555555;
            x = (x | (x >> 1)) & 0x33333333;
            x = (x | (x >> 2)) & 0x0F0F0F0F;
            x = (x | (x >> 4)) & 0x00FF00FF;
            x = (x | (x >> 8)) & 0x0000FFFF;
            return x;
        }

// morton2 - extract odd and even bits

        /// <summary>
        /// 
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        public static uint2 morton2(uint z)
        {
            return new uint2(morton1(z), morton1(z >> 1));
        }

        /// <summary>
        /// top, right, bottom, left
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        public static uint4 Nearest4Neighbours(uint z)
        {
            return new uint4(CalculateTop(z), CalculateRight(z), CalculateBottom(z), CalculateLeft(z));
        }
        
        /// <summary>
        /// top left, top right, bottom right, bottom left
        /// </summary>
        /// <param name="z"></param>
        /// <returns></returns>
        public static uint4 DiagonalNeighbours(uint z)
        {
            return new uint4(CalculateTopLeft(z), CalculateTopRight(z), CalculateBottomRight(z), CalculateBottomLeft(z));
        }

        public static uint4x2 Nearest8Neighbours(uint z)
        {
            var straightNeighbours = Nearest4Neighbours(z);
            return new uint4x2(straightNeighbours,
                new uint4(CalculateLeft(straightNeighbours.x), CalculateRight(straightNeighbours.x),
                    CalculateRight(straightNeighbours.z),CalculateLeft(straightNeighbours.z)));
        }
    }
}