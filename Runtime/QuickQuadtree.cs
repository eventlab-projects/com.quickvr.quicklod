using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuickVR
{

	public struct QuickQuadtreeValue<T>
    {

		public Bounds _bounds;
		public T _value;

    }

	public abstract class QuickQuadtreeNode
    {

		protected QuickQuadtree _quadtree = null;

		protected Bounds _aabb;
		protected QuickQuadtreeNode _parent = null;
		protected List<QuickQuadtreeNode> _childs = new List<QuickQuadtreeNode>();

		protected Bounds[] _childBoxes = new Bounds[4];

		public QuickQuadtreeNode(Bounds aabb, QuickQuadtreeNode parent, QuickQuadtree quadtree)
		{
			_aabb = aabb;
			_parent = parent;
			_quadtree = quadtree;

			Vector3 center = _aabb.center;
			Vector3 min = _aabb.min;
			Vector3 max = _aabb.max;
			_childBoxes[0] = CreateBounds(min, center);
			_childBoxes[1] = CreateBounds(new Vector3(center.x, min.y, 0), new Vector3(max.x, center.y, 0));
			_childBoxes[2] = CreateBounds(new Vector3(min.x, center.y, 0), new Vector3(center.x, max.y, 0));
			_childBoxes[3] = CreateBounds(center, max);
		}

		protected Bounds CreateBounds(Vector3 min, Vector3 max)
		{
			Bounds result = new Bounds();
			result.SetMinMax(min, max);

			return result;
		}

		public virtual QuickQuadtreeNode GetParent()
		{
			return _parent;
		}

		public virtual List<QuickQuadtreeNode> GetChilds()
		{
			return _childs;
		}

		protected virtual void AddChild(QuickQuadtreeNode c)
		{
			_childs.Add(c);
		}

		public virtual Bounds GetBounds()
		{
			return _aabb;
		}

		public abstract bool IsEmpty();

	}

	public class QuickQuadtreeNode<T> : QuickQuadtreeNode
	{

        #region PROTECTED ATTRIBUTES

		protected HashSet<QuickQuadtreeValue<T>> _objects = new HashSet<QuickQuadtreeValue<T>>();

        public QuickQuadtreeNode(Bounds aabb, QuickQuadtreeNode parent, QuickQuadtree Quadtree) : base(aabb, parent, Quadtree)
        {

        }

        #endregion

        #region CREATION AND DESTRUCTION

        ~QuickQuadtreeNode()
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

		public virtual void Insert(QuickQuadtreeValue<T> obj)
		{
			_objects.Add(obj);

			//Check for the leaf conditions, i.e., there is only one object in the list or
			//the Quadtree has reached the minimum size, i.e., its current size is > 2 * _Quadtree._minNodeSize.
			float minSize = _quadtree.GetMinNodeSize();
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
            //		AddChild(new QuickQuadtreeNode<T>(b, this, _Quadtree));
            //	}

            //	//2.2) Check if the objects are contained in any of the newly created child and update the object lists
            //	//accordingly. 
            //	HashSet<QuickQuadtreeValue<T>> tmp = new HashSet<QuickQuadtreeValue<T>>(_objects);
            //	foreach (QuickQuadtreeValue<T> itObject in tmp)
            //	{
            //		Bounds aabb = itObject._bounds;
            //		int i = 0;
            //		for (; i < _childs.Count; i++)
            //		{
            //			if (_childs[i].GetBounds().Intersects(aabb))
            //                     {
            //				((QuickQuadtreeNode<T>)_childs[i]).Insert(itObject);
            //			}
            //			if (_childs[i].GetBounds().Contains(aabb))
            //			{
            //				((QuickQuadtreeNode<T>)_childs[i]).Insert(itObject);
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
                    AddChild(new QuickQuadtreeNode<T>(b, this, _quadtree));
                }

                //2.2) Check if the objects are contained in any of the newly created child and update the object lists
                //accordingly. 
                HashSet<QuickQuadtreeValue<T>> tmp = new HashSet<QuickQuadtreeValue<T>>(_objects);
                foreach (QuickQuadtreeValue<T> itObject in tmp)
                {
                    Bounds aabb = itObject._bounds;
                    int i = 0;
                    for (; i < _childs.Count; i++)
                    {
                        if (_childs[i].GetBounds().Contains(aabb))
                        {
                            ((QuickQuadtreeNode<T>)_childs[i]).Insert(itObject);
                            _objects.Remove(itObject);
                            break;
                        }
                    }
                }
            }

			foreach (QuickQuadtreeNode<T> c in _childs)
            {
				if (c.GetBounds().Intersects(obj._bounds))
                {
					c.Insert(obj);
                }
            }
        }

		public virtual HashSet<QuickQuadtreeValue<T>> GetObjects()
        {
			return _objects;
        }

		public virtual HashSet<QuickQuadtreeValue<T>> GetObjects(Bounds bounds)
        {
			HashSet<QuickQuadtreeValue<T>> result = new HashSet<QuickQuadtreeValue<T>>();

			if (_aabb.Intersects(bounds))
            {
				result = _objects;
				foreach (QuickQuadtreeNode<T> nChild in _childs)
                {
					result.UnionWith(nChild.GetObjects(bounds));
                }
            }

			return result;
		}

		public virtual HashSet<QuickQuadtreeValue<T>> GetObjectsRec()
        {
			HashSet<QuickQuadtreeValue<T>> result = new HashSet<QuickQuadtreeValue<T>>();

			QuickQuadtreeNode<T> tmp = this;
			do
			{
				foreach (QuickQuadtreeValue<T> obj in tmp.GetObjects())
				{
					result.Add(obj);
				}

				tmp = (QuickQuadtreeNode<T>)tmp.GetParent();
			} while (tmp != null);

			return result;
		}

		public virtual void Remove(QuickQuadtreeValue<T> obj)
		{
			_objects.Remove(obj);
		}

		public virtual bool Contains(QuickQuadtreeValue<T> obj)
		{
			return _objects.Contains(obj);
		}

		public virtual void ComputeIntersectedObjects(Bounds aabb, ref List<T> result)
		{
			//1) Check for intersections with the objects contained in this node. 
			foreach (QuickQuadtreeValue<T> it in _objects)
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
					((QuickQuadtreeNode<T>)_childs[i]).ComputeIntersectedObjects(aabb, ref result);
				}
			}
		}

		public virtual void ComputeIntersectedObjects(Ray ray, ref HashSet<T> result, float maxDistance = Mathf.Infinity)
		{
			//1) Check for intersections with the objects contained in this node.
			foreach (QuickQuadtreeValue<T> it in _objects)
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
					((QuickQuadtreeNode<T>)_childs[i]).ComputeIntersectedObjects(ray, ref result);
				}
			}
		}

	};

	public abstract class QuickQuadtree
    {
		
		#region PROTECTED ATTRIBUTES

		protected QuickQuadtreeNode _root = null;
		protected float _minNodeSize = 1;

        #endregion

        #region GET AND SET

		public virtual float GetMinNodeSize()
        {
			return _minNodeSize;
        }

		public virtual QuickQuadtreeNode GetRoot()
		{
			return _root;
		}

		public virtual QuickQuadtreeNode FindContainer(Vector3 p)
		{
			//Find the node of the Quadtree that contains point p (if any)
			if (!_root.GetBounds().Contains(p))
			{
				return null;
			}

			return FindContainer(p, _root);
		}

		public virtual QuickQuadtreeNode FindContainer(Bounds aabb)
		{
			//Find the node of the Quadtree that perfectly contains the defined AABB
			if (!_root.GetBounds().Contains(aabb))
			{
				return null;
			}

			return FindContainer(aabb, _root);
		}

		protected virtual QuickQuadtreeNode FindContainer(Vector3 p, QuickQuadtreeNode node)
		{
			//Look for the child that completely contains the object (if any). 
			List<QuickQuadtreeNode> childs = node.GetChilds();
			QuickQuadtreeNode container = null;
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

		protected virtual QuickQuadtreeNode FindContainer(Bounds aabb, QuickQuadtreeNode node)
		{
			//Look for the child that completely contains the object (if any). 
			List<QuickQuadtreeNode> childs = node.GetChilds();
			QuickQuadtreeNode container = null;
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

		protected virtual bool IsEmptyBranch(QuickQuadtreeNode node)
		{
			//Indicates if the branch starting at node is empty or not, i.e., the node nor any of its childs
			//contains any object. 
			List<QuickQuadtreeNode> childs = node.GetChilds();
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

    public class QuickQuadtree<T> : QuickQuadtree
	{

        #region CREATION AND DESTRUCTION

        public QuickQuadtree(Bounds aabb, float minNodeSize) 
		{
			//The AABB passed as parameter is just a hint for the size of the 
			//AABB of the whole tree. By convention, we are going to construct a 
			//cubic Quadtree, with a size that is power of 2. 
			Bounds rootAABB = aabb;
			Vector3 size = rootAABB.size;
			float m = Mathf.Max(size.x, size.y, size.z);
			float s = Mathf.NextPowerOfTwo((int)Mathf.Ceil(m));
			rootAABB.SetMinMax(rootAABB.min, rootAABB.min + new Vector3(s, s, s));
			rootAABB.center = aabb.center;

			_root = new QuickQuadtreeNode<T>(rootAABB, null, this);
			_minNodeSize = minNodeSize;
		}

        #endregion

        #region GET AND SET

		public virtual QuickQuadtreeNode FindContainer(QuickQuadtreeValue<T> obj)
		{
			//Find the node of the Quadtree that perfectly contains the object. 
			return FindContainer(obj._bounds);
		}

		public virtual void Insert(QuickQuadtreeValue<T> obj)
		{
			QuickQuadtreeNode<T> node = (QuickQuadtreeNode<T>)FindContainer(obj);

			if (node == null) return;
			
			node.Insert(obj);
		}

		public virtual void Remove(QuickQuadtreeValue<T> obj)
		{
			QuickQuadtreeNode<T> node = (QuickQuadtreeNode<T>)FindContainer(obj);

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
            ((QuickQuadtreeNode<T>)_root).ComputeIntersectedObjects(aabb, ref result);
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
			((QuickQuadtreeNode<T>)_root).ComputeIntersectedObjects(ray, ref result, maxDistance);
		}

        #endregion

    };

};
