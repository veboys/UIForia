﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;

namespace Src {

    public interface IHierarchical {

        IHierarchical Element { get; }
        IHierarchical Parent { get; }

    }

    public interface ISkipTreeTraversable : IHierarchical {

        void OnParentChanged(ISkipTreeTraversable newParent);
        void OnBeforeTraverse();
        void OnAfterTraverse();

    }

    // need to support a version w/ integer keys instead of / in addition to item keys
    // should not require traversal callbacks
    // specify node type? ie extend SkipTreeNode<T>
    // better ToList / ToArray support w/o allocation
    // maintain enabled count?

    public class SkipTree<T> where T : class, IHierarchical, ISkipTreeTraversable {

        private readonly SkipTreeNode<T> root;
        private readonly Dictionary<IHierarchical, SkipTreeNode<T>> nodeMap;
        private static readonly List<T> scratchList = new List<T>();

        public SkipTree() {
            root = new SkipTreeNode<T>(default(T));
            nodeMap = new Dictionary<IHierarchical, SkipTreeNode<T>>();
        }

        [PublicAPI]
        public int Size => nodeMap.Count;

        [PublicAPI]
        public T[] GetRootItems() {
            T[] retn = new T[root.childCount];
            GetRootItems(ref retn);
            return retn;
        }

        [PublicAPI]
        public void GetRootItems(IList<T> roots) {
            SkipTreeNode<T> ptr = root.firstChild;
            while (ptr != null) {
                roots.Add(ptr.item);
                ptr = ptr.nextSibling;
            }
        }

        [PublicAPI]
        public int GetRootItems(ref T[] roots) {
            if (roots.Length < root.childCount) {
                Array.Resize(ref roots, root.childCount);
            }

            int i = 0;
            SkipTreeNode<T> ptr = root.firstChild;
            while (ptr != null) {
                roots[i] = ptr.item;
                i++;
                ptr = ptr.nextSibling;
            }

            return root.childCount;
        }

        [PublicAPI]
        public void AddItem(T item) {
            SkipTreeNode<T> node;
            IHierarchical element = item.Element;
            if (nodeMap.TryGetValue(element, out node)) {
                return;
            }

            node = new SkipTreeNode<T>(item);
            nodeMap[element] = node;
            SkipTreeNode<T> parent = FindParent(element);
            Insert(parent ?? root, node);
        }

        [PublicAPI]
        public T GetItem<U>(U key) where U : class, IHierarchical {
            SkipTreeNode<T> node;

            if (nodeMap.TryGetValue(key, out node)) {
                return node.item;
            }

            return default(T);
        }

        [PublicAPI]
        public int GetSiblingIndex(T element) {
            SkipTreeNode<T> node;
            if (!nodeMap.TryGetValue(element, out node)) {
                return -1;
            }

            if (node.parent == null) {
                return -1;
            }

            SkipTreeNode<T> ptr = node.parent.firstChild;
            int index = 0;
            while (ptr != node) {
                index++;
                ptr = ptr.nextSibling;
            }

            return index;
        }

        [PublicAPI]
        public void SetSiblingIndex(T element, int index) {
            // todo -- remove and insert
            SkipTreeNode<T> node;
            if (!nodeMap.TryGetValue(element, out node)) {
                return;
            }

            if (node == root) return;

            int count = node.parent.childCount;

            if (index < count) index = count;

            SkipTreeNode<T> ptr = null;
            SkipTreeNode<T> trail = null;

            if (node.parent.firstChild == node) {
                node.parent.firstChild = node.nextSibling;
            }
            else {
                ptr = node.parent.firstChild;

                while (ptr != null) {
                    if (ptr == node) {
                        // ReSharper disable once PossibleNullReferenceException
                        trail.nextSibling = node.nextSibling;
                        break;
                    }

                    trail = ptr;
                    ptr = ptr.nextSibling;
                }
            }

            if (index <= 0) {
                node.nextSibling = node.parent.firstChild;
                node.parent.firstChild = node;
                return;
            }

            ptr = node.parent.firstChild;
            int i = 0;
            while (ptr != null && i < index) {
                if (i == index) {
                    node.nextSibling = ptr;
                    // ReSharper disable once PossibleNullReferenceException
                    trail.nextSibling = node;
                    return;
                }

                i++;
                trail = ptr;
                ptr = ptr.nextSibling;
            }
        }

