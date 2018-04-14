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
using System.Threading;

namespace Program
{
    class Global
    {
        public static string modelOptimizer = "sapo.avg";//sapo (SAPO model), perc.avg/naive, mira.avg/naive (here rand0 works much better), miran.avg/naive (n-best mira)
        public static string evalMetric = "tok.acc";//tok.acc (token accuracy), str.acc (string accuracy), f1 (F1-score)
        public static int ttlIter = 100;//# of training iterations
        public static bool formatConvert = false;
        public static bool dev = true;
        public static string negFeatureMode = "edge";//null, node, edge, full. edge is default.
        public static int nNegEdgeFeat = 10;//10
        public static bool useTraditionalEdge = true;//true
        public static bool richFeat2 = true;
        public static double edgeReduce = 1;//0 for only non-rich features
        public static string regMode = "L2";//'GL' (groupLasso) is only for averaged training, 'no' for no reg, 'L2' for L2 reg
        public static double reg = 2;
        public static bool structReg = true;
        //public static double[] srAry = { 0, 1.5, 2.5, 3.5, 5.5, 10.5, 15.5, 20.5 };//these values relate to alphas!
        public static double[] srAry = { 1.5};//these values relate to alphas!
        public static double miniSize;
        public static int nThread = 20;
        public static int nBest = 5;
        public static bool beamSearch = false;
        public static int beamSize = 100;
        public static double stopSearchFactor = 20;
        //SGD optimizer
        public static double rate0 = 0.02;//decay rate in SGD and ADF training
        public const double decayFactor = 0.90;//decay factor in SGD training
        public static double r_k;

        public static int stepGap = 1;
        public static int overlapLength = 0;//typically 0

        //default values
        public static string runMode = "train";//train (training with rich edge), test, cv (cross validation)
        public static int random = 1;//0 for 0-initialization of model weights, 1 for random init of model weights, 0 is important for GL reg
        public static float randScale = 0.01f;
        public static double trainSizeScale = 1;//for scaling the size of training data
        public static string outFolder = "out";
        public static int save = 1;//save model file
        public static bool rawResWrite = true;
        public static int nCV = 4;//automatic #-fold cross validation
        public static List<List<dataSeq>> threadX;
        public static List<List<dataSeqTest>> threadXX;
        public static bool tuneInit = true;
        public static int xsize = 0;
        public static int k = 0;
        public static int wUpdate = 0;

        //general
        public static bool writeTagBasedAcc = false;
        public const double tuneSplit = 0.8;//size of data split for tuning
        public static bool debug = false;//some debug code will run in debug mode
        //tuning
        public const int nTuneRound = 5;
        public const int iterTuneWeightInit = 20;//default 20

        //global variables
        public static List<double> threadError;
        public static double nbestCount;
        public static int nbestNorm;
        public static List<double> nbestProbList;
        public static List<double> srList = new List<double>(srAry);
        public static List<int> groupStart;
        public static List<int> groupEnd;
        public static nodeFeature[][] idNodeFeatures;
        public static edgeFeature[][] idEdgeFeatures;
        public static edgeFeature[][] idEdgeFeatures2;
        public static bool tuneWeightInit = false;
        public static baseHashMap<int, string> chunkTagMap = new baseHashMap<int, string>();
        public static string metric;
        public static float[] tmpW = null;
        public static float[] optimW = null;
        public static double ttlScore = 0;
        public static string outDir = "";
        public static List<List<double>> scoreListList = new List<List<double>>();
        public static List<double> timeList = new List<double>();
        public static List<double> errList = new List<double>();
        public static List<double> diffList = new List<double>();
        public static int glbIter = 0;
        public static double diff = 1e100;//relative difference from the previous object value, for convergence test
        public static int countWithIter = 0;
        public static StreamWriter swTune;
        public static StreamWriter swLog;
        public static StreamWriter swResRaw;
        public static StreamWriter swOutput;
        public const string fTrain = "train.txt";
        public const string fTest = "test.txt";
        public const string fDev = "dev.txt";
        public const string fTune = "tune.txt";
        public const string fLog = "trainLog.txt";
        public const string fResSum = "summarizeResult.txt";
        public const string fResRaw = "rawResult.txt";
        public const string fFeatureTrain = "ftrain.txt";
        public const string fGoldTrain = "gtrain.txt";
        public const string fFeatureTest = "ftest.txt";
        public const string fGoldTest = "gtest.txt";
        public const string fFeatureDev = "fdev.txt";
        public const string fGoldDev = "gdev.txt";
        public const string fOutput = "outputTag.txt";
        public const string fModel = "model/model.txt";
        public const string modelDir = "model/";
        public static char[] lineEndAry = { '\n' };
        public static string[] biLineEndAry = { "\n\n" };
        public static string[] triLineEndAry = { "\n\n\n" };
        public static char[] barAry = { '-' };
        public static char[] dotAry = { '.'};
        public static char[] underlnAry = { '_' };
        public static char[] commaAry = { ',' };
        public static char[] tabAry = { '\t' };
        public static char[] vertiBarAry = { '|' };
        public static char[] colonAry = { ':' };
        public static char[] blankAry = { ' ' };
        public static char[] starAry = { '*' };
        public static char[] slashAry = { '/' };
 
