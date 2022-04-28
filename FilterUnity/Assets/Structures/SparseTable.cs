using System;
using System.Collections;

public abstract class SparseTable
{
    private readonly ArrayList table;

    public SparseTable(ref float[] arr)
    {
        int n = arr.Length;
        int h = (int)Math.Log(n, 2);

        table = new ArrayList { arr };

        for (int i = 1; i <= h; ++i)
        {
            float[] prev = table[table.Count - 1] as float[];
            float[] next = new float[prev.Length - (1 << i - 1)];
            for (int j = 0; j < next.Length; ++j)
            {
                next[j] = Math.Min(prev[j], prev[j + (1 << (i - 1))]);
            }
            table.Add(next);
        }
    }

    public float Get(int left, int right)
    {
        int lg = (int)Math.Log(right - left, 2);
        int len = 1 << lg;
        float[] arr = table[lg] as float[];
        return Math.Min(arr[left], arr[right - len]);
    }

    protected abstract float Operation(float a, float b);
}
