using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace BettingBot.Common.UtilityClasses
{
    public class CustomNameValueCollection
    {
        protected NameValueCollection _customNVC;

        public int Count => _customNVC.Count;

        public bool IsReadOnly { get; }

        public string this[int index]
        {
            get { return _customNVC[index]; }
            set { _customNVC.Set(_customNVC.GetKey(index), value); }
        }

        public string this[string key]
        {
            get { return _customNVC[key]; }
            set { _customNVC.Set(key, value); }
        }

        public CustomNameValueCollection(bool isReadOnly = false)
        {
            _customNVC = new NameValueCollection();
            IsReadOnly = isReadOnly;
        }

        public IEnumerator GetEnumerator()
        {
            return new CustomNameValueCollectionEnumerator(_customNVC);
        }

        public void Add(string name, string value)
        {
            _customNVC.Add(name, value);
        }
        
        public void Clear()
        {
            _customNVC.Clear();
        }

        public bool Contains(KeyValuePair<string, string> kvp)
        {
            return _customNVC.AllKeys.Contains(kvp.Key) && _customNVC.GetValues(kvp.Key).Contains(kvp.Value);
        }

        public bool ContainsKey(string key)
        {
            return _customNVC.AllKeys.Contains(key);
        }

        public bool ContainsValue(string value)
        {
            for (var i = 0; i < _customNVC.Count; i++)
                if (_customNVC[i] == value)
                    return true;

            return false;
        }

        public void Remove(string key)
        {
            _customNVC.Remove(key);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _customNVC.CopyTo(array, arrayIndex);
        }

        public int IndexOfValue(string value)
        {
            for (var i = 0; i < _customNVC.Count; i++)
                if (_customNVC[i] == value)
                    return i;
            return -1;
        }

        public int IndexOfKey(string key)
        {
            return Array.IndexOf(_customNVC.AllKeys, key);
        }

        public void Set(string name, string value)
        {
            _customNVC.Set(name, value);
        }

        public void Set(KeyValuePair<string, string> kvp)
        {
            _customNVC.Set(kvp.Key, kvp.Value);
        }

        public void RemoveAt(int index)
        {
            _customNVC.Remove(_customNVC.GetKey(index));
        }

        public string Get(string key)
        {
            return _customNVC.Get(key);
        }

        public string GetKey(int index)
        {
            return _customNVC.GetKey(index);
        }

        public string[] GetValues(string key)
        {
            return _customNVC.GetValues(key);
        }

        public string[] AllKeys()
        {
            return _customNVC.AllKeys;
        }

        public string[] AllValues()
        {
            return _customNVC.AllKeys.SelectMany(k => _customNVC.GetValues(k)).ToArray();
        }

        public bool HasKeys()
        {
            return _customNVC.HasKeys();
        }
    }

    public class CustomNameValueCollectionEnumerator : IEnumerator
    {
        private readonly NameValueCollection _customNVC;
        private int _position = -1;

        public string Current
        {
            get
            {
                try
                {
                    return _customNVC[_position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        object IEnumerator.Current => Current;

        public CustomNameValueCollectionEnumerator(NameValueCollection customNVC)
        {
            _customNVC = customNVC;
        }

        public bool MoveNext()
        {
            _position++;
            return _position < _customNVC.Count;
        }

        public void Reset()
        {
            _position = -1;
        }

        public void Dispose()
        {
        }
    }
}
