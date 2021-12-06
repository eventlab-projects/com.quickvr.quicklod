using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuickVR
{

	public static class QuickBoundsExtensions
	{
		
		public static bool Contains(this Bounds bounds, Bounds b)
        {
			return bounds.Contains(b.min) && bounds.Contains(b.max);
        }

	}

	public struct QuickOctreeValue<T>
    {

		public Bounds _bounds;
		public T _value;

    }

	public abstract class QuickOctreeNode
    {

		protected QuickOctree _octree = null;

		protected Bounds _aabb;
		protected QuickOctreeNode _parent = null;
		protected List<QuickOctreeNode> _childs = new List<QuickOctreeNode>();

		protected Bounds[] _childBoxes = new Bounds[8];

		public QuickOctreeNode(Bounds aabb, QuickOctreeNode parent, QuickOctree octree)
		{
			_aabb = aabb;
			_parent = parent;
			_octree = octree;

			Vector3 center = _aabb.center;
			Vector3 min = _aabb.min;
			Vector3 max = _aabb.max;
			_childBoxes[0] = CreateBounds(min, center);
			_childBoxes[1] = CreateBounds(new Vector3(center.x, min.y, min.z), new Vector3(max.x, center.y, center.z));
			_childBoxes[2] = CreateBounds(new Vector3(center.x, min.y, center.z), new Vector3(max.x, center.y, max.z));
			_childBoxes[3] = CreateBounds(new Vector3(min.x, min.y, center.z), new Vector3(center.x, center.y, max.z));
			_childBoxes[4] = CreateBounds(new Vector3(min.x, center.y, min.z), new Vector3(center.x, max.y, center.z));
			_childBoxes[5] = CreateBounds(new Vector3(center.x, center.y, min.z), new Vector3(max.x, max.y, center.z));
			_childBoxes[6] = CreateBounds(center, max);
			_childBoxes[7] = CreateBounds(new Vector3(min.x, center.y, center.z), new Vector3(center.x, max.y, max.z));
		}

		protected Bounds CreateBounds(Vector3 min, Vector3 max)
		{
			Bounds result = new Bounds();
			result.SetMinMax(min, max);

			return result;
		}

		public virtual QuickOctreeNode GetParent()
		{
			return _parent;
		}

		public virtual List<QuickOctreeNode> GetChilds()
		{
			return _childs;
		}

		protected virtual void AddChild(QuickOctreeNode c)
		{
			_childs.Add(c);
		}

		public virtual Bounds GetBounds()
		{
			return _aabb;
		}

		public abstract bool IsEmpty();

	}

	public class QuickOctreeNode<T> : QuickOctreeNode
	{

        #region PROTECTED ATTRIBUTES

		protected HashSet<QuickOctreeValue<T>> _objects = new HashSet<QuickOctreeValue<T>>();

        public QuickOctreeNode(Bounds aabb, QuickOctreeNode parent, QuickOctree octree) : base(aabb, parent, octree)
        {

        }

        #endregion

        #region CREATION AND DESTRUCTION

        ~QuickOctreeNode()
		{
			Clear();
		}

		#endregion

        public virtual void Clear()
		{
			_childs.Clear();
			_objects.Clear();
		}

		public override bool IsEmpty()
		{
			return _objects.Count == 0;
		}

		public virtual void Insert(QuickOctreeValue<T> obj)
		{
			_objects.Add(obj);

			//Check for the leaf conditions, i.e., there is only one object in the list or
			//the octree has reached the minimum size, i.e., its current size is > 2 * _octree._minNodeSize.
			float minSize = _octree.GetMinNodeSize();
			float childSize = _aabb.size.x * 0.5f;
			if (_objects.Count == 1 || MathfExtensions.Less(childSize, minSize))
            {
				return;
            }

            //The node is not a leaf, so it is possible that we need to subdivide the node, if the 
            //object is completely contained in one of the childs. 

            ////1) Check if any of the child boxes completely contains the object. 
            //int iContainer = 0;
            //while ((iContainer < _childBoxes.Length) && !_childBoxes[iContainer].Contains(obj._bounds)) 
            //{
            //	iContainer++;
            //}
            //if (iContainer < _childBoxes.Length)
            //{
            //	//The object IS completely contained in the boxes[iContainer]. So we have to create the childs and 
            //	//(possibly) insert the objects of this node in the corresponding childs. 

            //	//2.1) Create the childs
            //	foreach (Bounds b in _childBoxes)
            //             {
            //		AddChild(new QuickOctreeNode<T>(b, this, _octree));
            //	}

            //	//2.2) Check if the objects are contained in any of the newly created child and update the object lists
            //	//accordingly. 
            //	HashSet<QuickOctreeValue<T>> tmp = new HashSet<QuickOctreeValue<T>>(_objects);
            //	foreach (QuickOctreeValue<T> itObject in tmp)
            //	{
            //		Bounds aabb = itObject._bounds;
            //		int i = 0;
            //		for (; i < _childs.Count; i++)
            //		{
            //			if (_childs[i].GetBounds().Intersects(aabb))
            //                     {
            //				((QuickOctreeNode<T>)_childs[i]).Insert(itObject);
            //			}
            //			if (_childs[i].GetBounds().Contains(aabb))
            //			{
            //				((QuickOctreeNode<T>)_childs[i]).Insert(itObject);
            //				_objects.Remove(itObject);
            //			}
            //		}
            //	}
            //}

            //1) Check if any of the child boxes completely contains the object. 
            int iContainer = 0;
            while ((iContainer < _childBoxes.Length) && !_childBoxes[iContainer].Contains(obj._bounds))
            {
                iContainer++;
            }
            if (iContainer < _childBoxes.Length)
            {
                //The object IS completely contained in the boxes[iContainer]. So we have to create the childs and 
                //(possibly) insert the objects of this node in the corresponding childs. 

                //2.1) Create the childs
                foreach (Bounds b in _childBoxes)
                {
                    AddChild(new QuickOctreeNode<T>(b, this, _octree));
                }

                //2.2) Check if the objects are contained in any of the newly created child and update the object lists
                //accordingly. 
                HashSet<QuickOctreeValue<T>> tmp = new HashSet<QuickOctreeValue<T>>(_objects);
                foreach (QuickOctreeValue<T> itObject in tmp)
                {
                    Bounds aabb = itObject._bounds;
                    int i = 0;
                    for (; i < _childs.Count; i++)
                    {
                        if (_childs[i].GetBounds().Contains(aabb))
                        {
                            ((QuickOctreeNode<T>)_childs[i]).Insert(itObject);
                            _objects.Remove(itObject);
                            break;
                        }
                    }
                }
            }

			foreach (QuickOctreeNode<T> c in _childs)
            {
				if (c.GetBounds().Intersects(obj._bounds))
                {
					c.Insert(obj);
                }
            }
        }

		public virtual HashSet<QuickOctreeValue<T>> GetObjects()
        {
			return _objects;
        }

		public virtual HashSet<QuickOctreeValue<T>> GetObjects(Bounds bounds)
        {
			HashSet<QuickOctreeValue<T>> result = new HashSet<QuickOctreeValue<T>>();

			if (_aabb.Intersects(bounds))
            {
				result = _objects;
				foreach (QuickOctreeNode<T> nChild in _childs)
                {
					result.UnionWith(nChild.GetObjects(bounds));
                }
            }

			return result;
		}

		public virtual HashSet<QuickOctreeValue<T>> GetObjectsRec()
        {
			HashSet<QuickOctreeValue<T>> result = new HashSet<QuickOctreeValue<T>>();

			QuickOctreeNode<T> tmp = this;
			do
			{
				foreach (QuickOctreeValue<T> obj in tmp.GetObjects())
				{
					result.Add(obj);
				}

				tmp = (QuickOctreeNode<T>)tmp.GetParent();
			} while (tmp != null);

			return result;
		}

		public virtual void Remove(QuickOctreeValue<T> obj)
		{
			_objects.Remove(obj);
		}

		public virtual bool Contains(QuickOctreeValue<T> obj)
		{
			return _objects.Contains(obj);
		}

		public virtual void ComputeIntersectedObjects(Bounds aabb, ref List<T> result)
		{
			//1) Check for intersections with the objects contained in this node. 
			foreach (QuickOctreeValue<T> it in _objects)
			{
				if (aabb.Intersects(it._bounds))
				{
					result.Add(it._value);
				}
			}

			//2) Check if aabb intersects with the aabb of any of the childs. If so, recursive call. 
			for (int i = 0; i < _childs.Count; i++)
			{
				if (aabb.Intersects(_childs[i].GetBounds()))
				{
					((QuickOctreeNode<T>)_childs[i]).ComputeIntersectedObjects(aabb, ref result);
				}
			}
		}

		public virtual void ComputeIntersectedObjects(Ray ray, ref HashSet<T> result, float maxDistance = Mathf.Infinity)
		{
			//1) Check for intersections with the objects contained in this node.
			foreach (QuickOctreeValue<T> it in _objects)
			{
				if (it._bounds.IntersectRay(ray, out float d) && d <= maxDistance)
				{
					result.Add(it._value);
				}
			}

			//2) Check if ray intersects with the aabb of any of the childs. If so, recursive call. 
			for (int i = 0; i < _childs.Count; i++)
			{
				if (_childs[i].GetBounds().IntersectRay(ray))
				{
					((QuickOctreeNode<T>)_childs[i]).ComputeIntersectedObjects(ray, ref result);
				}
			}
		}

	};

	public abstract class QuickOctree
    {
		
		#region PROTECTED ATTRIBUTES

		protected QuickOctreeNode _root = null;
		protected float _minNodeSize = 1;

        #endregion

        #region GET AND SET

		public virtual float GetMinNodeSize()
        {
			return _minNodeSize;
        }

		public virtual QuickOctreeNode GetRoot()
		{
			return _root;
		}

		public virtual QuickOctreeNode FindContainer(Vector3 p)
		{
			//Find the node of the octree that contains point p (if any)
			if (!_root.GetBounds().Contains(p))
			{
				return null;
			}

			return FindContainer(p, _root);
		}

		public virtual QuickOctreeNode FindContainer(Bounds aabb)
		{
			//Find the node of the octree that perfectly contains the defined AABB
			if (!_root.GetBounds().Contains(aabb))
			{
				return null;
			}

			return FindContainer(aabb, _root);
		}

		protected virtual QuickOctreeNode FindContainer(Vector3 p, QuickOctreeNode node)
		{
			//Look for the child that completely contains the object (if any). 
			List<QuickOctreeNode> childs = node.GetChilds();
			QuickOctreeNode container = null;
			int i = 0;
			while (container == null && (i < childs.Count))
			{
				if (childs[i].GetBounds().Contains(p))
				{
					container = childs[i];
				}
				i++;
			}

			return container != null ? FindContainer(p, container) : node;
		}

		protected virtual QuickOctreeNode FindContainer(Bounds aabb, QuickOctreeNode node)
		{
			//Look for the child that completely contains the object (if any). 
			List<QuickOctreeNode> childs = node.GetChilds();
			QuickOctreeNode container = null;
			int i = 0;
			while (container == null && (i < childs.Count))
			{
				if (childs[i].GetBounds().Contains(aabb))
				{
					container = childs[i];
				}
				i++;
			}

			return container != null ? FindContainer(aabb, container) : node;
		}

		protected virtual bool IsEmptyBranch(QuickOctreeNode node)
		{
			//Indicates if the branch starting at node is empty or not, i.e., the node nor any of its childs
			//contains any object. 
			List<QuickOctreeNode> childs = node.GetChilds();
			int i = 0;
			bool empty = node.IsEmpty();
			while (empty && (i < childs.Count))
			{
				empty = empty && IsEmptyBranch(childs[i]);
				i++;
			}

			return empty;
		}

		#endregion

	}

    public class QuickOctree<T> : QuickOctree
	{

        #region CREATION AND DESTRUCTION

        public QuickOctree(Bounds aabb, float minNodeSize) 
		{
			//The AABB passed as parameter is just a hint for the size of the 
			//AABB of the whole tree. By convention, we are going to construct a 
			//cubic Octree, with a size that is power of 2. 
			Bounds rootAABB = aabb;
			Vector3 size = rootAABB.size;
			float m = Mathf.Max(size.x, size.y, size.z);
			float s = Mathf.NextPowerOfTwo((int)Mathf.Ceil(m));
			rootAABB.SetMinMax(rootAABB.min, rootAABB.min + new Vector3(s, s, s));
			rootAABB.center = aabb.center;

			_root = new QuickOctreeNode<T>(rootAABB, null, this);
			_minNodeSize = minNodeSize;
		}

        #endregion

        #region GET AND SET

		public virtual QuickOctreeNode FindContainer(QuickOctreeValue<T> obj)
		{
			//Find the node of the octree that perfectly contains the object. 
			return FindContainer(obj._bounds);
		}

		public virtual void Insert(QuickOctreeValue<T> obj)
		{
			QuickOctreeNode<T> node = (QuickOctreeNode<T>)FindContainer(obj);

			if (node == null) return;
			
			node.Insert(obj);
		}

		public virtual void Remove(QuickOctreeValue<T> obj)
		{
			QuickOctreeNode<T> node = (QuickOctreeNode<T>)FindContainer(obj);

			if (node == null) return;
			node.Remove(obj);

			//Check the integrity of the branch after removing this object. 
			if (IsEmptyBranch(node))
			{
				node.Clear();
			}
		}

        //public virtual void ComputeIntersectedObjects(const Bounds& aabb, ElementIDSet& result)
        //{
        //	std::set<T, U> tmp;
        //	computeIntersectedObjects(aabb, tmp);
        //	for (std::set<T, U>::iterator it = tmp.begin(); it != tmp.end(); it++)
        //	{
        //		result.insert((*it)->getID());
        //	}
        //}

        public virtual void ComputeIntersectedObjects(Bounds aabb, ref List<T> result)
        {
            ((QuickOctreeNode<T>)_root).ComputeIntersectedObjects(aabb, ref result);
        }

        //virtual void computeIntersectedObjects(const NeoGeometrySolver::Ray3& ray, ElementIDSet& result)
        //{
        //	std::set<T, U> tmp;
        //	computeIntersectedObjects(ray, tmp);
        //	for (std::set<T, U>::iterator it = tmp.begin(); it != tmp.end(); it++)
        //	{
        //		result.insert((*it)->getID());
        //	}
        //}

        public virtual void ComputeIntersectedObjects(Ray ray, ref HashSet<T> result, float maxDistance = Mathf.Infinity)
		{
			((QuickOctreeNode<T>)_root).ComputeIntersectedObjects(ray, ref result, maxDistance);
		}

        #endregion

    };

};
