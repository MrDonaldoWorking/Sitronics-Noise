using System;
// Provides forecasting more than 1 futures
public class DoubleExpFilter : ExpFilter
{
    protected float[] b;
    protected readonly float beta;
    private bool firstTime;

    public DoubleExpFilter(int len, ref float[] init, float a, float b) : base(len, ref init, a)
    {
        this.b = init;
        beta = b;
        firstTime = true;
    }

    public new float[] GetNext(ref float[] measure)
    {
        if (firstTime)
        {
            firstTime = false;
            // b0 = x1 - x0
            for (int i = 0; i < len; ++i)
            {
                b[i] = measure[i] - s[i];
            }
        }

        float[] prevS = s;
        float[] prevB = b;
        for (int i = 0; i < len; ++i)
        {
            s[i] = alpha * measure[i] + (1 - alpha) * (prevS[i] + prevB[i]);
        }
        for (int i = 0; i < len; ++i)
        {
            b[i] = beta * (s[i] - prevS[i]) + (1 - beta) * prevB[i];
        }

        return s;
    }

    public float[] GetNext(int steps)
    {
        float[] res = new float[len];
        // F[t+m] = s[t] + m*b[t]
        for (int i = 0; i < len; ++i)
        {
            res[i] = s[i] + steps * b[i];
        }
        return res;
    }
}
