using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;

public class Filter
{
    private int WAIT = 5;
    private int CONSID_ELEMS;
    private int QUAT_N = 4;
    private int VEC3_N = 3;
    private int K = 10;
    private int MAX_DEGREE = 360;

    private int cnt = 0;

    private ArrayList vectorList;
    private ArrayList vectorTime;
    private Vector3 initPos;

    private ArrayList quatList;
    private ArrayList quatTime;
    private ArrayList eulAnList;
    private Quaternion initRot;

    private ArrayList polyCoef;

    private SingleExp seFilter;

    public void Init(Vector3 position, Quaternion rotation)
    {
        initPos = position;
        initRot = rotation;

        CONSID_ELEMS = WAIT * 2 + 1;

        vectorList = new ArrayList();
        vectorTime = new ArrayList();

        quatList = new ArrayList();
        quatTime = new ArrayList();
        eulAnList = new ArrayList();

        CalcPolynomialCoef(CONSID_ELEMS);

        seFilter = new SingleExp(initRot.eulerAngles, 0.7f);
    }

    // polynomial coefficient for (1 + x + x^2 + ... + x^(m-1))^k
    private void CalcPolynomialCoef(int m)
    {
        polyCoef = new ArrayList();
        polyCoef.Add(new int[1]);

        int[] firstPow = new int[m];
        for (int i = 0; i < m; ++i)
        {
            firstPow[i] = 1;
        }
        polyCoef.Add(firstPow);
        Debug.Log("polyCoef[1]: " + string.Join(", ", firstPow));

        for (int k = 0; k < K; ++k)
        {
            int[] prev = polyCoef[polyCoef.Count - 1] as int[];
            int[] next = new int[prev.Length + m - 1];
            for (int i = 0; i < prev.Length; ++i)
            {
                for (int j = i; j < i + m; ++j)
                {
                    next[j] += prev[i];
                }
            }
            polyCoef.Add(next);
            Debug.Log("polyCoef[" + (k + 2) + "]: " + string.Join(", ", next));
        }
    }

    private readonly struct SparseTableMin
    {
        private readonly ArrayList table;

        public SparseTableMin(float[] arr)
        {
            int n = arr.Length;
            int h = (int)Math.Log(n, 2);

            table = new ArrayList();
            table.Add(arr);

            for (int i = 1; i <= h; ++i)
            {
                float[] prev = table[table.Count - 1] as float[];
                float[] next = new float[prev.Length - (1 << i - 1)];
                for (int j = 0; j < next.Length; ++j)
                {
                    next[j] = Math.Min(prev[j], prev[j + (1 << (i - 1))]);
                }
                table.Add(next);
            }
        }

        public float Get(int left, int right)
        {
            int lg = (int)Math.Log(right - left, 2);
            int len = 1 << lg;
            float[] arr = table[lg] as float[];
            return Math.Min(arr[left], arr[right - len]);
        }
    }

    private readonly struct SparseTableMax
    {
        private readonly ArrayList table;

        public SparseTableMax(float[] arr)
        {
            int n = arr.Length;
            int h = (int)Math.Log(n, 2);

            table = new ArrayList();
            table.Add(arr);

            for (int i = 1; i <= h; ++i)
            {
                float[] prev = table[table.Count - 1] as float[];
                float[] next = new float[prev.Length - (1 << i - 1)];
                for (int j = 0; j < next.Length; ++j)
                {
                    next[j] = Math.Max(prev[j], prev[j + (1 << (i - 1))]);
                }
                table.Add(next);
            }
        }

        public float Get(int left, int right)
        {
            int lg = (int)Math.Log(right - left, 2);
            int len = 1 << lg;
            float[] arr = table[lg] as float[];
            return Math.Max(arr[left], arr[right - len]);
        }
    }

    private ArrayList ToVec3ArrayList(ArrayList arr)
    {
        ArrayList result = new ArrayList();
        int len = (arr[0] as float[]).Length;
        // string strvec = "";
        for (int i = 0; i < len; ++i)
        {
            Vector3 vec = new Vector3(0f, 0f, 0f);
            for (int q = 0; q < VEC3_N; ++q)
            {
                float[] cur = arr[q] as float[];
                vec[q] = cur[i];
            }
            // strvec += vec + " ";
            result.Add(vec);
        }
        // Debug.Log("res: " + strvec);

        return result;
    }

