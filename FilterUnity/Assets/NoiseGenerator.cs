using System;

public class NoiseGenerator
{
    public NoiseGenerator(float staticFluctuation, float randomFluctuation)
    {
        this.sf = staticFluctuation;
        this.rf = randomFluctuation;
        this.machine = new Random();
    }

    public float Noise()
    {
        float value = (float)machine.NextDouble() * sf;
        if (machine.Next(50) == 0)
        {
            value += (float)machine.NextDouble() * rf;
        }
        return value;
    }

    private readonly float sf;
    private readonly float rf;
    private readonly Random machine;
}