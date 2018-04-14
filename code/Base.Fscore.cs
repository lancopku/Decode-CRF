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
using System.Diagnostics;

namespace Program
{
    class fscore
    {
        public static List<double> getFscore(List<string> goldTagList, List<string> resTagList, List<double> infoList)
        {
            List<double> scoreList = new List<double>();
            if (resTagList.Count != goldTagList.Count)
                throw new Exception("error");

            //convert original tags to 3 tags: B(x), I, O
            getNewTagList(Global.chunkTagMap, ref goldTagList);
            getNewTagList(Global.chunkTagMap, ref resTagList);
            List<string> goldChunkList = getChunks(goldTagList);
            List<string> resChunkList = getChunks(resTagList);

            int gold_chunk = 0, res_chunk = 0, correct_chunk = 0;
            for (int i = 0; i < goldChunkList.Count; i++)
            {
                string res = resChunkList[i];
                string gold = goldChunkList[i];
                string[] resChunkAry = res.Split(Global.commaAry, StringSplitOptions.RemoveEmptyEntries);
                string[] goldChunkAry = gold.Split(Global.commaAry, StringSplitOptions.RemoveEmptyEntries);
                gold_chunk += goldChunkAry.Length;
                res_chunk += resChunkAry.Length;
                baseHashSet<string> goldChunkSet = new baseHashSet<string>();
                foreach (string im in goldChunkAry)
                    goldChunkSet.Add(im);

                foreach (string im in resChunkAry)
                {
                    if (goldChunkSet.Contains(im))
                        correct_chunk++;
                }
            }
            double pre = (double)correct_chunk / (double)res_chunk * 100;
            double rec = (double)correct_chunk / (double)gold_chunk * 100;
            double f1 = 2 * pre * rec / (pre + rec);
            scoreList.Add(f1);
            scoreList.Add(pre);
            scoreList.Add(rec);

            infoList.Add(gold_chunk);
            infoList.Add(res_chunk);
            infoList.Add(correct_chunk);
            return scoreList;
        }

        //convert number-based list to letter-based list
        static void getNewTagList(baseHashMap<int, string> tagMap, ref List<string> tagList)
        {
            List<string> tmpList = new List<string>();
            foreach (string im in tagList)
            {
                string[] tagAry = im.Split(Global.commaAry, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < tagAry.Length; i++)
                {
                    int index = int.Parse(tagAry[i]);
                    if (!tagMap.ContainsKey(index))
                        throw new Exception("error");
                    tagAry[i] = tagMap[index];
                }
                string newTags = string.Join(",", tagAry);
                tmpList.Add(newTags);
            }
            tagList.Clear();
            foreach (string im in tmpList)
                tagList.Add(im);
        }

        //convert tags to chunks with type, length, & position info
        static List<string> getChunks(List<string> tagList)
        {
            List<string> tmpList = new List<string>();
            foreach (string im in tagList)
            {
                string[] tagAry = im.Split(Global.commaAry, StringSplitOptions.RemoveEmptyEntries);
                string chunks = "";
                for (int i = 0; i < tagAry.Length; i++)
                {
                    if (tagAry[i].StartsWith("B"))
                    {
                        int pos = i;
                        int length = 1;
                        string type = tagAry[i];
                        for (int j = i + 1; j < tagAry.Length; j++)
                        {
                            if (tagAry[j] == "I")
                                length++;
                            else
                                break;
                        }
                        string chunk = type + "*" + length + "*" + pos;
                        chunks += chunk + ",";
                    }
                }
                tmpList.Add(chunks);
            }
            return tmpList;
        }

    }
}