    // https://en.wikipedia.org/wiki/Lulu_smoothing
    // L operator of LULU filter
    private ArrayList L(ArrayList vals, int n)
    {
        // string valstr = "";
        // foreach (Vector3 vec in vals)
        // {
        //     valstr += vec + " ";
        // }
        // Debug.Log("L vals = " + valstr);
        ArrayList seqs = new ArrayList();
        for (int q = 0; q < VEC3_N; ++q)
        {
            float[] arr = new float[vals.Count];
            for (int i = 0; i < vals.Count; ++i)
            {
                Vector3 curVec = (Vector3)vals[i];
                arr[i] = curVec[q];
            }

            SparseTableMin st = new SparseTableMin(arr);
            float[] mins = new float[vals.Count - n];
            for (int i = 0; i + n < vals.Count; ++i)
            {
                mins[i] = st.Get(i, i + n);
            }
            // Debug.Log("mins: " + string.Join(", ", mins));
            seqs.Add(mins);
        }

        ArrayList arrL = new ArrayList();
        for (int q = 0; q < VEC3_N; ++q)
        {
            float[] mins = seqs[q] as float[];
            SparseTableMax st = new SparseTableMax(mins);
            float[] curL = new float[mins.Length - n];
            for (int i = 0; i + n < mins.Length; ++i)
            {
                curL[i] = st.Get(i, i + n);
            }
            // Debug.Log("curL: " + string.Join(", ", curL));
            arrL.Add(curL);
        }

        return ToVec3ArrayList(arrL);
    }

    // U operator of LULU filter
    private ArrayList U(ArrayList vals, int n)
    {
        // string valstr = "";
        // foreach (Vector3 vec in vals)
        // {
        //     valstr += vec + " ";
        // }
        // Debug.Log("U vals = " + valstr);
        ArrayList seqs = new ArrayList();
        for (int q = 0; q < VEC3_N; ++q)
        {
            float[] arr = new float[vals.Count];
            for (int i = 0; i < vals.Count; ++i)
            {
                Vector3 curVec = (Vector3)vals[i];
                arr[i] = curVec[q];
            }

            SparseTableMax st = new SparseTableMax(arr);
            float[] maxs = new float[vals.Count - n];
            for (int i = 0; i + n < vals.Count; ++i)
            {
                maxs[i] = st.Get(i, i + n);
            }
            // Debug.Log("maxs: " + string.Join(", ", maxs));
            seqs.Add(maxs);
        }

        ArrayList arrU = new ArrayList();
        for (int q = 0; q < VEC3_N; ++q)
        {
            float[] maxs = seqs[q] as float[];
            SparseTableMin st = new SparseTableMin(maxs);
            float[] curU = new float[maxs.Length - n];
            for (int i = 0; i + n < maxs.Length; ++i)
            {
                curU[i] = st.Get(i, i + n);
            }
            // Debug.Log("curU: " + string.Join(", ", curU));
            arrU.Add(curU);
        }

        return ToVec3ArrayList(arrU);
    }

    // https://en.wikipedia.org/wiki/Exponential_smoothing
    private struct SingleExp
    {
        private Vector3 s;
        private readonly float alpha;

        public SingleExp(Vector3 init, float a)
        {
            s = init;
            alpha = a;
        }

        public Vector3 GetNext(Vector3 measure)
        {
            s = alpha * measure + (1 - alpha) * s;
            return s;
        }
    }

    private struct DoubleExp
    {
        private Vector3 s;
        private readonly float alpha;
        private Vector3 b;
        private readonly float beta;
        private bool firstTime;

        public DoubleExp(Vector3 init, float a, float b)
        {
            s = init;
            this.b = init;
            alpha = a;
            beta = b;
            firstTime = true;
        }

        public Vector3 GetNext(Vector3 measure)
        {
            if (firstTime)
            {
                firstTime = false;
                // b0 = x1 - x0
                b = measure - s;
            }

            Vector3 prevS = s;
            Vector3 prevB = b;
            s = alpha * measure + (1 - alpha) * (prevS + prevB);
            b = beta * (s - prevS) + (1 - beta) * prevB;

            return s;
        }
    }

    // ArrayList of Vector3
    // m = CONSID_ELEMS, - window size
    // k - iterations count
    // https://wires.onlinelibrary.wiley.com/doi/pdf/10.1002/wics.71
    private Vector3 KolZur(ArrayList vals, int k)
    {
        // vals.Count = CONSID_ELEMS = 2 * WAIT + 1
        int t = WAIT;
        int m = CONSID_ELEMS;
        Vector3 result = new Vector3(0f, 0f, 0f);
        // string calcstr = "";
        for (int q = 0; q < VEC3_N; ++q)
        {
            for (int s = k * -WAIT; s < k * WAIT; ++s)
            {
                Vector3 val = (Vector3)vals[Math.Min(vals.Count - 1, Math.Max(0, t + s))];
                int[] coef = polyCoef[k] as int[];
                float dividend = val[q] * (float)coef[s + k * WAIT];
                // if (q == 0)
                // {
                //     calcstr += val[q] + " * " + coef[s + k * WAIT] + " / " + Math.Pow(m, k) + "; ";
                // }
                result[q] += (float)(dividend / Math.Pow(m, k));
            }
        }

        // string v3str = "vals = ";
        // foreach (Vector3 val in vals)
        // {
        //     v3str += val.ToString();
        //     v3str += " ";
        // }

        // Debug.Log(v3str);
        // Debug.Log("was = " + vals[t]);
        // Debug.Log("res = " + result);
        // Debug.Log("calc: " + calcstr);

        return result;
    }

