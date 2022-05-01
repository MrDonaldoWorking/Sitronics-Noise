using System;
using System.Collections;
using System.IO;

// Convolution coefficients are described here: https://agora.cs.wcu.edu/~huffman/figures/sgpaper1964.pdf
// How it generated described here: https://github.com/mathnet/mathnet-filtering/issues/4
public class SavGolFilter
{
    private readonly ArrayList coefficients;
    private readonly int minPoints = 2;
    private readonly int maxPoints = 10;
    private readonly int sidePoints;
    private readonly int degree;

    public SavGolFilter(int sidePoints, int degree)
    {
        this.sidePoints = sidePoints;
        this.degree = degree;
        using (TextReader reader = File.OpenText("./SavGolC.conf"))
        {
            for (int sPoints = minPoints; sPoints <= maxPoints; ++sPoints)
            {
                string line = reader.ReadLine();
                string[] datas = line.Split(':');
                int check = int.Parse(datas[1]);

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
                    curCoefs[i] = (float)raws[i] / coefs.Length;
                }
                for (int i = 0; i < coefs.Length - 1; ++i)
                {
                    curCoefs[i] = curCoefs[coefs.Length - 2 - i];
                }
                coefficients.Add(curCoefs);
            }
        }
    }

    public float[] Filter(ArrayList vals, int len)
    {
        float[] coefs = coefficients[sidePoints - minPoints] as float[];
        float[] res = new float[len];
        for (int q = 0; q < len; ++q)
        {
            for (int i = 0; i < vals.Count; ++i)
            {
                float[] curVals = vals[i] as float[];
                res[q] += coefs[i] * curVals[q];
            }
        }
        return res;
    }
}
