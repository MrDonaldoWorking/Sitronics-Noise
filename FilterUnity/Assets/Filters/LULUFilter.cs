using System;
using System.Collections;

public class LULUFilter
{
    private ArrayList Process(ref ArrayList vals, int n, int len)
    {
        ArrayList seqs = new ArrayList();
        for (int q = 0; q < len; ++q)
        {
            float[] arr = new float[vals.Count];
            for (int i = 0; i < vals.Count; ++i)
            {
                float[] currVals = vals[i] as float[];
                arr[i] = currVals[q];
            }

            // TODO
            SparseTable st = new SparseTableMin(ref arr);
            float[] mins = new float[vals.Count - n];
            for (int i = 0; i + n < vals.Count; ++i)
            {
                mins[i] = st.Get(i, i + n);
            }
            // Debug.Log("mins: " + string.Join(", ", mins));
            seqs.Add(mins);
        }

        ArrayList arrL = new ArrayList();
        for (int q = 0; q < len; ++q)
        {
            float[] mins = seqs[q] as float[];
            // TODO
            SparseTableMax st = new SparseTableMax(ref mins);
            float[] curL = new float[mins.Length - n];
            for (int i = 0; i + n < mins.Length; ++i)
            {
                curL[i] = st.Get(i, i + n);
            }
            // Debug.Log("curL: " + string.Join(", ", curL));
            arrL.Add(curL);
        }

        return arrL;

    }

    // https://en.wikipedia.org/wiki/Lulu_smoothing
    // L operator of LULU filter
    // Returns ArrayList of float[]
    public static ArrayList L(ref ArrayList vals, int n, int len)
    {
        // string valstr = "";
        // foreach (Vector3 vec in vals)
        // {
        //     valstr += vec + " ";
        // }
        // Debug.Log("L vals = " + valstr);
        ArrayList seqs = new ArrayList();
        for (int q = 0; q < len; ++q)
        {
            float[] arr = new float[vals.Count];
            for (int i = 0; i < vals.Count; ++i)
            {
                float[] currVals = vals[i] as float[];
                arr[i] = currVals[q];
            }

            SparseTableMin st = new SparseTableMin(ref arr);
            float[] mins = new float[vals.Count - n];
            for (int i = 0; i + n < vals.Count; ++i)
            {
                mins[i] = st.Get(i, i + n);
            }
            // Debug.Log("mins: " + string.Join(", ", mins));
            seqs.Add(mins);
        }

        ArrayList arrL = new ArrayList();
        for (int q = 0; q < len; ++q)
        {
            float[] mins = seqs[q] as float[];
            SparseTableMax st = new SparseTableMax(ref mins);
            float[] curL = new float[mins.Length - n];
            for (int i = 0; i + n < mins.Length; ++i)
            {
                curL[i] = st.Get(i, i + n);
            }
            // Debug.Log("curL: " + string.Join(", ", curL));
            arrL.Add(curL);
        }

        return arrL;
    }

    // U operator of LULU filter
    public static ArrayList U(ref ArrayList vals, int n, int len)
    {
        // string valstr = "";
        // foreach (Vector3 vec in vals)
        // {
        //     valstr += vec + " ";
        // }
        // Debug.Log("U vals = " + valstr);
        ArrayList seqs = new ArrayList();
        for (int q = 0; q < len; ++q)
        {
            float[] arr = new float[vals.Count];
            for (int i = 0; i < vals.Count; ++i)
            {
                float[] currVals = vals[i] as float[];
                arr[i] = currVals[q];
            }

            SparseTable st = new SparseTableMax(ref arr);
            float[] maxs = new float[vals.Count - n];
            for (int i = 0; i + n < vals.Count; ++i)
            {
                maxs[i] = st.Get(i, i + n);
            }
            // Debug.Log("maxs: " + string.Join(", ", maxs));
            seqs.Add(maxs);
        }

        ArrayList arrU = new ArrayList();
        for (int q = 0; q < len; ++q)
        {
            float[] maxs = seqs[q] as float[];
            SparseTable st = new SparseTableMin(ref maxs);
            float[] curU = new float[maxs.Length - n];
            for (int i = 0; i + n < maxs.Length; ++i)
            {
                curU[i] = st.Get(i, i + n);
            }
            // Debug.Log("curU: " + string.Join(", ", curU));
            arrU.Add(curU);
        }

        return arrU;
    }
}
