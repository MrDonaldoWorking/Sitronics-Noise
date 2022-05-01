using System;
using System.Collections;

public class KernelFilter
{
    // b - kernel width, len - Vector3 or Quaternion
    public static float[] GaussianKernelSmoothing(ref ArrayList vals, ref ArrayList time, float b, int len)
    {
        float[] kernel = new float[vals.Count];
        Util.GaussianKernel(ref time, b, ref kernel);
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
}
