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
    class optimizer
    {
        protected model _model;
        protected dataSet _X;
        protected inference _inf;
        protected featureGenerator _fGene;

        //for convergence test
        protected Queue<double> _preVals_loss = new Queue<double>();
        protected Queue<double> _preVals_score = new Queue<double>();

        virtual public double optimize()
        {
            throw new Exception("error");
        }

        public double convergeTest_loss(double err)
        {
            double val = 1e100;
            if (_preVals_loss.Count > 1)
            {
                double prevVal = _preVals_loss.Peek();
                if (_preVals_loss.Count == 10)
                {
                    double trash = _preVals_loss.Dequeue();
                }
                double averageImprovement = (prevVal - err) / _preVals_loss.Count;
                double relAvgImpr = averageImprovement / Math.Abs(err);
                val = relAvgImpr;
            }
            _preVals_loss.Enqueue(err);
            return val;
        }

        public double convergeTest_score(double err)
        {
            double val = 1e100;
            if (_preVals_score.Count > 1)
            {
                double prevVal = _preVals_score.Peek();
                if (_preVals_score.Count == 10)
                {
                    double trash = _preVals_score.Dequeue();
                }
                double averageImprovement = (prevVal - err) / _preVals_score.Count;
                double relAvgImpr = averageImprovement / Math.Abs(err);
                val = relAvgImpr;
            }
            _preVals_score.Enqueue(err);
            return val;
        }
    }
}