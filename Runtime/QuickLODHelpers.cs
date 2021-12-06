using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuickVR.QuickLOD
{

    public enum BlendShapesMixamo
    {
        Blink_Left,
        Blink_Right,

        BrowsDown_Left,
        BrowsDown_Right,
        BrowsIn_Left,
        BrowsIn_Right,
        BrowsOuterLower_Left,
        BrowsOuterLower_Right,
        BrowsUp_Left,
        BrowsUp_Right,

        CheeckPuff_Left,
        CheeckPuff_Right,

        EyesWide_Left,
        EyesWide_Right,

        Frown_Left,
        Frown_Right,

        JawBackward,
        JawForward,
        JawRotateY_Left,
        JawRotateY_Right,
        JawRotateZ_Left,
        JawRotateZ_Right,
        Jaw_Down,
        Jaw_Left,
        Jaw_Right,
        Jaw_Up,

        LowerLipDown_Left,
        LowerLipDown_Right,
        LowerLipIn,
        LowerLipOut,

        Midmouth_Left,
        Midmouth_Right,

        MouthDown,
        MouthNarrow_Left,
        MouthNarrow_Right,
        MouthOpen,
        MouthUp,

        MouthWhistle_NarrowAdjust_Left,
        MouthWhistle_NarrowAdjust_Right,

        NoseScrunch_Left,
        NoseScrunch_Right,

        Smile_Left,
        Smile_Right,

        Squint_Left,
        Squint_Right,

        TongueUp,

        UpperLipIn,
        UpperLipOut,
        UpperLipUp_Left,
        UpperLipUp_Right,
    }

    public struct QuickEdge
    {

        #region PUBLIC ATTRIBUTES

        public Vector3 _v0 { get; private set; }
        public Vector3 _v1 { get; private set; }

        public Vector3 _direction { get; private set; }

        public float _length { get; private set; }

        #endregion

        #region CREATION AND DESTRUCTION

        public QuickEdge(Vector3 v0, Vector3 v1)
        {
            _v0 = _v1 = _direction = Vector3.zero;
            _length = 0;

            SetVertices(v0, v1);
        }

        #endregion

        #region GET AND SET

        public void SetVertices(Vector3 v0, Vector3 v1)
        {
            _v0 = v0;
            _v1 = v1;

            Vector3 dir = _v1 - _v0;
            _direction = dir.normalized;
            _length = dir.magnitude;
        }

        public Vector3 GetClosestPoint(Vector3 p)
        {
            Vector3 v = p - _v0;
            float d = Mathf.Clamp(Vector3.Dot(v, _direction), 0, _length);

            return _v0 + _direction * d;
        }

        #endregion

    }

    public class QuickTriangleMesh : QuickTriangle
    {

        #region PUBLIC ATTRIBUTES

        public int _vertexID0 { get; private set; }
        public int _vertexID1 { get; private set; }
        public int _vertexID2 { get; private set; }

        public Mesh _mesh { get; private set; }

        #endregion

        #region PROTECTED ATTRIBUTES

        protected Color32[] _vertexColors = new Color32[3];

        #endregion

        #region CREATION AND DESTRUCTION

        public static QuickTriangleMesh Create(int i0, int i1, int i2, Mesh mesh)
        {
            QuickTriangleMesh triangle = new QuickTriangleMesh();
            triangle.SetVertices(i0, i1, i2, mesh);

            return triangle;
        }

        public virtual void SetVertices(int i0, int i1, int i2, Mesh mesh)
        {
            //The vertices ID
            _vertexID0 = i0;
            _vertexID1 = i1;
            _vertexID2 = i2;

            _mesh = mesh;

            _vertexColors[0] = _vertexID0 < mesh.colors32.Length ? mesh.colors32[_vertexID0] : new Color32(0, 0, 0, 0);
            _vertexColors[1] = _vertexID1 < mesh.colors32.Length ? mesh.colors32[_vertexID1] : new Color32(0, 0, 0, 0);
            _vertexColors[2] = _vertexID2 < mesh.colors32.Length ? mesh.colors32[_vertexID2] : new Color32(0, 0, 0, 0);

            SetVertices(mesh.vertices[_vertexID0], mesh.vertices[_vertexID1], mesh.vertices[_vertexID2]);
        }

        #endregion

        #region GET AND SET

        public int GetClosestVertexID(Vector3 p)
        {
            int iMin = GetClosestVertexInternalID(p);

            if (iMin == 0) return _vertexID0;
            if (iMin == 1) return _vertexID1;
            if (iMin == 2) return _vertexID2;

            return -1;
        }

        public virtual bool HasColor(Color32 color)
        {
            return color.Equals(_vertexColors[0]) || color.Equals(_vertexColors[1]) || color.Equals(_vertexColors[2]);
        }

        #endregion

    }

    public class QuickTriangle
    {

        #region PUBLIC ATTRIBUTES

        public Vector3 _v0 { get; private set; }
        public Vector3 _v1 { get; private set; }
        public Vector3 _v2 { get; private set; }

        public QuickEdge _e0 { get; private set; }
        public QuickEdge _e1 { get; private set; }
        public QuickEdge _e2 { get; private set; }

        public Vector3 _normal
        {
            get
            {
                return _plane.normal;
            }
        }

        public Vector3 this[int i]
        {
            get
            {
                if (i == 0) return _v0;
                if (i == 1) return _v1;
                if (i == 2) return _v2;

                return Vector3.zero;
            }
        }

        #endregion

        #region PROTECTED ATTRIBUTES

        protected Plane _plane;     //The main plane supporting the triangle

        protected Bounds _bounds;

        #endregion

        public static QuickTriangle Create(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            QuickTriangle triangle = new QuickTriangle();
            triangle.SetVertices(v0, v1, v2);

            return triangle;
        }

        public virtual void SetVertices(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            //Triangle vertices
            _v0 = v0;
            _v1 = v1;
            _v2 = v2;

            //Normal computation
            _e0 = new QuickEdge(_v0, _v1);
            _e1 = new QuickEdge(_v1, _v2);
            _e2 = new QuickEdge(_v2, _v0);

            _plane = new Plane(Vector3.Cross(_e0._direction, -_e2._direction), v0);

            //The bounds of the triangle
            _bounds = new Bounds();
            _bounds.center = (_v0 + _v1 + _v2) / 3;
            _bounds.Encapsulate(_v0);
            _bounds.Encapsulate(_v1);
            _bounds.Encapsulate(_v2);
        }

        public Bounds GetBounds()
        {
            return _bounds;
        }

        public Vector3 ComputeProjection(Vector3 p)
        {
            return _plane.ClosestPointOnPlane(p);
        }

        public bool PointInTriangle(Vector3 p)
        {
            Vector3 bCoords = ComputeBarycentricCoordinates(p);
            for (int i = 0; i < 3; i++)
            {
                if (bCoords[i] < 0) return false;
            }

            return true;
        }

        public Vector3 ComputeBarycentricCoordinates(Vector3 p)
        {
            //https://www.cdsimpson.net/2014/10/barycentric-coordinates.html

            //Check if p is any of triangle vertices
            if (MathfExtensions.Equal(p, _v0))
            {
                return new Vector3(1, 0, 0);
            }

            if (MathfExtensions.Equal(p, _v1))
            {
                return new Vector3(0, 1, 0);
            }

            if (MathfExtensions.Equal(p, _v2))
            {
                return new Vector3(0, 0, 1);
            }

            Vector3 vap = p - _v0;
            Vector3 vbp = p - _v1;
            Vector3 vcp = p - _v2;

            Vector3 vab = _v1 - _v0;
            Vector3 vca = _v0 - _v2;
            Vector3 vbc = _v2 - _v1;
            Vector3 vac = _v2 - _v0;

            Vector3 n = Vector3.Cross(vab, vac);
            Vector3 na = Vector3.Cross(vbc, vbp);
            Vector3 nb = Vector3.Cross(vca, vcp);
            Vector3 nc = Vector3.Cross(vab, vap);

            float ndot = Vector3.Dot(n, n);
            float u = Vector3.Dot(n, na) / ndot;
            float v = Vector3.Dot(n, nb) / ndot;
            float w = Vector3.Dot(n, nc) / ndot;

            return new Vector3(u, v, w);
        }

        public Vector3 GetClosestPoint(Vector3 p)
        {
            Vector3 result = Vector3.zero;

            if (PointInTriangle(p))
            {
                result = p;
            }
            else
            {
                //The point is outside the triangle. Find the closest point on the boundary of the triangle. 
                Vector3[] q = { _e0.GetClosestPoint(p), _e1.GetClosestPoint(p), _e2.GetClosestPoint(p) };

                int iMin = -1;
                float dMin = Mathf.Infinity;
                for (int i = 0; i < q.Length; i++)
                {
                    float d = (p - q[i]).sqrMagnitude;
                    if (d < dMin)
                    {
                        iMin = i;
                        dMin = d;
                    }
                }

                result = q[iMin];
            }

            return result;
        }

        public Vector3 GetClosestVertex(Vector3 p)
        {
            return this[GetClosestVertexInternalID(p)];
        }

        protected int GetClosestVertexInternalID(Vector3 p)
        {
            float dMin = Mathf.Infinity;
            int iMin = -1;

            for (int i = 0; i < 3; i++)
            {
                float d = (p - this[i]).sqrMagnitude;
                if (d < dMin)
                {
                    dMin = d;
                    iMin = i;
                }
            }

            return iMin;
        }

        public Vector3 ToPoint(Vector3 barycentricCoordinates)
        {
            return barycentricCoordinates.x * _v0 + barycentricCoordinates.y * _v1 + barycentricCoordinates.z * _v2;
        }

        public float Distance(Vector3 p)
        {
            return Mathf.Sqrt(Distance2(p));
        }

        public float Distance2(Vector3 p)
        {
            return Vector3.SqrMagnitude(p - GetClosestPoint(p));
        }

        public bool RayIntersection(Ray r, out Vector3 hitPoint) 
        {
            _plane.Raycast(r, out float d);
            hitPoint = r.origin + r.direction * d;

            return PointInTriangle(hitPoint);
        }

    }

    public class BlendshapeData
    {
        public Vector3[] _dVertices;
        public Vector3[] _dNormals;
        public Vector3[] _dTangents;

        public BlendshapeData(Mesh mesh, int blandshapeID)
        {
            int numVertices = mesh.vertexCount;
            _dVertices = new Vector3[numVertices];
            _dNormals = new Vector3[numVertices];
            _dTangents = new Vector3[numVertices];

            mesh.GetBlendShapeFrameVertices(blandshapeID, 0, _dVertices, _dNormals, _dTangents);
        }
    }

    [System.Serializable]
    public class RenderGroup
    {
        public string _name;
        public List<Renderer> _renderers = new List<Renderer>();
        public int _atlasResolution = 1024;  //The resolution of the atlas for the RenderGroup

        public virtual bool IsSkinned()
        {
            return _renderers.Count > 0 ? _renderers[0].GetType() == typeof(SkinnedMeshRenderer) : false;
        }

        public virtual SkinnedMeshRenderer[] GetSkinnedMeshrenderers()
        {
            if (!IsSkinned()) return null;

            List<SkinnedMeshRenderer> result = new List<SkinnedMeshRenderer>();
            foreach (Renderer r in _renderers)
            {
                result.Add((SkinnedMeshRenderer)r);
            }

            return result.ToArray();
        }

        public virtual Mesh[] GetMeshes()
        {
            if (_renderers.Count == 0) return null;

            List<Mesh> result = new List<Mesh>();
            foreach (Renderer r in _renderers)
            {
                result.Add(r.GetMesh());
            }

            return result.ToArray();
        }
    }

    public struct VertexData
    {
        public Vector3 _position;
        public Vector3 _normal;
        public Color _color;

        public VertexData(Vector3 position, Vector3 normal, Color color)
        {
            _position = position;
            _normal = normal;
            _color = color;
        }
    }

}
