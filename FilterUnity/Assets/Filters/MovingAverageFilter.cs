using System;
using System.Collections;
using UnityEngine;

public class MovingAverageFilter
{
    public static float[] Filter(ref ArrayList vals, int len)
    {
        if (len == 3)
        {
            // for (int q = 0; q < len; ++q)
            // {
            //     float[] axis = new float[vals.Count];
            //     for (int i = 0; i < vals.Count; ++i)
            //     {
            //         float[] curr = vals[i] as float[];
            //         axis[i] = curr[q];
            //     }
            //     Debug.Log($"Vector[{q}]: size={axis.Length} \n{string.Join(", ", axis)}");
            // }
        }
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
        Debug.Log($"res: {string.Join(", ", res)}");

        return res;
    }
}
