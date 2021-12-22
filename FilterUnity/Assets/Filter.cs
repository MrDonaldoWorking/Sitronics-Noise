using UnityEngine;
using System;
using System.Collections;

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
                Vector3 val = (Vector3) vals[Math.Min(vals.Count - 1, Math.Max(0, t + s))];
                int[] coef = polyCoef[k] as int[];
                float dividend = val[q] * (float) coef[s + k * WAIT];
                // if (q == 0)
                // {
                //     calcstr += val[q] + " * " + coef[s + k * WAIT] + " / " + Math.Pow(m, k) + "; ";
                // }
                result[q] += (float) (dividend / Math.Pow(m, k));
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
                Vector3 val = (Vector3) vals[i];
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

        Vector3 result = KolZur(posWindow, 3);

        return result;
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
            Vector3 prev = (Vector3) fixedWindow[fixedWindow.Count - 1];
            Vector3 linkToNext = (Vector3) eulAnList[i];
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

        Vector3 filtered = KolZur(fixedWindow, 3);
        for (int q = 0; q < VEC3_N; ++q)
        {
            while (filtered[q] < 0)
            {
                filtered[q] += MAX_DEGREE;
            }
            while (filtered[q] > MAX_DEGREE) {
                filtered[q] -= MAX_DEGREE;
            }
        }

        // return medianRotation;
        return Quaternion.Euler(filtered.x, filtered.y, filtered.z);
    }
}
