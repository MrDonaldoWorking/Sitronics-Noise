using System;

namespace Sitronics_Noise
{
    class Point
    {
        public float getX()
        {
            return x;
        }

        public float getY()
        {
            return y;
        }

        public float getZ()
        {
            return z;
        }

        public Point(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        private readonly float x;
        private readonly float y;
        private readonly float z;
    }

    class Quaternion
    {
        public float getX()
        {
            return p.getX();
        }

        public float getY()
        {
            return p.getY();
        }

        public float getZ()
        {
            return p.getZ();
        }

        public float getW()
        {
            return w;
        }

        public Quaternion(Point p, float w)
        {
            this.p = p;
            this.w = w;
        }

        public Quaternion(float x, float y, float z, float w)
        {
            this.p = new Point(x, y, z);
            this.w = w;
        }

        private readonly Point p;
        private readonly float w;
    }

    class Program
    {
        static Point Run(float time, Point p, Quaternion q)
        {
            // TODO: Implement algorithm
            return null;
        }

        static Point Filter(
            float time,
            float px, float py, float pz,
            float qx, float qy, float qz, float qw
            )
        {
            Point p = new Point(px, py, pz);
            Quaternion q = new Quaternion(qx, qy, qz, qw);
            return Run(time, p, q);
        }

        static void Main()
        {

        }
    }
}
