using System.Collections.Generic;
using System.Linq;

namespace Snork.TextWrap
{
    public class LineBuilder
    {
        public bool Any()
        {
            return _items.Any();
        }
        private readonly List<string> _items = new List<string>();
        private int _length;
        public int Length
        {
            get { return _length; }
        }
        public void Add(string item)
        {
            _items.Add(item);
            _length += item.Length;
        }

        public string Concat()
        {
            return string.Concat(_items);
        }
        public string Last()
        {
            return _items.Last();
        }
        public void RemoveLast()
        {
            _items.RemoveAt(_items.Count - 1);
            _length = _items.Sum(i => i.Length);
        }
    }
}