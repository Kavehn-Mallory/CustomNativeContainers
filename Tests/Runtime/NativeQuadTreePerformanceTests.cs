using System;
using BonnFireGames.CustomNativeContainers;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using Unity.PerformanceTesting;
using Random = UnityEngine.Random;

namespace Tests.Runtime
{
    public class NativeQuadTreePerformanceTests
    {
        [Test, Performance]
        public void ComparisonLinearAccessVsQuadTreePrimitiveType()
        {
            var terrainDimensions = new float2(512, 512);
            var gridCellSize = new float2(0.25f, 0.25f);
            var targetAreaSize = new int2(64, 64);
            var gridDimensions = (int2)math.ceil(terrainDimensions / gridCellSize);
            
            var terrainArray = new NativeArray<int>(gridDimensions.x * gridDimensions.y, Allocator.Temp);
            
            

            for (int i = 0; i < terrainArray.Length; i++)
            {
                terrainArray[i] = 1;
            }
            
            
            var area = new NativeArray<int>(terrainArray, Allocator.Temp);

            var quadTree = new UnsafeQuadTree<int>(Allocator.Temp, gridDimensions, area);
            
            for (int i = 0; i < area.Length; i++)
            {
                area[i] = i;
            }
            
            
            var nativeGrid = new NativeGrid<int>(Allocator.Temp, gridDimensions, area);

            
            var targetArea = new int4(Random.Range(0, gridDimensions.x - targetAreaSize.x),
                Random.Range(0, gridDimensions.y - targetAreaSize.y), targetAreaSize);
            
            
            var sg = new SampleGroup("Linear Access - Uniform Values", SampleUnit.Microsecond);
            var sg2 = new SampleGroup("QuadTree - Uniform Values", SampleUnit.Microsecond);
            var sg3 = new SampleGroup("QuadTree - Uniform Values Direct Access", SampleUnit.Microsecond);
            var sg4 = new SampleGroup("NativeGrid - Uniform Values", SampleUnit.Microsecond);
            var sgMarker = new SampleGroup("Linear Access");
            var quadTreeMarker = new SampleGroup("Quad Tree Access");
            var quadTreeMarker2 = new SampleGroup("Quad Tree Access - Direct Access");
            var nativeGridMarker = new SampleGroup("Native Grid - Z hopping");

            NativeArray<int> result1 = default;
            NativeArray<int> result2 = default;
            
            Measure.Method(() =>
            {
                result1 = GetAreaWithLinearAccess(ref area, targetArea, gridDimensions.x);
            }).SampleGroup(sg).ProfilerMarkers(sgMarker)
                .WarmupCount(5)
                .IterationsPerMeasurement(1)
                .MeasurementCount(5)
                .Run();

            Measure.Method(() =>
            {
                _ = GetAreaWithQuadTreeAccess(ref quadTree, targetArea);
            }).SampleGroup(sg2).ProfilerMarkers(quadTreeMarker)
                .WarmupCount(5)
                .IterationsPerMeasurement(1)
                .MeasurementCount(5)
                .Run();
            
            Measure.Method(() =>
                {
                    _ = GetAreaWithQuadTreeAccessDirectPointAccess(ref quadTree, targetArea);
                }).SampleGroup(sg3).ProfilerMarkers(quadTreeMarker2)
                .WarmupCount(5)
                .IterationsPerMeasurement(1)
                .MeasurementCount(5)
                .Run();
            
            Measure.Method(() =>
                {
                    result2 = nativeGrid.GetArea(targetArea.xy, targetAreaSize);
                }).SampleGroup(sg4).ProfilerMarkers(nativeGridMarker)
                .WarmupCount(5)
                .IterationsPerMeasurement(1)
                .MeasurementCount(5)
                .Run();
            
            
            Assert.AreEqual(1, quadTree.NodeCount);
            CollectionAssert.AreEquivalent(result1, result2);
        }
        
