using System;

public class NoiseGenerator
{
    public NoiseGenerator(float staticFluctuation, float randomFluctuation)
    {
        this.sf = staticFluctuation;
        this.rf = randomFluctuation;
        this.machine = new Random();
    }

    private float Extend(float rnd, float bound)
    {
        return (2 * rnd - 1) * bound;
    }

    public float Noise()
    {
        float value = Extend((float)machine.NextDouble(), sf);
        if (machine.Next(50) == 0)
        {
            value += Extend((float)machine.NextDouble(), rf);
        }
        return value;
    }

    private readonly float sf;
    private readonly float rf;
    private readonly Random machine;
}