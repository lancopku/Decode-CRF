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
    class structReg
    {
        public static dataSet structSplit(dataSet X)
        {
            //make fractions
            dataSet X2 = new dataSet(X.NTag, X.NFeature);

            for (int t = 0; t < X.Count; t++)
            {
                dataSeq x = X[t];

                if (Global.structReg && Global.miniSize != 0)
                {
                    /*int step = getStep();
                    //if (x.Count > 4 * step)
                    if (x.Count > 4 * step && Global.segStep.ToString().Contains(".") == false)//divide x to 2 segments, then do fine segments
                    {
                        int rand = randomTool.getOneRandom_int(step, x.Count - step);
                        dataSeq x1 = new dataSeq(x, 0, rand);
                        dataSeq x2 = new dataSeq(x, rand, x.Count);
                        getSegments(x1, X2);
                        getSegments(x2, X2);
                    }
                    else*/
                    getSegments(x, X2);
                }
                else
                    X2.Add(x);
            }

            return X2;
        }

        static int getStep()
        {
            int step;
            if (Global.miniSize.ToString().Contains("."))
            {
                int min = (int)Global.miniSize;
                double div = (Global.miniSize * 1000) % 1000;//a number between 0 & 1000
                int rand = randomTool.getOneRandom_int(0, 1000);
                if (rand < div)
                    step = min + Global.stepGap;
                else
                    step = min;
            }
            else
            {
                step = (int)Global.miniSize;
            }
            return step;
        }

        static void getSegments(dataSeq x, dataSet X2)
        {
            int rand = randomTool.getOneRandom_int(-100, 100);
            if (rand <= 0)//forward
            {
                for (int node = 0; node < x.Count; )
                {
                    int step = getStep();
                    dataSeq x2 = new dataSeq(x, node, step + Global.overlapLength);
                    X2.Add(x2);
                    node += step;
                }
            }
            else//backward
            {
                for (int node = x.Count - 1; node >= 0; )
                {
                    int step = getStep();
                    dataSeq x2 = new dataSeq(x, node, step + Global.overlapLength, false);
                    X2.Add(x2);
                    node -= step;
                }
            }
        }


    }
}