        [Test, Performance]
        public void ComparisonLinearAccessVsZOrder()
        {
            var terrainDimensions = new float2(512, 512);
            var gridCellSize = new float2(0.25f, 0.25f);
            var targetAreaSize = new int2(64, 64);
            var gridDimensions = (int2)math.ceil(terrainDimensions / gridCellSize);

            var warmupCount = 10;
            var iterationsPerMeasurement = 100;
            var measurementCount = 10;
            
            var area = new NativeArray<int>(gridDimensions.x * gridDimensions.y, Allocator.Temp);
            
            for (int i = 0; i < area.Length; i++)
            {
                area[i] = i;
            }
            
            
            var nativeGrid = new NativeGrid<int>(Allocator.Temp, gridDimensions, area);

            
            var targetArea = new int4(Random.Range(0, gridDimensions.x - targetAreaSize.x),
                Random.Range(0, gridDimensions.y - targetAreaSize.y), targetAreaSize);
            
            
            var sg = new SampleGroup("Linear Access - Uniform Values", SampleUnit.Microsecond);
            var sg4 = new SampleGroup("NativeGrid - Uniform Values", SampleUnit.Microsecond);
            var sgMarker = new SampleGroup("Linear Access");
            var nativeGridMarker = new SampleGroup("Native Grid - Linear Access");

            NativeArray<int> result1 = default;
            NativeArray<int> result2 = default;
            
            Measure.Method(() =>
            {
                result1 = GetAreaWithLinearAccess(ref area, targetArea, gridDimensions.x);
            }).SampleGroup(sg).ProfilerMarkers(sgMarker)
                .WarmupCount(warmupCount)
                .IterationsPerMeasurement(iterationsPerMeasurement)
                .MeasurementCount(measurementCount)
                .Run();
            
            Measure.Method(() =>
                {
                    result2 = nativeGrid.GetArea(targetArea.xy, targetAreaSize);
                }).SampleGroup(sg4).ProfilerMarkers(nativeGridMarker)
                .WarmupCount(warmupCount)
                .IterationsPerMeasurement(iterationsPerMeasurement)
                .MeasurementCount(measurementCount)
                .Run();
            
            CollectionAssert.AreEquivalent(result1, result2);
        }


        internal struct ComplicatedDataType : IComparable<ComplicatedDataType>
        {
            public half HeightValue;
            public float3 Normal;
            public short GroundType;
            public AdditionalWasteOfSpace WasteOfSpace;

            public int CompareTo(ComplicatedDataType other)
            {
                return GroundType.CompareTo(other.GroundType);
            }
        }

        internal struct AdditionalWasteOfSpace
        {
            public double4x4 TonsOfData;
            public float2 AlignmentRuin;
            public double4x4 TonsOfData2;
            
        }
        
        [Test, Performance]
        public void ComparisonLinearAccessVsQuadTreeStructType()
        {
            var terrainDimensions = new float2(512, 512);
            var gridCellSize = new float2(0.25f, 0.25f);
            var targetAreaSize = new int2(64, 64);
            var gridDimensions = (int2)math.ceil(terrainDimensions / gridCellSize);
            
            var terrainArray = new NativeArray<ComplicatedDataType>(gridDimensions.x * gridDimensions.y, Allocator.Temp);

            for (int i = 0; i < terrainArray.Length; i++)
            {
                terrainArray[i] = new ComplicatedDataType
                {
                    Normal = Random.onUnitSphere,
                    HeightValue = (half)Random.Range(0, half.MaxValue),
                    GroundType = 1,
                };
            }
            
            
            var area = new NativeArray<ComplicatedDataType>(terrainArray, Allocator.Temp);

            var quadTree = new UnsafeQuadTree<ComplicatedDataType>(Allocator.Temp, gridDimensions, area);

            
            var targetArea = new int4(Random.Range(0, gridDimensions.x - targetAreaSize.x),
                Random.Range(0, gridDimensions.y - targetAreaSize.y), targetAreaSize);
            
            
            var sg = new SampleGroup("Linear Access - Uniform Values", SampleUnit.Microsecond);
            var sg2 = new SampleGroup("QuadTree - Uniform Values", SampleUnit.Microsecond);
            var sg3 = new SampleGroup("QuadTree - Uniform Values Direct Access", SampleUnit.Microsecond);
            var sgMarker = new SampleGroup("Linear Access");
            var quadTreeMarker = new SampleGroup("Quad Tree Access");
            var quadTreeMarker2 = new SampleGroup("Quad Tree Access - Direct Access");

            NativeArray<ComplicatedDataType> result1 = default;
            NativeArray<ComplicatedDataType> result2 = default;
            
            Measure.Method(() =>
            {
                result1 = GetAreaWithLinearAccess(ref area, targetArea, gridDimensions.x);
            }).SampleGroup(sg).ProfilerMarkers(sgMarker)
                .WarmupCount(5)
                .IterationsPerMeasurement(1)
                .MeasurementCount(5)
                .Run();

            Measure.Method(() =>
            {
                result2 = GetAreaWithQuadTreeAccess(ref quadTree, targetArea);
            }).SampleGroup(sg2).ProfilerMarkers(quadTreeMarker)
                .WarmupCount(5)
                .IterationsPerMeasurement(1)
                .MeasurementCount(5)
                .Run();

            result2 = default;
            Measure.Method(() =>
                {
                    result2 = GetAreaWithQuadTreeAccessDirectPointAccess(ref quadTree, targetArea);
                }).SampleGroup(sg3).ProfilerMarkers(quadTreeMarker2)
                .WarmupCount(5)
                .IterationsPerMeasurement(1)
                .MeasurementCount(5)
                .Run();
            
            Assert.AreEqual(1, quadTree.NodeCount);
        }
        
