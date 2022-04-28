using System;
// https://en.wikipedia.org/wiki/Exponential_smoothing
public class ExpFilter
{
    protected readonly int len;
    protected float[] s;
    protected readonly float alpha;

    public ExpFilter(int len, ref float[] init, float alpha)
    {
        this.len = len;
        s = init;
        this.alpha = alpha;
    }

    // Predict next value by consuming current
    public float[] GetNext(ref float[] measure)
    {
        for (int i = 0; i < len; ++i)
        {
            s[i] = alpha * measure[i] + (1 - alpha) * s[i];
        }
        return s;
    }
}
