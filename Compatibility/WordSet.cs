using System.Collections.Generic;

namespace TurkishDeasciifier.Compatibility
{
    internal class WordSet<T> :  ICollection<T>
    {
        private readonly Dictionary<T, object> d;
        public WordSet(int capacity)
        {
            d = new Dictionary<T, object>(capacity);
        }

        public WordSet()
            : this(0)
        {
        }

        public WordSet(IEnumerable<T> items)
            : this()
        {
            if (items == null) { return; }
            foreach (T item in items)
            {
                Add(item);
            }
        }

        public void Add(T item)
        {
            d.Add(item, null);
        }

        public void Clear()
        {
            d.Clear();
        }

        public bool Contains(T item)
        {
            return d.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            d.Keys.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return d.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return d.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return d.Keys.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