        public void Clear() {
            nodeMap.Clear();
            root.childCount = 0;
            root.firstChild = null;
            root.nextSibling = null;
        }

        [PublicAPI]
        public int GetChildCount(T element) {
            SkipTreeNode<T> node;
            if (!nodeMap.TryGetValue(element, out node)) {
                return 0;
            }

            return node.childCount;
        }

        [PublicAPI]
        public int GetActiveChildCount(T element) {
            SkipTreeNode<T> node;
            if (!nodeMap.TryGetValue(element, out node)) {
                return 0;
            }

            SkipTreeNode<T> ptr = node.parent.firstChild;
            int count = 0;
            while (ptr != null) {
                if (!ptr.isDisabled) {
                    count++;
                }

                ptr = ptr.nextSibling;
            }

            return count;
        }

        [PublicAPI]
        public void RemoveItem(T item) {
            SkipTreeNode<T> node;
            IHierarchical element = item.Element;
            if (!nodeMap.TryGetValue(element, out node)) {
                return;
            }

            SkipTreeNode<T> parent = node.parent;
            SkipTreeNode<T> ptr = node.firstChild;
            SkipTreeNode<T> nodeNext = node.nextSibling;
            SkipTreeNode<T> nodePrev = FindPreviousSibling(node);
            SkipTreeNode<T> lastChild = null;

            while (ptr != null) {
                ptr.parent = node.parent;
                node.parent.childCount++;
                ptr.item.OnParentChanged(ptr.parent.item);
                lastChild = ptr;
                ptr = ptr.nextSibling;
            }

            if (parent.firstChild == node) {
                parent.firstChild = node.firstChild;
            }
            else {
                nodePrev.nextSibling = node.firstChild;
                if (lastChild != null) {
                    lastChild.nextSibling = nodeNext;
                }
            }

            node.parent = null;
            node.item = default(T);
            node.nextSibling = null;
            node.firstChild = null;
            nodeMap.Remove(element);
        }

        [PublicAPI]
        public void EnableHierarchy(T item) {
            TraverseNodes(false, item, (isDisabled, n) => n.isDisabled = isDisabled, true);
        }

        [PublicAPI]
        public void DisableHierarchy(T item) {
            TraverseNodes(true, item, (isDisabled, n) => n.isDisabled = isDisabled, false);
        }

        [PublicAPI]
        public void RemoveHierarchy(T item) {
            SkipTreeNode<T> node;
            IHierarchical element = item.Element;
            if (!nodeMap.TryGetValue(element, out node)) {
                return;
            }

            SkipTreeNode<T> ptr = node;
            SkipTreeNode<T> nodeNext = node.nextSibling;
            SkipTreeNode<T> nodePrev = FindPreviousSibling(node);

            if (nodePrev != null) {
                nodePrev.nextSibling = nodeNext;
            }

            Stack<SkipTreeNode<T>> stack = new Stack<SkipTreeNode<T>>();

            stack.Push(ptr);

            while (stack.Count > 0) {
                SkipTreeNode<T> current = stack.Pop();

                nodeMap.Remove(current.item);

                ptr = current.firstChild;

                while (ptr != null) {
                    stack.Push(ptr);
                    ptr = ptr.nextSibling;
                }
            }
        }

        public void TraversePreOrder(Action<T> traverseFn, bool includeDisabled = false) {
            // SkipTreeNode<T> ptr = root.firstChild;
            //  while (ptr != null) {
            TraversePreOrderCallbackStep(root, traverseFn, includeDisabled);
            //    ptr = ptr.nextSibling;
            // }
        }

        public void TraversePreOrder<U>(U closureArg, Action<U, T> traverseFn, bool includeDisabled = false) {
            TraversePreOrderCallbackStep(root, closureArg, traverseFn, includeDisabled);
        }

        public void TraversePreOrder(T item, Action<T> traverseFn, bool includeDisabled = false) {
            SkipTreeNode<T> node;
            if (nodeMap.TryGetValue(item, out node)) {
                TraversePreOrderCallbackStep(node, traverseFn, includeDisabled);
                return;
            }
            SkipTreeNode<T> parent = FindParent(item);
            parent = parent ?? root;
            SkipTreeNode<T> ptr = parent.firstChild;
            while (ptr != null) {
                if (IsDescendentOf(ptr.item, item)) {
                    TraversePreOrderCallbackStep(ptr, traverseFn, includeDisabled);
                }
                ptr = ptr.nextSibling;
            }
        }

