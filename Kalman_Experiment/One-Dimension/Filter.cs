using System;

namespace One_Dimension
{
    // Estimating the current position of point with accelaration
    public class Filter
    {
        public Filter(float a, float b, float c)
        {
            this.alpha = a;
            this.beta = b;
            this.gamma = c;
        }

        public void init(float pos, float vel, float acc)
        {
            this.position = pos;
            this.velocity = vel;
            this.accelaration = acc;
        }

        // time since previous update
        public void update(float time, float pos)
        {
            predict(time);
            estimate(time, pos);
        }

        private void predict(float time)
        {
            nextP = position + velocity * time + accelaration * time * time / 2;
            nextV = velocity + accelaration * time;
            nextA = accelaration;
        }

        private void estimate(float time, float pos)
        {
            float deviation = pos - nextP;
            position = nextP + alpha * deviation;
            velocity = nextV + beta * deviation / time;
            accelaration = nextA + gamma * deviation / 0.5f / time / time;
        }

        public float getP()
        {
            return position;
        }

        public float getV()
        {
            return velocity;
        }

        public float getA()
        {
            return accelaration;
        }

        // factors
        private readonly float alpha;
        private readonly float beta;
        private readonly float gamma;

        // current states
        private float position;
        private float velocity;
        private float accelaration;

        // predictions
        private float nextP;
        private float nextV;
        private float nextA;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
