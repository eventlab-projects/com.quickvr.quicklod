using System.IO;
using System.Collections.Generic;

using Simplygon;

using UnityEngine;

using UnityEditor;

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

        protected List<Texture2D> _triangleTextureMaps = null;

        protected Material _materialBake
        {
            get
            {
                if (m_MaterialBake == null)
                {
                    m_MaterialBake = new Material(Resources.Load<Shader>("BakeVertexColorMap"));
                }

                return m_MaterialBake;
            }
        }
        protected Material m_MaterialBake = null;

        protected Material _materialDilate
        {
            get
            {
                if (m_MaterialDilate == null)
                {
                    m_MaterialDilate = new Material(Resources.Load<Shader>("Dilate"));
                }

                return m_MaterialDilate;
            }
        }
        protected Material m_MaterialDilate = null;

        #endregion

        #region GET AND SET

        protected virtual Texture2D ComputeTriangleTextureMap(Mesh mSource)
        {
            Texture2D result = null;

            if (mSource != null)
            {
                int resolution = 2048;
                _newRegionColor = Vector3Int.zero;
                RenderTextureDescriptor desc = new RenderTextureDescriptor(resolution, resolution, RenderTextureFormat.ARGB32, 0);
                desc.sRGB = false;
                RenderTexture renderTexture = RenderTexture.GetTemporary(desc);
                RenderTexture currentTexture = RenderTexture.active;
                RenderTexture.active = renderTexture;
                _materialBake.SetPass(0);

                GL.Clear(false, true, Color.black, 1.0f);

                for (int i = 0; i < mSource.triangles.Length; i += 3)
                {
                    int v0ID = mSource.triangles[i];
                    int v1ID = mSource.triangles[i + 1];
                    int v2ID = mSource.triangles[i + 2];

                    Color32 triangleColor = ComputeNewRegionColor();
                    Mesh m = new Mesh();
                    m.vertices = new Vector3[] { mSource.vertices[v0ID], mSource.vertices[v1ID], mSource.vertices[v2ID] };
                    m.normals = new Vector3[] { mSource.normals[v0ID], mSource.normals[v1ID], mSource.normals[v2ID] };
                    m.uv = new Vector2[] { mSource.uv[v0ID], mSource.uv[v1ID], mSource.uv[v2ID] };
                    m.colors32 = new Color32[] { triangleColor, triangleColor, triangleColor };
                    m.triangles = new int[] { 0, 1, 2 };

                    Graphics.DrawMeshNow(m, Vector3.zero, Quaternion.identity);
                }

                //SaveTexture(renderTexture, "VertexColors");

                // create a second render target 
                RenderTexture rt2 = RenderTexture.GetTemporary(desc);
                // use the dilate shader on our first render target, output to rt2
                for (int i = 0; i < 10; i++)
                {
                    Graphics.Blit(renderTexture, rt2, _materialDilate);
                    Graphics.Blit(rt2, renderTexture);
                }

                //SaveTexture(rt2, "VertexColorsDilated");
                result = ToTexture2D(rt2);

                RenderTexture.active = currentTexture;

                RenderTexture.ReleaseTemporary(renderTexture);
                RenderTexture.ReleaseTemporary(rt2);

            }

            return result;
        }

        Texture2D ToTexture2D(RenderTexture rTex, TextureFormat tFormat = TextureFormat.RGB24)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, tFormat, false);
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();

            return tex;
        }

        private void SaveTexture(Texture2D tex, string mname)
        {
            string fullPath = Application.dataPath + "/" + mname + ".png";
            byte[] bytes = tex.EncodeToPNG();
            File.Delete(fullPath);
            File.WriteAllBytes(fullPath, bytes);

            AssetDatabase.Refresh();
        }

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

            Texture2D triangleMap = _triangleTextureMaps[meshID];
            Color tColor = triangleMap.Sample(vData._color.r, vData._color.g);

            int tID = ToTriangleID(tColor);

            QuickTriangleMesh result = null;
            if (tID != -1)
            {
                Mesh m = mSources[meshID];

                //Debug.Log("triangleID = " + tID);
                //Debug.Log("numTriangles = " + m.triangles.Length);

                result = QuickTriangleMesh.Create(m.triangles[tID * 3 + 0], m.triangles[tID * 3 + 1], m.triangles[tID * 3 + 2], m);
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
            _triangleTextureMaps = new List<Texture2D>();
        }

        protected override GameObject Simplify(ISimplygon simplygon, Renderer r, int renderID, float reductionFactor)
        {
            Texture2D tMap = ComputeTriangleTextureMap(r.GetMesh());
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