        public void TraversePreOrder<U>(T item, U closureArg, Action<U, T> traverseFn, bool includeDisabled = false) {
            SkipTreeNode<T> node;
            if (nodeMap.TryGetValue(item, out node)) {
                TraversePreOrderCallbackStep(node, closureArg, traverseFn, includeDisabled);
                return;
            }
            SkipTreeNode<T> parent = FindParent(item);
            parent = parent ?? root;
            SkipTreeNode<T> ptr = parent.firstChild;
            while (ptr != null) {
                if (IsDescendentOf(ptr.item, item)) {
                    TraversePreOrderCallbackStep(ptr, closureArg, traverseFn, includeDisabled);
                }
                ptr = ptr.nextSibling;
            }
        }

        public void TraverseRecursePreOrder() {
            SkipTreeNode<T> ptr = root.firstChild;

            while (ptr != null) {
                TraverseRecursePreorderStep(ptr);
                ptr = ptr.nextSibling;
            }
        }

        public T[] ToArray(bool includeDisabled = false) {
            // todo accept array argument to avoid allocation
            TraversePreOrder(scratchList, (list, node) => {
                list.Add(node);
            }, includeDisabled);
            T[] retn = scratchList.ToArray();
            scratchList.Clear();
            return retn;
        }

        private void TraverseNodes<U>(U closureArg, T item, Action<U, SkipTreeNode<T>> traverseFn, bool includeDisabled) {
            SkipTreeNode<T> node;
            if (nodeMap.TryGetValue(item, out node)) {
                TraverseNodesStep(closureArg, node, traverseFn, includeDisabled);
                return;
            }
            SkipTreeNode<T> parent = FindParent(item);
            parent = parent ?? root;
            SkipTreeNode<T> ptr = parent.firstChild;
            while (ptr != null) {
                if (IsDescendentOf(ptr.item, item)) {
                    TraverseNodesStep(closureArg, ptr, traverseFn, includeDisabled);
                }
                ptr = ptr.nextSibling;
            }
        }

        private void TraverseNodesStep<U>(U closureArg, SkipTreeNode<T> startNode, Action<U, SkipTreeNode<T>> traverseFn, bool includeDisabled) {
            if (startNode.isDisabled && !includeDisabled) {
                return;
            }
            
            traverseFn(closureArg, startNode);

            SkipTreeNode<T> ptr = startNode.firstChild;

            if (ptr == null) return;

            // todo -- pool stacks
            Stack<SkipTreeNode<T>> stack = new Stack<SkipTreeNode<T>>();

            while (ptr != null) {
                if (includeDisabled || !ptr.isDisabled) {
                    stack.Push(ptr);
                }
                ptr = ptr.nextSibling;
            }

            while (stack.Count > 0) {
                SkipTreeNode<T> current = stack.Pop();
                traverseFn(closureArg, current);
                ptr = current.firstChild;
                while (ptr != null) {
                    if (includeDisabled || !ptr.isDisabled) {
                        stack.Push(ptr);
                    }
                    ptr = ptr.nextSibling;
                }
            }
        }

        private void TraverseRecursePreorderStep(SkipTreeNode<T> node) {
            if (node.isDisabled) return;

            node.item.OnBeforeTraverse();

            SkipTreeNode<T> ptr = node.firstChild;
            while (ptr != null) {
                TraverseRecursePreorderStep(ptr);
                ptr = ptr.nextSibling;
            }

            node.item.OnAfterTraverse();
        }

        private void TraversePreOrderCallbackStep(SkipTreeNode<T> startNode, Action<T> traverseFn, bool includeDisabled) {

            if (startNode.isDisabled && !includeDisabled) {
                return;
            }

            if (startNode != root) {
                traverseFn(startNode.item);
            }
            
            if (startNode.firstChild == null) {
                return;
            }

            SkipTreeNode<T> ptr = startNode.firstChild;
            // todo -- pool stacks
            Stack<SkipTreeNode<T>> stack = new Stack<SkipTreeNode<T>>();

            while (ptr != null) {
                if (includeDisabled || !ptr.isDisabled) {
                    stack.Push(ptr);
                }
                ptr = ptr.nextSibling;
            }

            while (stack.Count > 0) {
                SkipTreeNode<T> current = stack.Pop();
                traverseFn(current.item);
                ptr = current.firstChild;
                while (ptr != null) {
                    if (includeDisabled || !ptr.isDisabled) {
                        stack.Push(ptr);
                    }
                    ptr = ptr.nextSibling;
                }
            }
        }

