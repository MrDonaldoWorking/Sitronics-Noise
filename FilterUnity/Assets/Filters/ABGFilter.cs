using System;
using System.Collections;

// Original Alpha-Beta-Gamma Filter
public class ABG // Assuming it's movement with changing accelaration
{
    private static readonly int ARGS_N = 3;
    private readonly float[] factors;
    private readonly float[] currents;
    private readonly float[] predictions;

    public ABG(float a, float b, float c, ref float[] init)
    {
        factors = new float[ARGS_N];
        currents = new float[ARGS_N];
        predictions = new float[ARGS_N];
        Init(a, b, c, ref init);
    }

    public ABG(ref float[] init)
    {
        factors = new float[ARGS_N];
        currents = new float[ARGS_N];
        predictions = new float[ARGS_N];
        Init(0.5f, 0.4f, 0.1f, ref init);
    }

    private void Init(float a, float b, float c, ref float[] init)
    {
        factors[0] = a;
        factors[1] = b;
        factors[2] = c;
        for (int i = 0; i < Math.Min(ARGS_N, init.Length); ++i)
        {
            currents[i] = init[i];
        }
    }

    public void Update(float time, float position) // measure current position since time from previous Measure
    {
        Predict(time);
        Estimate(time, position);
    }

    public void Update(float time) // If there is no measure
    {
        Predict(time);
        Estimate(time, predictions[0]);
    }

    public float[] GetCurrent()
    {
        return currents;
    }

    public float[] GetPrediction()
    {
        return predictions;
    }

    private void Predict(float time)
    {
        predictions[0] = currents[0] + currents[1] * time + currents[2] * time * time / 2;
        predictions[1] = currents[1] + currents[2] * time;
        predictions[2] = currents[2];
    }

    private void Estimate(float time, float position)
    {
        float deviation = position - predictions[0];
        currents[0] = predictions[0] + factors[0] * deviation;
        currents[1] = predictions[1] + factors[1] * deviation / time;
        currents[2] = predictions[2] + factors[2] * deviation / (0.5f * time * time);
    }

    public static float[] Predict(ArrayList vals, ArrayList time, int len)
    {
        ArrayList ABGs = new ArrayList();
        // init ABG
        for (int q = 0; q < len; ++q)
        {
            float[] startPos = vals[0] as float[];
            float[] init = { startPos[q] };
            ABGs.Add(new ABG(ref init));
        }
        // Prepare ABG for prediction
        for (int i = 1; i < vals.Count; ++i)
        {
            for (int q = 0; q < len; ++q)
            {
                ABG axis = (ABG)ABGs[q];
                axis.Update((float)time[i] - (float)time[i - 1], (vals[i] as float[])[q]);
            }
        }
        // Predict next value
        float[] predict = new float[len];
        for (int q = 0; q < len; ++q)
        {
            ABG axis = (ABG)ABGs[q];
            predict[q] = axis.GetCurrent()[0];
        }
        return predict;
    }
}
