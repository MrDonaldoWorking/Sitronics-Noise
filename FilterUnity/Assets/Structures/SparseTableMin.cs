using System;

public class SparseTableMin : SparseTable
{
    public SparseTableMin(ref float[] arr) : base(ref arr) { }

    protected override float Operation(float a, float b)
    {
        return Math.Min(a, b);
    }
}
