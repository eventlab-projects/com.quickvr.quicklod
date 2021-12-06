//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace QuickVR
//{

//    public class TestBlendShapes : MonoBehaviour
//    {

//        public GameObject _source = null;
//        public GameObject _target = null;

//        [System.Serializable]
//        public struct RenderPair
//        {

//            public SkinnedMeshRenderer _renderOrigin;
//            public SkinnedMeshRenderer _renderDest;

//            public RenderPair(SkinnedMeshRenderer origin, SkinnedMeshRenderer dest)
//            {
//                _renderOrigin = origin;
//                _renderDest = dest;
//            }

//        }

//        public List<RenderPair> _renderMap = null;

//        public bool _drawOctree = false;
//        public bool _drawMissingVertices = false;

//        [Range(0.001f, 0.01f)]
//        public float _radiusMissingVertices = 0.001f;

//        protected List<Vector3> _missedVertices = new List<Vector3>();
//        protected QuickTriangleMesh _closestTriangle = null;
//        protected Mesh _meshTest;
//        protected Bounds? _bounds = null;
//        protected Bounds? _bounds2 = null;

//        protected virtual void ClearBlendShapes(GameObject go)
//        {
//            foreach (SkinnedMeshRenderer smRenderer in go.GetComponentsInChildren<SkinnedMeshRenderer>())
//            {
//                smRenderer.sharedMesh.ClearBlendShapes();
//            }
//        }

//        public virtual void InitRenderMap()
//        {
//            if (_source && _target)
//            {
//                _renderMap = new List<RenderPair>();
//                foreach (SkinnedMeshRenderer rOrigin in _target.GetComponentsInChildren<SkinnedMeshRenderer>())
//                {
//                    SkinnedMeshRenderer rDest = null;
//                    foreach (SkinnedMeshRenderer tmp in _source.GetComponentsInChildren<SkinnedMeshRenderer>())
//                    {
//                        if (tmp.name == rOrigin.name)
//                        {
//                            rDest = tmp;
//                        }
//                    }
//                    _renderMap.Add(new RenderPair(rOrigin, rDest));
//                }
//            }
//        }

//        //[ButtonMethod]
//        //public virtual void TransferBlendShapes()
//        //{
//        //    _meshTest = new Mesh();
//        //    _missedVertices.Clear();

//        //    for (int i = 0; i < _renderMap.Count; i++)
//        //    {
//        //        RenderPair p = _renderMap[i];

//        //        QuickLOD._instance.TransferBlendShapes(p._renderDest.GetMesh(), p._renderOrigin.GetMesh());
//        //    }
//        //}

//        #region DEBUG

//        [ButtonMethod]
//        public virtual void TestBounds()
//        {
//            //_bounds = new Bounds(Vector3.zero, Vector3.one);
//            //_bounds.Value.Encapsulate(new Vector3(10, 10, 10));
//            //_bounds.Value.Encapsulate(new Vector3(12, 12, 12));
//            //Debug.Log("center = " + _bounds.Value.center.ToString("f3"));
//            //Debug.Log("size = " + _bounds.Value.size.ToString("f3"));

//            Vector3 v1 = new Vector3(4, 4, 4);
//            Vector3 v2 = new Vector3(-4, -4, -4);
//            Bounds b = new Bounds();
//            b.center = (v1 + v2) / 2;
//            b.Encapsulate(v1);
//            b.Encapsulate(v2);
//            //Debug.Log("center = " + b.center.ToString("f3"));
//            //Debug.Log("size = " + b.size.ToString("f3"));

//            _bounds = b;
//            //Ray r = new Ray(new Vector3(5, 0, 0), -Vector3.right);
//            Ray r = new Ray(new Vector3(0, 0, 0), -Vector3.right);
//            Debug.Log(_bounds.Value.IntersectRay(r, out float d));
//            Debug.Log(d.ToString("f3"));