        public static void reinitGlobal()
        {
            diff = 1e100;
            countWithIter = 0;
            glbIter = 0;
        }

        public static void globalCheck()
        {
            if (runMode.Contains("test"))
                ttlIter = 1;

            if (evalMetric == "f1")
                getChunkTagMap();

            if (evalMetric == "f1")
                metric = "f-score";
            else if (evalMetric == "tok.acc")
                metric = "token-accuracy";
            else if (evalMetric == "str.acc")
                metric = "string-accuracy";
            else throw new Exception("error");

            if (Global.trainSizeScale <= 0)
                throw new Exception("error");
            if (Global.ttlIter <= 0)
                throw new Exception("error");
        }

        public static void printGlobals()
        {
            swLog.WriteLine("mode: {0}", runMode);
            swLog.WriteLine("modelOptimizer: {0}", modelOptimizer);
            swLog.WriteLine("random: {0}", random);
            swLog.WriteLine("evalMetric: {0}", evalMetric);
            swLog.WriteLine("trainSizeScale: {0}", trainSizeScale);
            swLog.WriteLine("ttlIter: {0}", ttlIter);
            swLog.WriteLine("outFolder: {0}", outFolder);
            swLog.WriteLine("formatConvert: {0}", formatConvert);
            swLog.WriteLine("negFeatureMode: {0}", negFeatureMode);
            swLog.WriteLine("edgeReduce: {0}", edgeReduce);
            swLog.WriteLine("useTraditionalEdge: {0}", useTraditionalEdge);
            swLog.WriteLine("richFeat2: {0}", richFeat2);
            swLog.WriteLine("regMode: {0}", regMode);
            swLog.WriteLine("structReg: {0}", structReg);
            swLog.WriteLine("segStep: {0}", miniSize);
            swLog.WriteLine("nThread: {0}", nThread);
            swLog.Flush();
        }

        //the system must know the B (begin-chunk), I (in-chunk), O (out-chunk) information for computing f-score
        //since such BIO information is task-dependent, tagIndex.txt is required
        static void getChunkTagMap()
        {
            chunkTagMap.Clear();

            //read the labelMap.txt for chunk tag information
            StreamReader sr = new StreamReader("tagIndex.txt");
            string a = sr.ReadToEnd();
            a = a.Replace("\r", "");
            string[] ary = a.Split(Global.lineEndAry, StringSplitOptions.RemoveEmptyEntries);
            foreach (string im in ary)
            {
                string[] imAry = im.Split(Global.blankAry, StringSplitOptions.RemoveEmptyEntries);
                int index = int.Parse(imAry[1]);
                string[] tagAry = imAry[0].Split(Global.starAry, StringSplitOptions.RemoveEmptyEntries);
                string tag = tagAry[tagAry.Length - 1];//the last tag is the current tag
                //merge I-tag/O-tag: no need to use diversified I-tag/O-tag in computing F-score
                if (tag.StartsWith("I"))
                    tag = "I";
                if (tag.StartsWith("O"))
                    tag = "O";
                chunkTagMap[index] = tag;
            }

            sr.Close();
        }

    }

}
