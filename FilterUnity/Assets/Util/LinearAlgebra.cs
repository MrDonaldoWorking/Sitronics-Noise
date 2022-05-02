using System;

public static class LinearAlgebra
{
	public static float[,] Multiply(ref float[,] m, ref float[,] o)
    {
		if (m.GetLength(1) != o.GetLength(0))
		{
			throw new ArgumentException($"Incompatible matrices " +
				$"{m.GetLength(0)}x{m.GetLength(1)} and " +
				$"{o.GetLength(0)}x{o.GetLength(1)}");
		}
		float[,] res = new float[m.GetLength(0), o.GetLength(1)];
		for (int i = 0; i < m.GetLength(0); ++i)
		{
			for (int j = 0; j < o.GetLength(1); ++j)
			{
				res[i, j] = 0f;
				for (int k = 0; k < m.GetLength(1); ++k)
				{
					res[i, j] += m[i, k] * o[k, j];
				}
			}
		}
		return res;
	}

	// Transpose matrix m and multiply with matrix o
	public static float[,] TransposeAndMultiplyWith(ref float[,] m, ref float[,] o)
    {
        // Assume m(m.r x m.c) and o(o.r x o.c)
        // The result will be mT(m.c x m.r) x o(o.r x o.c)
        // m.r == o.r is essential
        if (m.GetLength(0) != o.GetLength(0))
        {
            throw new ArgumentException($"Expected same size of rows, but got " +
                $"{m.GetLength(0)} and {o.GetLength(0)}");
        }
        float[,] res = new float[m.GetLength(1), o.GetLength(1)];
        for (int i = 0; i < m.GetLength(1); ++i)
        {
            for (int j = 0; j < o.GetLength(1); ++j)
            {
                res[i, j] = 0f;
                for (int k = 0; k < m.GetLength(0); ++k)
                {
                    res[i, j] += m[k, i] * o[k, j];
                }
            }
        }
        return res;
    }

	// Matrix multiplication with transposed matrix o
	public static float[,] MultiplyWithTransposed(ref float[,] m, ref float[,] o)
    {
		if (m.GetLength(1) != o.GetLength(1))
        {
			throw new ArgumentException($"Incompatible matrices " +
				$"{m.GetLength(0)}x{m.GetLength(1)} and " +
				$"{o.GetLength(0)}x{o.GetLength(1)}T");
        }
		float[,] res = new float[m.GetLength(0), o.GetLength(0)];
		for (int i = 0; i < m.GetLength(0); ++i)
        {
			for (int j = 0; j < o.GetLength(0); ++j)
            {
				res[i, j] = 0f;
				for (int k = 0; k < m.GetLength(1); ++k)
                {
					res[i, j] += m[i, k] * o[j, k];
                }
            }
        }
		return res;
    }

	// Calculates determinant of matrix A
	public static float Det(float[,] a)
	{
		int k = a.GetLength(0);
		for (int i = 0; i < k; ++i)
		{
			for (int j = i + 1; j < k; ++j)
			{
				float x = (a[j, i] / a[i, i]);
				for (int t = i; t < k; ++t)
				{
					a[j, t] -= a[i, t] * x;
				}
			}
		}
		float sum = 1;
		for (int i = 0; i < k; ++i)
		{
			sum *= a[i, i];
		}
		return sum;
	}

	// Calculates minor on row x and column y of matrix A
	public static double Minor(float[,] a, int x, int y)
	{
		float[,] mn = new float[a.GetLength(0) - 1, a.GetLength(1) - 1];
		int curRow = 0;
		for (int i = 0; i < a.GetLength(0); ++i)
		{
			if (i == x)
			{
				continue;
			}
			int curCol = 0;
			for (int j = 0; j < a.GetLength(1); ++j)
			{
				if (j == y)
				{
					continue;
				}
				mn[curRow, curCol] = a[i, j];
				++curCol;
			}
			++curRow;
		}
		return Det(mn);
	}

	// Trasposes given matrix A
	public static float[,] Transpose(ref float[,] a)
	{
		int n = a.GetLength(0);
		int m = a.GetLength(1);
		float[,] tmp = new float[m, n];
		for (int i = 0; i < m; ++i)
		{
			for (int j = 0; j < n; ++j)
			{
				tmp[i, j] = a[j, i];
			}
		}
		return tmp;
	}

	// Divides matrix a by x
	public static void DivideByScalar(ref float[,] a, float x)
	{
		for (int i = 0; i < a.GetLength(0); ++i)
		{
			for (int j = 0; j < a.GetLength(1); ++j)
			{
				a[i, j] /= x;
			}
		}
	}

	// Calculates inverse of given matrix
	public static float[,] Inverse(float[,] a)
	{
		int n = a.GetLength(0);
		if (n != a.GetLength(1))
        {
			throw new ArgumentException($"Expected square matrix, but got " +
				$"matrix {n}x{a.GetLength(1)}");
        }

		// Add identity matrix to the end
		float[,] prc = new float[n, 2 * n];
		for (int i = 0; i < n; ++i)
		{
			for (int j = 0; j < n; ++j)
			{
				prc[i, j] = a[i, j];
			}
			for (int j = 0; j < n; ++j)
            {
				prc[i, n + j] = (i == j ? 1 : 0);
            }
		}
		// Calculates Determinant and Adj
		for (int i = 0; i < n; ++i)
		{
			for (int j = i + 1; j < n; ++j)
			{
				float x = (prc[j, i] / prc[i, i]);
				for (int t = i; t < 2 * n; ++t)
				{
					prc[j, t] -= prc[i, t] * x;
				}
			}
		}
		for (int i = n - 1; i >= 0; --i)
		{
			for (int j = i - 1; j >= 0; --j)
			{
				float x = (prc[j, i] / prc[i, i]);
				for (int t = i; t < 2 * n; ++t)
				{
					prc[j, t] -= prc[i, t] * x;
				}
			}
		}
		// Divides with calculated determinants
		float[,] res = new float[n, n];
		for (int i = 0; i < n; ++i)
		{
			for (int j = 0; j < n; ++j)
			{
				res[i, j] = prc[i, j + n] / prc[i, i];
			}
		}
		return res;
	}
}
