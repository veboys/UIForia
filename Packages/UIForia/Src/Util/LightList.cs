using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UIForia.Layout;
using UnityEngine;

namespace UIForia.Util {

    [DebuggerDisplay("Count = {" + nameof(size) + "}")]
    public class LightList<T> : IReadOnlyList<T>, IList<T> {

        private int size;
        private T[] array;

        [DebuggerStepThrough]
        public LightList(int size = 8) {
            this.array = ArrayPool<T>.GetMinSize(size);
            this.size = 0;
        }

        [DebuggerStepThrough]
        public LightList(T[] items) {
            this.array = items;
            this.size = items.Length;
        }

        [DebuggerStepThrough]
        public LightList(IList<T> items) {
            this.array = new T[items.Count];
            this.size = items.Count;
            for (int i = 0; i < items.Count; i++) {
                array[i] = items[i];
            }
        }

        public T[] Array => array;

        public int Count {
            [DebuggerStepThrough] get => size;
            [DebuggerStepThrough] set => size = value;
        }

        public bool IsReadOnly => false;

        public int Capacity => array.Length;

        public T First {
            [DebuggerStepThrough] get { return array[0]; }
            [DebuggerStepThrough] set { array[0] = value; }
        }

        public T Last {
            [DebuggerStepThrough] get { return array[size - 1]; }
            [DebuggerStepThrough] set { array[size - 1] = value; }
        }

        [DebuggerStepThrough]
        public void Add(T item) {
            if (size + 1 > array.Length) {
                ArrayPool<T>.Resize(ref array, (size + 1) * 2);
            }

            array[size] = item;
            size++;
        }

        [DebuggerStepThrough]
        public void AddRange(IEnumerable<T> collection) {
            if (collection == null || Equals(collection, this)) {
                return;
            }

            if (collection is LightList<T> list) {
                EnsureAdditionalCapacity(list.size);
                System.Array.Copy(list.array, 0, array, size, list.size);
                size += list.size;
                return;
            }

            if (collection is List<T> l) {
                EnsureAdditionalCapacity(l.Count);
                T[] a = ListAccessor<T>.GetArray(l);
                System.Array.Copy(a, 0, array, size, l.Count);
                size += l.Count;
                return;
            }

            if (collection is T[] cArray) {
                EnsureAdditionalCapacity(cArray.Length);
                System.Array.Copy(cArray, 0, array, size, cArray.Length);
                size += cArray.Length;
                return;
            }

            foreach (var item in collection) {
                Add(item);
            }
        }

        public void AddUnchecked(T item) {
            array[size++] = item;
        }

        public void QuickClear() {
            System.Array.Clear(array, 0, size);
            size = 0;
        }

        public void Clear() {
            System.Array.Clear(array, 0, array.Length);
            size = 0;
        }

        public void ResetSize() {
            size = 0;
        }

        // todo -- remove boxing
        public bool Contains(T item) {
            for (int i = 0; i < size; i++) {
                if (array[i].Equals(item)) return true;
            }

            return false;
        }

        public List<T> ToList(List<T> list = null) {
            list = list ?? new List<T>();
            // can't use AddRange because our array is oversized
            for (int i = 0; i < Count; i++) {
                list.Add(array[i]);
            }

            return list;
        }

        public void CopyTo(T[] array, int arrayIndex) {
            for (int i = 0; i < size; i++) {
                array[arrayIndex + i] = this.array[i];
            }
        }

        public void Remove<U>(U closureArg, Func<T, U, bool> fn) {
            int idx = FindIndex(closureArg, fn);
            if (idx != -1) {
                RemoveAt(idx);
            }
        }

        // todo -- remove boxing
        public bool Remove(T item) {
            for (int i = 0; i < size; i++) {
                if (array[i].Equals(item)) {
                    for (int j = i; j < size - 1; j++) {
                        array[j] = array[j + 1];
                    }

                    array[size - 1] = default(T);
                    size--;
                    return true;
                }
            }

            return false;
        }

