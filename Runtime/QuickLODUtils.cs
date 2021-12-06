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


