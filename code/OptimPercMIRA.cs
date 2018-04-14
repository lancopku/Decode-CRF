//
//  SAPO
//
//  Copyright(C) Xu Sun <xusun@pku.edu.cn>
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Program
{
    class optimPercMIRA : optimizer
    {
        float[] _sumW, _tmpW;
        bool _recoverFlag;

        public optimPercMIRA(toolbox tb)
        {
            _model = tb.Model;
            _X = tb.X;
            _inf = tb.Inf;
            _fGene = tb.FGene;

            _sumW = new float[_model.W.Length];
            _tmpW = new float[_model.W.Length];
            _recoverFlag = false;

            //reinit globals
            Global.reinitGlobal();
        }

        public override double optimize()
        {
            double err = trainPercMIRA_para();

            Global.swLog.Flush();
            return err;
        }


        void weightAverageOrNot(float[] w)
        {
            if (Global.modelOptimizer.EndsWith("avg"))
            {
                //backup model weights
                for (int i = 0; i < _sumW.Length; i++)
                    _tmpW[i] = w[i];
                //averaging (for test)
                for (int i = 0; i < _sumW.Length; i++)
                    w[i] = _sumW[i] / (float)Global.countWithIter;
                //a flag for recover
                _recoverFlag = true;

                //reg
                if (Global.regMode == "GL")
                    groupLassoReg(w);
            }

            int nNon0 = 0;
            foreach (float im in w)
            {
                if (im != 0)
                    nNon0++;
            }
            double share = (double)nNon0 / (double)w.Length * 100.0;
            Global.swLog.WriteLine("nonzero weights: {0} / {1} = {2}%", nNon0, w.Length, share.ToString("f2"));
            Console.WriteLine("nonzero weights: {0}/{1}={2}%", nNon0, w.Length, share.ToString("f2"));
        }


        //parallel training
        double trainPercMIRA_para()
        {
            Console.Write("perc/MIRA: ");
            Global.swLog.Write("perc/MIRA: ");

            Global.nbestCount = 0;
            Global.nbestNorm = 0;
            Global.nbestProbList = new List<double>(new double[10]);

            if (_recoverFlag)
            {
                _model.W = _tmpW;
                _recoverFlag = false;
            }

            dataSet X2 = structReg.structSplit(_X);

            Global.xsize = X2.Count;

            List<int> ri;//debug
            if (Global.glbIter <= 1)
            {
                ri = randomToolList<int>.getSortedIndexList(Global.xsize);
                Console.WriteLine("sorted index!");
            }
            else
                ri = randomToolList<int>.getShuffledIndexList(Global.xsize);

            //re-init temp vector in every iteration
            for (int i = 0; i < _tmpW.Length; i++)
                _tmpW[i] = Global.xsize * _model.W[i];

            //multi threading
            Global.k = 0;
            Global.wUpdate = 0;
            Global.countWithIter = 0;
            Global.threadX = new List<List<dataSeq>>();
            for (int i = 0; i < Global.nThread; i++)
                Global.threadX.Add(new List<dataSeq>());

            for (int t = 0; t < Global.xsize; t++)
            {
                int idx = t % Global.nThread;
                dataSeq x = X2[ri[t]];
                Global.threadX[idx].Add(x);
            }

            Stopwatch timer = new Stopwatch();
            timer.Start();

            //start multi threading
            Task[] taskAry = new Task[Global.nThread];
            for (int ii = 0; ii < Global.nThread; ii++)
            {
                if (Global.modelOptimizer.StartsWith("miran"))
                    taskAry[ii] = new Task(taskRunner_nbestMIRA, ii, TaskCreationOptions.LongRunning);
                else
                    taskAry[ii] = new Task(taskRunner_percMIRA, ii, TaskCreationOptions.LongRunning);

                taskAry[ii].Start();
            }

            Task.WaitAll(taskAry);
            //Console.WriteLine("all threads done.");

            timer.Stop();
            double time = timer.Elapsed.TotalSeconds;
            Global.swLog.WriteLine("**********para run time (sec): " + time.ToString());

            //accumulate for the averaged weight vector
            for (int i = 0; i < _sumW.Length; i++)
            {
                _sumW[i] += _tmpW[i];
            }
            Global.swLog.WriteLine("iter{0}    #update={1}", Global.glbIter, Global.wUpdate);

            weightAverageOrNot(_model.W);

            //statistics on weights
            float val = 0;
            foreach (float im in _model.W)
                val += Math.Abs(im);
            Global.swLog.WriteLine("avg-weight:{0}  sum-weight:{1}", val / _model.W.Length, val);

            return 0;
        }


        //perc or MIRA
        public void taskRunner_percMIRA(object task)
        {
            int i = (int)task;
            foreach (dataSeq x in Global.threadX[i])
            {
                List<dMatrix> YYlist = new List<dMatrix>();
                List<List<double>> Ylist = new List<List<double>>();
                _inf.getYYandY(_model, x, YYlist, Ylist);

                //find the output Viterbi hidden path
                List<int> outStates = new List<int>();
                _inf.decodeViterbi_train(_model, x, YYlist, Ylist, outStates);

                //oracle Viterbi path
                List<int> goldStates = new List<int>(x.getTags());

                //update the weights	
                int diff = listTool.compare(outStates, goldStates);
                if (diff > 0)
                {
                    if (Global.modelOptimizer.StartsWith("mira"))
                        //mira
                        updateWeights(x, outStates, goldStates, _model.W, _tmpW, Global.xsize, Global.k, diff);
                    else
                        //perc
                        updateWeights(x, outStates, goldStates, _model.W, _tmpW, Global.xsize, Global.k);
                    Global.wUpdate++;
                }

                Global.k++;
                Global.countWithIter++;
            }
        }


        //nbest MIRA
        public void taskRunner_nbestMIRA(object task)
        {
            int i = (int)task;
            foreach (dataSeq x in Global.threadX[i])
            {
                //find the output nbest path
                List<double> logNumers = new List<double>();
                List<List<int>> taggingList = new List<List<int>>();
                AStarSearch aStar = new AStarSearch();
                aStar.getNBest(_model, _inf, x, Global.nBest, ref taggingList, ref logNumers);

                //oracle Viterbi path
                List<int> goldStates = new List<int>(x.getTags());

                foreach (List<int> outStates in taggingList)
                {
                    int diff = listTool.compare(outStates, goldStates);
                    if (diff > 0)
                    {
                        updateWeights(x, outStates, goldStates, _model.W, _tmpW, Global.xsize, Global.k, diff);
                        Global.wUpdate++;
                    }
                }
                Global.k++;
                Global.countWithIter++;
            }
        }


        void groupLassoReg(float[] w)
        {
            if (Global.reg != 0)
            {
                double nonZero = 0;
                double reg = Global.reg;
                for (int i = 0; i < Global.groupStart.Count; i++)
                {
                    int start = Global.groupStart[i];
                    int end = Global.groupEnd[i];
                    //get L2 value
                    double L2 = 0;
                    for (int ft = start; ft < end; ft++)
                    {
                        //node
                        foreach (nodeFeature f in Global.idNodeFeatures[ft])
                            L2 += w[f._id] * w[f._id];
                        //rich
                        foreach (edgeFeature f in Global.idEdgeFeatures[ft])
                            L2 += w[f._id] * w[f._id];
                        //rich2
                        if (Global.richFeat2)
                        {
                            foreach (edgeFeature f in Global.idEdgeFeatures2[ft])
                                L2 += w[f._id] * w[f._id];
                        }
                    }
                    L2 = Math.Sqrt(L2);
                    //d
                    //double d = end - start;
                    //double d = Math.Sqrt((double)(end - start));
                    double d = Math.Pow((double)(end - start), 1.0 / 4.0);

                    if (L2 <= reg * d)//set to 0
                    {
                        for (int ft = start; ft < end; ft++)
                        {
                            //node
                            foreach (nodeFeature f in Global.idNodeFeatures[ft])
                                w[f._id] = 0;
                            //rich
                            foreach (edgeFeature f in Global.idEdgeFeatures[ft])
                                w[f._id] = 0;
                            //rich2
                            if (Global.richFeat2)
                            {
                                foreach (edgeFeature f in Global.idEdgeFeatures2[ft])
                                    w[f._id] = 0;
                            }
                        }
                    }
                    else//scale
                    {
                        nonZero++;
                        double scale = (L2 - reg * d) / L2;
                        for (int ft = start; ft < end; ft++)
                        {
                            //original form: w[f] *= (float)scale;

                            //node
                            foreach (nodeFeature f in Global.idNodeFeatures[ft])
                            {
                                float grad_estim = (float)(1 - scale) * w[f._id];
                                w[f._id] -= (float)(grad_estim);
                            }
                            //rich
                            foreach (edgeFeature f in Global.idEdgeFeatures[ft])
                            {
                                float grad_estim = (float)(1 - scale) * w[f._id];
                                w[f._id] -= (float)(grad_estim);
                            }
                            //rich2
                            if (Global.richFeat2)
                            {
                                foreach (edgeFeature f in Global.idEdgeFeatures2[ft])
                                {
                                    float grad_estim = (float)(1 - scale) * w[f._id];
                                    w[f._id] -= (float)(grad_estim);
                                }
                            }
                        }
                    }
                }
                double percent = nonZero / (double)(Global.groupStart.Count) * 100.0;
                Console.WriteLine("GL-reg non-0 percent: {0}", percent.ToString("f2"));
                Global.swLog.WriteLine("GL-reg non-0 percent: {0}", percent.ToString("f2"));
            }
        }


        //for mira
        void updateWeights(dataSeq x, List<int> outStates, List<int> goldStates, float[] w, float[] accumW, int nSamples, int k, double diff)
        {
            float t = nSamples - k;

            //get a_t = F(y*) - F(y)
            baseHashMap<int, double> a = new baseHashMap<int, double>();
            for (int n = 0; n < x.Count; n++)
            {
                int outState = outStates[n];
                int goldState = goldStates[n];
                List<featureTemp> fList = _fGene.getFeatureTemp(x, n);

                //node feature
                foreach (featureTemp im in fList)
                {
                    double fv = im.val;
                    foreach (nodeFeature feat in Global.idNodeFeatures[im.id])
                    {
                        int s = feat._s;
                        int f = feat._id;
                       
                        if (s == outState)
                            a[f] -= fv;
                        if (s == goldState)
                            a[f] += fv;
                    }
                }

                //edge feature
                if (n > 0)
                {
                    //non-rich
                    if (Global.useTraditionalEdge)
                    {
                        int f = _fGene.getEdgeFeatID(outStates[n - 1], outState);
                        a[f]--;

                        f = _fGene.getEdgeFeatID(goldStates[n - 1], goldState);
                        a[f]++;
                    }

                    //rich
                    foreach (featureTemp im in fList)
                    {
                        double fv = im.val;
                        foreach (edgeFeature feat in Global.idEdgeFeatures[im.id])
                        {
                            int s = feat._s;
                            int sPre = feat._sPre;
                            int f = feat._id;

                            if (sPre == outStates[n - 1] && s == outState)
                                a[f] -= fv;
                            if (sPre == goldStates[n - 1] && s == goldState)
                                a[f] += fv;
                        }
                    }

                    //rich2
                    if (Global.richFeat2)
                    {
                        fList = _fGene.getFeatureTemp(x, n - 1);
                        foreach (featureTemp im in fList)
                        {
                            double fv = im.val;
                            foreach (edgeFeature feat in Global.idEdgeFeatures2[im.id])
                            {
                                int s = feat._s;
                                int sPre = feat._sPre;
                                int f = feat._id;

                                if (sPre == outStates[n - 1] && s == outState)
                                    a[f] -= fv;
                                if (sPre == goldStates[n - 1] && s == goldState)
                                    a[f] += fv;
                            }
                        }
                    }
                }
            }

            //compute w*a, ||a||^2
            double wa = 0, norm = 0;
            foreach(baseHashMap<int,double>.KeyValuePair kv in a)
            {
                wa += w[kv.Key] * kv.Value;
                norm += kv.Value * kv.Value;
            }

            //compute the scalar
            double scale = (Math.Sqrt(diff) - wa) / norm;

            //compute w_{t+1}
            foreach (baseHashMap<int, double>.KeyValuePair kv in a)
            {
                int f = kv.Key;
                float val = (float)(scale * kv.Value);
                w[f] += val;
                accumW[f] += t * val;
            }
        }


        //for perc
        void updateWeights(dataSeq x, List<int> outStates, List<int> goldStates, float[] w, float[] accumW, int nSamples, int k)
        {
            float t = nSamples - k;

            for (int n = 0; n < x.Count; n++)
            {
                int outState = outStates[n];
                int goldState = goldStates[n];
                List<featureTemp> fList = _fGene.getFeatureTemp(x, n);
                
                //update the weights and accumulative weights
                //node feature
                foreach (featureTemp im in fList)
                {
                    float fv = (float)im.val;
                    foreach (nodeFeature feat in Global.idNodeFeatures[im.id])
                    {
                        int s = feat._s;
                        int f = feat._id;

                        if(s==outState)
                        {
                            w[f] -= fv;
                            accumW[f] -= t * fv;
                        }
                        if(s==goldState)
                        {
                            w[f] += fv;
                            accumW[f] += t * fv;
                        }
                    }
                }

                //edge feature
                if (n > 0)
                {
                    //non-rich
                    if (Global.useTraditionalEdge)
                    {
                        int f = _fGene.getEdgeFeatID(outStates[n - 1], outState);
                        w[f]--;
                        accumW[f] -= t;

                        f = _fGene.getEdgeFeatID(goldStates[n - 1], goldState);
                        w[f]++;
                        accumW[f] += t;
                    }

                    //rich
                    foreach (featureTemp im in fList)
                    {
                        float fv = (float)im.val;
                        foreach (edgeFeature feat in Global.idEdgeFeatures[im.id])
                        {
                            int s = feat._s;
                            int sPre = feat._sPre;
                            int f = feat._id;

                            if (sPre == outStates[n - 1] && s == outState)
                            {
                                w[f]-=fv;
                                accumW[f] -= t*fv;
                            }
                            if (sPre == goldStates[n - 1] && s == goldState)
                            {
                                w[f]+=fv;
                                accumW[f] += t*fv;
                            }
                        }
                    }

                    //rich2
                    if (Global.richFeat2)
                    {
                        fList = _fGene.getFeatureTemp(x, n - 1);
                        foreach (featureTemp im in fList)
                        {
                            float fv = (float)im.val;
                            foreach (edgeFeature feat in Global.idEdgeFeatures2[im.id])
                            {
                                int s = feat._s;
                                int sPre = feat._sPre;
                                int f = feat._id;

                                if (sPre == outStates[n - 1] && s == outState)
                                {
                                    w[f]-=fv;
                                    accumW[f] -= t*fv;
                                }
                                if (sPre == goldStates[n - 1] && s == goldState)
                                {
                                    w[f]+=fv;
                                    accumW[f] += t*fv;
                                }
                            }
                        }
                    }
                }
            }
        }  
    }
}
