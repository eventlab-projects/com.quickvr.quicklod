using System.IO;
using System.Collections.Generic;

using UnityEngine;

namespace QuickVR.QuickLOD
{

    [System.Serializable]
    public class QuickTriangleTextureMap
    {

        #region PROTECTED ATTRIBUTES

        protected List<RenderTexture> _renderTextures = null;
        protected List<QuickQuadtree<QuickTriangle>> _quadtrees = null;
        protected List<Texture2D> _textureMaps = null;

        protected Vector3Int _newRegionColor = Vector3Int.zero;

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

        #region CREATION AND DESTRUCTION

        protected virtual RenderTexture CreateRenderTexture(int resolution)
        {
            RenderTextureDescriptor desc = new RenderTextureDescriptor(resolution, resolution, RenderTextureFormat.ARGB32, 0);
            desc.sRGB = false;
            RenderTexture renderTexture = RenderTexture.GetTemporary(desc);
            RenderTexture currentTexture = RenderTexture.active;

            RenderTexture.active = renderTexture;
            GL.Clear(false, true, Color.black, 1.0f);
            RenderTexture.active = currentTexture;

            return renderTexture;
        }

        protected virtual QuickQuadtree<QuickTriangle> CreateQuadtree(int resolution)
        {
            Bounds hintBounds = new Bounds();
            hintBounds.SetMinMax(Vector3.zero, new Vector3(resolution, resolution, 0));

            return new QuickQuadtree<QuickTriangle>(hintBounds, 64);
        }

        #endregion

        #region GET AND SET

        protected virtual RenderTexture GetFirstCollisionFreeTextureMap(Mesh m)
        {
            //Compute the pixel coords of each vertex of the triangle. 
            int resolution = 2048;
            Vector3 v0 = new Vector3(m.uv[0].x * resolution, m.uv[0].y * resolution, 0);
            Vector3 v1 = new Vector3(m.uv[1].x * resolution, m.uv[1].y * resolution, 0);
            Vector3 v2 = new Vector3(m.uv[2].x * resolution, m.uv[2].y * resolution, 0);
            
            Vector3 c = (v0 + v1 + v2) / 3.0f;
            Bounds pBounds = new Bounds(c, new Vector3(0.001f, 0.001f, 0));

            bool isFree = false;
            int i = 0;

            while (i < _renderTextures.Count && !isFree)
            {
                List<QuickTriangle> tmp = new List<QuickTriangle>();
                _quadtrees[i].ComputeIntersectedObjects(pBounds, ref tmp);

                bool hit = false;
                for (int k = 0; k < tmp.Count && !hit; k++)
                {
                    hit = tmp[k].PointInTriangle(c);
                }

                isFree = !hit;
                if (!isFree)
                {
                    i++;
                }
            }

            if (!isFree)
            {
                _renderTextures.Add(CreateRenderTexture(resolution));
                _quadtrees.Add(CreateQuadtree(resolution));
            }

            //Debug.Log("i = " + i);
            //Debug.Log("numQuadTrees = " + _quadtrees.Count);
            //Debug.Log("isFree = " + isFree);
            QuickQuadtreeValue<QuickTriangle> v = new QuickQuadtreeValue<QuickTriangle>();
            QuickTriangle t = QuickTriangle.Create(v0, v1, v2);
            v._value = t;
            v._bounds = t.GetBounds();
            
            _quadtrees[i].Insert(v);

            return _renderTextures[i];
        }
        public virtual void ComputeTriangleTextureMap(Mesh mSource)
        {

            if (mSource != null)
            {
                int resolution = 2048;
                _newRegionColor = Vector3Int.zero;
                _renderTextures = new List<RenderTexture>();
                _quadtrees = new List<QuickQuadtree<QuickTriangle>>();

                _materialBake.SetPass(0);

                RenderTexture currentTexture = RenderTexture.active;

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

                    RenderTexture.active = GetFirstCollisionFreeTextureMap(m);
                    Graphics.DrawMeshNow(m, Vector3.zero, Quaternion.identity);
                }

                _textureMaps = new List<Texture2D>();
                for (int i = 0; i < _renderTextures.Count; i++)
                {
                    RenderTexture rTex = _renderTextures[i];
                    //SaveTexture(rTex, "VertexColors_" + mSource.name + i.ToString());

                    // create a second render target 
                    RenderTexture rt2 = RenderTexture.GetTemporary(rTex.descriptor);
                    
                    // use the dilate shader on our first render target, output to rt2
                    for (int j = 0; j < 10; j++)
                    {
                        Graphics.Blit(rTex, rt2, _materialDilate);
                        Graphics.Blit(rt2, rTex);
                    }

                    //SaveTexture(rt2, "VertexColorsDilated");
                    _textureMaps.Add(ToTexture2D(rt2));

                    RenderTexture.ReleaseTemporary(rTex);
                    RenderTexture.ReleaseTemporary(rt2);
                }
                
                RenderTexture.active = currentTexture;
            }
        }

        protected virtual Color32 ComputeNewRegionColor()
        {
            if (_newRegionColor.x >= 256)
            {
                Debug.LogWarning("Maxmimum region count reached!!!");
                return new Color32(255, 255, 255, 255);
            }

            _newRegionColor.z++;
            if (_newRegionColor.z == 256)
            {
                _newRegionColor.z = 0;
                _newRegionColor.y++;
            }
            if (_newRegionColor.y == 256)
            {
                _newRegionColor.z = 0;
                _newRegionColor.y = 0;
                _newRegionColor.x++;
            }

            return new Color32((byte)_newRegionColor.x, (byte)_newRegionColor.y, (byte)_newRegionColor.z, 255);
        }

        protected virtual Texture2D ToTexture2D(RenderTexture rTex, TextureFormat tFormat = TextureFormat.RGB24)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, tFormat, false);
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();

            return tex;
        }

        protected virtual void SaveTexture(RenderTexture rTex, string name)
        {
            SaveTexture(ToTexture2D(rTex), name);
        }

        protected virtual void SaveTexture(Texture2D tex, string mname)
        {
            string fullPath = Application.dataPath + "/" + mname + ".png";
            byte[] bytes = tex.EncodeToPNG();
            File.Delete(fullPath);
            File.WriteAllBytes(fullPath, bytes);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

        public virtual void Sample(float u, float v, out List<Color> colors)
        {
            colors = new List<Color>();
            foreach (Texture2D tex in _textureMaps)
            {
                colors.Add(tex.Sample(u, v));
            }
        }

        #endregion

    }

}
