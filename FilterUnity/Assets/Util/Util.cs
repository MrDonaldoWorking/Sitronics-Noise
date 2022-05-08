using System;
using System.Collections;
using System.IO;
using UnityEngine;
using System.Text;

public class Util
{
    // Calculates Gaussian Kernel assuming max value is in middle
    public static void GaussianKernel(ref ArrayList time, float b, ref float[] kernel)
    {
        float midTime = (float)time[time.Count / 2];
        float sum = 0;
        for (int i = 0; i < time.Count; ++i)
        {
            kernel[i] = (float)Math.Exp(-(Math.Pow((float)time[i] - midTime, 2)) / (2 * b * b));
            sum += kernel[i];
        }
        for (int i = 0; i < time.Count; ++i)
        {
            kernel[i] /= sum;
        }
    }

    // K = D(|x - x*| / b)
    public static void EpanechnikovKernel(ref ArrayList time, float b, ref float[] kernel)
    {
        float midTime = (float)time[time.Count / 2];
        for (int i = 0; i < time.Count; ++i)
        {
            float t = Math.Abs(midTime - (float)time[i]) / b;
            kernel[i] = Math.Abs(t) > 1 ? 0f : (3.0f / 4) * (1 - t * t);
            // Debug.Log($"t = {t}; kernel = {Math.Abs(t)} > 1 ? {0f} : {(3 / 4) * (1 - t * t)} : {Math.Abs(t) > 1}");
        }
    }

    public static void TricubeKernel(ref ArrayList time, float b, ref float[] kernel)
    {
        float midTime = (float)time[time.Count / 2];
        for (int i = 0; i < time.Count; ++i)
        {
            float t = Math.Abs(midTime - (float)time[i]) / b;
            kernel[i] = Math.Abs(t) > 1 ? 0f : (70.0f / 81) * (float)Math.Pow(1 - Math.Abs(t) * t * t, 3);
        }
    }

    // Prints 2 dimensional array
    public static void Print2DArray(float[,] matrix)
    {
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                Console.Write(matrix[i, j].ToString("f7") + "\t");
            }
            Console.WriteLine();
        }
    }

    public static ArrayList ToVec3ArrayList(ref ArrayList arr)
    {
        ArrayList result = new ArrayList();
        int len = (arr[0] as float[]).Length;
        // string strvec = "";
        for (int i = 0; i < len; ++i)
        {
            Vector3 vec = new Vector3(0f, 0f, 0f);
            for (int q = 0; q < Filter.VEC3_N; ++q)
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

    public static float[] V3ToArr(Vector3 vector)
    {
        float[] values = new float[Filter.VEC3_N];
        for (int q = 0; q < Filter.VEC3_N; ++q)
        {
            values[q] = vector[q];
        }
        return values;
    }

    public static Vector3 ArrToV3(ref float[] arr)
    {
        return new Vector3(arr[0], arr[1], arr[2]);
    }

    public static void Log2File(Vector3 bfr, Vector3 aft)
    {
        for (int q = 0; q < Filter.VEC3_N; ++q)
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

    public static float[] QuatsToAngle(Quaternion from, Quaternion to)
    {
        float[] angle = { (float)Quaternion.Angle(from, to) };
        return angle;
    }

    public static float[] QuatToArr(Quaternion quat)
    {
        float[] res = new float[Filter.QUAT_N];
        for (int q = 0; q < Filter.QUAT_N; ++q)
        {
            res[q] = quat[q];
        }
        return res;
    }

    public static Quaternion ArrToQuat(ref float[] arr)
    {
        Quaternion quat = new Quaternion(0, 0, 0, 1);
        for (int q = 0; q < Filter.QUAT_N; ++q)
        {
            quat[q] = arr[q];
        }
        return quat;
    }

    public static string ObjectArrToString(object element, int len)
    {
        StringBuilder builder = new StringBuilder("[ ");
        float[] arr = element as float[];
        if (element == null || arr == null)
        {
            builder.Append("null");
        }
        else
        {
            for (int q = 0; q < len; ++q)
            {
                builder.Append(((float)arr[q]).ToString("f5"));
                builder.Append(" ");
            }
        }
        builder.Append("]");
        return builder.ToString();
    }

    public static string ObjectArrsToString(ref ArrayList elements, int len)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < elements.Count; ++i)
        {
            builder.Append(ObjectArrToString(elements[i], len));
            builder.Append(" ");
        }
        return builder.ToString();
    }

    public static string ObjectFloatsToString(ref ArrayList elements)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < elements.Count; ++i)
        {
            builder.Append((float)elements[i]);
            builder.Append(" ");
        }
        return builder.ToString();
    }

    // ref keyword must be applied to variables that can change
    public static float[] ObjectArrsAt(ArrayList elements, int index)
    {
        float[] res = new float[elements.Count];
        for (int i = 0; i < elements.Count; ++i)
        {
            float[] vals = elements[i] as float[];
            res[i] = vals[index];
        }
        return res;
    }
}
