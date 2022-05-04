using System;
using System.Collections;
using UnityEngine;

public class KernelFilter
{
    private static float[] ApplyKernel(ref ArrayList vals, ref float[] kernel, int len)
    {
        float[] result = new float[len];
        for (int q = 0; q < len; ++q)
        {
            float val = 0;
            for (int i = 0; i < vals.Count; ++i)
            {
                float[] valArr = vals[i] as float[];
                val += valArr[q] * kernel[i];
            }
            result[q] = val;
        }
        return result;
    }

    // b - kernel width, len - Vector3 or Quaternion
    public static float[] Gaussian(ref ArrayList vals, ref ArrayList time, float b, int len)
    {
        Debug.Log($"vals: {Util.ObjectArrsToString(ref vals, len)}, time: {Util.ObjectFloatsToString(ref time)}, b: {b}, len: {len}");
        float[] kernel = new float[vals.Count];
        Util.GaussianKernel(ref time, b, ref kernel);
        return ApplyKernel(ref vals, ref kernel, len);
    }
}
