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
    //to summarize the results written in the .txt file.
    class resSummarize
    {
        public static void summarize(string fn = "f2")
        {
            StreamReader sr = new StreamReader(Global.outDir + Global.fResRaw);
            string txt = sr.ReadToEnd();
            sr.Close();
            txt = txt.Replace("\r", "");
            string[] regions = txt.Split(Global.triLineEndAry, StringSplitOptions.RemoveEmptyEntries);
            StreamWriter sw = new StreamWriter(Global.outDir + Global.fResSum);
            foreach (string region in regions)
            {
                string[] blocks = region.Split(Global.biLineEndAry, StringSplitOptions.RemoveEmptyEntries);
                List<dMatrix> mList = new List<dMatrix>();
                foreach (string im in blocks)
                {
                    dMatrix m = new dMatrix(im);
                    mList.Add(m);
                }

                //get average
                dMatrix avgM = new dMatrix(mList[0]);
                avgM.set(0);
                foreach (dMatrix m in mList)
                    avgM.add(m);
                avgM.divide(mList.Count);
                //get devi
                dMatrix deviM = mathTool.getDeviation(mList);
                
                sw.WriteLine("%averaged values:");
                avgM.write(sw, fn);
                sw.WriteLine();
                sw.WriteLine("%deviations:");
                deviM.write(sw, fn);
                sw.WriteLine();
                sw.WriteLine("%avg & devi:");

                sMatrix sAvgM = new sMatrix(avgM, fn);
                sAvgM.add("+-");
                sMatrix sDeviM = new sMatrix(deviM, fn);
                sAvgM.add(sDeviM);
                sAvgM.write(sw);
                sw.WriteLine("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%\n\n");
            }
            sw.Close();
        }
        
        public static void write()
        {
            if (Global.runMode.Contains("test"))
            {
                if (Global.scoreListList.Count > 1 || Global.timeList.Count > 1)
                    throw new Exception("error");
                if (Global.rawResWrite) Global.swResRaw.WriteLine("%test results:");
                List<double> list = Global.scoreListList[0];
                //reader format
                if (Global.evalMetric == "f1")
                {
                    if (Global.rawResWrite) Global.swResRaw.Write("% f-score={0}%  precision={1}%  recall={2}%  ", list[0].ToString("f2"), list[1].ToString("f2"), list[2].ToString("f2"));
                }
                else
                {
                    if (Global.rawResWrite) Global.swResRaw.Write("% {0}={1}%  ", Global.metric, list[0].ToString("f2"));
                }
                if (Global.rawResWrite) Global.swResRaw.WriteLine("test-time(sec)={0}", Global.timeList[0].ToString("f2"));
                //matrix format
                foreach (double im in list)
                {
                    if (Global.rawResWrite) Global.swResRaw.Write(im.ToString("f2") + ",");
                }
                if (Global.rawResWrite) Global.swResRaw.WriteLine(Global.timeList[0].ToString("f2") + ",");
            }
            else
            {
                if (Global.rawResWrite) Global.swResRaw.WriteLine("% training results:", Global.metric);
                //reader format
                for (int i = 0; i < Global.ttlIter; i++)
                {
                    double iter = i + 1;
                    if (Global.rawResWrite) Global.swResRaw.Write("% iter#={0}  ", iter);
                    List<double> list = Global.scoreListList[i];
                    if (Global.evalMetric == "f1")
                    {
                        if (Global.rawResWrite) Global.swResRaw.Write("f-score={0}%  precision={1}%  recall={2}%  ", list[0].ToString("f2"), list[1].ToString("f2"), list[2].ToString("f2"));
                    }
                    else
                    {
                        if (Global.rawResWrite) Global.swResRaw.Write("{0}={1}%  ", Global.metric, list[0].ToString("f2"));
                    }
                    double time = 0;
                    for (int k = 0; k <= i; k++)
                        time += Global.timeList[k];
                    if (Global.rawResWrite) Global.swResRaw.Write("cumulative-time(sec)={0}  ", time.ToString("f2"));
                    if (Global.rawResWrite) Global.swResRaw.Write("objective={0}  ", Global.errList[i].ToString("f2"));
                    if (Global.rawResWrite) Global.swResRaw.Write("diff={0}", Global.diffList[i].ToString("e2"));
                    if (Global.rawResWrite) Global.swResRaw.WriteLine();
                }

                //matrix format
                Global.ttlScore = 0;
                for (int i = 0; i < Global.ttlIter; i++)
                {
                    double iter = i + 1;
                    if (Global.rawResWrite) Global.swResRaw.Write(iter.ToString() + ",");
                    List<double> list = Global.scoreListList[i];
                    Global.ttlScore += list[0];

                    foreach (double im in list)
                    {
                        if (Global.rawResWrite) Global.swResRaw.Write(im.ToString("f2") + ",");
                    }
                    double time = 0;
                    for (int k = 0; k <= i; k++)
                        time += Global.timeList[k];
                    if (Global.rawResWrite) Global.swResRaw.Write(time.ToString("f2") + ",");
                    if (Global.rawResWrite) Global.swResRaw.Write(Global.errList[i].ToString("f2") + ",");
                    if (Global.rawResWrite) Global.swResRaw.WriteLine(Global.diffList[i].ToString("e2"));
                }
                if (Global.rawResWrite) Global.swResRaw.Flush();
            }

            //clear
            Global.scoreListList.Clear();
            Global.timeList.Clear();
            Global.errList.Clear();
            Global.diffList.Clear();
        }

    }
    
}
