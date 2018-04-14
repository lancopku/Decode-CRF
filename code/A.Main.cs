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
using System.Diagnostics; 

namespace Program
{
    class MainClass
    {
        static void Main(string[] args)
        {
            Stopwatch timer = new Stopwatch();//for getting running time
            timer.Start();

            Console.WriteLine("latent structured perceptron toolkit v1.0");
            Console.WriteLine("Copyright(C) Xu Sun <xusun@pku.edu.cn>, All rights reserved.");

            //convert format 
            if (Global.formatConvert)
            {
                dataFormat df = new dataFormat();
                df.convert();
            }

            int flag = readCommand(args);
            if (flag == 1)
            {
                return;
            }
            else if (flag == 2)
            {
                Console.WriteLine("command wrong...type 'help' for help on command.");
                return;
            }
            Global.globalCheck();//should check after readCommand()
            directoryCheck();
        
            Global.swLog = new StreamWriter(Global.outDir + Global.fLog);//to record the most detailed runing info
            Global.swResRaw = new StreamWriter(Global.outDir + Global.fResRaw);//to record results
            Global.swTune = new StreamWriter(Global.outDir + Global.fTune);//to record tuning info

            Global.swLog.WriteLine("exe command:");
            string cmd = "";
            foreach (string im in args)
                cmd += im + " ";
            Global.swLog.WriteLine(cmd);
            Global.printGlobals();

            if (Global.runMode.Contains("train"))//to train
            {
                Console.WriteLine("\nstart training...");
                Global.swLog.WriteLine("\nstart training...");
                if(Global.tuneInit)
                    tuneWeightInit();
                train();
            }
            else if (Global.runMode.Contains("test"))//to test
            {
                Console.WriteLine("\nstart test...");
                Global.swLog.WriteLine("\nstart test...");
                test();
            }
            else if (Global.runMode.Contains("cv"))//for cross validation
            {
                Console.WriteLine("\nstart cross validation...");
                Global.swLog.WriteLine("\nstart cross validation...");
                crossValidation();
            }
            else throw new Exception("error");

            timer.Stop();
            double time = timer.ElapsedMilliseconds / 1000.0;
            Console.WriteLine("\ndone. run time (sec): " + time.ToString());
            Global.swLog.WriteLine("\ndone. run time (sec): " + time.ToString());

            Global.swLog.Close();
            Global.swResRaw.Close();
            Global.swTune.Close();
            //summarize results
            resSummarize.summarize();
            //Console.ReadLine();
        }

        static double train(dataSet X = null, dataSet XX = null)
        {
            //load data
            if (X == null && XX == null)
            {
                Console.WriteLine("\nreading training & test data...");
                Global.swLog.WriteLine("\nreading training & test data...");
                X = new dataSet(Global.fFeatureTrain, Global.fGoldTrain);
                XX = new dataSet(Global.fFeatureTest, Global.fGoldTest);
                dataSizeScale(X);

                double trainLength = 0, testLength = 0;
                foreach (dataSeq x in X)
                    trainLength += x.Count;
                trainLength /= (double)X.Count;
                foreach (dataSeq x in XX)
                    testLength += x.Count;
                testLength /= (double)XX.Count;

                Console.WriteLine("data sizes (train, test): {0} {1}", X.Count, XX.Count);
                Global.swLog.WriteLine("data sizes (train, test): {0} {1}", X.Count, XX.Count);
                Global.swLog.WriteLine("sample length (train, test): {0} {1}", trainLength.ToString("f2"), testLength.ToString("f2"));
                Console.WriteLine("sample length (train, test): {0} {1}", trainLength, testLength);
                if (Global.structReg)
                {
                    double trainAlpha = trainLength / Global.miniSize;
                    Global.swLog.WriteLine("train-alpha in structReg: {0}", trainAlpha.ToString("f2"));
                }
                Global.swLog.Flush();
            }

            double score = 0;
            if (Global.structReg)
            {
                foreach (double sr in Global.srList)
                {
                    Global.miniSize = sr;

                    Global.swLog.WriteLine("\n%sr:{0}", sr);
                    Console.WriteLine("\n%sr:{0}", sr);
                    if (Global.rawResWrite) Global.swResRaw.WriteLine("\n%sr:{0}", sr);

                    toolbox tb = new toolbox(X);
                    score = baseTrain(XX, tb);
                    resSummarize.write();
                    //save model
                    if (Global.save == 1)
                        tb.Model.save(Global.fModel);
                }
            }
            else
            {
                toolbox tb = new toolbox(X);
                score = baseTrain(XX, tb);
                resSummarize.write();
                //save model
                if (Global.save == 1)
                    tb.Model.save(Global.fModel);
            }

            return score;
        }


