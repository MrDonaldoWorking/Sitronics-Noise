using System;
using System.Collections;
using System.IO;

// Convolution coefficients are described here: https://agora.cs.wcu.edu/~huffman/figures/sgpaper1964.pdf
// How it generated described here: https://github.com/mathnet/mathnet-filtering/issues/4
public class SavGolFilter
{
    private readonly float[,] coefsMatrix;
    private readonly float[,] s;
    private readonly int minPoints = 2;
    private readonly int maxPoints = 10;
    private readonly int sidePoints;

    public SavGolFilter(int sidePoints, int degree)
    {
        this.sidePoints = sidePoints;
        ArrayList coefficients = new ArrayList();
        using (TextReader reader = File.OpenText("./SavGolC.conf"))
        {
            for (int sPoints = minPoints; sPoints <= maxPoints; ++sPoints)
            {
                string line = reader.ReadLine() ?? throw new ArgumentException("Expected new line");
                string[] datas = line.Split(':');
                int check = int.Parse(datas[1]);

                //Console.WriteLine($"From file: {datas[0]}");

                string[] coefs = datas[0].Split(' ');
                int[] raws = new int[coefs.Length];
                int sum = 0;
                for (int i = 0; i < coefs.Length; ++i)
                {
                    raws[i] = int.Parse(coefs[i]);
                    sum += raws[i];
                }
                if (2 * sum - raws[coefs.Length - 1] != check)
                {
                    throw new FileCorruptedException($"Expected {check}, but " +
                        $"found {2 * sum - raws[coefs.Length - 1]} at " +
                        $"{sPoints} iteration");
                }

                float[] curCoefs = new float[coefs.Length * 2 - 1];
                for (int i = 0; i < coefs.Length; ++i)
                {
                    curCoefs[i] = (float)raws[i] / check;
                }
                for (int i = 0; i < coefs.Length - 1; ++i)
                {
                    curCoefs[i + coefs.Length] = curCoefs[coefs.Length - 2 - i];
                }
                //Console.WriteLine($"converted to: {String.Join(" ", curCoefs.Cast<float>())}");
                coefficients.Add(curCoefs);
            }
        }

        // matrix with degrees from 0 to sidePoints of [-m..m]
        float[,] s = new float[sidePoints * 2 + 1, degree + 1];
        for (int i = 0; i <= degree; ++i)
        {
            for (int m = -sidePoints; m <= sidePoints; ++m)
            {
                s[m + sidePoints, i] = (float)Math.Pow(m, i);
            }
        }
        this.s = s;

        // Square matrix
        float[,] sq = LinearAlgebra.TransposeAndMultiplyWith(ref s, ref s);
        //Console.WriteLine($"sq: ");
        //Print2DArray(sq);
        // inverted matrix
        float[,] inv = LinearAlgebra.Inverse(sq);
        //Console.WriteLine($"inv: ");
        //Print2DArray(inv);
        // multiplied with inversed
        float[,] mul = LinearAlgebra.Multiply(ref s, ref inv);
        //Console.WriteLine("mul: ");
        //Print2DArray(mul);
        // Сalculated coefficients
        coefsMatrix = LinearAlgebra.MultiplyWithTransposed(ref mul, ref s);
        //Console.WriteLine($"coefs: ");
        //Print2DArray(coefsMatrix);

        if (degree == 2 || degree == 3)
        {
            float[] flt = coefficients[sidePoints - minPoints] as float[];
            for (int i = 0; i < coefsMatrix.GetLength(1); ++i)
            {
                if (Math.Abs(coefsMatrix[sidePoints, i] - flt[i]) >= 1e-5)
                {
                    Console.WriteLine("Coefficients differ!");
                    //Console.WriteLine($"from file: {String.Join(" ", flt.Cast<float>())}");
                    throw new ArgumentException("From file differs");
                }
            }
        }
    }

    public float[,] GetS()
    {
        return s;
    }

    public float[,] GetCoefsMatrix()
    {
        return coefsMatrix;
    }

    public float[] Filter(ref ArrayList vals, int len)
    {
        float[] res = new float[len];
        for (int q = 0; q < len; ++q)
        {
            for (int i = 0; i < vals.Count; ++i)
            {
                float[] curVals = vals[i] as float[] ?? throw new ArgumentException($"vals[{i}] is null");
                res[q] += coefsMatrix[i, sidePoints] * curVals[q];
            }
        }
        return res;
    }
}