        [Test, Performance]
        public void ComparisonLinearAccessVsNativeGridStructType()
        {
            var terrainDimensions = new float2(512, 512);
            var gridCellSize = new float2(0.25f, 0.25f);
            var targetAreaSize = new int2(64, 64);
            var gridDimensions = (int2)math.ceil(terrainDimensions / gridCellSize);
            
            var terrainArray = new NativeArray<ComplicatedDataType>(gridDimensions.x * gridDimensions.y, Allocator.Temp);

            for (int i = 0; i < terrainArray.Length; i++)
            {
                terrainArray[i] = new ComplicatedDataType
                {
                    Normal = Random.onUnitSphere,
                    HeightValue = (half)Random.Range(0, half.MaxValue),
                    GroundType = 1,
                };
            }
            
            
            var area = new NativeArray<ComplicatedDataType>(terrainArray, Allocator.Temp);

            var nativeGrid = new NativeGrid<ComplicatedDataType>(Allocator.Temp, gridDimensions, area);

            
            var targetArea = new int4(Random.Range(0, gridDimensions.x - targetAreaSize.x),
                Random.Range(0, gridDimensions.y - targetAreaSize.y), targetAreaSize);
            
            
            var sg = new SampleGroup("Linear Access - Uniform Values", SampleUnit.Microsecond);
            var sg3 = new SampleGroup("Native Grid - Uniform Values Linear Access", SampleUnit.Microsecond);
            var sgMarker = new SampleGroup("Linear Access");
            var nativeGridMarker2 = new SampleGroup("Native Grid Access - Linear Access");

            NativeArray<ComplicatedDataType> result1 = default;
            NativeArray<ComplicatedDataType> result2 = default;
            
            Measure.Method(() =>
            {
                result1 = GetAreaWithLinearAccess(ref area, targetArea, gridDimensions.x);
            }).SampleGroup(sg).ProfilerMarkers(sgMarker)
                .WarmupCount(5)
                .IterationsPerMeasurement(1)
                .MeasurementCount(5)
                .Run();


            
            Measure.Method(() =>
                {
                    result2 = nativeGrid.GetArea(targetArea.xy, targetAreaSize);
                }).SampleGroup(sg3).ProfilerMarkers(nativeGridMarker2)
                .WarmupCount(5)
                .IterationsPerMeasurement(1)
                .MeasurementCount(5)
                .Run();
            
        }

        private NativeArray<T> GetAreaWithLinearAccess<T>(ref NativeArray<T> values, int4 targetArea, int width) where T : unmanaged
        {
            var result = new NativeArray<T>(targetArea.z * targetArea.w, Allocator.Temp);

            var startIndex = targetArea.x + targetArea.y * width;
            for (int y = 0; y < targetArea.w; y++)
            {
                for (int x = 0; x < targetArea.z; x++)
                {
                    var index = x + y * targetArea.z;
                    var valueIndex = startIndex + x + y * width;

                    result[index] = values[valueIndex];
                } 
            }

            return result;
        }
        
        private NativeArray<T> GetAreaWithQuadTreeAccess<T>(ref UnsafeQuadTree<T> values, int4 targetArea) where T : unmanaged, IComparable<T>
        {
            var result = new NativeArray<T>(targetArea.z * targetArea.w, Allocator.Temp);

            var parentNode = values.FindParentNodeForArea(targetArea.xy, targetArea.xy + targetArea.zw);
            var startPoint = targetArea.xy;
            var node = values.FindNodeForPoint(startPoint, parentNode);
            for (int y = 0; y < targetArea.w; y++)
            {
                for (int x = 0; x < targetArea.z; x++)
                {
                    var index = x + y * targetArea.z;
                    
                    if (!values.TryGetValueFromNode(node, startPoint + new int2(x, y), out var value))
                    {
                        node = values.FindNodeForPoint(startPoint + new int2(x, y), parentNode);
                        value = values.GetValueFromNode(node, startPoint + new int2(x, y));
                    }
                    result[index] = value;
                    
                } 
            }

            return result;
        }
        
