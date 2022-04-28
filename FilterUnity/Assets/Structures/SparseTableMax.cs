using System;
public class SparseTableMax : SparseTable
{
    public SparseTableMax(ref float[] arr) : base(ref arr) { }

    protected override float Operation(float a, float b)
    {
        return Math.Max(a, b);
    }
}
