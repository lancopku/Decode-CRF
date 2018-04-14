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
    //convert the format of the input files
    class dataFormat
    {
        baseHashMap<string, int> featureIndexMap = new baseHashMap<string, int>();
        baseHashMap<string, int> tagIndexMap = new baseHashMap<string, int>();

        public void convert()
        {
            getMaps(Global.fTrain);
            convertFile(Global.fTrain);
            convertFile(Global.fTest);
            if(Global.dev)
                convertFile(Global.fDev);
        }

        public void getMaps(string file)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine("file {0} no exist!", file);
                return;
            }
            Console.WriteLine("file {0} converting...", file);
            StreamReader sr = new StreamReader(file);

            baseHashMap<string, int> featureFreqMap = new baseHashMap<string, int>();
            baseHashSet<string> tagSet = new baseHashSet<string>();

            //get feature-freq info and tagset
            int nFeatTemp = 0;
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                line = line.Replace("\t", " ");
                line = line.Replace("\r", "");

                if (line == "")
                    continue;

                string[] ary = line.Split(Global.blankAry, StringSplitOptions.RemoveEmptyEntries);
                nFeatTemp = ary.Length - 2;
                for (int i = 1; i < ary.Length - 1; i++)
                {

                    if (ary[i] == "/")//no feature here
                        continue;
                    string[] ary2 = ary[i].Split(Global.slashAry, StringSplitOptions.RemoveEmptyEntries);//for real-value features
                    string feature = i.ToString() + "." + ary2[0];
                    if (featureFreqMap.ContainsKey(feature) == false)
                        featureFreqMap[feature] = 1;
                    else
                        featureFreqMap[feature]++;
                }

                string tag = ary[ary.Length - 1];
                tagSet.Add(tag);
            }

            //sort features
            List<string> sortList = new List<string>();
            foreach (baseHashMap<string, int>.KeyValuePair kv in featureFreqMap)
                sortList.Add(kv.Key + " " + kv.Value);
            if (Global.regMode == "GL")//sort based on feature templates
            {
                sortList.Sort(listSortFunc.compareKV_key);
                //sortList.Reverse();

                Global.groupStart = new List<int>();
                Global.groupEnd = new List<int>();
                Global.groupStart.Add(0);
                for (int k = 1; k < sortList.Count; k++)
                {
                    string[] thisAry = sortList[k].Split(Global.dotAry, StringSplitOptions.RemoveEmptyEntries);
                    string[] preAry = sortList[k - 1].Split(Global.dotAry, StringSplitOptions.RemoveEmptyEntries);
                    string str = thisAry[0], preStr = preAry[0];
                    if (str != preStr)
                    {
                        Global.groupStart.Add(k);
                        Global.groupEnd.Add(k);
                    }
                }
                Global.groupEnd.Add(sortList.Count);
            }
            else//sort based on feature frequency
            {
                sortList.Sort(listSortFunc.compareKV_value);//sort feature based on freq, for 1)compress .txt file 2)better edge features
                sortList.Reverse();
            }

            if (Global.regMode == "GL")
            {
                if (nFeatTemp != Global.groupStart.Count)
                    throw new Exception("inconsistent # of features per line, check the feature file for consistency!");
            }

            //feature index should begin from 0
            StreamWriter swFeat = new StreamWriter("featureIndex.txt");
            for (int i = 0; i < sortList.Count; i++)
            {
                string[] ary = sortList[i].Split(Global.blankAry);
                featureIndexMap[ary[0]] = i;
                swFeat.WriteLine("{0} {1}", ary[0], i);
            }
            swFeat.Close();

            //label index should begin from 0
            StreamWriter swTag = new StreamWriter("tagIndex.txt");
            List<string> tagSortList = new List<string>();
            foreach (string tag in tagSet)
                tagSortList.Add(tag);
            tagSortList.Sort();//sort tags
            for (int i = 0; i < tagSortList.Count; i++)
            {
                tagIndexMap[tagSortList[i]] = i;
                swTag.WriteLine("{0} {1}", tagSortList[i], i);
            }
            swTag.Close();

            sr.Close();
        }


        //for small memory load, should read line by line
        public void convertFile(string file)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine("file {0} no exist!", file);
                return;
            }
            Console.WriteLine("file {0} converting...", file);
            StreamReader sr = new StreamReader(file);

            //convert to files of new format
            StreamWriter swFeature, swGold;
            if (file == Global.fTrain)
            {
                swFeature = new StreamWriter(Global.fFeatureTrain);
                swGold = new StreamWriter(Global.fGoldTrain);
            }
            else if (file == Global.fTest)
            {
                swFeature = new StreamWriter(Global.fFeatureTest);
                swGold = new StreamWriter(Global.fGoldTest);
            }
            else
            {
                swFeature = new StreamWriter(Global.fFeatureDev);
                swGold = new StreamWriter(Global.fGoldDev);
            }

            swFeature.WriteLine(featureIndexMap.Count);
            swFeature.WriteLine();
            swGold.WriteLine(tagIndexMap.Count);
            swGold.WriteLine();

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                line = line.Replace("\t", " ");
                line = line.Replace("\r", "");
                if (line == "")//end of a sample
                {
                    swFeature.WriteLine();
                    swGold.WriteLine();
                    swGold.WriteLine();
                    continue;
                }

                string[] ary = line.Split(Global.blankAry, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < ary.Length - 1; i++)
                {
                    if (ary[i] == "/")//no feature here
                        continue;
                    string[] ary2 = ary[i].Split(Global.slashAry, StringSplitOptions.RemoveEmptyEntries);//for real-value features
                    string feature = i.ToString() + "." + ary2[0];
                    string value = "";
                    bool real = false;
                    if (ary2.Length > 1)
                    {
                        value = ary2[1];
                        real = true;
                    }

                    if (featureIndexMap.ContainsKey(feature) == false)
                        continue;
                    int fIndex = featureIndexMap[feature];
                    if (!real)
                        swFeature.Write("{0},", fIndex);
                    else
                        swFeature.Write("{0}/{1},", fIndex, value);
                }
                swFeature.WriteLine();

                string tag = ary[ary.Length - 1];
                int tIndex = tagIndexMap[tag];
                swGold.Write("{0},", tIndex);
            }

            sr.Close();
            swFeature.Close();
            swGold.Close();
        }

    }


    class datasetList:List<dataSet>
    {
        protected int _nTag;
        protected int _nFeature;

        public datasetList(string fileFeature, string fileTag)
        {
            StreamReader srfileFeature = new StreamReader(fileFeature);
            StreamReader srfileTags = new StreamReader(fileTag);

            string txt = srfileFeature.ReadToEnd();
            txt = txt.Replace("\r", "");
            string[] fAry = txt.Split(Global.triLineEndAry, StringSplitOptions.RemoveEmptyEntries);
            
            txt = srfileTags.ReadToEnd();
            txt = txt.Replace("\r", "");
            string[] tAry = txt.Split(Global.triLineEndAry, StringSplitOptions.RemoveEmptyEntries);

            if (fAry.Length != tAry.Length)
                throw new Exception("error");

            _nFeature = int.Parse(fAry[0]);
            _nTag = int.Parse(tAry[0]);
            
            for (int i = 1; i < fAry.Length; i++)
            {
                string fBlock = fAry[i];
                string tBlock = tAry[i];
                dataSet ds = new dataSet();
                string[] fbAry = fBlock.Split(Global.biLineEndAry, StringSplitOptions.RemoveEmptyEntries);
                string[] tbAry = tBlock.Split(Global.biLineEndAry, StringSplitOptions.RemoveEmptyEntries);

                for (int k = 0; k < fbAry.Length; k++)
                {
                    string fm = fbAry[k];
                    string tm = tbAry[k];
                    dataSeq seq = new dataSeq();
                    seq.read(fm, tm);
                    ds.Add(seq);
                }
                Add(ds);
            }
            srfileFeature.Close();
            srfileTags.Close();
        }
    }

    class dataSet : List<dataSeq>
    {
        protected int _nTag;
        protected int _nFeatureTemp;

        public dataSet()
        {
        }

        public dataSet(int nTag, int nFeatureTemp)
        {
            _nTag = nTag;
            _nFeatureTemp = nFeatureTemp;
        }

        public dataSet(string fileFeature, string fileTags)
        {
            load(fileFeature, fileTags);
        }

        public dataSet randomShuffle()
        {
            List<int> ri = randomToolList<int>.getShuffledIndexList(this.Count);
            dataSet X = new dataSet(this.NTag, this.NFeature);
            foreach (int i in ri)
                X.Add(this[i]);
            return X;
        }

        virtual public int[,] EdgeFeature()
        {
            throw new Exception("error");
        }


        public List<string> readBlocks(string file)
        {
            if (!File.Exists(file))
                throw new Exception("file " + file + " no exist!");

            StreamReader sr = new StreamReader(file);
            List<string> blockList = new List<string>();
            string block = "";

            while (true)
            {
                string line = sr.ReadLine();
                line = line.Replace("\t", " ");
                line = line.Replace("\r", "");
                if (line == "")//end of a sample
                {
                    if (block != "")
                        blockList.Add(block.Trim(Global.lineEndAry));
                    block = "";
                    if (sr.EndOfStream)
                        break;
                    else
                        continue;
                }
              
                block += line + "\n";

                if (sr.EndOfStream)
                {
                    if (block != "")
                        blockList.Add(block.Trim(Global.lineEndAry));
                    break;
                }
            }

            sr.Close();
            return blockList;
        }

        virtual public void load(string fileFeature, string fileTag)
        {
            List<string> featureBlockList = readBlocks(fileFeature);
            List<string> tagBlockList = readBlocks(fileTag);

            if (featureBlockList.Count != tagBlockList.Count)
                throw new Exception("error");

            _nFeatureTemp = int.Parse(featureBlockList[0]);
            _nTag = int.Parse(tagBlockList[0]);
            for (int i = 1; i < tagBlockList.Count; i++)
            {
                string features = featureBlockList[i];
                string tags = tagBlockList[i];
                dataSeq seq = new dataSeq();
                seq.read(features, tags);
                Add(seq);
            }
        }

        public int NTag
        {
            get { return _nTag; }
            set { _nTag = value; }
        }

        public int NFeature
        {
            get { return _nFeatureTemp; }
            set { _nFeatureTemp = value; }
        }

        public void setDataInfo(dataSet X)
        {
            _nTag = X.NTag;
            _nFeatureTemp = X.NFeature;
        }

    }

    class dataSeqTest
    {
        public dataSeq _x;
        public List<int> _yOutput;

        public dataSeqTest(dataSeq x, List<int> yOutput)
        {
            _x = x;
            _yOutput = yOutput;
        }
    }

    class dataSeq
    {
        protected List<List<featureTemp>> featureTemps = new List<List<featureTemp>>();
        protected List<int> yGold = new List<int>();
        protected dMatrix goldStatesPerNode = new dMatrix(); //no inited when reading data, but during FB of training
        /*
        records the matrix hiddenStatesNodes, 
        e.g.:
        0 0
        0 0
        0 1
        0 1
        1 0
        1 0
        when the tag sentence is 2,1 and every tag have two hidden states
        */

        public dataSeq()
        {
        }

        public dataSeq(dataSeq x, int n, int length, bool forward = true)
        {
            int start = -1, end = -1;
            if (forward)//forward
            {
                start = n;
                if (n + length < x.Count)
                    end = n + length;
                else
                    end = x.Count;
            }
            else//backward
            {
                end = n + 1;
                if (end - length >= 0)
                    start = end - length;
                else
                    start = 0;
            }

            for (int i = start; i < end; i++)
            {
                featureTemps.Add(x.featureTemps[i]);
                yGold.Add(x.yGold[i]);
            }
        }

        public dataSeq(List<List<featureTemp>> feat, List<int> y)
        {
            featureTemps = new List<List<featureTemp>>(feat);
            for (int i = 0; i < feat.Count; i++)
                featureTemps[i] = new List<featureTemp>(feat[i]);
            yGold = new List<int>(y);
        }

        public dataSeq(dataSeq x, int n, int length)
        {
            int end = 0;
            if (n + length < x.Count)
                end = n + length;
            else
                end = x.Count;
            for (int i = n; i < end; i++)
            {
                featureTemps.Add(x.featureTemps[i]);
                yGold.Add(x.yGold[i]);
            }
        }

        virtual public List<List<int>> getNodeFeature(int n)
        {
            throw new Exception("error");
        }

        virtual public void read(string a, int nState, string b)
        {
            throw new Exception("error");
        }

        public void read(string a, string b)
        {
            //features
            string[] lineAry = a.Split(Global.lineEndAry, StringSplitOptions.RemoveEmptyEntries);
            foreach (string im in lineAry)
            {
                List<featureTemp> nodeList = new List<featureTemp>();
                string[] imAry = im.Split(Global.commaAry, StringSplitOptions.RemoveEmptyEntries);
                foreach (string imm in imAry)
                {
                    if (imm.Contains("/"))
                    {
                        string[] biAry = imm.Split(Global.slashAry, StringSplitOptions.RemoveEmptyEntries);
                        featureTemp ft = new featureTemp(int.Parse(biAry[0]), double.Parse(biAry[1]));
                        nodeList.Add(ft);
                    }
                    else
                    {
                        featureTemp ft = new featureTemp(int.Parse(imm), 1);
                        nodeList.Add(ft);
                    }
                }
                featureTemps.Add(nodeList);
            }
            //yGold
            lineAry = b.Split(Global.commaAry, StringSplitOptions.RemoveEmptyEntries);
            foreach (string im in lineAry)
            {
                yGold.Add(int.Parse(im));
            }
        }

        virtual public int Count
        {
            get { return featureTemps.Count; }
        }

        public List<List<featureTemp>> getFeatureTemp()
        {
            return featureTemps;
        }

        public List<featureTemp> getFeatureTemp(int node)
        {
            return featureTemps[node];
        }

        public int getTags(int node)
        {
            return yGold[node];
        }

        public List<int> getTags()
        {
            return yGold;
        }

        public void setTags(List<int> tags)
        {
            if (tags.Count != yGold.Count)
                throw new Exception("error");
            for (int i = 0; i < tags.Count; i++)
                yGold[i] = tags[i];
        }

        public dMatrix GoldStatesPerNode
        {
            get{return goldStatesPerNode;}
            set { goldStatesPerNode = value; }
        }
            
    }














}