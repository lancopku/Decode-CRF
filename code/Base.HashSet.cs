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
    class baseHashSet<K> : IEnumerable<K>
    {
        private K[] _ary;
        private bool[] _aryValid;
        private int _count = 0;
        private static double _maxLoadFactor = 0.75;

        public static double MaxLoadFactor
        {
            get { return _maxLoadFactor; }
            set
            {
                _maxLoadFactor = value;
            }
        }

        public baseHashSet(int initSize)
        {
            int size = mathTool.nextPrime((int)Math.Ceiling(initSize / _maxLoadFactor));
            _ary = new K[size];
            //bool[] will have the default value of false
            _aryValid = new bool[size];
        }

        public baseHashSet() : this(10) { }

        public bool Contains(K key)
        {
            return loc_query(key) >= 0;
        }

        public bool Add(K key)
        {
            int loc = loc_query(key);
            if (loc >= 0) return false;

            if (++_count > _ary.Length * _maxLoadFactor)
            {
                rehash();
                loc = loc_findSlot(key);
            }
            else
            {
                loc = -loc - 1;
            }

            _ary[loc] = key;
            _aryValid[loc] = true;
            return true;
        }

        private int loc_query(K key)
        {
            int hash = Math.Abs(key.GetHashCode());
            int loc = hash % _ary.Length;

            if (!_aryValid[loc]) return -loc - 1;
            if (_ary[loc].Equals(key)) return loc;

            int stepSize = (hash % (_ary.Length - 1) + 1);
            loc += stepSize;
            loc %= _ary.Length;

            while (_aryValid[loc])
            {
                if (_ary[loc].Equals(key)) return loc;
                loc += stepSize;
                loc %= _ary.Length;
            }
            return -loc - 1;
        }

        public bool Remove(K key)
        {
            int loc = loc_query(key);
            if (loc < 0) return false;

            _aryValid[loc] = false;
            _ary[loc] = default(K);
            _count--;
            return true;
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
            foreach (K key in this)
            {
                sb.Append(key.ToString() + " ");
            }
            sb.Append("}");
            return sb.ToString();
        }

        private void rehash()
        {
            K[] oldAry = _ary;
            bool[] oldAryValid = _aryValid;
            _ary = new K[mathTool.nextPrime(oldAry.Length * 2)];
            _aryValid = new bool[_ary.Length];
            for (int i = 0; i < oldAry.Length; i++)
            {
                if (oldAryValid[i])
                {
                    int loc = loc_findSlot(oldAry[i]);
                    _ary[loc] = oldAry[i];
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

        public IEnumerator<K> GetEnumerator()
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
    }
}
