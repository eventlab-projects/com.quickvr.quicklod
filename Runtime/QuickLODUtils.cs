using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace QuickVR.QuickLOD
{
    public static class QuickLODUtils
    {

        public static string dataPath
        {
            get
            {
                return Application.dataPath;
            }
        }

        public static string projectPath
        {
            get
            {
                string dPath = dataPath;
                return dPath.Substring(0, dataPath.Length - "/Assets".Length);
            }
        }

        public static Color Sample(this Texture2D tex, Vector2 uv)
        {
            return tex.Sample(uv.x, uv.y);
        }

        public static Color Sample(this Texture2D tex, float u, float v)
        {
            int x = Mathf.RoundToInt(tex.width * u);
            int y = Mathf.RoundToInt(tex.height * v);

            return tex.GetPixel(x, y);
        }

        public static Mesh GetMesh(this Renderer r)
        {
            Mesh result = null;
            if (r.GetType() == typeof(SkinnedMeshRenderer))
            {
                result = ((SkinnedMeshRenderer)r).sharedMesh;
            }
            else
            {
                MeshFilter mFilter = r.GetComponent<MeshFilter>();
                if (mFilter)
                {
                    result = mFilter.sharedMesh;
                }
            }

            return result;
        }

        public static void SetMesh(this Renderer r, Mesh mesh)
        {
            if (r.GetType() == typeof(SkinnedMeshRenderer))
            {
                ((SkinnedMeshRenderer)r).sharedMesh = mesh;
            }
            else
            {
                MeshFilter mFilter = r.GetComponent<MeshFilter>();
                if (!mFilter)
                {
                    mFilter = r.gameObject.AddComponent<MeshFilter>();
                }

                mFilter.sharedMesh = mesh;
            }
        }

        public static int GetNumTriangles(this Mesh m)
        {
            return m.triangles.Length / 3;
        }

        public static Mesh ComputeSubMesh(this Mesh m, List<int> subMeshTriangles)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();

            Dictionary<int, int> vertexMap = new Dictionary<int, int>();

            for (int i = 0; i < subMeshTriangles.Count; i += 3)
            {
                //For each vertex of the triangle, check if it is a new vertex or it has been already introduced to the 
                //new triangles list of the submesh
                int[] vTriangle = new int[] { subMeshTriangles[i + 0], subMeshTriangles[i + 1], subMeshTriangles[i + 2] };
                foreach (int vID in vTriangle)
                {
                    if (!vertexMap.ContainsKey(vID))
                    {
                        vertices.Add(m.vertices[vID]);
                        normals.Add(m.normals[vID]);
                        uv.Add(m.uv[vID]);

                        vertexMap[vID] = vertices.Count - 1;
                    }

                    triangles.Add(vertexMap[vID]);
                }
            }

            Mesh result = new Mesh();

            result.vertices = vertices.ToArray();
            result.normals = normals.ToArray();
            result.uv = uv.ToArray();
            result.triangles = triangles.ToArray();

            return result;
        }

        public static Renderer Bake(this Renderer r)
        {
            Renderer result = (r.GetType() == typeof(SkinnedMeshRenderer) ? ((SkinnedMeshRenderer)r).Bake() : UnityEngine.Object.Instantiate(r));
            result.name = r.name;
            result.GetMesh().name = r.GetMesh().name;

            return result;
        }

        public static SkinnedMeshRenderer Bake(this SkinnedMeshRenderer rSource)
        {
            Mesh mSource = rSource.GetMesh();
            Mesh mBaked = new Mesh();
            rSource.BakeMesh(mBaked);
            mBaked.name = mSource.name;

            SkinnedMeshRenderer result = UnityEngine.Object.Instantiate(rSource);
            result.name = rSource.name;
            result.transform.parent = rSource.transform.parent;
            result.transform.ResetTransformation();

            //Transform the vertices of the bakedMesh, so it accounts for the local position and rotation of rSource
            List<Vector3> vertices = new List<Vector3>();
            foreach (Vector3 v in mBaked.vertices)
            {
                Matrix4x4 m = Matrix4x4.TRS(rSource.transform.localPosition, rSource.transform.localRotation, Vector3.one);
                vertices.Add(m.MultiplyPoint(v));
            }
            mBaked.vertices = vertices.ToArray();

            //Copy the skeleton
            List<Matrix4x4> bindPoses = new List<Matrix4x4>();
            foreach (Transform tBone in result.bones)
            {
                bindPoses.Add(tBone.worldToLocalMatrix * result.transform.localToWorldMatrix);
            }
            mBaked.bindposes = bindPoses.ToArray();
            mBaked.boneWeights = mSource.boneWeights;

            //Copy the blendshapes
            Vector3[] dVertices = new Vector3[mSource.vertexCount];
            Vector3[] dNormals = new Vector3[mSource.vertexCount];
            Vector3[] dTangents = new Vector3[mSource.vertexCount];
            for (int bsID = 0; bsID < mSource.blendShapeCount; bsID++)
            {
                string bsName = mSource.GetBlendShapeName(bsID);
                mSource.GetBlendShapeFrameVertices(bsID, 0, dVertices, dNormals, dTangents);

                //The vertex displacement must take into account the scale of the original SkinnedMeshRenderer
                //Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, rSource.transform.localScale);
                Matrix4x4 m = Matrix4x4.TRS(rSource.transform.localPosition, rSource.transform.localRotation, rSource.transform.localScale);
                for (int i = 0; i < dVertices.Length; i++)
                {
                    dVertices[i] = m.MultiplyPoint(dVertices[i]);
                    dNormals[i] = Vector3.zero;
                    //dNormals[i] = m.MultiplyVector(dNormals[i]).normalized;
                    //dTangents[i] = m.MultiplyVector(dTangents[i]).normalized;
                }

                mBaked.AddBlendShapeFrame(bsName, 100, dVertices, dNormals, dTangents);
            }

            result.SetMesh(mBaked);

            return result;
        }

        public static List<T> GetEnumValues<T>()
        {
            return new List<T>((IEnumerable<T>)(System.Enum.GetValues(typeof(T))));
        }

        public static List<string> GetEnumValuesToString<T>()
        {
            List<T> eValues = GetEnumValues<T>();
            List<string> names = new List<string>();
            foreach (T val in eValues) names.Add(val.ToString());
            return names;
        }

        public static Transform CreateChild(this Transform transform, string name, bool checkName = true)
        {
            Transform t = name.Length != 0 ? transform.Find(name) : null;
            if (!t || !checkName)
            {
                t = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("GameObject")).transform;
                t.name = name;
                t.parent = transform;
                t.ResetTransformation();
            }

            return t;
        }

        public static void ResetTransformation(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public static void DestroyImmediate(UnityEngine.GameObject[] gameObjects)
        {
            for (int i = gameObjects.Length - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(gameObjects[i]);
            }
        }

        public static void DestroyImmediate(UnityEngine.Component[] components)
        {
            for (int i = components.Length - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(components[i].gameObject);
            }
        }

    }

}


