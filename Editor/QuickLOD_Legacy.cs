using System;
using System.Collections.Generic;

using UnityEngine;

namespace QuickVR.QuickLOD
{

    [System.Serializable]
    public class QuickLOD_Legacy : QuickLODBase
    {

        #region PUBLIC ATTRIBUTES

        public static QuickLOD _instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new QuickLOD();
                }

                return m_Instance;
            }
        }
        private static QuickLOD m_Instance = null;

        #endregion

        #region PROTECTED ATTRIBUTES

        protected QuickOctree<QuickTriangleMesh> _octree = null;
        
        #endregion

        #region CREATION AND DESTRUCTION

        protected virtual void CreateOctree(Mesh[] mSources)
        {
            //Create the bounds of the octree
            Bounds hintBounds = new Bounds();
            List<Vector3> pList = new List<Vector3>();

            hintBounds.center = Vector3.zero;
            foreach (Mesh m in mSources)
            {
                Vector3 min = m.bounds.min;
                Vector3 max = m.bounds.max;

                pList.Add(min);
                pList.Add(max);

                hintBounds.center += min + max;
            }
            hintBounds.center /= pList.Count;

            foreach (Vector3 p in pList)
            {
                hintBounds.Encapsulate(p);
            }
            hintBounds.size *= 1.01f;

            //Create the octree
            _octree = new QuickOctree<QuickTriangleMesh>(hintBounds, 0.05f);

            foreach (Mesh m in mSources)
            {
                for (int i = 0; i < m.triangles.Length; i += 3)
                {
                    QuickOctreeValue<QuickTriangleMesh> v = new QuickOctreeValue<QuickTriangleMesh>();
                    QuickTriangleMesh t = QuickTriangleMesh.Create(m.triangles[i], m.triangles[i + 1], m.triangles[i + 2], m);

                    v._value = t;
                    v._bounds = t.GetBounds();

                    _octree.Insert(v);
                }
            }
        }

        #endregion

        #region GET AND SET

        protected override QuickTriangleMesh GetClosestTriangle(VertexData vData, Mesh[] mSources, bool print = false)
        {
            Vector3 p = vData._position;
            QuickTriangleMesh result = null;
            
            float bSize = 0.001f;
            Bounds pBounds;
            List<QuickTriangleMesh> intersectedTriangles = new List<QuickTriangleMesh>();
            int numIterations = 0;
            do
            {
                pBounds = new Bounds(p, Vector3.one * bSize);
                List<QuickTriangleMesh> tmp = new List<QuickTriangleMesh>();
                _octree.ComputeIntersectedObjects(pBounds, ref tmp);

                foreach (QuickTriangleMesh t in tmp)
                {
                    if (t.HasColor(vData._color) && Vector3.Dot(t._normal, vData._normal) > 0)
                    {
                        intersectedTriangles.Add(t);
                    }
                }

                bSize *= 2.0f;
                numIterations++;
            } while (intersectedTriangles.Count == 0 && numIterations < 10);

            //Look for the closest triangle to p
            float minDist = Mathf.Infinity;

            foreach (QuickTriangleMesh t in intersectedTriangles)
            {
                Vector3 q = t.GetClosestPoint(t.ComputeProjection(p));

                float d = (p - q).sqrMagnitude;

                if (d < minDist)
                {
                    result = t;
                    minDist = d;
                }
            }

            return result;
        }

        protected override QuickTriangleMesh[] ComputeClosestTriangles(Mesh[] mSources, Mesh mTarget)
        {
            CreateOctree(mSources);

            return base.ComputeClosestTriangles(mSources, mTarget);
        }

        #endregion

        #region UPDATE

        public override GameObject Simplify(GameObject goSource, float[] reductionFactors, List<RenderGroup> renderGroupsSource)
        {
            _newRegionColor = Vector3Int.zero;

            return base.Simplify(goSource, reductionFactors, renderGroupsSource);
        }

        protected override void InitRenderGroupData(RenderGroup rGroup)
        {
            foreach (Renderer r in rGroup._renderers)
            {
                ComputeConnectedRegions(r.GetMesh());
            }
        }

        #endregion

        protected virtual void ComputeConnectedRegions(Mesh m)
        {
            //Compute the connected regions of the Mesh m. We use the color component of each vertex
            //to store the value of the region it belongs to. 
            int[] vertexMap = CreateVertexMap(m);

            int newRegionID = 0;
            Dictionary<int, HashSet<int>> regions = new Dictionary<int, HashSet<int>>();  //For each region, it returns the list of all the vertices on that region. 
            Dictionary<int, int> vertexRegion = new Dictionary<int, int>();         //For each vertex, it returns the region that contains that vertex. 
            for (int i = 0; i < m.vertexCount; i++)
            {
                vertexRegion[i] = -1;
            }

            for (int i = 0; i < m.triangles.Length; i += 3)
            {
                int vID0 = m.triangles[i];
                int vID1 = m.triangles[i + 1];
                int vID2 = m.triangles[i + 2];

                int rID0 = vertexRegion[vertexMap[vID0]];
                int rID1 = vertexRegion[vertexMap[vID1]];
                int rID2 = vertexRegion[vertexMap[vID2]];
                int regionID = Mathf.Max(rID0, rID1, rID2);

                if (regionID == -1)
                {
                    //None of the vertices has been assigned yet to a region. Create a new region that contains these 3 vertices. 
                    regions[newRegionID] = new HashSet<int>(new int[] { vID0, vID1, vID2 });
                    vertexRegion[vertexMap[vID0]] = vertexRegion[vertexMap[vID1]] = vertexRegion[vertexMap[vID2]] = newRegionID++;
                }
                else
                {
                    //At least one of the vertex is already part of an existing Region. 
                    //Merge all the regions of the vertices to regionID
                    int[] vertexIDs = { vID0, vID1, vID2 };
                    int[] vRegions = { rID0, rID1, rID2 };

                    for (int j = 0; j < 3; j++)
                    {
                        if (!regions.ContainsKey(regionID))
                        {
                            Debug.Log(regionID);
                        }

                        int vID = vertexIDs[j];
                        regions[regionID].Add(vID);
                        vertexRegion[vertexMap[vID]] = regionID;

                        int rID = vRegions[j];
                        if (rID != -1)
                        {
                            foreach (int v in regions[rID])
                            {
                                regions[regionID].Add(v);
                                vertexRegion[vertexMap[v]] = regionID;
                            }
                        }
                    }

                    //Remove the old regions
                    foreach (int rID in vRegions)
                    {
                        if (rID != regionID)
                        {
                            regions.Remove(rID);
                        }
                    }
                }
            }

            Color32[] colors = new Color32[m.vertexCount];

            //Debug.Log(m.name);
            //Debug.Log("numRegions = " + regions.Count);

            foreach (var pair in regions)
            {
                Color32 color = ComputeNewRegionColor();
                //Debug.Log(color);

                foreach (int vID in pair.Value)
                {
                    colors[vID] = color;
                }
            }

            m.colors32 = colors;
        }

        public class QuickComparerVector3 : IComparer<Vector3>
        {

            public int Compare(Vector3 v1, Vector3 v2)
            {
                if (!Mathf.Approximately(v1.x, v2.x)) return v1.x > v2.x ? 1 : -1;
                if (!Mathf.Approximately(v1.y, v2.y)) return v1.y > v2.y ? 1 : -1;
                if (!Mathf.Approximately(v1.z, v2.z)) return v1.z > v2.z ? 1 : -1;

                return 0;
            }

        }

        protected virtual int FindVector3(Vector3[] array, Vector3 value)
        {
            int result = Array.BinarySearch(array, value, new QuickComparerVector3());

            //The index of the specified value in the specified array, if value is found; otherwise, a negative number.

            //If value is not found and value is less than one or more elements in array, the negative number returned 
            //is the bitwise complement of the index of the first element that is larger than value.

            //If value is not found and value is greater than all elements in array, the negative number returned
            //is the bitwise complement of(the index of the last element plus 1). 

            if (result < 0)
            {
                int next = ~result;
                if (next == array.Length)
                {
                    result = array.Length - 1;
                }
                else
                {
                    int prev = next > 0 ? next - 1 : 0;

                    if (Vector3.SqrMagnitude(value - array[prev]) < Vector3.SqrMagnitude(value - array[next]))
                    {
                        result = prev;
                    }
                    else
                    {
                        result = next;
                    }
                }
            }

            //Check if the selected vertex is close enough to our value
            //if (Vector3.SqrMagnitude(value - array[result]) > 0.001f)
            if (Vector3.Distance(value, array[result]) > 0.001f)
            {
                result = -1;
            }

            return result;
        }

        protected virtual int[] CreateVertexMap(Mesh m)
        {
            int[] result;

            bool uvOverlap = false;
            if (uvOverlap)
            {
                result = CreateVertexMap(m, m);
            }
            else
            {
                result = new int[m.vertexCount];
                for (int i = 0; i < m.vertexCount; i++)
                {
                    result[i] = i;
                }
            }

            return result;
        }

        protected virtual int[] CreateVertexMap(Mesh mSource, Mesh mTarget)
        {
            Vector3[] vSource = new Vector3[mSource.vertices.Length];
            Array.Copy(mSource.vertices, vSource, mSource.vertices.Length);
            Array.Sort(vSource, new QuickComparerVector3());

            //vSource contains the vertices of mSource sorted by its position. 

            int[] tmp = new int[mSource.vertexCount];
            for (int i = 0; i < mSource.vertexCount; i++)
            {
                int idSource = FindVector3(vSource, mSource.vertices[i]);
                if (idSource < 0)
                {
                    Debug.Log(i);
                    Debug.Log(mSource.vertices[i]);
                }
                tmp[idSource] = i;
            }

            int[] result = new int[mTarget.vertexCount];
            for (int i = 0; i < mTarget.vertexCount; i++)
            {
                int idSource = FindVector3(vSource, mTarget.vertices[i]);
                result[i] = idSource >= 0 ? tmp[idSource] : -1;
            }

            return result;
        }

    }

}
