using System;

public class NoiseGenerator
{
    public NoiseGenerator(float staticFluctuation, float randomFluctuation, int ticks)
    {
        this.sf = staticFluctuation;
        this.rf = randomFluctuation;
        this.ticks = ticks;
        this.machine = new Random();
    }

    private float Extend(float rnd, float bound)
    {
        return (2 * rnd - 1) * bound;
    }

    public float Noise()
    {
        float value = Extend((float)machine.NextDouble(), sf);
        if (machine.Next(ticks) == 0)
        {
            value += Extend((float)machine.NextDouble(), rf);
            happened = true;
        }
        else
        {
            happened = false;
        }
        return value;
    }

    public float GetSF()
    {
        return sf;
    }

    public float GetRF()
    {
        return rf;
    }

    public int GetTicks()
    {
        return ticks;
    }

    public bool GetHappened()
    {
        return happened;
    }

    private readonly float sf;
    private readonly float rf;
    private readonly int ticks;
    private bool happened = false;
    private readonly Random machine;
}