        private void TraversePreOrderCallbackStep<U>(SkipTreeNode<T> startNode, U closureArg, Action<U, T> traverseFn, bool includeDisabled) {
            SkipTreeNode<T> ptr = startNode.firstChild;
            // todo -- pool stacks
            Stack<SkipTreeNode<T>> stack = new Stack<SkipTreeNode<T>>();

            while (ptr != null) {
                if (includeDisabled || !ptr.isDisabled) {
                    stack.Push(ptr);
                }
                ptr = ptr.nextSibling;
            }

            while (stack.Count > 0) {
                SkipTreeNode<T> current = stack.Pop();
                traverseFn(closureArg, current.item);
                ptr = current.firstChild;
                while (ptr != null) {
                    if (includeDisabled || !ptr.isDisabled) {
                        stack.Push(ptr);
                    }
                    ptr = ptr.nextSibling;
                }
            }
        }

        private SkipTreeNode<T> FindPreviousSibling(SkipTreeNode<T> node) {
            SkipTreeNode<T> ptr = node.parent.firstChild;
            while (ptr != null) {
                if (ptr.nextSibling == node) {
                    return ptr;
                }

                ptr = ptr.nextSibling;
            }

            return null;
        }

        private void Insert(SkipTreeNode<T> parent, SkipTreeNode<T> inserted) {
            inserted.parent = parent;
            parent.childCount++;
            inserted.item.OnParentChanged(parent.item);

            SkipTreeNode<T> ptr = parent.firstChild;
            ISkipTreeTraversable element = inserted.item;

            // set parent
            // walk through current parent's children
            // if any of those are decsendents of inserted
            // remove from parent
            // attach as first sibling to inserted

            SkipTreeNode<T> insertedLastChild = null;
            SkipTreeNode<T> parentPreviousChild = null;

            while (ptr != null) {
                ISkipTreeTraversable currentElement = ptr.item;
                if (IsDescendentOf(currentElement, element)) {
                    SkipTreeNode<T> next = ptr.nextSibling;

                    if (ptr == parent.firstChild) {
                        parent.firstChild = next;
                    }
                    else if (parentPreviousChild != null) {
                        parentPreviousChild.nextSibling = next;
                    }

                    if (insertedLastChild != null) {
                        insertedLastChild.nextSibling = ptr;
                    }
                    else {
                        inserted.firstChild = ptr;
                    }

                    ptr.parent.childCount--;
                    ptr.parent = inserted;
                    inserted.childCount++;
                    ptr.item.OnParentChanged(inserted.item);
                    ptr.nextSibling = null;
                    insertedLastChild = ptr;
                    ptr = next;
                }
                else {
                    parentPreviousChild = ptr;
                    ptr = ptr.nextSibling;
                }
            }

            inserted.isDisabled = parent.isDisabled;
            inserted.nextSibling = parent.firstChild;
            parent.firstChild = inserted;
        }

        private SkipTreeNode<T> FindParent(IHierarchical element) {
            IHierarchical ptr = element.Parent;
            while (ptr != null) {
                SkipTreeNode<T> node;
                if (nodeMap.TryGetValue(ptr, out node)) {
                    return node;
                }
                ptr = ptr.Parent;
            }
            return null;
        }

        private static bool IsDescendentOf(IHierarchical child, ISkipTreeTraversable parent) {
            IHierarchical ptr = child;
            while (ptr != null) {
                if (ptr == parent.Element) {
                    return true;
                }
                ptr = ptr.Parent;
            }
            return false;
        }

        [DebuggerDisplay("{item}")]
        private class SkipTreeNode<U> {

            public U item;
            public bool isDisabled;
            public SkipTreeNode<U> parent;
            public SkipTreeNode<U> nextSibling;
            public SkipTreeNode<U> firstChild;
            public int childCount;

            public SkipTreeNode(U item) {
                this.item = item;
                this.isDisabled = false;
            }

        }

    }

}