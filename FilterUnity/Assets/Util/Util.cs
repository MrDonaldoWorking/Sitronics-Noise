using System;
using System.Collections;

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
}
