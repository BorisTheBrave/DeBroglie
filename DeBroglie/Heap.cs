using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie
{
    interface IHeapNode<TPriority> where TPriority : IComparable<TPriority>
    {
        int HeapIndex { get; set; }

        TPriority Priority { get; }
    }

    class Heap<T, TPriority> where T : IHeapNode<TPriority> where TPriority : IComparable<TPriority>
    {
        T[] data;
        int size;

        private static int Parent(int i) => (i - 1) >> 1;

        private static int Left(int i) => (i << 1) + 1;
        private static int Right(int i) => (i << 1) + 2;

        public Heap()
        {
            data = new T[0];
            size = 0;
        }

        public Heap(int capacity)
        {
            data = new T[capacity];
            size = 0;
        }

        public Heap(T[] items)
        {
            data = new T[items.Length];
            size = data.Length;
            Array.Copy(items, data, data.Length);
            for (var i = 0; i<size;i++)
            {
                data[i].HeapIndex = i;
            }
            Heapify();
        }

        public Heap(IEnumerable<T> items)
        {
            data = items.ToArray();
            size = data.Length;
            for (var i = 0; i < size; i++)
            {
                data[i].HeapIndex = i;
            }
            Heapify();
        }

        public int Count => size;

        public T Peek()
        {
            if (size == 0)
                throw new Exception("Heap is empty");
            return data[0];
        }

        // Returns all items with lowest priority
        public IEnumerable<T> PeekEqual()
        {
            var stack = new Stack<int>();
            if (size == 0)
                yield break;
            var priority = data[0].Priority;
            stack.Push(0);
            while(stack.Count > 0)
            {
                var i = stack.Pop();
                var item = data[i];
                if (item.Priority.CompareTo(priority) > 0)
                    continue;
                yield return item;
                var l = Left(i);
                if (l < size)
                    stack.Push(l);
                var r = Right(i);
                if (r < size)
                    stack.Push(r);
            }
        }

        public void Heapify()
        {
            for (var i = Parent(size); i >= 0; i--)
            {
                Heapify(i);
            }
        }

        private void Heapify(int i)
        {
            var ip = data[i].Priority;
            var smallest = i;
            var smallestP = ip;
            var l = Left(i);
            if (l < size)
            {
                var lp = data[l].Priority;
                if (lp.CompareTo(smallestP) < 0)
                {
                    smallest = l;
                    smallestP = lp;
                }
            }
            var r = Right(i);
            if (r < size)
            {
                var rp = data[r].Priority;
                if (rp.CompareTo(smallestP) < 0)
                {
                    smallest = r;
                    smallestP = rp;
                }
            }
            if(i == smallest)
            {
                data[i].HeapIndex = i;
            }
            else
            {
                (data[i], data[smallest]) = (data[smallest], data[i]);
                data[i].HeapIndex = i;
                Heapify(smallest);
            }
        }

        public void DecreasedKey(T item)
        {
            var i = item.HeapIndex;
            var priority = item.Priority;
            while (true)
            {
                if (i == 0)
                {
                    item.HeapIndex = i;
                    return;
                }

                var p = Parent(i);
                var parent = data[p];
                var parentP = parent.Priority;

                if (parentP.CompareTo(priority) > 0)
                {
                    (data[p], data[i]) = (data[i], data[p]);
                    parent.HeapIndex = i;
                    i = p;
                    continue;
                }
                else
                {
                    item.HeapIndex = i;
                    return;
                }
            }
        }

        public void IncreasedKey(T item)
        {
            Heapify(item.HeapIndex);
        }

        public void ChangedKey(T item)
        {
            DecreasedKey(item);
            IncreasedKey(item);
        }

        public void Insert(T item)
        {
            if(data.Length == size)
            {
                var data2 = new T[size * 2];
                Array.Copy(data, data2, size);
                data = data2;
            }
            data[size] = item;
            item.HeapIndex = size;
            size++;
            DecreasedKey(item);
        }

        public void Delete(T item)
        {
            var i = item.HeapIndex;
            if (i == size - 1)
            {
                size--;
            }
            else
            {
                item = data[i] = data[size - 1];
                item.HeapIndex = i;
                size--;
                IncreasedKey(item);
                DecreasedKey(item);
            }
        }

        public void Clear()
        {
            size = 0;
        }
    }
}
