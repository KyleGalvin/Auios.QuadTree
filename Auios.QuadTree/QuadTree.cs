﻿// Copyright 2020 Connor Andrew Ngo
// Licensed under the MIT License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Auios.QuadTree
{
    /// <summary>
    /// A tree data structure in which each node has exactly four children. 
    /// Used to partition a two-dimensional space by recursively subdividing it into four quadrants.
    /// Allows to efficiently find objects spatially relative to each other.
    /// </summary>
    /// <typeparam name="T">The type of elements in the QuadTree.</typeparam>
    public class QuadTree<T>
    {
        /// <summary>The area of this quadrant.</summary>
        public QuadTreeRect Area;
        /// <summary>Objects in this quadrant.</summary>
        private readonly HashSet<T> _objects;
        /// <summary>The bounds which this quadrant expects its objects to conform to.</summary>
        private readonly IQuadTreeObjectBounds<T> _objectBounds;
        /// <summary>If this quadrant has sub quadrants. Objects only exist on quadrants without children.</summary>
        private bool _hasChildren;

        /// <summary>Top left quadrant.</summary>
        private QuadTree<T> quad_TL;
        /// <summary>Top right quadrant.</summary>
        private QuadTree<T> quad_TR;
        /// <summary>Bottom left quadrant.</summary>
        private QuadTree<T> quad_BL;
        /// <summary>Bottom right quadrant.</summary>
        private QuadTree<T> quad_BR;

        /// <summary>Gets the current depth level of this quadrant.</summary>
        public int CurrentLevel { get; }
        /// <summary>Gets the max depth level.</summary>
        public int MaxLevel { get; }
        /// <summary>Gets the max number of objects in this quadrant.</summary>
        public int MaxObjects { get; }

        /// <summary>Initializes a new instance of the <see cref="T:Auios.QuadTree.QuadTree`1"></see> class.</summary>
        /// <param name="x">The x-coordinate of the upper-left corner of the boundary rectangle.</param>
        /// <param name="y">The y-coordinate of the upper-left corner of the boundary rectangle.</param>
        /// <param name="width">The width of the boundary rectangle.</param>
        /// <param name="height">The height of the boundary rectangle.</param>
        /// <param name="objectBounds">The set of methods for getting boundaries of an element.</param>
        /// <param name="maxObjects">The max number of elements in one rectangle.</param>
        /// <param name="maxLevel">The max depth level.</param>
        /// <param name="currentLevel">The current depth level. Leave default if this is the root QuadTree.</param>
        public QuadTree(float x, float y, float width, float height, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0)
        {
            Area = new QuadTreeRect(x, y, width, height);
            _objects = new HashSet<T>();
            _objectBounds = objectBounds;

            CurrentLevel = currentLevel;
            MaxLevel = maxLevel;
            MaxObjects = maxObjects;

            _hasChildren = false;
        }

        /// <summary>Initializes a new instance of the <see cref="T:Auios.QuadTree.QuadTree`1"></see> class.</summary>
        /// <param name="width">The width of the boundary rectangle.</param>
        /// <param name="height">The height of the boundary rectangle.</param>
        /// <param name="objectBounds">The set of methods for getting boundaries of an element.</param>
        /// <param name="maxObjects">The max number of elements in one rectangle.</param>
        /// <param name="maxLevel">The max depth level.</param>
        /// <param name="currentLevel">The current depth level. Leave default if this is the root QuadTree.</param>
        public QuadTree(float width, float height, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0)
            : this(0, 0, width, height, objectBounds, maxObjects, maxLevel, currentLevel) { }

        /// <summary>Initializes a new instance of the <see cref="T:Auios.QuadTree.QuadTree`1"></see> class.</summary>
        /// <param name="size">The size of the boundary rectangle.</param>
        /// <param name="objectBounds">The set of methods for getting boundaries of an element.</param>
        /// <param name="maxObjects">The max number of elements in one rectangle.</param>
        /// <param name="maxLevel">The max depth level.</param>
        /// <param name="currentLevel">The current depth level. Leave default if this is the root QuadTree.</param>
        public QuadTree(Vector2 size, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0)
            : this(0, 0, size.X, size.Y, objectBounds, maxObjects, maxLevel, currentLevel) { }

        /// <summary>Initializes a new instance of the <see cref="T:Auios.QuadTree.QuadTree`1"></see> class.</summary>
        /// <param name="position">The position of the boundary rectangle.</param>
        /// <param name="size">The size of the boundary rectangle.</param>
        /// <param name="objectBounds">The set of methods for getting boundaries of an element.</param>
        /// <param name="maxObjects">The max number of elements in one rectangle.</param>
        /// <param name="maxLevel">The max depth level.</param>
        /// <param name="currentLevel">The current depth level. Leave default if this is the root QuadTree.</param>
        public QuadTree(Vector2 position, Vector2 size, IQuadTreeObjectBounds<T> objectBounds, int maxObjects = 10, int maxLevel = 5, int currentLevel = 0)
            : this(position.X, position.Y, size.X, size.Y, objectBounds, maxObjects, maxLevel, currentLevel) { }

        private bool IsObjectInside(T obj)
        {
            if (_objectBounds.GetTop(obj) > Area.Bottom) return false;
            if (_objectBounds.GetBottom(obj) < Area.Top) return false;
            if (_objectBounds.GetLeft(obj) > Area.Right) return false;
            if (_objectBounds.GetRight(obj) < Area.Left) return false;
            return true;
        }

        /// <summary>Checks if the current quadrant is overlapping with a <see cref="T:Auios.QuadTree.QuadTreeRect"></see></summary>
        private bool IsOverlapping(QuadTreeRect rect)
        {
            if (rect.Right < Area.Left || rect.Left > Area.Right) return false;
            if (rect.Top > Area.Bottom || rect.Bottom < Area.Top) return false;
            Area.isOverlapped = true;
            return true;
        }

        /// <summary>Splits the current quadrant into four new quadrants and drops all objects to the lower quadrants.</summary>
        private void Quarter()
        {
            if (CurrentLevel >= MaxLevel) return;

            int nextLevel = CurrentLevel + 1;
            _hasChildren = true;
            quad_TL = new QuadTree<T>(Area.X, Area.Y, Area.HalfWidth, Area.HalfHeight, _objectBounds, MaxObjects, MaxLevel, nextLevel);
            quad_TR = new QuadTree<T>(Area.CenterX, Area.Y, Area.HalfWidth, Area.HalfHeight, _objectBounds, MaxObjects, MaxLevel, nextLevel);
            quad_BL = new QuadTree<T>(Area.X, Area.CenterY, Area.HalfWidth, Area.HalfHeight, _objectBounds, MaxObjects, MaxLevel, nextLevel);
            quad_BR = new QuadTree<T>(Area.CenterX, Area.CenterY, Area.HalfWidth, Area.HalfHeight, _objectBounds, MaxObjects, MaxLevel, nextLevel);

            foreach (T obj in _objects)
            {
                Insert(obj);
            }
            _objects.Clear();
        }

        /// <summary> Removes all elements from the <see cref="T:Auios.QuadTree.QuadTree`1"></see>.</summary>
        public void Clear()
        {
            if(_hasChildren)
            {
                quad_TL.Clear();
                quad_TL = null;
                quad_TR.Clear();
                quad_TR = null;
                quad_BL.Clear();
                quad_BL = null;
                quad_BR.Clear();
                quad_BR = null;
            }

            _objects.Clear();
            _hasChildren = false;
            Area.isOverlapped = false;
        }

        /// <summary> Inserts an object into the <see cref="T:Auios.QuadTree.QuadTree`1"></see>.</summary>
        /// <param name="obj">The object to insert.</param>
        /// <returns>true if the object is successfully added to the <see cref="T:Auios.QuadTree.QuadTree`1"></see>; false if object is not added to the <see cref="T:Auios.QuadTree.QuadTree`1"></see>.</returns>
        public bool Insert(T obj)
        {
            if(obj == null) throw new ArgumentNullException(nameof(obj));

            if (!IsObjectInside(obj)) return false;

            if (_hasChildren)
            {
                if (quad_TL.Insert(obj)) return true;
                if (quad_TR.Insert(obj)) return true;
                if (quad_BL.Insert(obj)) return true;
                if (quad_BR.Insert(obj)) return true;
            }
            else
            {
                _objects.Add(obj);
                if(_objects.Count > MaxObjects)
                {
                    Quarter();
                }
            }

            return true;
        }

        /// <summary> Inserts a collection of objects into the <see cref="T:Auios.QuadTree.QuadTree`1"></see>.</summary>
        /// <param name="objects">The collection of objects to insert.</param>
        public void InsertRange(IEnumerable<T> objects)
        {
            foreach(T obj in objects)
            {
                Insert(obj);
            }
        }

        /// <summary>Returns the total number of obejcts in the <see cref="T:Auios.QuadTree.QuadTree`1"></see> and its children.</summary>
        /// <returns>the total number of objects in this instance.</returns>
        public int Count()
        {
            int count = 0;
            if (_hasChildren)
            {
                count += quad_TL.Count();
                count += quad_TR.Count();
                count += quad_BL.Count();
                count += quad_BR.Count();
            }
            else
            {
                count = _objects.Count;
            }

            return count;
        }

        /// <summary> Returns every <see cref="T:Auios.QuadTree.QuadTreeRect"></see> from the <see cref="T:Auios.QuadTree.QuadTree`1"></see>.</summary>
        /// <returns> an array of <see cref="T:Auios.QuadTree.QuadTreeRect"></see> from the <see cref="T:Auios.QuadTree.QuadTree`1"></see>.</returns>
        public QuadTreeRect[] GetGrid()
        {
            List<QuadTreeRect> grid = new List<QuadTreeRect> {Area};
            if (_hasChildren)
            {
                grid.AddRange(quad_TL.GetGrid());
                grid.AddRange(quad_TR.GetGrid());
                grid.AddRange(quad_BL.GetGrid());
                grid.AddRange(quad_BR.GetGrid());
            }
            return grid.ToArray();
        }

        /// <summary>Searches for objects in any quadrants which the passed region overlaps, but not specifically within that region.</summary>
        /// <param name="rect">The search region.</param>
        /// <returns>an array of objects.</returns>
        public T[] FindObjects(QuadTreeRect rect)
        {
            List<T> foundObjects = new List<T>();
            if(_hasChildren)
            {
                foundObjects.AddRange(quad_TL.FindObjects(rect));
                foundObjects.AddRange(quad_TR.FindObjects(rect));
                foundObjects.AddRange(quad_BL.FindObjects(rect));
                foundObjects.AddRange(quad_BR.FindObjects(rect));
            }
            else
            {
                if(IsOverlapping(rect))
                {
                    foundObjects.AddRange(_objects);
                }
            }

            HashSet<T> result = new HashSet<T>();
            result.UnionWith(foundObjects);

            return result.ToArray();
        }
    }
}
