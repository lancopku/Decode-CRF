//
//  SAPO
//
//  Copyright(C) Xu Sun <xusun@pku.edu.cn>
//

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;
using System.IO.Compression;

namespace Program
{
    class baseHashMap<K, V> : IEnumerable<baseHashMap<K, V>.KeyValuePair>
    {
        private pair[] _ary;
        private bool[] _aryValid;
        private int _count = 0;
        private static double _maxLoadFactor = 0.7;
        private V _defaultVal = default(V);

        public static double MaxLoadFactor
        {
            get { return _maxLoadFactor; }
            set
            {
                _maxLoadFactor = value;
            }
        }

        public baseHashMap(int initSize)
        {
            int size = mathTool.nextPrime((int)Math.Ceiling(initSize / _maxLoadFactor));
            _ary = new pair[size];
            //bool[] will have the default value of false
            _aryValid = new bool[size];
        }

        public baseHashMap() : this(11) { }

        public V DefaultValue
        {
            get
            {
                return _defaultVal;
            }
            set
            {
                _defaultVal = value;
            }
        }

        public bool ContainsKey(K key)
        {
            return loc_query(key) >= 0;
        }

        private void AddNew(K key, int loc, V val)
        {
            if (++_count > _ary.Length * _maxLoadFactor)
            {
                rehash();
                loc = loc_findSlot(key);
            }
            else
            {
                loc = -loc - 1;
            }

            _ary[loc].key = key;
            _ary[loc].value = val;
            _aryValid[loc] = true;
        }

        public V this[K key]
        {
            get
            {
                int loc = loc_query(key);
                if (loc < 0) return _defaultVal;
                else return _ary[loc].value;
            }
            set
            {
                int loc = loc_query(key);

                if (loc < 0)
                {
                    AddNew(key, loc, value);
                }
                else
                {
                    _ary[loc].value = value;
                }
            }
        }

        private int loc_query(K key)
        {
            int hash = Math.Abs(key.GetHashCode());
            int loc = hash % _ary.Length;

            if (!_aryValid[loc]) return -loc - 1;
            if (_ary[loc].key.Equals(key)) return loc;

            int stepSize = (hash % (_ary.Length - 1) + 1);
            loc += stepSize;
            loc %= _ary.Length;

            while (_aryValid[loc])
            {
                if (_ary[loc].key.Equals(key)) return loc;
                loc += stepSize;
                loc %= _ary.Length;
            }

            return -loc - 1;
        }

        public int Count { get { return _count; } }

        public void Clear()
        {
            for (int i = 0; i < _aryValid.Length; i++)
                _aryValid[i] = false;
            _count = 0;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("{");
            foreach (KeyValuePair kvp in this)
            {
                sb.Append(kvp.ToString() + " ");
            }
            sb.Append("}");
            return sb.ToString();
        }

        public IEnumerator<KeyValuePair> GetEnumerator()
        {
            for (int i = 0; i < _ary.Length; i++)
            {
                if (_aryValid[i]) yield return _ary[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void rehash()
        {
            pair[] oldAry = _ary;
            bool[] oldAryValid = _aryValid;
            int newSize = mathTool.nextPrime(oldAry.Length * 2);
            _ary = new pair[newSize];
            _aryValid = new bool[_ary.Length];
            for (int i = 0; i < oldAry.Length; i++)
            {
                if (oldAryValid[i])
                {
                    int loc = loc_findSlot(oldAry[i].key);
                    _ary[loc].key = oldAry[i].key;
                    _ary[loc].value = oldAry[i].value;
                    _aryValid[loc] = true;
                }
            }
        }

        private int loc_findSlot(K key)
        {
            int hash = Math.Abs(key.GetHashCode());
            int loc = hash % _ary.Length;

            if (!_aryValid[loc]) return loc;

            int stepSize = (hash % (_ary.Length - 1) + 1);
            loc += stepSize;
            loc %= _ary.Length;

            while (_aryValid[loc])
            {
                loc += stepSize;
                loc %= _ary.Length;
            }
            return loc;
        }

        private struct pair : KeyValuePair
        {
            public K key;
            public V value;

            public pair(K key, V value)
            {
                this.key = key;
                this.value = value;
            }

            public override string ToString()
            {
                return "(" + key + "," + value.ToString() + ")";
            }

            public K Key { get { return key; } }
            public V Value { get { return value; } }
        }

        public interface KeyValuePair
        {
            K Key { get; }
            V Value { get; }
        }
    }

}
