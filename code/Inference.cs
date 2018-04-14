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

namespace Program
{
    class belief
    {
        public List<List<double>> belState;
        public List<dMatrix> belEdge;
        public double Z;

        public belief(int nNodes, int nStates)
        {
            belState = new List<List<double>>(new List<double>[nNodes]);
            double[] dAry = new double[nStates];
            for (int i = 0; i < nNodes; i++)
            {
                belState[i] = new List<double>(dAry);
            }

            belEdge = new List<dMatrix>(new dMatrix[nNodes]);
            for (int i = 1; i < nNodes; i++)
                belEdge[i] = new dMatrix(nStates, nStates);

            belEdge[0] = null;
            Z = 0;
        }
    }

    class inference
    {
        protected optimizer _optim;
        protected featureGenerator _fGene;

        public inference(toolbox tb)
        {
            _optim = tb.Optim;
            _fGene = tb.FGene;
        }

        public void getLogYY(model m, dataSeq x, int i, ref dMatrix YY, ref List<double> Y, bool takeExp, bool mask)
        {
            YY.set(0);
            listTool.listSet(ref Y, 0);

            float[] w = m.W;
            List<featureTemp> fList = _fGene.getFeatureTemp(x, i);
            int nTag = m.NTag;

            //node feature
            foreach (featureTemp im in fList)
            {
                nodeFeature[] features = Global.idNodeFeatures[im.id];
                foreach (nodeFeature feat in features)
                {
                    int f = feat._id;
                    int s = feat._s;

                    Y[s] += w[f] * im.val;
                }
            }

            if (i > 0)
            {
                //non-rich edge
                if (Global.useTraditionalEdge)
                {
                    for (int s = 0; s < nTag; s++)
                    {
                        for (int sPre = 0; sPre < nTag; sPre++)
                        {
                            int f = _fGene.getEdgeFeatID(sPre, s);
                            YY[sPre, s] += w[f];
                        }
                    }
                }

                //rich edge
                foreach (featureTemp im in fList)
                {
                    edgeFeature[] features = Global.idEdgeFeatures[im.id];
                    foreach (edgeFeature feat in features)
                    {
                        YY[feat._sPre, feat._s] += w[feat._id] * im.val;
                    }
                }

                //rich2
                if (Global.richFeat2)
                {
                    List<featureTemp> fList2 = _fGene.getFeatureTemp(x, i - 1);
                    foreach (featureTemp im in fList2)
                    {
                        edgeFeature[] features = Global.idEdgeFeatures2[im.id];
                        foreach (edgeFeature feat in features)
                        {
                            YY[feat._sPre, feat._s] += w[feat._id] * im.val;
                        }
                    }
                }
            }
            double maskValue = double.MinValue;
            if (takeExp)
            {
                listTool.listExp(ref Y);
                YY.eltExp();
                maskValue = 0;
            }
            if (mask)
            {
                dMatrix statesPerNodes = m.getStatesPerNode(x);
                for (int s = 0; s < Y.Count; s++)
                {
                    if (statesPerNodes[i, s] == 0)
                        Y[s] = maskValue;
                }
            }
        }

        public void getYYandY(model m, dataSeq x, List<dMatrix> YYlist, List<List<double>> Ylist)
        {
            int nNodes = x.Count;
            //int nTag = m.NTag;
            int nTag = m.NTag;
            double[] dAry = new double[nTag];
            bool mask = false;

            try
            {
                //Global.rwlock.AcquireReaderLock(Global.readWaitTime);

                for (int i = 0; i < nNodes; i++)
                {
                    dMatrix YYi = new dMatrix(nTag, nTag);
                    List<double> Yi = new List<double>(dAry);
                    //compute the Mi matrix
                    getLogYY(m, x, i, ref YYi, ref Yi, false, mask);
                    YYlist.Add(YYi);
                    Ylist.Add(Yi);
                }

                //Global.rwlock.ReleaseReaderLock();
            }
            catch (ApplicationException)
            {
                Console.WriteLine("read out time!");
            }
        }


        public void getYYandY(model m, dataSeq x, List<dMatrix> YYlist, List<List<double>> Ylist, List<dMatrix> maskYYlist, List<List<double>> maskYlist)
        {
            int nNodes = x.Count;
            //int nTag = m.NTag;
            int nTag = m.NTag;
            double[] dAry = new double[nTag];
            bool mask = false;

            try
            {
                //Global.rwlock.AcquireReaderLock(Global.readWaitTime);

                for (int i = 0; i < nNodes; i++)
                {
                    dMatrix YYi = new dMatrix(nTag, nTag);
                    List<double> Yi = new List<double>(dAry);
                    //compute the Mi matrix
                    getLogYY(m, x, i, ref YYi, ref Yi, false, mask);
                    YYlist.Add(YYi);
                    Ylist.Add(Yi);

                    maskYYlist.Add(new dMatrix(YYi));
                    maskYlist.Add(new List<double>(Yi));
                }

                //Global.rwlock.ReleaseReaderLock();
            }
            catch (ApplicationException)
            {
                Console.WriteLine("read out time!");
            }

            //get the masked YY and Y
            double maskValue = double.MinValue;
            dMatrix statesPerNodes = m.getStatesPerNode(x);
            for (int i = 0; i < nNodes; i++)
            {
                List<double> Y = maskYlist[i];
                for (int s = 0; s < Y.Count; s++)
                {
                    if (statesPerNodes[i, s] == 0)
                        Y[s] = maskValue;
                }
            }
        }

        //fast viterbi decode without probability
        public void decodeViterbi_train(model m, dataSeq x, List<dMatrix> YYlist, List<List<double>> Ylist, List<int> tags)
        {
            int nNode = x.Count;
            int nTag = m.NTag;
            Viterbi viter = new Viterbi(nNode, nTag);

            for (int i = 0; i < nNode; i++)
            {
                viter.setScores(i, Ylist[i], YYlist[i]);
            }

            double numer = viter.runViterbi(ref tags);
        }

        //get n-best
        public void decodeNbest_train(model m, dataSeq x, List<dMatrix> YYlist, List<List<double>> Ylist, List<List<int>> nBestTags)
        {

        }

        //fast viterbi decode without probability
        public void decodeViterbi_test(model m, dataSeq x, List<int> tags)
        {
            tags.Clear();

            int nNode = x.Count;
            int nTag = m.NTag;
            dMatrix YY = new dMatrix(nTag, nTag);
            double[] dAry = new double[nTag];
            List<double> Y = new List<double>(dAry);
            Viterbi viter = new Viterbi(nNode, nTag);

            for (int i = 0; i < nNode; i++)
            {
                getLogYY(m, x, i, ref YY, ref Y, false, false);
                viter.setScores(i, Y, YY);
            }

            viter.runViterbi(ref tags);
        }

    }
}