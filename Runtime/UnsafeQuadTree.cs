using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace BonnFireGames.CustomNativeContainers
{
    
    [BurstCompile]
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct UnsafeQuadTree<T> : IDisposable where T : unmanaged, IComparable<T>
    {
        
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;

        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<UnsafeQuadTree<T>>();
#endif
        
        [NativeDisableUnsafePtrRestriction]
        public TreeNode* _nodes;
        [NativeDisableUnsafePtrRestriction]
        public T* _data;

        private readonly int2 _dimensions;
        private int _nodeCount;
        private int _dataCount;
        

        public int2 Dimensions => _dimensions;


        public int NodeCount => _nodeCount;
        public int DataCount => _dataCount;
        
        public bool IsCreated => _nodes != null;

        private AllocatorManager.AllocatorHandle _handle;
        

        public UnsafeQuadTree(AllocatorManager.AllocatorHandle handle, int2 dimensions, NativeArray<T> data)
        {
            _dimensions = math.ceilpow2(dimensions);
            _dimensions = math.cmax(_dimensions);
            var nodes = new NativeList<TreeNode>(0, Allocator.Temp);
            var actualData = new NativeList<T>(0, Allocator.Temp);
            _handle = handle;
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = CollectionHelper.CreateSafetyHandle(handle);

            InitNativeContainer(m_Safety);
            CollectionHelper.SetStaticSafetyId<UnsafeQuadTree<T>>(ref m_Safety, ref s_staticSafetyId.Data);
#endif
            
            if (data.Length <= 0)
            {
                _nodes = null;
                _data = null;
                _dataCount = 0;
                _nodeCount = 0;
                return;
            }
            
            var dataStorage = data;

            var elementCount = _dimensions.x * _dimensions.y;
            if (data.Length != elementCount)
            {
                dataStorage = new NativeArray<T>(elementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

                for (int y = 0; y < _dimensions.y; y++)
                {
                    for (int x = 0; x < _dimensions.x; x++)
                    {
                        var index = x + y * _dimensions.x;
                        if (x < dimensions.x && y < dimensions.y)
                        {
                            var oldIndex = x + y * dimensions.x;
                            dataStorage[index] = data[oldIndex];
                        }
                        else
                        {
                            dataStorage[index] = new T();
                        }
                    }
                }
            }
            
            var orderedData = new NativeArray<T>(dataStorage.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int y = 0; y < _dimensions.y; y++)
            {
                for (int x = 0; x < _dimensions.x; x++)
                {
                    var zOrderValue = ZOrderGenerator.CalculateZOrderForPosition(new int2(x, y));
                    var index = x + y * _dimensions.x;
                    orderedData[zOrderValue] = dataStorage[index];
                }
            }
            
            CreateTreeBottomUp(orderedData.AsReadOnly(), _dimensions, ref nodes, ref actualData);


            _nodes = AllocatorManager.Allocate<TreeNode>(_handle, nodes.Length);

            for (int i = 0; i < nodes.Length; i++)
            {
                _nodes[i] = nodes[i];
            }
            
            _data = AllocatorManager.Allocate<T>(_handle, actualData.Length);

            for (int i = 0; i < actualData.Length; i++)
            {
                _data[i] = actualData[i];
            }

            _dataCount = actualData.Length;
            _nodeCount = nodes.Length;
            
            
            nodes.Dispose();
            actualData.Dispose();

        }
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", GenericTypeArguments = new[] { typeof(NativeArray<int>) })]
        internal static void InitNativeContainer(AtomicSafetyHandle handle)
        {
            if (UnsafeUtility.IsNativeContainerType<T>())
                AtomicSafetyHandle.SetNestedContainer(handle, true);
        }
#endif

        private static void CreateTreeBottomUp(NativeArray<T>.ReadOnly data, int2 dimensions, ref NativeList<TreeNode> nodes, ref NativeList<T> elements)
        {
            //preprocess the data 
            var areaSize = new int2(2, 2);
            
            for (int i = 0; i < data.Length; i+=4)
            {
                var p0 = data[i];
                var p1 = data[i + 1];
                var p2 = data[i + 2];
                var p3 = data[i + 3];
                var elementCount = 4;
                if (p0.CompareTo(p1) == 0 && p1.CompareTo(p2) == 0 && p2.CompareTo(p3) == 0)
                {
                    //good region
                    elementCount = 1;
                }
                nodes.Add(new TreeNode
                {
                    ElementCount = elementCount,
                    FirstChildIndex = i
                });
            }
            
            while (math.all(areaSize < dimensions))
            {
                var cellCountPreviousLevel = (dimensions / areaSize).x * (dimensions / areaSize).y;
                areaSize *= 2;
                var elementCountForLevel = areaSize.x * areaSize.y;
                
                var cellCountToInsert = cellCountPreviousLevel / 4;
                var insertionIndex = cellCountToInsert - 1;
                nodes.InsertRange(0, cellCountToInsert);
                
                
                //start index is at the first element that we haven't inserted + cellCountOfPreviousLevel
                for (int i = cellCountToInsert + cellCountPreviousLevel - 1; i >= cellCountToInsert; i -= 4)
                {
                    var p3 = nodes[i];
                    var p2 = nodes[i - 1];
                    var p1 = nodes[i - 2];
                    var p0 = nodes[i - 3];
                    
                    var firstChildIndex = nodes.Length - (i - 3);
                    
                    if ((ShouldCollapseNodes(ref data, p0, p1, p2, p3, elementCountForLevel, out var childCount)))
                    {
                        //collapse nodes
                        firstChildIndex = p0.FirstChildIndex;
                        nodes.RemoveAt(i);
                        nodes.RemoveAt(i - 1);
                        nodes.RemoveAt(i - 2);
                        nodes.RemoveAt(i - 3);
                    }
                    nodes[insertionIndex] = new TreeNode 
                    {
                        ElementCount = childCount,
                        FirstChildIndex = firstChildIndex
                    };

                    insertionIndex--;
                }
                Assert.AreEqual(-1, insertionIndex);
            }

            //change indices 
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                var firstChildIndex = nodes.Length - node.FirstChildIndex;
                if (node.ElementCount >= 0)
                {
                    
                    firstChildIndex = elements.Length;
                    
                    for (var elementIndex = 0; elementIndex < node.ElementCount; elementIndex++)
                    {
                        Assert.AreNotEqual(-1, node.FirstChildIndex + elementIndex);
                        elements.Add(data[node.FirstChildIndex + elementIndex]);
                    }
                }

                node.FirstChildIndex = firstChildIndex;

                nodes[i] = node;
            }

        }

        private static bool ShouldCollapseNodes(ref NativeArray<T>.ReadOnly data, TreeNode p0, TreeNode p1, TreeNode p2, TreeNode p3, int elementCountForLevel, out int numberOfChildObjects)
        {
            
            var elementCount = new int4(p0.ElementCount, p1.ElementCount, p2.ElementCount, p3.ElementCount);
            //Add 4 for the four nodes themselves. This should reduce undesirable node-quadrants where only one node combines to a new one
            if (math.csum(elementCount) + 4 >= elementCountForLevel)
            {
                //nodes need to store all their children, we can also combine that in a higher node
                numberOfChildObjects = elementCountForLevel;
                return true;
            }
            
            

            if (p0.ElementCount <= 0 || p1.ElementCount <= 0 || p2.ElementCount <= 0 || p3.ElementCount <= 0)
            {
                //cannot combine these regions bc at least one is already not a leaf-region
                numberOfChildObjects = -1;
                return false;
            }

            //all regions are represented by exactly one element, we need to check if we can combine those regions
            if (math.cmax(elementCount) == 1)
            {
                var t0 = data[p0.FirstChildIndex];
                var t1 = data[p0.FirstChildIndex];
                var t2 = data[p0.FirstChildIndex];
                var t3 = data[p0.FirstChildIndex];
                
                //are all regions the same?
                numberOfChildObjects = (t0.CompareTo(t1) == 0 && t1.CompareTo(t2) == 0 && t2.CompareTo(t3) == 0) ? 1 : -1;
                return numberOfChildObjects == 1;
            }

            numberOfChildObjects = -1;
            return false;
        }
        

        public NativeQueue<QuadNodeData> FindLeavesForArea(int2 min, int2 max)
        {
            return FindLeaves(_nodes, GenerateChildData(new int2(), _dimensions, 0, 0),
                new int4(min, max));
        }
        
        
        private static NativeQueue<QuadNodeData> FindLeaves(TreeNode* nodes, QuadNodeData root, int4 bounds)
        {
            NativeQueue<QuadNodeData> leaves = new NativeQueue<QuadNodeData>(Allocator.Temp);
            NativeQueue<QuadNodeData> toProcess = new NativeQueue<QuadNodeData>(Allocator.Temp);
            toProcess.Enqueue(root);
            
            while (toProcess.Count > 0)
            {
                var node = toProcess.Dequeue();

                // If this node is a leaf, insert it to the list.
                if (nodes[node.Index].ElementCount != -1)
                    leaves.Enqueue(node);
                else
                {
                    // Otherwise push the children that intersect the rectangle.
                    int fc = nodes[node.Index].FirstChildIndex;

                    var nodeHeight = new int4(node.Bounds.xy, node.Bounds.zw >> 1);
                    //                          left (x)         top (y)        right (z)       bottom (w)
                    var dimensions = new int4(nodeHeight.xy - nodeHeight.zw, nodeHeight.xy + nodeHeight.zw);

                    //                              X               Y           Z               W
                    //                          x <= centerX, z <=centerX, y <= centerY, w <= centerY
                    var comparison = new bool4(bounds.xz <= nodeHeight.x, bounds.yw <= nodeHeight.y);

                    //yMin <= centerY
                    if (comparison.z)
                    {
                        //xMin <= centerX
                        if (comparison.x)
                            toProcess.Enqueue(GenerateChildData(dimensions.xy, nodeHeight.zw, fc+0, node.Depth+1));
                        //!(xMax <= centerX) => xMax > centerX
                        if (!comparison.y)
                            toProcess.Enqueue(GenerateChildData(dimensions.zy, nodeHeight.zw, fc+1, node.Depth+1));
                    }
                    // !(yMax <= centerY) => yMax > centerY
                    if (!comparison.w)
                    {
                        if (comparison.x)
                            toProcess.Enqueue(GenerateChildData(dimensions.xw, nodeHeight.zw, fc+2, node.Depth+1));
                        if (!comparison.y)
                            toProcess.Enqueue(GenerateChildData(dimensions.zw, nodeHeight.zw, fc+3, node.Depth+1));
                    }
                }
            }
            return leaves;
        }

        private static QuadNodeData GenerateChildData(int2 minPosition, int2 dimensions, int index, int depth)
        {
            return new QuadNodeData
            {
                Bounds = new int4((minPosition + dimensions) / 2, dimensions),
                Index = index,
                Depth = depth
            };
        }



        [BurstCompile]
        public int FindNodeIndexForPoint(int2 point)
        {
            var nodeMin = new int2();
            var currentIndex = 0;
            var nodeDim = _dimensions;
            

            //do we even need to go lower or are we done?
            while (_nodes[currentIndex].ElementCount < 0)
            {
                var firstChild = _nodes[currentIndex].FirstChildIndex;
                nodeDim /= 2;

                var centerPoint = nodeMin + nodeDim;

                var result = centerPoint.xy >= point;

                if (math.all(result))
                {
                    //top left
                    currentIndex = firstChild;
                }
                else if (!math.any(result))
                {
                    //bottom right
                    currentIndex = firstChild + 3;
                    nodeMin = centerPoint;
                }
                else if (result.x)
                {
                    //bottom left I think because we can only be in the bottom half and the center point is rightwards of our minimum point (and we are within one quadrant)
                    currentIndex = firstChild + 2;
                    nodeMin.y = centerPoint.y;
                }
                else
                {
                    //top right?
                    currentIndex = firstChild + 1;
                    nodeMin.x = centerPoint.x;
                }
            }
            

            return currentIndex;
        }

        [BurstCompile]
        public T GetValue(int2 point)
        {
            var nodeMin = new int2();
            var currentIndex = 0;
            var nodeDim = _dimensions;
            

            //do we even need to go lower or are we done?
            while (_nodes[currentIndex].ElementCount < 0)
            {
                var firstChild = _nodes[currentIndex].FirstChildIndex;
                nodeDim /= 2;

                var centerPoint = nodeMin + nodeDim;
                
                var result = centerPoint.xy > point;

                if (math.all(result))
                {
                    //top left
                    currentIndex = firstChild;
                }
                else if (!math.any(result))
                {
                    //bottom right
                    currentIndex = firstChild + 3;
                    nodeMin = centerPoint;
                }
                else if (result.x)
                {
                    //bottom left I think because we can only be in the bottom half and the center point is rightwards of our minimum point (and we are within one quadrant)
                    currentIndex = firstChild + 2;
                    nodeMin.y = centerPoint.y;
                }
                else
                {
                    //top right?
                    currentIndex = firstChild + 1;
                    nodeMin.x = centerPoint.x;
                }
            }
            

            var node = _nodes[currentIndex];
            if (node.ElementCount > 1)
            {
                var offset = ZOrderGenerator.CalculateZOrderForPosition(point - nodeDim.xy);
                return _data[node.FirstChildIndex + offset];
            }

            return _data[node.FirstChildIndex];
        }
        
        [BurstCompile]
        public T GetValue(NodeData startingPoint, int2 point)
        {
            var nodeMin = startingPoint.Dimensions.xy;
            var currentIndex = startingPoint.Index;
            var nodeDim = startingPoint.Dimensions.zw;
            

            //do we even need to go lower or are we done?
            while (_nodes[currentIndex].ElementCount < 0)
            {
                var firstChild = _nodes[currentIndex].FirstChildIndex;
                nodeDim /= 2;

                var centerPoint = nodeMin + nodeDim;
                
                var result = centerPoint.xy > point;

                if (math.all(result))
                {
                    //top left
                    currentIndex = firstChild;
                }
                else if (!math.any(result))
                {
                    //bottom right
                    currentIndex = firstChild + 3;
                    nodeMin = centerPoint;
                }
                else if (result.x)
                {
                    //bottom left I think because we can only be in the bottom half and the center point is rightwards of our minimum point (and we are within one quadrant)
                    currentIndex = firstChild + 2;
                    nodeMin.y = centerPoint.y;
                }
                else
                {
                    //top right?
                    currentIndex = firstChild + 1;
                    nodeMin.x = centerPoint.x;
                }
            }
            

            var node = _nodes[currentIndex];
            if (node.ElementCount > 1)
            {
                var offset = ZOrderGenerator.CalculateZOrderForPosition(point - nodeDim.xy);
                return _data[node.FirstChildIndex + offset];
            }

            return _data[node.FirstChildIndex];
        }
        
        [BurstCompile]
        public NodeData FindNodeForPoint(int2 point)
        {
            var nodeMin = new int2();
            var currentIndex = 0;
            var nodeDim = _dimensions;
            

            //do we even need to go lower or are we done?
            while (_nodes[currentIndex].ElementCount < 0)
            {
                var firstChild = _nodes[currentIndex].FirstChildIndex;
                nodeDim /= 2;

                var centerPoint = nodeMin + nodeDim;
                
                var result = centerPoint.xy > point;

                if (math.all(result))
                {
                    //top left
                    currentIndex = firstChild;
                }
                else if (!math.any(result))
                {
                    //bottom right
                    currentIndex = firstChild + 3;
                    nodeMin = centerPoint;
                }
                else if (result.x)
                {
                    //bottom left I think because we can only be in the bottom half and the center point is rightwards of our minimum point (and we are within one quadrant)
                    currentIndex = firstChild + 2;
                    nodeMin.y = centerPoint.y;
                }
                else
                {
                    //top right?
                    currentIndex = firstChild + 1;
                    nodeMin.x = centerPoint.x;
                }
            }
            

            return new NodeData
            {
                Index = currentIndex,
                Dimensions = new int4(nodeMin, nodeDim)
            };
        }

        [BurstCompile]
        public T GetValueFromNode(NodeData data, int2 point)
        {
            if (TryGetValueFromNode(data, point, out var result))
            {
                return result;
            }

            throw new ArgumentOutOfRangeException(nameof(data), $"{nameof(NodeData)} does not contain point {point}. Node covers {data.Dimensions}");
        }

        [BurstCompile]
        public bool TryGetValueFromNode(NodeData data, int2 point, out T value)
        {
            value = default;
            if (data.Contains(point))
            {
                var node = _nodes[data.Index];
                if (node.ElementCount > 1)
                {
                    var offset = ZOrderGenerator.CalculateZOrderForPosition(point - data.Dimensions.xy);
                    value = _data[node.FirstChildIndex + offset];
                    return true;
                }

                value = _data[node.FirstChildIndex];
                return true;
            }

            return false;
        }
        
        [BurstCompile]
        public NodeData FindNodeForPoint(int2 point, NodeData startNode)
        {
            var nodeMin = startNode.Dimensions.xy;
            var currentIndex = startNode.Index;
            var nodeDim = startNode.Dimensions.zw;
            

            //do we even need to go lower or are we done?
            while (_nodes[currentIndex].ElementCount < 0)
            {
                var firstChild = _nodes[currentIndex].FirstChildIndex;
                nodeDim /= 2;

                var centerPoint = nodeMin + nodeDim;

                //center needs to be bigger than the point bc the center is already part of the next area
                var result = centerPoint.xy > point;

                if (math.all(result))
                {
                    //top left
                    currentIndex = firstChild;
                }
                else if (!math.any(result))
                {
                    //bottom right
                    currentIndex = firstChild + 3;
                    nodeMin = centerPoint;
                }
                else if (result.x)
                {
                    //bottom left I think because we can only be in the bottom half and the center point is rightwards of our minimum point (and we are within one quadrant)
                    currentIndex = firstChild + 2;
                    nodeMin.y = centerPoint.y;
                }
                else
                {
                    //top right?
                    currentIndex = firstChild + 1;
                    nodeMin.x = centerPoint.x;
                }
            }
            

            return new NodeData
            {
                Index = currentIndex,
                Dimensions = new int4(nodeMin, nodeDim)
            };
        }


        /// <summary>
        /// Returns the node that completely encapsulates the target area 
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        [BurstCompile]
        public NodeData FindParentNodeForArea(int2 min, int2 max)
        {
            var nodeMin = new int2();
            var currentIndex = 0;
            var nodeDim = _dimensions;

            var target = new int4(min, max);

            //do we even need to go lower or are we done?
            while (_nodes[currentIndex].ElementCount < 0)
            {
                
                nodeDim /= 2;
                var centerPoint = nodeMin + nodeDim;
                var result = centerPoint.xyxy > target;

                if (!math.all(result.xy == result.zw))
                {
                    //means min and max land in different quadrants -> we stay with this node and exit early 
                    
                    return new NodeData
                    {
                        Index = currentIndex,
                        Dimensions = new int4(nodeMin, nodeDim * 2)
                    };
                }
                
                var firstChild = _nodes[currentIndex].FirstChildIndex;
                if (math.all(result))
                {
                    //top left
                    currentIndex = firstChild;
                }
                else if (!math.any(result))
                {
                    //bottom right
                    currentIndex = firstChild + 3;
                    nodeMin = centerPoint;
                }
                else if (result.x)
                {
                    //bottom left I think because we can only be in the bottom half and the center point is rightwards of our minimum point (and we are within one quadrant)
                    currentIndex = firstChild + 2;
                    nodeMin.y = centerPoint.y;
                }
                else
                {
                    //top right?
                    currentIndex = firstChild + 1;
                    nodeMin.x = centerPoint.x;
                }
            }
            

            return new NodeData
            {
                Index = currentIndex,
                Dimensions = new int4(nodeMin, nodeDim)
            };
        }

        /// <summary>
        /// Returns the tree node at the given index
        /// A tree node represents a spacial partition segment of the quad tree and can either be the parent of four TreeNodes or contain actual data 
        /// </summary>
        /// <param name="index"></param>
        public unsafe TreeNode this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get
            {
                this.CheckElementReadAccess(index);
                return UnsafeUtility.ReadArrayElement<TreeNode>(this._nodes, index);
            }
        }

        /// <summary>
        /// Returns the value for a given tree node if the tree node is a leaf node 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="index"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public unsafe T this[TreeNode node, int index]
        {
            get
            {
                if (node.ElementCount < 0)
                {
                    throw new ArgumentException($"TreeNode is not a leaf node");
                }

                if (node.ElementCount <= index)
                {
                    throw new IndexOutOfRangeException(
                        $"Index {index} is out of range of '{node.ElementCount}' Length.");
                }
                return UnsafeUtility.ReadArrayElement<T>(this._data, node.FirstChildIndex + index);
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckElementReadAccess(int index)
        {
            if (index < 0 || index > _nodeCount)
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range of '{NodeCount}' Length.");
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
        }

        public void Dispose()
        {

            AllocatorManager.Free(_handle, _nodes, _nodeCount);
            AllocatorManager.Free(_handle, _data, _dataCount);
            
            _nodes = null;
            _data = null;
            _dataCount = 0;
            _nodeCount = 0;
        }
        

        public struct TreeNode
        {
            public int FirstChildIndex;
            public int ElementCount;
        }
        

        public void DrawDebugView(float2 size,float multiplier = 1)
        {


            var center = new float3();

            var areaSize = new float3(size.x, 0, size.y);
            
            Gizmos.DrawWireCube(center, areaSize);


            var topLeft = new float3(-1f / 4f, 0, 1f / 4f);
            var topRight = new float3(1f / 4f, 0, 1f / 4f);
            var bottomLeft = new float3(-1f / 4f, 0, -1f / 4f);
            var bottomRight = new float3(1f / 4f, 0, -1f / 4f);
            
            var elements = new List<(TreeNode node, float3 center, Vector3 dimensions, Color color)>();
            elements.Add((_nodes[0], center, areaSize, new Color(1, 1, 1, 0.5f)));
            
            while (elements.Count > 0)
            {
                var currentElement = elements[0];
                elements.RemoveAt(0);
                
                var dimensions = currentElement.dimensions / 2f;
                if (currentElement.node.ElementCount < 0)
                {
                    //Debug.Log(startElement.Item1.FirstChildIndex);
                    elements.Add((_nodes[currentElement.node.FirstChildIndex],
                        currentElement.center + (currentElement.dimensions * topLeft), dimensions, new Color(1f, 0, 0, 0.5f)));
                    elements.Add((_nodes[currentElement.node.FirstChildIndex + 1],
                        currentElement.center + (currentElement.dimensions * topRight), dimensions, new Color(0f, 1f, 0, 0.5f)));
                    elements.Add((_nodes[currentElement.node.FirstChildIndex + 2],
                        currentElement.center + (currentElement.dimensions * bottomLeft), dimensions, new Color(1f, 1f, 0, 0.5f)));
                    elements.Add((_nodes[currentElement.node.FirstChildIndex + 3],
                        currentElement.center + (currentElement.dimensions * bottomRight), dimensions, new Color(0f, 0, 1f, 0.5f)));
                    
                }
                else
                {
                    //Debug.Log($"Node represents {startElement.Item1.ElementCount} elements. First element is {ActualData[startElement.Item1.FirstChildIndex]}");
                    Gizmos.color = currentElement.color;
                    Gizmos.DrawSphere(currentElement.center, 0.05f);
                    Gizmos.DrawCube(currentElement.center, currentElement.dimensions * multiplier);
                }
                
                
            }
        }


        
    }
}