    // each element of ArrayList is Vector3 with floats
    private Vector3 Median(ArrayList vals)
    {
        float[][] coords = new float[VEC3_N][];
        for (int q = 0; q < VEC3_N; ++q)
        {
            float[] curCoord = new float[vals.Count];
            for (int i = 0; i < vals.Count; ++i)
            {
                Vector3 val = (Vector3)vals[i];
                curCoord[i] = val[q];
            }
            Array.Sort(curCoord);
            coords[q] = curCoord;
        }

        return new Vector3(coords[0][vals.Count / 2], coords[1][vals.Count / 2], coords[2][vals.Count / 2]);
    }

    public Vector3 FilterPosition(float time, Vector3 position, bool positionChanged)
    {
        vectorList.Add(position);
        vectorTime.Add(time);
        if (vectorList.Count < CONSID_ELEMS)
        {
            return initPos;
        }

        ArrayList posWindow = new ArrayList();
        for (int i = vectorList.Count - CONSID_ELEMS; i < vectorList.Count; ++i)
        {
            posWindow.Add(vectorList[i]);
        }

        Vector3 filtered = KolZur(posWindow, 3);

        return filtered;
    }

    private void Log2File(Vector3 bfr, Vector3 aft)
    {
        for (int q = 0; q < VEC3_N; ++q)
        {
            using (StreamWriter before = new StreamWriter("before" + q, true))
            {
                before.Write(bfr[q] + " ");
            }

            using (StreamWriter after = new StreamWriter("after" + q, true))
            {
                after.Write(aft[q] + " ");
            }
        }
    }

    public Quaternion FilterRotation(float time, Quaternion rotation, bool rotationChanged)
    {
        quatList.Add(rotation);
        eulAnList.Add(rotation.eulerAngles);
        quatTime.Add(time);
        if (quatList.Count < CONSID_ELEMS)
        {
            return initRot;
        }

        ArrayList fixedWindow = new ArrayList();
        fixedWindow.Add(eulAnList[eulAnList.Count - CONSID_ELEMS]);
        for (int i = eulAnList.Count - CONSID_ELEMS + 1; i < eulAnList.Count; ++i)
        {
            Vector3 prev = (Vector3)fixedWindow[fixedWindow.Count - 1];
            Vector3 linkToNext = (Vector3)eulAnList[i];
            Vector3 next = new Vector3(linkToNext.x, linkToNext.y, linkToNext.z);
            for (int q = 0; q < VEC3_N; ++q)
            {
                // assume that in 1 sec object can rotate 10 degree
                float diff = Math.Abs(next[q] - prev[q]);
                // float degree = ((float) quatTime[i] - (float) quatTime[i - 1]) * DEGREE_PER_SEC;
                float degree = 150;
                if (degree > 0 && diff > degree)
                {
                    if (cnt < 1500)
                    {
                        ++cnt;
                        // Debug.Log("N" + cnt + ") prevTime = " + quatTime[i - 1] + ", nextTime = " + quatTime[i]);
                        // Debug.Log("N" + cnt + ") diff = " + diff + ", degree = " + degree + ", prev = " + prev + ", next = " + next + ", q = " + q);
                    }
                    // 0 -> 360
                    if (prev[q] < next[q])
                    {
                        next[q] -= MAX_DEGREE;
                    }
                    // 360 -> 0
                    else
                    {
                        next[q] += MAX_DEGREE;
                    }

                    if (cnt < 15)
                    {
                        // Debug.Log("N" + cnt + ") next = " + next);
                    }
                }
            }
            fixedWindow.Add(next);
        }

        Vector3 filtered = seFilter.GetNext((Vector3)fixedWindow[WAIT]);
        Debug.Log("before: " + (Vector3)fixedWindow[WAIT] + ", after: " + filtered);
        Log2File((Vector3)fixedWindow[WAIT], filtered);
        for (int q = 0; q < VEC3_N; ++q)
        {
            while (filtered[q] < 0)
            {
                filtered[q] += MAX_DEGREE;
            }
            while (filtered[q] > MAX_DEGREE)
            {
                filtered[q] -= MAX_DEGREE;
            }
        }

        // return medianRotation;
        return Quaternion.Euler(filtered.x, filtered.y, filtered.z);
    }
}
