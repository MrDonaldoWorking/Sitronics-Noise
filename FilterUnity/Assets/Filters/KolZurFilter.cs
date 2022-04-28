using System;
using System.Collections;

using UnityEngine;

public class KolZurFilter
{
    private ArrayList polyCoef;
    private readonly int K = 10;
    private readonly int WAIT;
    private readonly int CONSID_ELEMS;

    public KolZurFilter(int WAIT)
    {
        this.WAIT = WAIT;
        CONSID_ELEMS = 2 * WAIT + 1;
        CalcPolynomialCoef(CONSID_ELEMS);
    }

    // polynomial coefficient for (1 + x + x^2 + ... + x^(m-1))^k
    private void CalcPolynomialCoef(int m)
    {
        polyCoef = new ArrayList { new int[1] };

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
    public float[] Filter(ref ArrayList vals, int k, int len)
    {
        // vals.Count = CONSID_ELEMS = 2 * WAIT + 1
        int t = WAIT;
        int m = CONSID_ELEMS;
        float[] result = new float[len];
        // string calcstr = "";
        for (int q = 0; q < len; ++q)
        {
            for (int s = k * -WAIT; s <= k * WAIT; ++s)
            {
                float[] val = vals[Math.Min(vals.Count - 1, Math.Max(0, t + s))] as float[];
                int[] coef = polyCoef[k] as int[];
                float dividend = val[q] * (float)coef[s + k * WAIT];
                result[q] += (float)(dividend / Math.Pow(m, k));
            }
        }

        return result;
    }
}
