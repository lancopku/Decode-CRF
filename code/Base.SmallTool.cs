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
    class arrayTool
    {
        public static double squareSum(float[] a)
        {
            double sum = 0;
            foreach (float im in a)
                sum += im * im;
            return sum;
        }

        public static double sum(float[] a)
        {
            double sum = 0;
            foreach (float im in a)
                sum += Math.Abs(im);
            return sum;
        }
    }

    class listTool
    {
        public static int compare(List<int> a, List<int> b)
        {
            int diff = 0;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i])
                {
                    diff++;
                }

            return diff;
        }

        public static List<double> getDoubleList(string a)
        {
            string[] tokens = a.Split(Global.commaAry, StringSplitOptions.RemoveEmptyEntries);
            List<double> dList = new List<double>();
            foreach (string token in tokens)
            {
                dList.Add(double.Parse(token));
            }
            return dList;
        }

        public static double squareSum(List<double> list)
        {
            double sum = 0;
            foreach (double im in list)
                sum += im * im;
            return sum;
        }

        public static void listSet(ref List<double> a, List<double> b)
        {
            if (a.Count != b.Count)
                throw new Exception("error");
            for (int i = 0; i < a.Count; i++)
                a[i] = b[i];
        }

        public static void listSet_add(ref List<double> a, List<double> b)
        {
            a.Clear();
            foreach (double im in b)
                a.Add(im);
        }

        public static void listSet(ref List<double> a, double v)
        {
            for (int i = 0; i < a.Count; i++)
                a[i] = v;
        }

        public static void listAdd(ref List<double> a, List<double> b)
        {
            if (a.Count != b.Count)
                throw new Exception("err90");
            for (int i = 0; i < a.Count; i++)
                a[i] += b[i];
        }

        public static void listAdd(ref List<double> a, double v)
        {
            for (int i = 0; i < a.Count; i++)
                a[i] += v;
        }

        public static void listMultiply(ref List<double> a, double v)
        {
            for (int i = 0; i < a.Count; i++)
                a[i] *= v;
        }

        public static void listExp(ref List<double> a)
        {
            for (int i = 0; i < a.Count; i++)
                a[i] = Math.Exp(a[i]);
        }

        public static void listSwap(ref List<double> a, ref List<double> b)
        {
            if (a.Count != b.Count)
                throw new Exception("err90");
            for (int i = 0; i < a.Count; i++)
            {
                double tmp = a[i];
                a[i] = b[i];
                b[i] = tmp;
            }
        }

        public static void showSample(List<double> a)
        {
            int L = a.Count;
            int step = L / 10;
            for (int i = 0; i < L; i += step)
                Console.WriteLine("#" + i.ToString() + " " + a[i].ToString("e2"));
            Console.WriteLine();
        }

        public static void showSample(StreamWriter sw, List<double> a)
        {
            int L = a.Count;
            int step = L / 10;
            for (int i = 0; i < L; i += step)
                sw.WriteLine("#" + i.ToString() + " " + a[i].ToString("e2"));
            sw.WriteLine();
        }

        public static void showSample(List<int> a)
        {
            int L = a.Count;
            int step = L / 10;
            if (step == 0)
                step = 1;
            for (int i = 0; i < L; i += step)
                Console.WriteLine("#" + i.ToString() + " " + a[i].ToString("e2"));
            Console.WriteLine();
        }
    }

    class fileTool
    {
        public static void removeFile(string folder)
        {
            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                File.Delete(file);
            }
        }
    }


    class listSortFunc
    {
        public static int compareKV_value(string a, string b)
        {
            string[] aAry = a.Split(Global.blankAry, StringSplitOptions.RemoveEmptyEntries);
            string[] bAry = b.Split(Global.blankAry, StringSplitOptions.RemoveEmptyEntries);
            double aProb = double.Parse(aAry[aAry.Length - 1]);
            double bProb = double.Parse(bAry[bAry.Length - 1]);
            if (aProb > bProb)
                return 1;
            else if (aProb < bProb)
                return -1;
            else return 0;
        }


        public static int compareKV_key(string a, string b)
        {
            string[] aAry = a.Split(Global.dotAry, StringSplitOptions.RemoveEmptyEntries);
            string[] bAry = b.Split(Global.dotAry, StringSplitOptions.RemoveEmptyEntries);
            double aProb = double.Parse(aAry[0]);
            double bProb = double.Parse(bAry[0]);
            if (aProb > bProb)
                return 1;
            else if (aProb < bProb)
                return -1;
            else return 0;
        }
    }

    class ordinalComparerTool : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return (string.CompareOrdinal(x, y));
        }
    }

    class stringTool
    {
        int _index;

        public stringTool()
        {
            _index = 0;
        }

        public static string charAry2str(char[] cAry)
        {
            string[] sAry = new string[cAry.Length];
            for (int i = 0; i < cAry.Length; i++)
                sAry[i] = cAry[i].ToString();
            return string.Join("", sAry);
        }

        public static void readToEndSB(StreamReader sr, ref StringBuilder output)
        {
            while (!(sr.EndOfStream))
            {
                string line = sr.ReadLine();
                output.AppendLine(line);
            }
        }

        public static int stringCount(string a, string mark, ref List<int> posList)
        {
            int count = 0;
            int i = 0;
            while (true)
            {
                i = a.IndexOf(mark, i);
                if (i != -1)
                {
                    count++;
                    posList.Add(i);
                }
                else
                    break;
                i++;
            }
            return count;
        }

        public bool nextSubstring(string a, string mark, ref string output)
        {
            if (_index == -1)
                return false;
            int nextIndex = a.IndexOf(mark, _index);
            if (nextIndex != -1)
            {
                output = a.Substring(_index, nextIndex - _index);
                _index = nextIndex + mark.Length;
            }
            else
            {
                output = a.Substring(_index);
                _index = -1;
            }
            return true;
        }
    }

    //T can be all kinds of numbers: int, double, float, etc
    class randomToolList<T>
    {
        public static List<T> randomShuffle(List<T> list)
        {
            Random rand = new Random();
            SortedDictionary<int, T> sortMap = new SortedDictionary<int, T>();
            foreach (T im in list)
            {
                int rdInt = rand.Next();
                while (sortMap.ContainsKey(rdInt))
                    rdInt = rand.Next();
                sortMap.Add(rdInt, im);
            }
            List<T> newList = new List<T>();
            foreach (KeyValuePair<int, T> im in sortMap)
            {
                newList.Add(im.Value);

            }
            return newList;
        }

        public static List<int> getShuffledIndexList(int n)
        {
            int[] dAry = new int[n];
            List<int> ri = new List<int>(dAry);
            for (int j = 0; j < n; j++)
                ri[j] = j;

            ri = randomToolList<int>.randomShuffle(ri);
            return ri;
        }

        public static List<int> getSortedIndexList(int n)
        {
            int[] dAry = new int[n];
            List<int> ri = new List<int>(dAry);
            for (int j = 0; j < n; j++)
                ri[j] = j;

            //ri = randomTool<int>.randomShuffle(ri);
            return ri;
        }
    }

    class randomTool
    {
        //this is for getOneRandom(), because rand.Next() should be from a non-new rand
        protected static Random _rand = new Random();

        //random double list between -1, 1
        public static List<double> getRandomList(int n)
        {
            List<double> list = new List<double>();
            Random rand = new Random();
            for (int i = 0; i < n; i++)
            {
                //a double between 0 and 1
                double val = rand.NextDouble();
                int sign = rand.Next(-10000, 10000);
                if (sign < 0)
                    val *= -1;
                list.Add(val);
            }
            return list;
        }

        //get random float list between -1*scale, 1*scale
        public static List<float> getRandomList_float(int n)
        {
            List<float> list = new List<float>();
            Random rand = new Random();
            for (int i = 0; i < n; i++)
            {
                //a double between 0 and 1
                double val = rand.NextDouble();
                int sign = rand.Next(-10000, 10000);
                if (sign < 0)
                    val *= -1;
                list.Add((float)val * Global.randScale);
            }
            return list;
        }

        //a random double between -1 & 1
        public static double getOneRandom_double()
        {
            //a double between 0 and 1
            double val = _rand.NextDouble();
            int sign = _rand.Next(-10000, 10000);
            if (sign < 0)
                val *= -1;
            return val;
        }

        //a random non-negative int
        public static int getOneRandom_int()
        {
            return _rand.Next();
        }

        //a random int between min & max
        public static int getOneRandom_int(int min, int max) //a value >=min, <max
        {
            return _rand.Next(min, max);//a value >=min, <max
        }
    }

}