//            //v1 = new Vector3(8, 8, 8);
//            //v2 = new Vector3(4, -4, -4);
//            v1 = new Vector3(2, 2, 2);
//            v2 = new Vector3(-2, -2, -2);
//            Bounds b2 = new Bounds();
//            b2.center = (v1 + v2) / 2;
//            b2.Encapsulate(v1);
//            b2.Encapsulate(v2);

//            _bounds2 = b2;

//            Debug.Log(_bounds.Value.Intersects(_bounds2.Value));
//            Debug.Log(_bounds2.Value.Intersects(_bounds.Value));
//        }

//        protected virtual void OnDrawGizmos()
//        {
//            if (_source)
//            {
//                foreach (Renderer r in _source.GetComponentsInChildren<Renderer>())
//                {
//                    DrawBounds(r.GetMesh().bounds);
//                }
//            }

//            //if (_drawOctree && _octree != null)
//            //{
//            //    DrawOctree();
//            //}

//            if (_bounds.HasValue)
//            {
//                Gizmos.matrix = Matrix4x4.TRS(_source.transform.position, _source.transform.rotation, _source.transform.localScale);
//                Gizmos.color = Color.red;
//                DrawBounds(_bounds.Value);
//                Gizmos.matrix = Matrix4x4.identity;
//            }

//            if (_bounds2.HasValue)
//            {
//                DrawBounds(_bounds2.Value);
//            }

//            if (_meshTest && _meshTest.vertexCount > 0)
//            {
//                Gizmos.color = Color.black;
//                Gizmos.matrix = Matrix4x4.TRS(_source.transform.position, _source.transform.rotation, _source.transform.localScale);
//                //Gizmos.matrix = Matrix4x4.TRS(_target.transform.position, _target.transform.rotation, _target.transform.localScale);
//                //Gizmos.matrix = Matrix4x4.identity;

//                //Vector3 c = Vector3.zero;
//                //foreach (Vector3 v in _meshTest.vertices)
//                //{
//                //    c += v;
//                //}
//                //c /= (float)_meshTest.vertexCount;

//                //Gizmos.matrix = Matrix4x4.TRS(-c, Quaternion.identity, Vector3.one);

//                if (_bounds.HasValue)
//                {
//                    DrawBounds(_bounds.Value);
//                }
//                Gizmos.DrawWireMesh(_meshTest);

//                Gizmos.matrix = Matrix4x4.identity;
//            }

//            if (_drawMissingVertices && _missedVertices.Count > 0)
//            {
//                Gizmos.color = Color.red;
//                //Gizmos.matrix = Matrix4x4.TRS(_target.transform.position, _target.transform.rotation, _target.transform.localScale);
//                Gizmos.matrix = Matrix4x4.TRS(_source.transform.position, _source.transform.rotation, _source.transform.localScale);

//                for (int i = 0; i < _missedVertices.Count; i++)
//                {
//                    Gizmos.DrawSphere(_missedVertices[i], _radiusMissingVertices);
//                }

//                Gizmos.matrix = Matrix4x4.identity;
//            }
//        }

//        protected virtual void DrawOctree()
//        {
//            Gizmos.matrix = Matrix4x4.TRS(_source.transform.position, _source.transform.rotation, _source.transform.localScale);
//            //Gizmos.matrix = Matrix4x4.TRS(_target.transform.position, _target.transform.rotation, _target.transform.localScale);
//            //Gizmos.matrix = Matrix4x4.identity;

//            Gizmos.color = Color.blue;
//            //DrawOctreeNode(_octree.GetRoot());

//            Gizmos.matrix = Matrix4x4.identity;
//        }

//        protected virtual void DrawOctreeNode(QuickOctreeNode node)
//        {
//            DrawBounds(node.GetBounds());

//            foreach (QuickOctreeNode n in node.GetChilds())
//            {
//                DrawOctreeNode(n);
//            }
//        }

//        protected virtual void DrawBounds(Bounds b)
//        {
//            Gizmos.DrawWireCube(b.center, b.size);
//        }

//        #endregion

//    }

//}
