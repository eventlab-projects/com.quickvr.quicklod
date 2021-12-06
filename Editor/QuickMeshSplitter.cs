using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuickVR.QuickLOD
{

    public class QuickMeshSplitter
    {

        #region PUBLIC ATTRIBUTES

        public static QuickMeshSplitter _instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new QuickMeshSplitter();
                }

                return m_Instance;
            }
        }
        private static QuickMeshSplitter m_Instance = null;

        public struct SplitDescriptor
        {
            public TextAsset _polygonsHead;
            public TextAsset _polygonsMouth;
            public TextAsset _polygonsHands;
            public TextAsset _polygonsFeet;
            public TextAsset _polygonsUpperBody;
            public TextAsset _polygonsLowerBody;
        }

        public enum BodyParts
        {
            Head,
            Mouth,
            Hands,
            Feet,
            UpperBody,
            LowerBody,
        }

        #endregion

        #region GET AND SET

        protected virtual Renderer GetBodyRenderer(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            Renderer maxR = null;
            Renderer result = null;

            for (int i = 0; i < renderers.Length && !result; i++)
            {
                Renderer r = renderers[i];
                if (r.name == "Body")
                {
                    result = r;
                }

                if (!maxR || r.GetMesh().vertexCount > maxR.GetMesh().vertexCount)
                {
                    maxR = r;
                }
            }

            if (!result)
            {
                result = maxR;
            }

            return result;
        }

        #endregion

        public virtual GameObject Split(GameObject goSource, SplitDescriptor desc, int bodyPartsMask = -1)
        {
            GameObject goSplit = null;

            if (goSource)
            {
                goSplit = Object.Instantiate(goSource, goSource.transform.position, goSource.transform.rotation);
                goSplit.name = goSource.name + "_SPLITTED";
                Renderer rBody = GetBodyRenderer(goSplit);

                if (desc._polygonsHead && (bodyPartsMask & (1 << (int)BodyParts.Head)) != 0)
                {
                    Split(rBody, desc._polygonsHead, BodyParts.Head);
                }

                if (desc._polygonsMouth && (bodyPartsMask & (1 << (int)BodyParts.Mouth)) != 0)
                {
                    Split(rBody, desc._polygonsMouth, BodyParts.Mouth);
                }

                if (desc._polygonsHands && (bodyPartsMask & (1 << (int)BodyParts.Hands)) != 0)
                {
                    Split(rBody, desc._polygonsHands, BodyParts.Hands);
                }

                if (desc._polygonsFeet && (bodyPartsMask & (1 << (int)BodyParts.Feet)) != 0)
                {
                    Split(rBody, desc._polygonsFeet, BodyParts.Feet);
                }

                if (desc._polygonsUpperBody && (bodyPartsMask & (1 << (int)BodyParts.UpperBody)) != 0)
                {
                    Split(rBody, desc._polygonsUpperBody, BodyParts.UpperBody);
                }

                if (desc._polygonsLowerBody && (bodyPartsMask & (1 << (int)BodyParts.LowerBody)) != 0)
                {
                    Split(rBody, desc._polygonsLowerBody, BodyParts.LowerBody);
                }

                Object.DestroyImmediate(rBody.gameObject);//
            }

            return goSplit;
        }

        public virtual GameObject ProcessFuseCharacter(GameObject goSource, SplitDescriptor desc, int bodyPartsMask = -1)
        {
            GameObject goSplit = Split(goSource, desc, bodyPartsMask);

            Renderer[] renderersSplit = goSplit.GetComponentsInChildren<Renderer>();
            float[] reductionFactors = new float[renderersSplit.Length];

            List<RenderGroup> renderGroups = new List<RenderGroup>();
            RenderGroup rGroupBody = new RenderGroup();
            rGroupBody._name = "Body";
            rGroupBody._atlasResolution = 2048;
            RenderGroup rGroupEyeLashes = new RenderGroup();
            rGroupEyeLashes._name = "EyeLashes";
            rGroupEyeLashes._atlasResolution = 1024;

            renderGroups.Add(rGroupBody);
            renderGroups.Add(rGroupEyeLashes);

            for (int i = 0; i < renderersSplit.Length; i++)
            {
                Renderer r = renderersSplit[i];
                string rName = r.name.ToLower();
                if (r.name.Contains("default"))
                {
                    rGroupEyeLashes._renderers.Add(r);
                    reductionFactors[i] = 1;
                }
                else 
                {
                    rGroupBody._renderers.Add(r);
                    if (rName.Contains("eyes"))
                    {
                        reductionFactors[i] = 0.25f;
                    }
                    else if (rName.Contains("shoes"))
                    {
                        reductionFactors[i] = 0.125f;
                    }
                    else if (rName.Contains(BodyParts.Head.ToString().ToLower()))
                    {
                        reductionFactors[i] = 0.3f;
                    }
                    else if (rName.Contains(BodyParts.Mouth.ToString().ToLower()))
                    {
                        reductionFactors[i] = 0.2f;
                    }
                    else if (rName.Contains(BodyParts.Hands.ToString().ToLower()))
                    {
                        reductionFactors[i] = 0.2f;
                    }
                    else if (rName.Contains(BodyParts.Feet.ToString().ToLower()))
                    {
                        reductionFactors[i] = 0.125f;
                    }
                    else if (rName.Contains(BodyParts.UpperBody.ToString().ToLower()))
                    {
                        reductionFactors[i] = 0.125f;
                    }
                    else if (rName.Contains(BodyParts.LowerBody.ToString().ToLower()))
                    {
                        reductionFactors[i] = 0.125f;
                    }
                    else
                    {
                        reductionFactors[i] = 0.5f;
                    }
                }
            }

            GameObject goSimplified = QuickLOD._instance.Simplify(goSplit, reductionFactors, renderGroups);
            UnityEngine.Object.DestroyImmediate(goSplit);

            return goSimplified;
        }

        protected virtual GameObject Split(Renderer rBody, TextAsset faces, BodyParts bodyPart)
        {
            string partName = bodyPart.ToString();
            GameObject go = new GameObject(partName);
            go.transform.parent = rBody.transform.parent;
            go.transform.localPosition = rBody.transform.localPosition;
            go.transform.localRotation = rBody.transform.localRotation;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();

            Mesh mBody = rBody.GetMesh();
            Mesh m = new Mesh();
            m.name = partName;

            string[] facesID = faces.text.Replace("[", "").Replace("]", "").Split(',');
            Dictionary<int, int> vertexMap = new Dictionary<int, int>();
            Dictionary<int, int> closestTriangleMap = new Dictionary<int, int>();

            foreach (string s in facesID)
            {
                int fID = int.Parse(s) * 3;

                //Recover the vertices of the triangle
                for (int i = 0; i < 3; i++)
                {
                    if (fID + i < 0 || fID + i >= mBody.triangles.Length)
                    {
                        Debug.Log("ERROR!!!");
                        Debug.Log(fID);
                        Debug.Log(i);
                        Debug.Log(mBody.triangles.Length);
                    }
                    int vID = mBody.triangles[fID + i];
                    if (!vertexMap.ContainsKey(vID))
                    {
                        vertices.Add(mBody.vertices[vID]);
                        normals.Add(mBody.normals[vID]);
                        uv.Add(mBody.uv[vID]);

                        vertexMap[vID] = vertices.Count - 1;
                    }

                    triangles.Add(vertexMap[vID]);
                    closestTriangleMap[vertexMap[vID]] = fID;
                }

            }

            m.vertices = vertices.ToArray();
            m.normals = normals.ToArray();
            m.uv = uv.ToArray();
            m.triangles = triangles.ToArray();

            Renderer rResult = null;
            if (rBody.GetType() == typeof(SkinnedMeshRenderer))
            {
                rResult = go.AddComponent<SkinnedMeshRenderer>();
                rResult.SetMesh(m);
                TransferVertexData((SkinnedMeshRenderer)rBody, (SkinnedMeshRenderer)rResult, closestTriangleMap, bodyPart);
            }
            else
            {
                rResult = go.AddComponent<MeshRenderer>();
                rResult.SetMesh(m);
            }

            rResult.sharedMaterial = rBody.sharedMaterial;

            return go;
        }

        protected virtual void TransferVertexData(SkinnedMeshRenderer rSource, SkinnedMeshRenderer rResult, Dictionary<int, int> closestTriangleMap, BodyParts bodyPart)
        {
            Mesh mSource = rSource.GetMesh();
            Mesh mResult = rResult.GetMesh();
            QuickTriangleMesh[] closestTriangles = new QuickTriangleMesh[mResult.vertexCount];
            
            for (int i = 0; i < mResult.vertexCount; i++)
            {
                int tID = closestTriangleMap[i];
                closestTriangles[i] = QuickTriangleMesh.Create(mSource.triangles[tID + 0], mSource.triangles[tID + 1], mSource.triangles[tID + 2], mSource);
            }

            QuickLOD._instance.TransferSkinning(rSource, rResult, closestTriangles);

            if (bodyPart == BodyParts.Head || bodyPart == BodyParts.Mouth)
            {
                QuickLOD._instance.TransferBlendShapes(mSource, mResult, closestTriangles);
            }
        }

    }

}


