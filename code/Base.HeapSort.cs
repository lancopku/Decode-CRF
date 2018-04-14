//
//  SAPO
//
//  Copyright(C) Xu Sun <xusun@pku.edu.cn>
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Program
{
    // A node represents a possible state in the search
    // The user provided state type is included inside this type
    class heapNode
    {
        public heapNode _parent; // used during the search to record the parent of successor nodes
        //Node *child; // used after the search for the application to view the search in reverse
        public int _x;
        public int _y;
        public double _g; // cost of this node + it's predecessors
        public double _h; // heuristic estimate of distance to goal
        public double _f; // sum of cumulative cost of predecessors and self and heuristic

        //constructor
        public heapNode()
        {
            //_parent = null;
            //_g = 0;
            //_h = 0;
            //_f = 0;
            //_x = 0;
            //_y = 0;
        }

        public heapNode(int x, int y)
        {
            //_parent = null;
            //_g = 0;
            //_h = 0;
            //_f = 0;
            this._x = x;
            this._y = y;
        }

        //reload operator
        public static bool operator >(heapNode n1, heapNode n2)
        {
            bool b = n1._f > n2._f;
            return b;
        }

        //reload operator
        public static bool operator <(heapNode n1, heapNode n2)
        {
            return n1._f < n2._f;
        }

        //override func., must have this, 111 is used with random... 
        public override int GetHashCode()
        {
            return base.GetHashCode() * 111;
        }

        //override func., must have this 
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        //override func., must have this 
        public override string ToString()
        {
            return string.Format("({0},{1}):{2},{3},{4}", _x, _y, _g, _h, _f);
        }
    }


    //max heap
    class HeapSort
    {
        // array of integers to hold values
        private List<heapNode> _a;

        public HeapSort(ref List<heapNode> list)
        {
            _a = list;
            buildHeap();
        }

        //should be slow...to improve
        public void push_heap_slow(heapNode n)
        {
            _a.Add(n);
            buildHeap();//this should be improved for efficiency!! no need to build half
        }

        public void push_heap(heapNode n)
        {
            //prune
            if (Global.beamSearch && _a.Count > Global.beamSize)
            {
                if (n < _a[_a.Count - 1])
                    return;
            }

            _a.Add(n);
            int loc=_a.Count-1;

            int root;
            heapNode temp;
       
            while(loc!=0)
            {
                //check n is left child or right child
                if (loc % 2 == 0)//right 
                    root = (loc - 2) / 2;
                else
                    root = (loc - 1) / 2;//left

                if (_a[root] < _a[loc])
                {
                    temp = _a[root];
                    _a[root] = _a[loc];
                    _a[loc] = temp;
                    loc = root;
                }
                else
                    break;
            }

            //prune
            if (Global.beamSearch && _a.Count > Global.beamSize)
                _a.RemoveAt(_a.Count - 1);
        }

        public heapNode pop_heap()
        {
            if (_a.Count == 0)
                return null;

            heapNode n = _a[0];
            _a[0] = _a[_a.Count - 1];
            _a.RemoveAt(_a.Count - 1);
            siftDown(0, _a.Count - 1);
            return n;
        }

        public void buildHeap()
        {
            int L = _a.Count-1;
            for (int i = (L -1) / 2; i >= 0; i--)
            {
                siftDown(i, L);
            }
            // max value is at index 0
        }


        //this function assumes that the heap is already well-formed (i.e., buildHeap())
        //returns a min-to-max list
        //but then this heap is no longer well-formed
        void sortWholeList()
        {
            int L = _a.Count - 1;
            int i;
            heapNode temp;

            for (i = L; i >= 1; i--)
            {
                temp = _a[0];
                _a[0] = _a[i];
                _a[i] = temp;
                siftDown(0, i - 1);
            }
        }

        //e.g., beam search size, return min value
        public double pruneToSize(int size)
        {
            sortWholeList();
            _a.RemoveRange(0, _a.Count - size);
            double min = _a[0]._f;
            buildHeap();
            return min;
        }


        public void siftDown(int root, int bottom)
        {
            bool done = false;
            int maxChild;
            heapNode temp;

            //left child is n*2+1, right child is n*2+2
            while ((root * 2 +1 <= bottom) && (!done))
            {
                if (root * 2 +1 == bottom)//no right child
                    maxChild = root * 2 +1;
                else if (_a[root * 2+1] > _a[root * 2 + 2])//have right child & right child is smaller
                    maxChild = root * 2+1;
                else
                    maxChild = root * 2 + 2;//have right child & it's bigger

                if (_a[root] < _a[maxChild])
                {
                    temp = _a[root];
                    _a[root] = _a[maxChild];
                    _a[maxChild] = temp;
                    root = maxChild;
                }
                else
                {
                    done = true;
                }
            }
        }

        public int Size
        {
            get { return _a.Count; }
        }


        //a simple test for debug
        public static void debug()
        {
            heapNode a = new heapNode();
            a._f = 9;
            heapNode b = new heapNode();
            b._f = 50;
            heapNode c = new heapNode();
            c._f = 21;
            heapNode d = new heapNode();
            d._f = 98;
            heapNode e = new heapNode();
            e._f = 55;
            List<heapNode> tmpList = new List<heapNode>();
            tmpList.Add(a);
            HeapSort hs = new HeapSort(ref tmpList);
            hs.push_heap(b);
            hs.push_heap(c);
            hs.push_heap(d);
            hs.push_heap(e);

            hs.pruneToSize(2);

            heapNode x = hs.pop_heap();
            Console.WriteLine(x._f);
            x = hs.pop_heap();
            Console.WriteLine(x._f);
            x = hs.pop_heap();
            Console.WriteLine(x._f);
            x = hs.pop_heap();
            Console.WriteLine(x._f);
            x = hs.pop_heap();
            Console.WriteLine(x._f);
        }
    }

}