        // todo -- remove boxing
        public int IndexOf(T item) {
            for (int i = 0; i < size; i++) {
                if (array[i].Equals(item)) return i;
            }

            return -1;
        }

        public void Insert(int index, T item) {
            if (size + 1 >= array.Length) {
                ArrayPool<T>.Resize(ref array, (size + 1) * 2);
            }

            size++;
            if (index < 0 || index > array.Length) {
                throw new IndexOutOfRangeException();
            }

            System.Array.Copy(array, index, array, index + 1, size - index);
            array[index] = item;
        }

        public void Reverse() {
            System.Array.Reverse(array, 0, size);
        }

        public void InsertRange(int index, IEnumerable<T> collection) {
            if (collection == null) {
                return;
            }

            if ((uint) index > (uint) size) {
                throw new IndexOutOfRangeException();
            }

            if (collection is ICollection<T> objs) {
                int count = objs.Count;
                if (count > 0) {
                    this.EnsureCapacity(size + count);

                    if (index < size) {
                        System.Array.Copy(array, index, array, index + count, size - index);
                    }

                    if (Equals(this, objs)) {
                        System.Array.Copy(array, 0, array, index, index);
                        System.Array.Copy(array, index + count, array, index * 2, size - index);
                    }
                    else {
                        if (objs is LightList<T> list) {
                            System.Array.Copy(list.Array, 0, array, index, list.size);
                        }
                        else if (objs is T[] b) {
                            System.Array.Copy(b, 0, array, index, b.Length);
                        }
                        else {
                            T[] a = new T[count];
                            objs.CopyTo(a, 0);
                            a.CopyTo(array, index);
                        }
                    }

                    size += count;
                }
            }
            else {
                foreach (T obj in collection) {
                    this.Insert(index++, obj);
                }
            }
        }

        public void ShiftRight(int startIndex, int count) {
            if (count <= 0) return;
            if (startIndex < 0) startIndex = 0;
            EnsureCapacity(startIndex + count + count); // I think this is too big
            System.Array.Copy(array, startIndex, array, startIndex + count, count);
            System.Array.Clear(array, startIndex, count);
            size += count;
        }

        public void ShiftLeft(int startIndex, int count) {
            if (count <= 0) return;
            if (startIndex < 0) startIndex = 0;
            System.Array.Copy(array, startIndex, array, startIndex - count, size - startIndex);
            System.Array.Clear(array, size - count, count);
            size -= count;
        }

        public T RemoveLast() {
            T retn = array[size - 1];
            array[size - 1] = default;
            size--;
            return retn;
        }

        public void RemoveAt(int index) {
            if ((uint) index >= (uint) size) return;
            if (index == size - 1) {
                array[--size] = default;
            }
            else {
                for (int j = index; j < size - 1; j++) {
                    array[j] = array[j + 1];
                }

                array[--size] = default(T);
            }
        }

        public int FindIndex(Predicate<T> fn) {
            for (int i = 0; i < size; i++) {
                if (fn(array[i])) {
                    return i;
                }
            }

            return -1;
        }

        public bool Contains<U>(U closureArg, Func<T, U, bool> fn) {
            for (int i = 0; i < size; i++) {
                if (fn(array[i], closureArg)) {
                    return true;
                }
            }

            return false;
        }


        public int FindIndex<U>(U closureArg, Func<T, U, bool> fn) {
            for (int i = 0; i < size; i++) {
                if (fn(array[i], closureArg)) {
                    return i;
                }
            }

            return -1;
        }

        public T Find(Predicate<T> fn) {
            for (int i = 0; i < size; i++) {
                if (fn(array[i])) {
                    return array[i];
                }
            }

            return default(T);
        }

        public T Find<U>(U closureArg, Func<T, U, bool> fn) {
            for (int i = 0; i < size; i++) {
                if (fn(array[i], closureArg)) {
                    return array[i];
                }
            }

            return default(T);
        }

        public T this[int index] {
            [DebuggerStepThrough] get { return array[index]; }
            [DebuggerStepThrough] set { array[index] = value; }
        }

        public void EnsureCapacity(int capacity) {
            if (array.Length < capacity) {
                ArrayPool<T>.Resize(ref array, capacity * 2);
            }
        }