        private NativeArray<T> GetAreaWithQuadTreeAccessDirectPointAccess<T>(ref UnsafeQuadTree<T> values, int4 targetArea) where T : unmanaged, IComparable<T>
        {
            var result = new NativeArray<T>(targetArea.z * targetArea.w, Allocator.Temp);

            var parentNode = values.FindParentNodeForArea(targetArea.xy, targetArea.xy + targetArea.zw);
            var startPoint = targetArea.xy;
            var node = values.FindNodeForPoint(startPoint, parentNode);
            for (int y = 0; y < targetArea.w; y++)
            {
                for (int x = 0; x < targetArea.z; x++)
                {
                    result[x + y * targetArea.z] = values.GetValue(node, startPoint + new int2(x, y));
                    
                } 
            }

            return result;
        }
        
        [Test, Performance]
        public void ComparisonLinearAccessVsQuadTreeRandomPositions()
        {
            var numberOfElements = 10;
            var terrainDimensions = new float2(100, 100);
            var gridCellSize = new float2(0.25f, 0.25f);
            var gridDimensions = (int2)math.ceil(terrainDimensions / gridCellSize);
            
            var terrainArray = new NativeArray<int>(gridDimensions.x * gridDimensions.y, Allocator.Temp);

            for (int i = 0; i < terrainArray.Length; i++)
            {
                terrainArray[i] = 1;
            }
            
            
            var area = new NativeArray<int>(terrainArray, Allocator.Temp);

            var quadTree = new UnsafeQuadTree<int>(Allocator.Temp, gridDimensions, area);

            
            
            var sg = new SampleGroup("Linear Access - Uniform Values", SampleUnit.Microsecond);
            var sg2 = new SampleGroup("QuadTree - Uniform Values", SampleUnit.Microsecond);
            var sgMarker = new SampleGroup("Linear Access");
            var quadTreeMarker = new SampleGroup("Quad Tree Access");

            var results = new NativeList<int>(9, Allocator.Temp);
            
            Measure.Method(() =>
                {
                    
                    for (int i = 0; i < numberOfElements; i++)
                    {
                        results.Clear();
                        var x = Random.Range(0, gridDimensions.x);
                        var y = Random.Range(0, gridDimensions.y);
                        var index = x + y * gridDimensions.x;
                        results.Add(area[index]);
                        results.Add(area[math.min(area.Length - 1, index + 1)]);
                        results.Add(area[math.min(area.Length - 1, index + gridDimensions.x + 1)]);
                        results.Add(area[math.min(area.Length - 1, index + gridDimensions.x)]);
                        results.Add(area[math.min(area.Length - 1, index + gridDimensions.x - 1)]);
                        
                        results.Add(area[math.max(0, index - 1)]);
                        results.Add(area[math.max(0, index - gridDimensions.x + 1)]);
                        results.Add(area[math.max(0, index - gridDimensions.x)]);
                        results.Add(area[math.max(0, index - gridDimensions.x - 1)]);
                    }
                    
                }).SampleGroup(sg).ProfilerMarkers(sgMarker)
                .WarmupCount(5)
                .IterationsPerMeasurement(10)
                .MeasurementCount(10)
                .Run();

            Measure.Method(() =>
                {
                    var maxPosition = gridDimensions - new int2(1, 1);
                    for (int i = 0; i < numberOfElements; i++)
                    {
                        results.Clear();
                        var point = new int2(Random.Range(0, gridDimensions.x), Random.Range(0, gridDimensions.y));
                        //var node = quadTree.FindNodeForPoint(point);
                        results.Add(quadTree.GetValue(point));
                        
                        results.Add(quadTree.GetValue(math.min(maxPosition, point + new int2(1, 0))));
                        results.Add(quadTree.GetValue(math.min(maxPosition, point + new int2(-1, gridDimensions.y))));
                        results.Add(quadTree.GetValue(math.min(maxPosition, point + new int2(0, gridDimensions.y))));
                        results.Add(quadTree.GetValue(math.min(maxPosition, point + new int2(1, gridDimensions.y))));
                        
                        results.Add(quadTree.GetValue(math.max(int2.zero, point - new int2(1, 0))));
                        results.Add(quadTree.GetValue(math.max(int2.zero, point - new int2(-1, gridDimensions.y))));
                        results.Add(quadTree.GetValue(math.max(int2.zero, point - new int2(0, gridDimensions.y))));
                        results.Add(quadTree.GetValue(math.max(int2.zero, point - new int2(1, gridDimensions.y))));
                    }
                
                }).SampleGroup(sg2).ProfilerMarkers(quadTreeMarker)
                .WarmupCount(5)
                .IterationsPerMeasurement(10)
                .MeasurementCount(10)
                .Run();
            
        }
    }
}