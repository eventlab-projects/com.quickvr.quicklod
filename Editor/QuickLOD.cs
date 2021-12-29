using System.Collections.Generic;

using Simplygon;

using UnityEngine;

namespace QuickVR.QuickLOD
{

    [System.Serializable]
    public class QuickLOD : QuickLODBase
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

        protected List<QuickTriangleTextureMap> _triangleTextureMaps = null;

        #endregion

        #region GET AND SET

        public virtual void TestComputeTriangleTextureMap(Mesh mSource)
        {
            float timeStart = Time.realtimeSinceStartup;
            
            QuickTriangleTextureMap tMap = new QuickTriangleTextureMap();
            tMap.ComputeTriangleTextureMap(mSource);

            Debug.Log("timeComputeTriangleTextureMap = " + (Time.realtimeSinceStartup - timeStart).ToString("f3"));
        }

        //protected virtual Texture2D ComputeTriangleTextureMap(Mesh mSource)
        //{
        //    Texture2D result = null;

        //    if (mSource != null)
        //    {
        //        int resolution = 2048;
        //        _newRegionColor = Vector3Int.zero;
        //        RenderTextureDescriptor desc = new RenderTextureDescriptor(resolution, resolution, RenderTextureFormat.ARGB32, 0);
        //        desc.sRGB = false;
        //        RenderTexture renderTexture = RenderTexture.GetTemporary(desc);
        //        RenderTexture currentTexture = RenderTexture.active;
        //        RenderTexture.active = renderTexture;
        //        _materialBake.SetPass(0);

        //        GL.Clear(false, true, Color.black, 1.0f);

        //        HashSet<Vector2Int> test = new HashSet<Vector2Int>();

        //        for (int i = 0; i < mSource.triangles.Length; i += 3)
        //        {
        //            int v0ID = mSource.triangles[i];
        //            int v1ID = mSource.triangles[i + 1];
        //            int v2ID = mSource.triangles[i + 2];

        //            Vector3 c = (mSource.vertices[v0ID] + mSource.vertices[v1ID] + mSource.vertices[v2ID]) / 3.0f;
        //            QuickTriangle t = QuickTriangle.Create(mSource.vertices[v0ID], mSource.vertices[v1ID], mSource.vertices[v2ID]);
        //            Vector3 bCoords = t.ComputeBarycentricCoordinates(c);
        //            Vector2 uv = mSource.uv[v0ID] * bCoords.x + mSource.uv[v1ID] * bCoords.y + mSource.uv[v2ID] * bCoords.z;
        //            Vector2Int pixelCoords = new Vector2Int(Mathf.RoundToInt(resolution * uv.x), Mathf.RoundToInt(resolution * uv.y));
        //            if (test.Contains(pixelCoords))
        //            {
        //                Debug.Log("PIXEL COORDS COLLISION!!!");
        //            }
        //            else
        //            {
        //                test.Add(pixelCoords);
        //            }

        //            Color32 triangleColor = ComputeNewRegionColor();
        //            Mesh m = new Mesh();
        //            m.vertices = new Vector3[] { mSource.vertices[v0ID], mSource.vertices[v1ID], mSource.vertices[v2ID] };
        //            m.normals = new Vector3[] { mSource.normals[v0ID], mSource.normals[v1ID], mSource.normals[v2ID] };
        //            m.uv = new Vector2[] { mSource.uv[v0ID], mSource.uv[v1ID], mSource.uv[v2ID] };
        //            m.colors32 = new Color32[] { triangleColor, triangleColor, triangleColor };
        //            m.triangles = new int[] { 0, 1, 2 };

        //            Graphics.DrawMeshNow(m, Vector3.zero, Quaternion.identity);
        //        }

        //        //SaveTexture(renderTexture, "VertexColors");

        //        // create a second render target 
        //        RenderTexture rt2 = RenderTexture.GetTemporary(desc);
        //        // use the dilate shader on our first render target, output to rt2
        //        for (int i = 0; i < 10; i++)
        //        {
        //            Graphics.Blit(renderTexture, rt2, _materialDilate);
        //            Graphics.Blit(rt2, renderTexture);
        //        }

        //        //SaveTexture(rt2, "VertexColorsDilated");
        //        result = ToTexture2D(rt2);

        //        RenderTexture.active = currentTexture;

        //        RenderTexture.ReleaseTemporary(renderTexture);
        //        RenderTexture.ReleaseTemporary(rt2);

        //    }

        //    return result;
        //}

        protected virtual void ComputeVertexToTriangleMap(Mesh m, int meshID)
        {
            List<Color> colors = new List<Color>();
            float mID = meshID / 255.0f;

            for (int i = 0; i < m.vertexCount; i++)
            {
                colors.Add(new Color(m.uv[i].x, m.uv[i].y, mID));
            }

            m.colors = colors.ToArray();
        }

        protected override QuickTriangleMesh GetClosestTriangle(VertexData vData, Mesh[] mSources, bool print = false)
        {
            //The UV and Mesh ID is codified in the color component of the vertex. 
            int meshID = Mathf.RoundToInt(vData._color.b * 255.0f);

            //Debug.Log(vData._color.ToString("f3"));
            //Debug.Log("meshID = " + meshID);

            QuickTriangleTextureMap triangleMap = _triangleTextureMaps[meshID];
            triangleMap.Sample(vData._color.r, vData._color.g, out List<Color> triangleColors);

            QuickTriangleMesh result = null;
            Mesh m = mSources[meshID];
            foreach (Color tColor in triangleColors)
            {
                int tID = ToTriangleID(tColor);
                if (tID != -1)
                {
                    QuickTriangleMesh tmp = QuickTriangleMesh.Create(m.triangles[tID * 3 + 0], m.triangles[tID * 3 + 1], m.triangles[tID * 3 + 2], m);
                    if (result == null || tmp.Distance2(vData._position) < result.Distance2(vData._position))
                    {
                        result = tmp;
                    }
                }
            }

            return result;
        }

        protected virtual int ToTriangleID(Color tColor)
        {
            int r = Mathf.RoundToInt(tColor.r * 255);
            int g = Mathf.RoundToInt(tColor.g * 255);
            int b = Mathf.RoundToInt(tColor.b * 255);

            return (r * 256 * 256 + g * 256 + b) - 1;
        }

        #endregion

        #region UPDATE

        protected override void InitRenderGroupData(RenderGroup rGroup)
        {
            _triangleTextureMaps = new List<QuickTriangleTextureMap>();
        }

        protected override GameObject Simplify(ISimplygon simplygon, Renderer r, int renderID, float reductionFactor)
        {
            QuickTriangleTextureMap tMap = new QuickTriangleTextureMap();
            tMap.ComputeTriangleTextureMap(r.GetMesh());
            //SaveTexture(tMap, "TriangleMap_" + i.ToString());
            _triangleTextureMaps.Add(tMap);

            GameObject sGO = base.Simplify(simplygon, r, renderID, reductionFactor);

            Debug.Log(sGO);
            ComputeVertexToTriangleMap(sGO.GetComponent<Renderer>().GetMesh(), renderID);

            return sGO;
        }

        #endregion

    }

}
