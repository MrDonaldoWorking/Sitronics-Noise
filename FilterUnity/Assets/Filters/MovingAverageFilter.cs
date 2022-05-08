using System;
using System.Collections;

public class MovingAverageFilter
{
    public static float[] Filter(ref ArrayList vals, int len)
    {
        float[] res = new float[len];
        for (int i = 0; i < vals.Count; ++i)
        {
            for (int q = 0; q < len; ++q)
            {
                float[] curr = vals[i] as float[];
                res[q] += curr[q];
            }
        }

        for (int q = 0; q < len; ++q)
        {
            res[q] /= vals.Count;
        }

        return res;
    }
}