        //this function can be called by train(), cv(), & richEdge.train()
        public static double baseTrain(dataSet XTest, toolbox tb)
        {
            Global.reinitGlobal();
            double score = 0;

            for (int i = 0; i < Global.ttlIter; i++)
            {
                Global.glbIter++;
                Stopwatch timer = new Stopwatch();
                timer.Start();

                double err = tb.train();

                timer.Stop();
                double time = timer.ElapsedMilliseconds / 1000.0;

                Global.timeList.Add(time);
                Global.errList.Add(err);
                Global.diffList.Add(Global.diff);

                List<double> scoreList = tb.test(XTest);
                score = scoreList[0];
                Global.scoreListList.Add(scoreList);

                double scoreDiff = tb.Optim.convergeTest_score(score);
                Global.swLog.WriteLine("&&&&&&& DiffScore= {0}", scoreDiff.ToString("e2"));

                Global.swLog.WriteLine("iter{0}  error={1}  diffLoss={2}  train-time(sec)={3}  {4}={5}%", Global.glbIter, err.ToString("e2"), Global.diff.ToString("e2"), time.ToString("f2"), Global.metric, score.ToString("f2"));
                Global.swLog.WriteLine("------------------------------------------------");
                Global.swLog.Flush();
                Console.WriteLine("iter{0}  error={1} diff={2}  train-time(sec)={3}  {4}={5}%", Global.glbIter, err.ToString("e2"), Global.diff.ToString("e2"), time.ToString("f2"), Global.metric, score.ToString("f2"));

                //if (Global.diff < Global.convergeTol)
                //break;
            }
            return score;
        }


        public static double test()
        {
            Console.WriteLine("reading test data...");
            Global.swLog.WriteLine("reading test data...");
            dataSet XX = new dataSet(Global.fFeatureTest, Global.fGoldTest);
            Console.WriteLine("Done! test data size: {0}", XX.Count);
            Global.swLog.WriteLine("Done! test data size: {0}", XX.Count);
            //load model for testing
            toolbox tb = new toolbox(XX, false);
            
            Stopwatch timer = new Stopwatch();
            timer.Start();

            List<double> scoreList = tb.test(XX);

            timer.Stop();
            double time = timer.Elapsed.TotalSeconds;

            Global.timeList.Add(time);
            double score = scoreList[0];
            Global.scoreListList.Add(scoreList);
            resSummarize.write();
            return score;
        }

        static void crossValidation()
        {
            //load data
            Console.WriteLine("reading cross validation data...");
            Global.swLog.WriteLine("reading cross validation data...");
            List<dataSet> XList = new List<dataSet>();
            List<dataSet> XXList = new List<dataSet>();
            loadDataForCV(XList, XXList);

            for (int i = 0; i < Global.nCV; i++)
            {
                Global.swLog.WriteLine("\n#validation={0}", i + 1);
                Console.WriteLine("\n#validation={0}", i + 1);
                if (Global.rawResWrite) Global.swResRaw.WriteLine("% #validation={0}", i + 1);
                dataSet Xi = XList[i];
                toolbox tb = new toolbox(Xi);
                baseTrain(XXList[i], tb);

                resSummarize.write();
                if (Global.rawResWrite) Global.swResRaw.WriteLine();
            }
        }

        static void tuneWeightInit()
        {
            //tune good weight init for latent conditional models
            if (Global.modelOptimizer.StartsWith("lsp"))
            {
                Console.WriteLine("\nreading training & test data...");
                Global.swLog.WriteLine("\nreading training & test data...");
                dataSet origX = new dataSet(Global.fFeatureTrain, Global.fGoldTrain);
                dataSet X = new dataSet();
                dataSet XX = new dataSet();
                dataSplit(origX, Global.tuneSplit, X, XX);

                //backup & change setting
                int origTtlIter = Global.ttlIter;
                Global.ttlIter = Global.iterTuneWeightInit;
                Global.rawResWrite = false;
                Global.tuneWeightInit = true;

                Global.swTune.WriteLine("tuning weight initialization:");
                Console.WriteLine("tuning weight initialization:");
                double bestScore = 0;
                int n = Global.nTuneRound;
                for (int i = 0; i < n; i++)
                {
                    Console.WriteLine("\ntuning-weight-init round {0} (a step before real training!):",i+1);
                    train(X, XX);
                    double ttlScore = Global.ttlScore;
                    Global.swTune.WriteLine("score: {0}", ttlScore);
                    if (ttlScore > bestScore)
                    {
                        bestScore = ttlScore;
                        if (Global.optimW == null)
                            Global.optimW = new float[Global.tmpW.Length];
                        Global.tmpW.CopyTo(Global.optimW, 0);
                    }
                    else
                    {
                        Global.swTune.WriteLine("optimW no update.");
                    }
                }
                Global.swTune.Flush();
                //recover setting
                Global.random = 2;//2 means to init with optimal weights in toolbox
                Global.ttlIter = origTtlIter;
                Global.rawResWrite = true;
                Global.tuneWeightInit = false;
            }
        }


