﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace NewLife.Collections
{
    /// <summary>双向链表</summary>
    /// <typeparam name="T"></typeparam>
    public class LinkList<T> : ICollection<T>
    {
        #region 属性
        private Node _Head;
        /// <summary>头部</summary>
        protected Node Head { get => _Head; }

        private Node _Tail;
        /// <summary>尾部</summary>
        protected Node Tail { get => _Tail; }

        private Int32 _Count;
        /// <summary>元素个数</summary>
        public Int32 Count { get => _Count; }

        Boolean ICollection<T>.IsReadOnly => false;
        #endregion

        #region 方法
        /// <summary>添加项</summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            var node = new Node(item);

            // 首次可能为空
            while (_Head == null)
            {
                if (Interlocked.CompareExchange(ref _Head, node, null) == null)
                {
                    _Tail = node;

                    Interlocked.Increment(ref _Count);

                    return;
                }
            }

            // 附加到链表后面
            node.InsertAfter(ref _Tail);

            Interlocked.Increment(ref _Count);
        }

        /// <summary>删除项</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean Remove(T item)
        {
            for (var node = Head; node != null; node = node.Next)
            {
                if (Object.Equals(node.Value, item))
                {
                    node.Remove();

                    Interlocked.Decrement(ref _Count);

                    return true;
                }
            }

            return false;
        }

        /// <summary>清空链表</summary>
        public void Clear()
        {
            _Head = _Tail = null;
            _Count = 0;
        }

        /// <summary>是否包含</summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Boolean Contains(T item)
        {
            for (var node = Head; node != null; node = node.Next)
            {
                if (Object.Equals(node.Value, item)) return true;
            }

            return false;
        }

        /// <summary>拷贝到数组</summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, Int32 arrayIndex)
        {
            var k = 0;
            for (var node = Head; node != null; node = node.Next, k++)
            {
                array[arrayIndex + k] = node.Value;
            }
        }

        /// <summary>枚举</summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (var node = Head; node != null; node = node.Next)
            {
                yield return node.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => $"LinkList({Count:n0})";
        #endregion

        #region 节点
        /// <summary>双链表节点</summary>
        /// <typeparam name="T"></typeparam>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected class Node
        {
            #region 属性
            /// <summary>数值</summary>
            public T Value { get; set; }

            private Node _Prev;
            /// <summary>前一个</summary>
            public Node Prev { get => _Prev; set => _Prev = value; }

            private Node _Next;
            /// <summary>下一个</summary>
            public Node Next { get => _Next; set => _Next = value; }
            #endregion

            #region 构造
            /// <summary>实例化一个双链表节点</summary>
            public Node() { }

            /// <summary>实例化一个双链表节点</summary>
            /// <param name="value"></param>
            public Node(T value) { Value = value; }
            #endregion

            #region 方法
            /// <summary>在指定节点之后插入</summary>
            /// <param name="after"></param>
            public void InsertAfter(ref Node after)
            {
                if (after == null) throw new ArgumentNullException(nameof(after));

                //Prev = after;
                //Next = after.Next;

                //after.Next = this;
                //if (Next != null) Next.Prev = this;

                // 原子操作里面，把上一个节点的Next换成当前的下一个节点
                while (true)
                {
                    // 可能别的线程已经清空
                    var node = after;
                    if (node == null) return;

                    // 尝试替换
                    if (Interlocked.CompareExchange(ref node._Next, this, this) == this) break;
                }
            }

            /// <summary>在指定节点之前插入</summary>
            /// <param name="before"></param>
            public void InsertBefore(Node before)
            {
                Next = before ?? throw new ArgumentNullException(nameof(before));
                Prev = before.Prev;

                before.Prev = this;
                if (Prev != null) Prev.Next = this;
            }

            /// <summary>移除节点</summary>
            public Boolean Remove()
            {
                /*
                 * 原子操作的存在，别的线程删除当前节点时永远不会成功
                 */

                var p = _Prev;
                var n = _Next;

                // 原子操作里面，把上一个节点的Next换成当前的下一个节点
                if (p != null) Interlocked.CompareExchange(ref p._Next, n, this);

                // 原子操作里面，把下一个节点的Prev换成当前的上一个节点
                if (n != null) Interlocked.CompareExchange(ref n._Prev, p, this);

                _Prev = null;
                _Next = null;

                return true;
            }
            #endregion
        }
        #endregion
    }
}