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
    class decisionNode
    {
        public int _preY;
        public double _maxPreScore;
        //public double _maxNowScore;
        public bool _initCheck;

        public decisionNode()
        {
            _preY = -1;
            _maxPreScore = -1;
            //_maxNowScore = -1;
            _initCheck = false;
        }

    }

    class Viterbi
    {
        public int _w;
        public int _h;
        public List<List<double>> _nodeScore = new List<List<double>>();
        public List<dMatrix> _edgeScore = new List<dMatrix>();
        public decisionNode[,] _decisionLattice;

        public Viterbi(int w, int h)
        {
            _w = w;
            _h = h;
            _decisionLattice = new decisionNode[w, h];
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    _decisionLattice[i, j] = new decisionNode();
            for (int i = 0; i < w; i++)
            {
                double[] dAry = new double[h];
                List<double> dList = new List<double>(dAry);
                _nodeScore.Add(dList);

                dMatrix m = new dMatrix(h, h);
                _edgeScore.Add(m);
            }
            _edgeScore[0] = null;
        }

        public void setScores(int i, List<double> Y, dMatrix YY)
        {
            _nodeScore[i] = new List<double>(Y);
            if (i > 0)
                _edgeScore[i] = new dMatrix(YY);
        }

        public double getPathScore(int x, int y)
        {
            return _decisionLattice[x, y]._maxPreScore;
        }

        //run viterbi from right to left, then get tags from left to right
        //note: should not contain the value of current node into Heuristic,otherwise cur-val is added twice if it is also in "g"
        public double runViterbi(ref List<int> states)
        {
            for (int y = 0; y < _h; y++)
            {
                decisionNode curNode = _decisionLattice[_w - 1, y];
                curNode._initCheck = true;
                curNode._maxPreScore = 0;
                //curNode._maxNowScore = _nodeScore[_w - 1][y];
                curNode._preY = -1;
            }

            for (int i = _w - 2; i >= 0; i--)
                for (int y = 0; y < _h; y++)
                    for (int yPre = 0; yPre < _h; yPre++)
                    {
                        int iPre = i + 1;
                        //compute the new path-prob until now, compare it with the existing path-prob
                        //if the new one is bigger than current one, then update the path-prob and bkTrkNode
                        decisionNode preNode = _decisionLattice[iPre, yPre];
                        decisionNode curNode = _decisionLattice[i, y];
                        double score1 = _nodeScore[iPre][yPre];
                        double score2 = _edgeScore[iPre][y, yPre];
                        double score3 = preNode._maxPreScore;
                        //double score4 = _nodeScore[i][y];
                        double preScore = score1 + score2 + score3;

                        if (!curNode._initCheck)
                        {
                            curNode._initCheck = true;
                            curNode._maxPreScore = preScore;
                            //curNode._maxNowScore = preScore + score4;
                            curNode._preY = yPre;
                        }
                        else if (preScore >= curNode._maxPreScore)
                        {
                            curNode._maxPreScore = preScore;
                            //curNode._maxNowScore = preScore + score4;
                            curNode._preY = yPre;
                        }
                    }

            //get viterbi tags
            states.Clear();
            double max = _decisionLattice[0, 0]._maxPreScore + _nodeScore[0][0];
            int tag = 0;
            for (int y = 1; y < _h; y++)
            {
                double sc = _decisionLattice[0, y]._maxPreScore + _nodeScore[0][y];
                if (max < sc)
                {
                    max = sc;
                    tag = y;
                }
            }
            states.Add(tag);

            for (int i = 1; i < _w; i++)
            {
                int iPre = i - 1;
                tag = _decisionLattice[iPre, tag]._preY;
                states.Add(tag);
            }
            return max;
        }

    }



}