        public void EnsureAdditionalCapacity(int capacity) {
            if (array.Length < size + capacity) {
                ArrayPool<T>.Resize(ref array, (size + capacity) * 2);
            }
        }

        private void QuickSort(Comparison<T> comparison, int low, int high) {
            while (true) {
                if (low < high) {
                    int partition = Partition(comparison, low, high);
                    QuickSort(comparison, low, partition - 1);
                    low = partition + 1;
                    continue;
                }

                break;
            }
        }

        private void QuickSort(IComparer<T> comparison, int low, int high) {
            while (true) {
                if (low < high) {
                    int partition = Partition(comparison, low, high);
                    QuickSort(comparison, low, partition - 1);
                    low = partition + 1;
                    continue;
                }

                break;
            }
        }
        
        private int Partition(Comparison<T> comparison, int low, int high) {
            T temp;
            T pivot = array[high];

            int i = (low - 1);
            for (int j = low; j < high; j++) {
                if (comparison(array[j], pivot) <= 0) {
                    i++;

                    temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }
            }
            temp = array[i + 1];
            array[i + 1] = array[high];
            array[high] = temp;

            return i + 1;
        }
        
        private int Partition(IComparer<T> comparison, int low, int high) {
            T temp;
            T pivot = array[high];

            int i = (low - 1);
            for (int j = low; j <= high - 1; j++) {
                if (comparison.Compare(array[j], pivot) <= 0) {
                    i++;

                    temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }
            }
            temp = array[i + 1];
            array[i + 1] = array[high];
            array[high] = temp;

            return i + 1;
        }

        public void Sort(Comparison<T> comparison) {
            if (size < 2) return;
            System.Array.Sort(array, comparison);
        }

        public void Sort(Comparison<T> comparison, int start, int end) {
            if (size < 2) return;
            if (start < 0) start = 0;
            if (start >= size) start = size - 1;
            if (end >= size) end = size - 1;
            QuickSort(comparison, start, end);
        }

        public void Sort(IComparer<T> comparison, int start, int end) {
            if (size < 2) return;
            if (start < 0) start = 0;
            if (start >= size) start = size - 1;
            if (end >= size) end = size - 1;
            System.Array.Sort(array, start, end, comparison);
//            QuickSort(comparison, start, end);
        }

        public void Sort(IComparer<T> comparison) {
            if (size < 2) return;
            System.Array.Sort(array, 0, size, comparison);
//            QuickSort(comparison, 0, size - 1);
        }

        public int BinarySearch(T value, IComparer<T> comparer) {
            return InternalBinarySearch(array, 0, size, value, comparer);
        }

        public int BinarySearch(T value) {
            return InternalBinarySearch(array, 0, size, value, Comparer<T>.Default);
        }

        private static int InternalBinarySearch(T[] array, int index, int length, T value, IComparer<T> comparer) {
            int num1 = index;
            int num2 = index + length - 1;
            while (num1 <= num2) {
                int index1 = num1 + (num2 - num1 >> 1);
                int num3 = comparer.Compare(array[index1], value);

                if (num3 == 0) {
                    return index1;
                }

                if (num3 < 0) {
                    num1 = index1 + 1;
                }
                else {
                    num2 = index1 - 1;
                }
            }

            return ~num1;
        }
        
        public Enumerator GetEnumerator() {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new Enumerator(this);
        }

        public class Enumerator : IEnumerator<T> {

            private int index;
            private T current;
            private readonly LightList<T> list;

            internal Enumerator(LightList<T> list) {
                this.list = list;
                this.index = 0;
                this.current = default(T);
            }

            public void Dispose() { }

            public bool MoveNext() {
                if ((uint) index >= (uint) list.size) {
                    index = list.size + 1;
                    current = default(T);
                    return false;
                }

                current = list.array[index];
                ++index;
                return true;
            }

            public T Current => current;

            object IEnumerator.Current => Current;

            void IEnumerator.Reset() {
                index = 0;
                current = default(T);
            }

        }

    }

}