        public static void dataSizeScale(dataSet X)
        {
            dataSet XX = new dataSet();
            XX.setDataInfo(X);
            foreach (dataSeq im in X)
                XX.Add(im);
            X.Clear();

            int n = (int)(XX.Count * Global.trainSizeScale);
            for (int i = 0; i < n; i++)
            {
                int j = i;
                if (j > XX.Count - 1)
                    j %= XX.Count - 1;
                X.Add(XX[j]);
            }
            X.setDataInfo(XX);
        }

        public static void dataSplit(dataSet X, double v1, double v2, dataSet X1, dataSet X2)
        {
            if (v2 < v1)
                throw new Exception("error");
            X1.Clear();
            X2.Clear();
            X1.setDataInfo(X);
            X2.setDataInfo(X);
            int n1 = (int)(X.Count * v1);
            int n2 = (int)(X.Count * v2);
            for (int i = 0; i < X.Count; i++)
            {
                if (i >= n1 && i < n2)
                    X1.Add(X[i]);
                else
                    X2.Add(X[i]);
            }
        }

        public static void dataSplit(dataSet X, double v, dataSet X1, dataSet X2)
        {
            X1.Clear();
            X2.Clear();
            X1.setDataInfo(X);
            X2.setDataInfo(X);
            int n = (int)(X.Count * v);
            for (int i = 0; i < X.Count; i++)
            {
                if (i < n)
                    X1.Add(X[i]);
                else
                    X2.Add(X[i]);
            }
        }

        public static void loadDataForCV(List<dataSet> XList, List<dataSet> XXList)
        {
            XList.Clear();
            XXList.Clear();
            //only load train data for CV, test data should not be used in CV
            dataSet X = new dataSet(Global.fFeatureTrain, Global.fGoldTrain);
            double step = 1.0 / Global.nCV;
            for (double i = 0; i < 1; i += step)
            {
                dataSet Xi = new dataSet();
                dataSet XRest_i = new dataSet();
                dataSplit(X, i, i + step, Xi, XRest_i);
                XList.Add(XRest_i);
                XXList.Add(Xi);
            }

            Console.WriteLine("Done! cross-validation train/test data sizes (cv_1, ..., cv_n): ");
            Global.swLog.WriteLine("Done! cross-validation train/test data sizes (cv_1, ..., cv_n): ");
            for (int i = 0; i < Global.nCV; i++)
            {
                Global.swLog.WriteLine("{0}/{1}, ", XList[i].Count, XXList[i].Count);
            }
        }

        //check & set directory environment
        static void directoryCheck()
        {
            if (!Directory.Exists(Global.modelDir))
                Directory.CreateDirectory(Global.modelDir);
            Global.outDir = Directory.GetCurrentDirectory() + "/" + Global.outFolder + "/";
            if (!Directory.Exists(Global.outDir))
                Directory.CreateDirectory(Global.outDir);
            fileTool.removeFile(Global.outDir);
        }

        //should only read command here, should do the command validity check in globalCheck()
        static int readCommand(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg == "help")
                {
                    helpCommand();
                    return 1;
                }
                string[] ary = arg.Split(Global.colonAry, StringSplitOptions.RemoveEmptyEntries);
                if (ary.Length != 2)
                    return 2;
                string opt = ary[0], val = ary[1];

                switch (opt)
                {
                    case "m":
                        Global.runMode = val;
                        break;
                    case "mo":
                        Global.modelOptimizer = val;
                        break;
                    case "d":
                        Global.random = int.Parse(val);
                        break;
                    case "e":
                        Global.evalMetric = val;
                        break;
                    case "ss":
                        Global.trainSizeScale = double.Parse(val);
                        break;
                    case "i":
                        Global.ttlIter = int.Parse(val);
                        break;
                    case "s":
                        if (val == "1")
                            Global.save = 1;
                        else
                            Global.save = 0;
                        break;
                    case "of":
                        Global.outFolder = val;
                        break;
                    default:
                        return 2;
                }
            }
            return 0;//success
        }

        static void helpCommand()
        {
            Console.WriteLine("'option1:value1  option2:value2 ...' for setting values to options.");
            Console.WriteLine("'m' for runMode. Default: {0}", Global.runMode);
            Console.WriteLine("'mo' for modelOptimizer. Default: {0}", Global.modelOptimizer);
            Console.WriteLine("'d' for random. Default: {0}", Global.random);
            Console.WriteLine("'e' for evalMetric. Default: {0}", Global.evalMetric);
            Console.WriteLine("'ss' for trainSizeScale. Default: {0}", Global.trainSizeScale);
            Console.WriteLine("'i' for ttlIter. Default: {0}", Global.ttlIter);
            Console.WriteLine("'s' for save. Default: {0}", Global.save);
            Console.WriteLine("'of' for outFolder. Default: {0}", Global.outFolder);
        }
    }
}
