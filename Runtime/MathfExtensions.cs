using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuickVR
{

    public static class MathfExtensions
    {

		public enum ProjectionPlane
        {
			XY, //The Z is 0
			XZ, //The Y is 0 
			YZ, //The X is 0
		}


		public const float QUICK_FLOAT_PRECISION = 0.0001f;

		public static bool Equal(float f1, float f2, float epsilon = QUICK_FLOAT_PRECISION)
        {
			return Mathf.Abs(f1 - f2) < epsilon;
        }

		public static bool Equal(Vector3 v1, Vector3 v2, float epsilon = QUICK_FLOAT_PRECISION)
        {
			return Equal(v1.x, v2.x, epsilon) && Equal(v1.y, v2.y, epsilon) && Equal(v1.z, v2.z, epsilon);

		}

		public static bool Less(float f1, float f2, float epsilon = QUICK_FLOAT_PRECISION)
		{
			if (Equal(f1, f2, epsilon)) return false;
			return (f1 < f2);
		}

		public static bool LessOrEqual(float f1, float f2, float epsilon = QUICK_FLOAT_PRECISION)
		{
			return Less(f1, f2, epsilon) || Equal(f1, f2, epsilon);
		}

		public static bool Greater(float f1, float f2, float epsilon = QUICK_FLOAT_PRECISION)
		{
			if (Equal(f1, f2, epsilon)) return false;
			return (f1 > f2);
		}

		public static bool GreaterOrEqual(float f1, float f2, float epsilon = QUICK_FLOAT_PRECISION)
		{
			return Greater(f1, f2, epsilon) || Equal(f1, f2, epsilon);
		}

		public static Vector2 To2D(this Vector3 v, ProjectionPlane projectionPlane) 
		{
			if (projectionPlane == ProjectionPlane.XZ) return new Vector2(v.x, v.z);
			if (projectionPlane == ProjectionPlane.XY) return new Vector2(v.x, v.y);
			return new Vector2(v.y, v.z);
		}

		public static bool IsParallelTo(this Vector3 v, Vector3 v1)
		{
			return Equal(Vector3.Cross(v, v1), Vector3.zero);
		}

		public static float Det(this Vector2 u, Vector2 v)
        {
			return u.x * v.y - u.y * v.x;
        }

		public static float Dot(this Vector2 u, Vector2 v)
        {
			return Vector2.Dot(u, v);
        }

		public static Vector2 Perpendicular(this Vector2 u)
        {
			return Vector2.Perpendicular(u);
        }

		public static float PointToPlaneDistanceSigned(Vector3 v, Vector3 p0, Vector3 pNormal)
		{
			//Returns the signed distance from v to the plane defined by the point p0 and the normal pNormal
			return Vector3.Dot(v - p0, pNormal);
		}

		public static float PointToPlaneDistanceAbs(Vector3 v, Vector3 p0, Vector3 pNormal)
        {
			//Returns the absolute value of the signed distance from v to the plane defined by the point p0 and the normal pNormal
			Plane p = new Plane(pNormal, p0);
			return Mathf.Abs(p.GetDistanceToPoint(v));

			//return Mathf.Abs(PointToPlaneDistanceSigned(v, p0, pNormal));
		}

		public static Vector3 PointToPlaneProjection(Vector3 v, Vector3 p0, Vector3 pNormal)
		{
			//Returns the projection of v in the plane defined by p0 and pNormal
			float d = PointToPlaneDistanceSigned(v, p0, pNormal);
			return v - d * pNormal;
		}

		public enum IntersectionType
		{
			Undefined,
			Parallel,		//The line and the plane are parallel and the line is not contained in the plane. 
			Coincident,     //The line and the plane are parallel and the line is contained in the plane. 
			SinglePoint,	//The line and the plane are not parallel and therefore, they intersect in a single point. 
		};

		public class IntersectionResult<T>
		{

			public T _intersectionPoint;
			public T _intersectionPoint2;  //The second intersection point. It only has sense in the COINCIDENT intersection type. 
			public IntersectionType _intersectionType;

			public bool isIntersection()
			{
				return ((_intersectionType == IntersectionType.SinglePoint) || (_intersectionType == IntersectionType.Coincident));
			}

		};

		public class IntersectionResult2 : IntersectionResult<Vector2>
        {

        }

		public class IntersectionResult3 : IntersectionResult<Vector3>
        {

        }

		public struct QuickPlane
		{
			public Vector3 _v0;
			public Vector3 _normal;
			//public Vector3 _hintDir;   //Defines the direction of the semiplane. Set it to ZERO to use an ordinary infinite plane. 

			//QuickPlane(Vector3 v0, Vector3 normal, Vector3 hintDir = Vector3.zero) 
			public QuickPlane(Vector3 v0, Vector3 normal)
			{
				_v0 = v0;
				_normal = normal;
				if (!Equal(normal.sqrMagnitude, 1))
                {
					_normal.Normalize();
                }
				//_hintDir = hintDir;
			}

		};

		#region POINT TO LINE

		public static float PointLineDistance2(Vector3 p, Vector3 l0, Vector3 l1) 
		{
			Vector3 u = (l1 - l0).normalized;
			Vector3 w = p - l0;

			return Vector3.Cross(u, w).sqrMagnitude;
		}

		public static float PointLineDistance2(Vector2 p, Vector2 l0, Vector2 l1)
		{
			return PointLineDistance2(new Vector3(p.x, p.y, 0), new Vector3(l0.x, l0.y, 0), new Vector3(l1.x, l1.y, 0));
		}

		public static float PointLineDistance(Vector3 p, Vector3 l0, Vector3 l1)
        {
			return Mathf.Sqrt(PointLineDistance2(p, l0, l1));
        }

		//public static Vector3 PointPlaneProjection(Vector3 p, const Plane& plane)
		//{
		//	//1) Compute the projection of p on the line supported by the plane's normal
		//	Vector3 nProj = pointLineProjection(p, plane._v0, plane._v0 + plane._normal);

		//	//2) Move p towards the plane in the direction of the normal. 
		//	return p + (plane._v0 - nProj);
		//}

		#endregion

		#region LINE LINE INTERSECTION

		public static bool LineLineIntersection(Vector3 p0, Vector3 p1, Vector3 q0, Vector3 q1, out IntersectionResult3 result) 
		{
			result = new IntersectionResult3();

			//The line intersection is computed in 2D
			Vector3 s = p1 - p0;
			Vector3 t = q1 - q0;

			ProjectionPlane pPlane;
			if (!s.IsParallelTo(Vector3.up) && !t.IsParallelTo(Vector3.up)) pPlane = ProjectionPlane.XZ;
			else if (!s.IsParallelTo(Vector3.right) && !t.IsParallelTo(Vector3.right)) pPlane = ProjectionPlane.YZ;
			else pPlane = ProjectionPlane.XY;

			IntersectionResult2 tmpResult;
			LineLineIntersection(p0.To2D(pPlane), p1.To2D(pPlane), q0.To2D(pPlane), q1.To2D(pPlane), out tmpResult);

			//Transform back the result to a 3D point
			result._intersectionType = tmpResult._intersectionType;
			if (result._intersectionType == IntersectionType.SinglePoint) 
			{
				Vector2 u = p1.To2D(pPlane) - p0.To2D(pPlane);
				Vector2 v = q1.To2D(pPlane) - q0.To2D(pPlane);
				Vector2 w = p0.To2D(pPlane) - q0.To2D(pPlane);

				float si = -v.Perpendicular().Dot(w) / v.Perpendicular().Dot(u);
				if (Equal(si, 0))
				{
					result._intersectionPoint = result._intersectionPoint2 = p0;
				}
				else if (Equal(si, 1))
				{
					result._intersectionPoint = result._intersectionPoint2 = p1;
				}
				else
				{
					result._intersectionPoint = result._intersectionPoint2 = p0 + (p1 - p0) * si;
				}
		
				//Check if the intersection point is a point of both lines. 
				if (
					!Equal(PointLineDistance2(result._intersectionPoint, p0, p1), 0) ||
					!Equal(PointLineDistance2(result._intersectionPoint, q0, q1), 0)
					)
				{
					result._intersectionType = IntersectionType.Undefined;
				}
			}

			return result.isIntersection();
		}

		public static bool LineLineIntersection(Vector2 p0, Vector2 p1, Vector2 q0, Vector2 q1, out IntersectionResult2 result)
		{
			result = new IntersectionResult2();

			Vector2 u = p1 - p0;
			Vector2 v = q1 - q0;
			Vector2 w = p0 - q0;

			if (Equal(u.Perpendicular().Dot(v), 0))
			{
				//Parallel lines. Check if they are coincident or not. 
				result._intersectionType = Equal(Vector2.Dot(Vector2.Perpendicular(w), v), 0)? IntersectionType.Coincident : IntersectionType.Parallel;
			}
			else
			{
				float si = -v.Perpendicular().Dot(w) / v.Perpendicular().Dot(u);
				if (Equal(si, 0))
				{
					result._intersectionPoint = result._intersectionPoint2 = p0;
				}
				else if (Equal(si, 1))
				{
					result._intersectionPoint = result._intersectionPoint2 = p1;
				}
				else
				{
					result._intersectionPoint = result._intersectionPoint2 = p0 + u * si;
				}

				result._intersectionType = IntersectionType.SinglePoint;
			}

			return result.isIntersection();
		}

		public static bool LineSegmentIntersection(Vector3 p0, Vector3 p1, Vector3 q0, Vector3 q1, out IntersectionResult3 result) 
		{
			LineLineIntersection(p0, p1, q0, q1, out result);
			if (result._intersectionType == IntersectionType.Coincident) 
			{
				result._intersectionPoint = q0;
				result._intersectionPoint2 = q1;
			}
			else if (result._intersectionType == IntersectionType.SinglePoint) 
			{
				if (!IsPointInSegment(result._intersectionPoint, q0, q1)) 
				{
					result._intersectionType = IntersectionType.Undefined;
				}
			}

			return result.isIntersection();
		}

		#endregion

		#region LINE PLANE INTERSECTION

		public static bool LinePlaneIntersection(Vector3 p0, Vector3 p1, QuickPlane plane, out IntersectionResult3 result) 
		{
			result = new IntersectionResult3();

			Vector3 v0 = plane._v0; ;
			Vector3 n = plane._normal;
			Vector3 u = p1 - p0;
			Vector3 w = p0 - v0;
			float d = Vector3.Dot(n, u);
			if (Equal(d, 0)) 
			{
				//The line and the plane are parallel. Check if the line is contained in the plane or not. 
				//We check if one of the endpoints of the line is also a point of the plane, so:
				float dist = PointToPlaneDistanceAbs(p0, v0, n); //plane.GetDistanceToPoint(p0);
				if (Equal(dist, 0))
				{
					result._intersectionType = IntersectionType.Coincident;
					result._intersectionPoint = PointToPlaneProjection(p0, v0, n);	//p0;
					result._intersectionPoint2 = PointToPlaneProjection(p1, v0, n); //p1;
				}
				else
				{
					result._intersectionType = IntersectionType.Parallel;
				}
			}
			else
			{
				//The line and the plane are not parallel, so they intersect in a single point
				float si = Vector3.Dot(n, -w) / d;
				if (Equal(si, 0))
				{
					result._intersectionPoint = result._intersectionPoint2 = p0;
				}
				else if (Equal(si, 1))
				{
					result._intersectionPoint = result._intersectionPoint2 = p1;
				}
				else
				{
					result._intersectionPoint = result._intersectionPoint2 = p0 + u * si;
				}

				//Check if the intersection point is in the direction pointed by the 
				//hintDir of the plane. 
				//Vector3 t = result._intersectionPoint - v0;
				//result._intersectionType = (Math::greaterOrEqual(plane._hintDir.dot(t), 0)) ? IntersectionType.SinglePoint : IntersectionType.Undefined;

				result._intersectionType = IntersectionType.SinglePoint;
			}

			return result.isIntersection();
		}

		public static bool RayPlaneIntersection(Ray ray, QuickPlane plane, out IntersectionResult3 result)
		{
			LinePlaneIntersection(ray.origin, ray.origin + ray.direction, plane, out result);
			if ((result._intersectionType == IntersectionType.SinglePoint) && Less(Vector3.Dot(ray.direction, result._intersectionPoint - ray.origin), 0))
			{
				result._intersectionType = IntersectionType.Undefined;
			}

			return result.isIntersection();
		}

		public static bool SegmentPlaneIntersection(Vector3 p0, Vector3 p1, QuickPlane plane, out IntersectionResult3 result)
		{
			LinePlaneIntersection(p0, p1, plane, out result);
			if ((result._intersectionType == IntersectionType.SinglePoint) && !IsPointInSegment(result._intersectionPoint, p0, p1))
			{
				result._intersectionType = IntersectionType.Undefined;
			}

			return result.isIntersection();
		}

        #endregion

        #region LINE TRIANGLE INTERSECTION

		//public static bool LineTriangleIntersection(Vector3 p0, Vector3 p1, QuickTriangle triangle, out IntersectionResult3 result)
		//{
		//	Vector3 a = triangle._v0;
		//	Vector3 b = triangle._v1;
		//	Vector3 c = triangle._v2;
		//	LinePlaneIntersection(p0, p1, new QuickPlane(a, triangle._normal), out result);

		//	if (result._intersectionType == IntersectionType.SinglePoint)
		//	{
		//		//Check the barycentric coordinates of the single intersection point. 
		//		if (!triangle.ComputeBarycentricCoordinates(result._intersectionPoint, out Vector3 bCoordinates))
		//		{
		//			result._intersectionType = IntersectionType.Undefined;
		//		}
		//	}
		//	else if (result._intersectionType == IntersectionType.Coincident)
		//	{
		//		//Check if the line intersects any of the edges of the triangle
		//		LineSegmentIntersection(p0, p1, a, b, out IntersectionResult3 tmpAB);
		//		LineSegmentIntersection(p0, p1, b, c, out IntersectionResult3 tmpBC);
		//		LineSegmentIntersection(p0, p1, c, a, out IntersectionResult3 tmpCA);

		//		//Case 1: The line is coincident with one of the edges of the triangle
		//		if (tmpAB._intersectionType == IntersectionType.Coincident) result = tmpAB;
		//		else if (tmpBC._intersectionType == IntersectionType.Coincident) result = tmpBC;
		//		else if (tmpCA._intersectionType == IntersectionType.Coincident) result = tmpCA;

		//		//Case 2: The line intersects 2 edges of the triangle in a single point each one. 
		//		else if ((tmpAB._intersectionType == IntersectionType.SinglePoint) && (tmpBC._intersectionType == IntersectionType.SinglePoint))
		//		{
		//			result._intersectionPoint = tmpAB._intersectionPoint;
		//			result._intersectionPoint2 = tmpBC._intersectionPoint;
		//		}
		//		else if ((tmpAB._intersectionType == IntersectionType.SinglePoint) && (tmpCA._intersectionType == IntersectionType.SinglePoint))
		//		{
		//			result._intersectionPoint = tmpAB._intersectionPoint;
		//			result._intersectionPoint2 = tmpCA._intersectionPoint;
		//		}
		//		else if ((tmpBC._intersectionType == IntersectionType.SinglePoint) && (tmpCA._intersectionType == IntersectionType.SinglePoint))
		//		{
		//			result._intersectionPoint = tmpBC._intersectionPoint;
		//			result._intersectionPoint2 = tmpCA._intersectionPoint;
		//		}

		//		//Case 3: No intersection at all. 
		//		else
		//		{
		//			result._intersectionType = IntersectionType.Undefined;
		//		}
		//	}

		//	return result.isIntersection();
		//}

		//public static bool RayTriangleIntersection(Ray ray, QuickTriangle triangle, out IntersectionResult3 result)
		//{
		//	IntersectionResult3 tmp;
		//	LineTriangleIntersection(ray.origin, ray.origin + ray.direction, triangle, out tmp);
		//	result = tmp;

		//	bool uSign = Mathf.Sign(Vector3.Dot(ray.direction, result._intersectionPoint - ray.origin)) > 0;    //1 => intersectionPoint in the direction of the ray; -1 otherwise
		//	bool vSign = Mathf.Sign(Vector3.Dot(ray.direction, result._intersectionPoint2 - ray.origin)) > 0;	//1 => intersectionPoint2 in the direction of the ray; -1 otherwise

		//	if ((result._intersectionType == IntersectionType.SinglePoint) && !uSign)
		//	{
		//		result._intersectionType = IntersectionType.Undefined;
		//	}
		//	else if (result._intersectionType == IntersectionType.Coincident)
		//	{
		//		if (!uSign && !vSign)
		//		{
		//			//Both intersection points are behind the ray. So there is no intersection at all. 
		//			result._intersectionType = IntersectionType.Undefined;
		//		}
		//		else if (uSign && !vSign)
		//		{
		//			//The first intersection point is in the direction of the ray, but the second intersection point is behind. 
		//			result._intersectionPoint = ray.origin;
		//			result._intersectionPoint2 = tmp._intersectionPoint;
		//		}
		//		else if (!uSign && vSign)
		//		{
		//			//The first intersection point is behind the ray, but the second intersection point is in the direction of the ray. 
		//			result._intersectionPoint = ray.origin;
		//			result._intersectionPoint2 = tmp._intersectionPoint2;
		//		}
		//	}

		//	return result.isIntersection();
		//}

		public static bool IsPointInSegment(Vector3 point, Vector3 s0, Vector3 s1) 
		{
			//Indicates if point is in the finite segment defined by s0 and s1. 
			if (Equal(point, s0) || Equal(point, s1))
			{
				return true;
			}

			float d = PointLineDistance(point, s0, s1);
			if (Greater(d, QUICK_FLOAT_PRECISION)) return false;

			float segmentLength = Vector3.Distance(s0, s1);
			float d0 = Vector3.Distance(s0, point);
			float d1 = Vector3.Distance(s1, point);

			return (LessOrEqual(d0, segmentLength) && LessOrEqual(d1, segmentLength));
		}

        #endregion

    }

}
