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
    //feature template
    struct featureTemp
    {
        public readonly int id;//feature id
        public readonly double val;//feature value

        public featureTemp(int a, double b)
        {
            id = a;
            val = b;
        }
    }

    //feature
    struct nodeFeature
    {
        public readonly short _s;//tag
        public readonly int _id;//true feature id

        //init node feature
        public nodeFeature(int s, int id)
        {
            //_edge = false;
            _s = (short)s;
            _id = id;
        }
    }

    //feature
    struct edgeFeature
    {
        public readonly short _s;//tag
        public readonly short _sPre;//pre tag
        public readonly int _id;//true feature id

        //init edge feature
        public edgeFeature(int s, int sPre, int id)
        {
            //_edge = true;
            _s = (short)s;
            _sPre = (short)sPre;
            _id = id;
        }
    }

    class featureGenerator
    {
        protected int _nFeatureTemp;
        protected int _nCompleteFeature;
        protected int _nEdge;//# of non-rich edge features
        protected int _nTag;

        //for training
        public featureGenerator(dataSet X)
        {
            _nFeatureTemp = X.NFeature;
            int ft_richEdge = (int)(X.NFeature * Global.edgeReduce);
            _nTag = X.NTag;
            _nEdge = _nTag * _nTag;
            Global.swLog.WriteLine("feature templates: {0}", _nFeatureTemp);

            //build feature mapping etc. information
            //baseHashMap<string, int> strIntMap = new baseHashMap<string, int>(_nFeatureTemp * _nTag, 0.65,2);
            baseHashSet<int>[] setAry = new baseHashSet<int>[_nFeatureTemp];
            for (int i = 0; i < setAry.Length; i++)
            {
                setAry[i] = new baseHashSet<int>();
            }
            List<nodeFeature>[] idNodeFeatures = new List<nodeFeature>[_nFeatureTemp];
            List<edgeFeature>[] idEdgeFeatures = new List<edgeFeature>[_nFeatureTemp];
            List<edgeFeature>[] idEdgeFeatures2 = new List<edgeFeature>[_nFeatureTemp];
            for (int i = 0; i < _nFeatureTemp; i++)
            {
                idNodeFeatures[i] = new List<nodeFeature>();
                idEdgeFeatures[i] = new List<edgeFeature>();
                idEdgeFeatures2[i] = new List<edgeFeature>();
            }
            int fIndex = _nEdge;//start from this

            int factor = 10000, factor2 = 100000;
            if (Global.negFeatureMode == "node")//neg features for node features
            {
                for (int id = 0; id < _nFeatureTemp; id++)
                    for (int tag = 0; tag < _nTag; tag++)
                    {
                        //node feature
                        int mark = tag;
                        if (!setAry[id].Contains(mark))
                        {
                            int fid = fIndex;
                            setAry[id].Add(mark);
                            fIndex++;

                            nodeFeature feat = new nodeFeature(tag, fid);
                            idNodeFeatures[id].Add(feat);
                        }
                    }
            }
            else if (Global.negFeatureMode == "edge")//neg features for node & edge features
            {
                //s2 case
                for (int id = 0; id < _nFeatureTemp; id++)
                    for (int tag = 0; tag < _nTag; tag++)
                    {
                        //node feature
                        int mark = tag;
                        if (!setAry[id].Contains(mark))
                        {
                            int fid = fIndex;
                            setAry[id].Add(mark);
                            fIndex++;

                            nodeFeature feat = new nodeFeature(tag, fid);
                            idNodeFeatures[id].Add(feat);
                        }
                    }

                //neg rich edge feature
                for (int id = 0; id < _nFeatureTemp; id++)
                {
                    //rich edge here, non-rich edge feature is already coded before
                    if (id < ft_richEdge)//pruning rich edge features, id relates to frequency of features
                    {
                        for (int random = 0; random < Global.nNegEdgeFeat; random++)
                        {
                            int tag = randomTool.getOneRandom_int(0, _nTag), preTag = randomTool.getOneRandom_int(0, _nTag);
                            int mark = tag * factor + preTag;
                            if (!setAry[id].Contains(mark))
                            {
                                int fid = fIndex;
                                setAry[id].Add(mark);
                                fIndex++;

                                edgeFeature feat = new edgeFeature(tag, preTag, fid);
                                idEdgeFeatures[id].Add(feat);
                            }
                        }

                        //rich2
                        if (Global.richFeat2)
                        {
                            for (int random = 0; random < Global.nNegEdgeFeat; random++)
                            {
                                int tag = randomTool.getOneRandom_int(0, _nTag), preTag = randomTool.getOneRandom_int(0, _nTag);
                                int mark = tag * factor2 + preTag;
                                if (!setAry[id].Contains(mark))
                                {
                                    int fid = fIndex;
                                    setAry[id].Add(mark);
                                    fIndex++;

                                    edgeFeature feat = new edgeFeature(tag, preTag, fid);
                                    idEdgeFeatures2[id].Add(feat);
                                }
                            }
                        }
                    }
                }
            }
            else if (Global.negFeatureMode == "full")//full negative features for node features & edge features
            {
                //s2 case
                for (int id = 0; id < _nFeatureTemp; id++)
                    for (int tag = 0; tag < _nTag; tag++)
                    {
                        //node feature
                        int mark = tag;
                        if (!setAry[id].Contains(mark))
                        {
                            int fid = fIndex;
                            setAry[id].Add(mark);
                            fIndex++;

                            nodeFeature feat = new nodeFeature(tag, fid);
                            idNodeFeatures[id].Add(feat);
                        }
                    }

                //neg rich edge feature
                for (int id = 0; id < _nFeatureTemp; id++)
                {
                    //rich edge here, non-rich edge feature is already coded before
                    if (id < ft_richEdge)//pruning rich edge features, id relates to frequency of features
                    {
                        for (int tag = 0; tag < _nTag; tag++)
                            for (int preTag = 0; preTag < _nTag; preTag++)
                            {
                                int mark = tag * factor + preTag;
                                if (!setAry[id].Contains(mark))
                                {
                                    int fid = fIndex;
                                    setAry[id].Add(mark);
                                    fIndex++;

                                    edgeFeature feat = new edgeFeature(tag, preTag, fid);
                                    idEdgeFeatures[id].Add(feat);
                                }
                            }

                        //rich2
                        if (Global.richFeat2)
                        {
                            for (int tag = 0; tag < _nTag; tag++)
                                for (int preTag = 0; preTag < _nTag; preTag++)
                                {
                                    int mark = tag * factor2 + preTag;
                                    if (!setAry[id].Contains(mark))
                                    {
                                        int fid = fIndex;
                                        setAry[id].Add(mark);
                                        fIndex++;

                                        edgeFeature feat = new edgeFeature(tag, preTag, fid);
                                        idEdgeFeatures2[id].Add(feat);
                                    }
                                }
                        }
                    }
                }
            }

            //true features
            foreach (dataSeq x in X)
            {
                for (int i = 0; i < x.Count; i++)
                {
                    List<featureTemp> fList = getFeatureTemp(x, i);
                    int tag = x.getTags(i);
                    foreach (featureTemp im in fList)
                    {
                        int id = im.id;
                        //node feature
                        int mark = tag;
                        if (!setAry[id].Contains(mark))
                        {
                            int fid = fIndex;
                            setAry[id].Add(mark);
                            fIndex++;

                            nodeFeature feat = new nodeFeature(tag, fid);
                            idNodeFeatures[id].Add(feat);
                        }
                        //rich edge here, non-rich edge feature is already coded before
                        if (i > 0 && id < ft_richEdge)//pruning rich edge features, id relates to frequency of features
                        {
                            int preTag = x.getTags(i - 1);
                            mark = tag * factor + preTag;
                            if (!setAry[id].Contains(mark))
                            {
                                int fid = fIndex;
                                setAry[id].Add(mark);
                                fIndex++;

                                edgeFeature feat = new edgeFeature(tag, preTag, fid);
                                idEdgeFeatures[id].Add(feat);
                            }
                        }

                        //rich2 feature
                        if (Global.richFeat2)
                        {
                            if (i < x.Count - 1 && id < ft_richEdge)//pruning rich edge features, id relates to frequency of features
                            {
                                int postTag = x.getTags(i + 1);
                                mark = tag * factor2 + postTag;
                                if (!setAry[id].Contains(mark))
                                {
                                    int fid = fIndex;
                                    setAry[id].Add(mark);
                                    fIndex++;

                                    edgeFeature feat = new edgeFeature(postTag, tag, fid);
                                    idEdgeFeatures2[id].Add(feat);
                                }
                            }
                        }
                    }
                }
            }

            //build globals
            Global.idNodeFeatures = new nodeFeature[_nFeatureTemp][];
            Global.idEdgeFeatures = new edgeFeature[_nFeatureTemp][];
            Global.idEdgeFeatures2 = new edgeFeature[_nFeatureTemp][];
            for (int i = 0; i < _nFeatureTemp; i++)
            {
                Global.idNodeFeatures[i] = idNodeFeatures[i].ToArray();
                Global.idEdgeFeatures[i] = idEdgeFeatures[i].ToArray();
                Global.idEdgeFeatures2[i] = idEdgeFeatures2[i].ToArray();
            }

            _nCompleteFeature = fIndex;

            Global.swLog.WriteLine("feature templates & rich-edge feature templates: {0}, {1}", _nFeatureTemp, ft_richEdge);
            //Global.swLog.WriteLine("nNodeFeature, nEdgeFeature1, nEdgeFeature2: {0}, {1}, {2}", nNodeFeature, nEdgeFeature1, nEdgeFeature2);
            Global.swLog.WriteLine("complete features: {0}", _nCompleteFeature);
            Global.swLog.WriteLine();
            Global.swLog.Flush();

            setAry = null;
            idNodeFeatures = null;
            idEdgeFeatures = null;
            idEdgeFeatures2 = null;
            GC.Collect();//should set null before memo collect
        }

        public List<featureTemp> getFeatureTemp(dataSeq x, int node)
        {
            return x.getFeatureTemp(node);
        }

        public int getEdgeFeatID(int sPre, int s)
        {
            return s * _nTag + sPre;
        }

        public int NCompleteFeature { get { return _nCompleteFeature; } }
    }
}

