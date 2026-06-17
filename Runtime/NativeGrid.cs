using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BonnFireGames.CustomNativeContainers
{
    public struct NativeGrid<T> : INativeDisposable where T : unmanaged
    {
        
        private static readonly bool _useZOrder = false;
        private readonly int2 _dimensions;
        private NativeArray<T> _data;
        

        public bool IsCreated => _data.IsCreated;
        public int2 Dimensions => _dimensions;

        public NativeGrid(AllocatorManager.AllocatorHandle handle, int2 dimensions, NativeArray<T> data)
        {
            _dimensions = dimensions;
            _data = new NativeArray<T>(data, handle.ToAllocator);
        }

        public T GetPoint(int2 point)
        {
            if (_useZOrder)
            {
                var zOrderValue = ZOrderGenerator.CalculateZOrderForPosition(point);
                return _data[zOrderValue];
            }

            return _data[point.x + point.y * _dimensions.x];
        }
        

        public NativeArray<T> GetArea(int2 startingPoint, int2 dimensions)
        {
            var result = new NativeArray<T>((dimensions.x * dimensions.y), Allocator.Temp);
            
            var startIndex = startingPoint.x + startingPoint.y * _dimensions.x;
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int x = 0; x < dimensions.x; x++)
                {
                    var valueIndex = startIndex + x + y * _dimensions.x;
                    var index = x + y * dimensions.x;
                    result[index] = _data[valueIndex];
                }
            }

            return result;
        }
        
        
        /*public NativeArray<T> GetArea(int2 startingPoint, int2 dimensions)
        {
            var max = startingPoint + dimensions;

            var zPositions = ZOrderGenerator.CalculateZOrderForArea(new int4(startingPoint, max));

            var result = new NativeArray<T>((dimensions.x * dimensions.y), Allocator.Temp);
            

            var consecutiveJunkData = 0;

            for (int zPosition = zPositions.x; zPosition < zPositions.y; zPosition++)
            {
                if (IsRelevant(startingPoint, max, zPosition, out var position))
                {
                    position -= startingPoint;
                    //var position = ZOrderGenerator.InvertZOrder(zPosition) - startingPoint;
                    result[position.x + position.y * dimensions.x] = _zOrderData[zPosition];
                    continue;
                }

                
                consecutiveJunkData++;
                if (consecutiveJunkData > MaxConsecutiveJunkData)
                {
                    consecutiveJunkData = 0;
                    zPosition = NextJumpIn(zPositions, zPosition);
                }

            }
            
            
            
            return result;

        }*/

        
        
        /*private int NextJumpIn(int2 zPositions, int zPosition)
        {
            var leadingZeroCount = new int3(math.lzcnt(zPosition), math.lzcnt(zPositions));
            var min = math.cmin(leadingZeroCount);

            var hasMin = leadingZeroCount == min;
            
            //cannot be 0,0,0 or 1,1,1
            
            //we have cases 001, 011, 101, 100
            
            //if x -> 101, 100; else 001,011
            //if y => 011 else: 101, 100, 001
            //if Z => 001, 011, 101; else: 100

            if (hasMin.x)
            {
                if (hasMin.z)
                {
                    //101
                }
                else
                {
                    //100
                    return zPositions.y;
                }
            }
            else
            {
                if (hasMin.y)
                {
                    //011
                    //BigMin = Min
                    return zPositions.y;
                }
                else
                {
                    //001
                    //bigMin = Load("1000", Min)
                    //Max = Load("0111", Max)
                }
            }

            return zPosition;
        }


        private bool IsRelevant(int2 min, int2 max, int zValue, out int2 position)
        {
            position = ZOrderGenerator.InvertZOrder(zValue);
            return (math.all(position >= min) && math.all(position < max));
        }
        
        private bool IsRelevant(int2 zPositions, int zPosition)
        {
            var leadingZeroCount = new int3(math.lzcnt(zPosition), math.lzcnt(zPositions));
            var min = math.cmin(leadingZeroCount);

            var hasMin = leadingZeroCount == min;

            return math.all(hasMin) || !math.any(hasMin);
        }*/
        
        
        public void Dispose()
        {
            if (_data.IsCreated)
            {
                _data.Dispose();
            }

        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            throw new NotImplementedException();
        }
    }
}