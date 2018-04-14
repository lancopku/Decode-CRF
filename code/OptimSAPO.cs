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
    class optimSAPO : optimizer
    {
        public optimSAPO(toolbox tb)
        {
            _model = tb.Model;
            _X = tb.X;
            _inf = tb.Inf;
            _fGene = tb.FGene;

            //reinit globals
            Global.reinitGlobal();
        }

        public override double optimize()
        {
            double err = trainSAPO_para();

            Global.swLog.Flush();
            return err;
        }


        //parallel training
        double trainSAPO_para()
        {
            Console.Write("sapo: ");
            Global.swLog.Write("sapo: ");
            Global.nbestCount = 0;
            Global.nbestNorm = 0;
            Global.nbestProbList = new List<double>(new double[10]);

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

            //multi threading
            double error = 0;
            Global.k = 0;
            Global.wUpdate = 0;
            Global.threadError = new List<double>();
            Global.threadX = new List<List<dataSeq>>();
            for (int i = 0; i < Global.nThread; i++)
            {
                Global.threadError.Add(0);
                Global.threadX.Add(new List<dataSeq>());
            }

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
                taskAry[ii] = new Task(taskRunner, ii, TaskCreationOptions.LongRunning);
                taskAry[ii].Start();
            }

            Task.WaitAll(taskAry);

            for (int ii = 0; ii < Global.nThread; ii++)
            {
                error += Global.threadError[ii];
            }

            //L2 reg
            float[] w = _model.W;
            if (Global.regMode=="L2" && Global.reg != 0)
            {
                int fsize = w.Length;
                for (int i = 0; i < fsize; i++)
                {
                    double grad_i = w[i] * 2.0 * Global.reg;
                    w[i] -= (float)(Global.r_k * grad_i);
                }
                double sum = arrayTool.squareSum(w);
                error += sum * Global.reg;
            }

            timer.Stop();
            double time = timer.Elapsed.TotalSeconds;
            Global.swLog.WriteLine("**********para run time (sec): " + time.ToString());

            double norm = (double)Global.nbestNorm;
            double avgCount = Global.nbestCount / norm;
            Global.swLog.WriteLine("NBEST: avgCount of nbest: " + avgCount.ToString("f2"));
            for(int i=0;i<10;i++)
            {
                double prob = Global.nbestProbList[i] / norm;
                Global.swLog.WriteLine("NBEST: {0}-best prob: {1}", i, prob.ToString("f2"));
            }

            //statistics on weights
            float val = 0;
            foreach (float im in w)
                val += Math.Abs(im);
            Global.swLog.WriteLine("avg-weight:{0}  sum-weight:{1}", val / w.Length, val);

            Global.diff = convergeTest_loss(error);
            return error;
        }


        public void taskRunner(object task)
        {
            int i = (int)task;
            foreach (dataSeq x in Global.threadX[i])
            {
                //find the output nbest path
                List<List<int>> taggingList = new List<List<int>>();
                List<double> logNumers = new List<double>();
                AStarSearch aStar = new AStarSearch();
                Global.threadError[i] += aStar.getNBest(_model, _inf, x, Global.nBest, ref taggingList, ref logNumers);

                //oracle Viterbi path
                List<int> goldStates = new List<int>(x.getTags());

                Global.r_k = Global.rate0 * Math.Pow(Global.decayFactor, (double)Global.countWithIter / (double)Global.xsize);
                if (Global.countWithIter % (Global.xsize / 4) == 0)
                    Global.swLog.WriteLine("iter{0}    decay_rate={1}", Global.glbIter, Global.r_k.ToString("e2"));

                if (taggingList.Count == 1 && listTool.compare(taggingList[0], goldStates) == 0)
                {
                    Global.k++;
                    Global.countWithIter++;
                    continue;
                }
                else
                {
                    //update the weights	
                    updateWeights(x, taggingList, logNumers, goldStates, _model.W, Global.xsize, Global.k);
                    Global.wUpdate++;

                    Global.k++;
                    Global.countWithIter++;
                }
            }
        }


        //for meta prob model
        void updateWeights(dataSeq x, List<List<int>> outTaggings, List<double> scores, List<int> goldStates, float[] w, int nSamples, int k)
        {
            float t = nSamples - k;

            //multi updates for neg taggings & 1 update for gold tagging
            for (int n = 0; n < x.Count; n++)
            {
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

                        for (int i = 0; i < outTaggings.Count; i++)
                        {
                            if (s == outTaggings[i][n])
                            {
                                w[f] -= fv * (float)scores[i] * (float)Global.r_k;
                            }
                        }

                        if (s == goldState)
                        {
                            w[f] += fv * (float)Global.r_k;
                        }
                    }
                }

                //edge feature
                if (n > 0)
                {
                    //non-rich
                    if (Global.useTraditionalEdge)
                    {
                        int f;
                        for (int i = 0; i < outTaggings.Count; i++)
                        {
                            f = _fGene.getEdgeFeatID(outTaggings[i][n - 1], outTaggings[i][n]);
                            w[f] -= (float)scores[i] * (float)Global.r_k;
                        }

                        f = _fGene.getEdgeFeatID(goldStates[n - 1], goldState);
                        w[f] += (float)Global.r_k;
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

                            for (int i = 0; i < outTaggings.Count; i++)
                            {
                                if (sPre == outTaggings[i][n - 1] && s == outTaggings[i][n])
                                {
                                    w[f] -= fv * (float)scores[i] * (float)Global.r_k;
                                }
                            }
                            if (sPre == goldStates[n - 1] && s == goldState)
                            {
                                w[f] += fv * (float)Global.r_k;
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

                                for (int i = 0; i < outTaggings.Count; i++)
                                {
                                    if (sPre == outTaggings[i][n - 1] && s == outTaggings[i][n])
                                    {
                                        w[f] -= fv * (float)scores[i] * (float)Global.r_k;
                                    }
                                }
                                if (sPre == goldStates[n - 1] && s == goldState)
                                {
                                    w[f] += fv * (float)Global.r_k;
                                }
                            }
                        }
                    }
                }
            }
        }  


    }
}
