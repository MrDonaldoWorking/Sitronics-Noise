using System;
using System.Collections;

public class WeightedMedianFilter
{
    // each element of ArrayList is Vector3 with floats
    // TODO
    public static float[] Median(ref ArrayList vals, int len)
    {
        float[][] coords = new float[len][];
        for (int q = 0; q < len; ++q)
        {
            float[] curCoord = new float[vals.Count];
            for (int i = 0; i < vals.Count; ++i)
            {
                float[] val = vals[i] as float[];
                curCoord[i] = val[q];
            }
            Array.Sort(curCoord);
            coords[q] = curCoord;
        }

        float[] res = new float[len];
        for (int i = 0; i < len; ++i)
        {
            res[i] = coords[i][vals.Count / 2];
        }
        return res;
    }
}
