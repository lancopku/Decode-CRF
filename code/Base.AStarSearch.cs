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

     class AStarSearch
    {
        Lattice _lattice;
        //Max Heap
        List<heapNode> _openList;
        HeapSort _heap;
        heapNode _nodeStart;
        heapNode _nodeGoal;
        int _w;
        int _h;

        public AStarSearch()
        {
            _openList = new List<heapNode>();
        }

        public double getNBest(model m, inference inf, dataSeq x, int N, ref List<List<int>> nBestTaggings, ref List<double> scores)
        {
            nBestTaggings.Clear();
            _w = x.Count;
            _h = m.NTag;
            _lattice = new Lattice(m, inf, x);
            setStartAndGoal(-1, 0, _w, 0);//a virtual begin node & a virtual end node

            for (int n = 0; n < N; n++)
            {
                List<int> tagging = new List<int>();
                double logNumer = searchForPath(ref tagging);
                if (logNumer == -2)//search fail
                    break;

                nBestTaggings.Add(tagging);
                scores.Add(logNumer);//log numerator

                double check= Math.Exp((scores[0]-scores[n]));
                if (check >= Global.stopSearchFactor)//20 times bigger then break
                    break;
            }

            double Z = logSum(scores);
            listTool.listAdd(ref scores, -Z);
            listTool.listExp(ref scores);//prob
            //error
            double error = Z - _lattice.ZGold;
           

            //update the profiler
            Global.nbestCount += scores.Count;
            Global.nbestNorm++;
            int small = scores.Count < 10 ? scores.Count : 10;
            for (int i = 0; i < small; i++)
                Global.nbestProbList[i] += scores[i];

            return error;
        }

        public double logSum(List<double> a)
        {
            double m1;
            double m2;
            double sum = a[0];
            for (int i = 1; i < a.Count; i++)
            {
                if (sum >= a[i])
                {
                    m1 = sum;
                    m2 = a[i];
                }
                else
                {
                    m1 = a[i];
                    m2 = sum;
                }
                sum = m1 + Math.Log(1 + Math.Exp(m2 - m1));
            }
            return sum;
        }

        public void setStartAndGoal(int x0, int y0, int xn, int yn)
        {
            _nodeStart = new heapNode(x0, y0);
            _nodeGoal = new heapNode(xn, yn);
            _heap = new HeapSort(ref _openList);
            // add a new element, and re-build the heap
            _heap.push_heap(_nodeStart);
        }

        public double searchForPath(ref List<int> tags)
        {
            while (true)
            {
                double score = doOneStep(ref tags);
                if (score == -1)//still searching 
                    continue;
                else if (score == -2)//search failure
                    return -2;
                else
                    return score;//search success
            }
        }

        public double doOneStep(ref List<int> tags)
        {
            double score = -1;
            // Pop the best node (max heap) 
            heapNode n = _heap.pop_heap();

            if (n == null)
                return -2;//heap empty, search failure

            // Check for the goal, once we pop that we're done
            if (n._x == _nodeGoal._x && n._y == _nodeGoal._y)
            {
                score = n._f;
                _nodeGoal._parent = n._parent;

                //build the "solution-node-sequence" in a backward way
                heapNode nodeParent = _nodeGoal._parent;
                while (nodeParent != _nodeStart)
                {
                    tags.Add(nodeParent._y);
                    nodeParent = nodeParent._parent;
                }
                tags.Reverse();
                return score;
            }

            else //not goal
            {
                //get successors
                int xNew = n._x + 1;
                for (int yNew = 0; yNew < _h; yNew++)
                {
                    if (xNew >= _w)
                    {
                        if (_lattice.getMap(xNew, yNew) <= -1e10)
                            continue;
                    }

                    heapNode successor = new heapNode(xNew, yNew);
                    successor._parent = n;
                    successor._g = n._g + getScoreGain(n, successor);
                    successor._h = _lattice.getHeuMap(xNew, yNew);
                    successor._f = successor._g + successor._h;//_f is necessary for building the heap! don't remove it

                    //add element, and re-build heap
                    _heap.push_heap(successor);
                }
                return -1;
            }
        }


        public double getScoreGain(heapNode preNode, heapNode node)
        {
            double preNodeScore = _lattice.getMap(preNode._x, preNode._y);
            double nodeScoreGain = _lattice.getMap(node._x, node._y);
            double edgeScoreGain = _lattice.getMap(node._x, preNode._y, node._y);
            if (preNodeScore <= -1e10 || nodeScoreGain <= -1e10 || edgeScoreGain <= -1e10)//anything touching with -1e10 will die..
                return -1e10;
            return nodeScoreGain + edgeScoreGain;
        }

    }


     class Lattice
     {
         public int _w;
         public int _h;
         public belief _logBel;
         public List<List<double>> _heuListList;
         public double ZGold;

         public Lattice(model m, inference inf, dataSeq x)
         {
             _w = x.Count;
             _h = m.NTag;

             _logBel = new belief(_w, _h);

             List<dMatrix> YYlist = new List<dMatrix>();
             List<List<double>> Ylist = new List<List<double>>();
             inf.getYYandY(m, x, YYlist, Ylist);

             for (int i = 0; i < _w; i++)
             {
                 _logBel.belState[i] = new List<double>(Ylist[i]);

                 if (i > 0)
                     _logBel.belEdge[i] = new dMatrix(YYlist[i]);
             }

             _heuListList = new List<List<double>>();
             for (int i = 0; i < _w; i++)
             {
                 _heuListList.Add(new List<double>(new double[_h]));
             }

             Viterbi _bwdViterbi = new Viterbi(_w, _h);
             for (int i = 0; i < _w; i++)
             {
                 _bwdViterbi.setScores(i, Ylist[i], YYlist[i]);
             }
             List<int> tags = new List<int>();
             _bwdViterbi.runViterbi(ref tags);
             //update the viterbiHeuristicMap
             for (int i = 0; i < _w; i++)
             {
                 for (int j = 0; j < _h; j++)
                 {
                     double h = _bwdViterbi.getPathScore(i, j);
                     setHeuMap(i, j, h);
                 }
             }

             //get zGold
             ZGold = 0;
             for (int i = 0; i < x.Count; i++)
             {
                 int s = x.getTags(i);
                 ZGold += Ylist[i][s];
                 if (i > 0)
                 {
                     int sPre = x.getTags(i - 1);
                     ZGold += YYlist[i][sPre, s];
                 }
             }

         }

         //for node
         public double getMap(int x, int y)
         {
             //for regular nodes
             if (x >= 0 && x < _w)
                 return _logBel.belState[x][y];
             //for  begin node and end node
             else if (x == -1 && y == 0)
                 return 0;
             else if (x == _w && y == 0)
                 return 0;
             //for impossible nodes
             else
             {
                 return -1e10;
             }
         }

         //for edge
         public double getMap(int node, int y1, int y2)
         {
             if (node > 0 && node < _w)
                 return _logBel.belEdge[node][y1, y2];
             else if (node == 0 && y1==0 || node==_w && y2==0)
                 return 0;
             else
                 return -1e10;
         }

         public void setHeuMap(int x, int y, double v)
         {
             if (x < 0 || x >= _w)
                 throw new Exception("error");
             _heuListList[x][y] = v;
         }

         public double getHeuMap(int x, int y)
         {
             if (x < 0 || x >= _w)
                 return 0;
             else
                 return _heuListList[x][y];
         }

         public double getScoreGain(int xPre, int yPre, int x, int y)
         {
             double preNodeProb = getMap(xPre, yPre);
             double nodeProbGain = getMap(x, y);
             double edgeProbGain = getMap(x, yPre, y);
             if (preNodeProb <= -1e10 || nodeProbGain <= -1e10 || edgeProbGain <= -1e10)
                 return -1e10;
             return nodeProbGain + edgeProbGain;
         }

     }

}