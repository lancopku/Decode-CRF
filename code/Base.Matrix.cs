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
    class sMatrix
    {
        string[,] _biAry;
        int _r, _c;

        public sMatrix(string a, bool old = false)
        {
            if (old)
                readOldFormat(a);
            else
                read(a);
        }

        public sMatrix(dMatrix m, string fn = "e2")
        {
            _r = m.R;
            _c = m.C;
            _biAry = new string[_r, _c];
            for (int r = 0; r < _r; r++)
            {
                for (int c = 0; c < _c; c++)
                {
                    double tmp = Math.Abs(m[r, c]);
                    if ((tmp < 1e5 && tmp > 0.1) || tmp == 0)
                        _biAry[r, c] = m[r, c].ToString(fn);
                    else
                        _biAry[r, c] = m[r, c].ToString("e2");
                }
            }
        }

        public void read(string a)
        {
            dMatrix m = new dMatrix(a);
            _r = m.R;
            _c = m.C;
            _biAry = new string[_r, _c];
            for (int r = 0; r < _r; r++)
            {
                for (int c = 0; c < _c; c++)
                {
                    _biAry[r, c] = m[r, c].ToString();
                }
            }
        }

        public void readOldFormat(string a)
        {
            a = a.Replace("\r", "");
            string[] ary = a.Split(Global.lineEndAry, StringSplitOptions.RemoveEmptyEntries);
            string info = ary[0];
            string[] infoAry = info.Split(Global.commaAry, StringSplitOptions.RemoveEmptyEntries);

            //get width and height
            _r = int.Parse(infoAry[2]);
            _c = int.Parse(infoAry[1]);

            _biAry = new string[_r, _c];

            for (int row = 0; row < _r; row++)
            {
                string rowStr = ary[row + 1];
                string[] rowStrAry = rowStr.Split(Global.commaAry, StringSplitOptions.RemoveEmptyEntries);

                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] = rowStrAry[col];
                }
            }
        }

        //write into matrix format
        public void write(StreamWriter sw)
        {
            for (int r = 0; r < _r; r++)
            {
                for (int c = 0; c < _c; c++)
                {
                    sw.Write(this[r, c] + ",");
                }
                sw.WriteLine();
            }
            sw.Flush();
        }

        public void add(sMatrix m)
        {
            if (_r != m.R || _c != m.C)
                throw new Exception("error");

            for (int r = 0; r < _r; r++)
            {
                for (int c = 0; c < _c; c++)
                {
                    this[r, c] += m[r, c];
                }
            }
        }

        public void add(string a)
        {
            for (int r = 0; r < _r; r++)
            {
                for (int c = 0; c < _c; c++)
                {
                    this[r, c] += a;
                }
            }
        }

        public string this[int r, int c]
        {
            get
            {
                return _biAry[r, c];
            }
            set
            {
                _biAry[r, c] = value;
            }
        }

        public int C
        {
            get{return _c;}
        }

        public int R
        {
            get{return _r;}
        }

    }

    class dMatrix
    {
        double[,] _biAry;
        int _r, _c;

        public dMatrix()
        {
            _biAry = null;
            _r = 0;
            _c = 0;
        }

        public dMatrix(int r, int c)
        {
            _biAry = new double[r, c];
            this._r = r;
            this._c = c;
        }

        public dMatrix(dMatrix m)
        {
            _c = m._c;
            _r = m._r;
            _biAry = new double[_r, _c];
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] = m[row, col];
                }
            }
        }

        //neglect % started lines
        public dMatrix(string a)
        {
            List<List<double>> listList = new List<List<double>>();
            string[] lines = a.Split(Global.lineEndAry, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (!line.StartsWith("%"))
                {
                    List<double> dList = listTool.getDoubleList(line);
                    listList.Add(dList);
                }
            }

            //init
            _r = listList.Count;
            _c = listList[0].Count;
            _biAry = new double[_r, _c];
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] = listList[row][col];
                }
            }
        }

        public dMatrix(List<List<double>> listList, bool trans = false)
        {
            if (!trans)
            {
                _r = listList.Count;
                _c = listList[0].Count;
                _biAry = new double[_r, _c];
                for (int row = 0; row < _r; row++)
                {
                    for (int col = 0; col < _c; col++)
                    {
                        _biAry[row, col] = listList[row][col];
                    }
                }
            }
            else//transpose
            {
                _c = listList.Count;
                _r = listList[0].Count;
                _biAry = new double[_r, _c];
                for (int row = 0; row < _r; row++)
                {
                    for (int col = 0; col < _c; col++)
                    {
                        _biAry[row, col] = listList[col][row];
                    }
                }
            }
        }

        public void write(StreamWriter sw, string fn = "e2")
        {
            for (int r = 0; r < _r; r++)
            {
                for (int c = 0; c < _c; c++)
                {
                    double tmp = Math.Abs(_biAry[r, c]);
                    if ((tmp < 1e5 && tmp > 0.1) || tmp == 0)
                        sw.Write(_biAry[r, c].ToString(fn) + ",");
                    else
                        sw.Write(_biAry[r, c].ToString("e2") + ",");
                }
                sw.WriteLine();
            }
            sw.Flush();
        }

        public void set(double a)
        {
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] = a;
                }
            }
        }

        public void set(dMatrix m)
        {
            if (_r != m._r || _c != m._c)
                throw new Exception("error");

            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] = m[row, col];
                }
            }
        }

        public int C
        {
            get { return _c; }
        }

        public int R
        {
            get { return _r; }
        }

        public double this[int r, int c]
        {
            get
            {
                return _biAry[r, c];
            }
            set
            {
                _biAry[r, c] = value;
            }
        }

        public void add(dMatrix m)
        {
            if (_r != m._r || _c != m._c)
                throw new Exception("err234");
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] += m[row, col];
                }
            }
        }

        public void negate()
        {
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] = -_biAry[row, col];
                }
            }
        }

        // this = m1 * m2; (right multiply)
        public void multiply(dMatrix m1, dMatrix m2)
        {
            if (m2._r != m1._c)
                throw new Exception("err234");

            _biAry = new double[m1._r, m2._c];

            int m1r, i, m2c;
            for (m1r = 0; m1r < m1._r; m1r++)
            {
                for (m2c = 0; m2c < m2._c; m2c++)
                {
                    double value = 0;
                    for (i = 0; i < m1._c; i++)
                    {
                        value += m1[m1r, i] * m2[i, m2c];
                    }
                    _biAry[m1r,m2c] = value;
                }
            }
        }

        //this = this * value;
        public void multiply(double a)
        {
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] *= a;
                }
            }
        }

        public void divide(double a)
        {
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] /= a;
                }
            }
        }

        public void add(double a)
        {
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] += a;
                }
            }
        }

        //this = exp(this);
        public void eltExp()
        {
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] = Math.Exp(_biAry[row, col]);
                }
            }
        }

        //this = log(this);
        public void eltLog()
        {
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    _biAry[row, col] = Math.Log(_biAry[row, col]);//natural log
                }
            }
        }

        //sum(this);
        public double sum()
        {
            double sum = 0;
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    sum += _biAry[row, col];
                }
            }
            return sum;
        }

        public void transpose()
        {
            if (_c == 0 || _r == 0)
            {
                return;
            }

            dMatrix tmp = new dMatrix(this);
            for (int col = 0; col < _c; col++)
            {
                for (int row = 0; row < _r; row++)
                {
                    _biAry[col, row] = tmp[row, col];
                }
            }
        }

        public double rowSum(int row)
        {
            double rowsum = 0;
            int col;
            for (col = 0; col < _c; col++)
            {
                rowsum += _biAry[row, col];
            }
            return rowsum;
        }

        public double colSum(int col)
        {
            double colsum = 0;
            int row;
            for (row = 0; row < _r; row++)
            {
                colsum += _biAry[row, col];
            }
            return colsum;
        }

        public double getMaxValue()
        {
            double max = double.MinValue;
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                {
                    double now = _biAry[row, col];
                    if (max < now)
                        max = now;
                }
            }
            return max;
        }

        public void display()
        {
            for (int row = 0; row < _r; row++)
            {
                for (int col = 0; col < _c; col++)
                    Console.Write(_biAry[row, col].ToString("e2") + ", ");
                Console.WriteLine();
            }
        }

    }

}