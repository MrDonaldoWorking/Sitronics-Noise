using System;
using System.Collections;
using System.Collections.Generic;

public class WeightedMedianFilter
{
    private static readonly float BOUND = 0.5f;

    // each element of ArrayList is Vector3 with floats
    // O(n log n)
    public static float[] Filter(ref ArrayList vals, ref ArrayList time, int len, bool lower)
    {
        float[] res = new float[len];
        float[] weights = new float[vals.Count];
        Util.GaussianKernel(ref time, 2, ref weights);
        for (int q = 0; q < len; ++q)
        {
            // Function to calculate weighted median
            // Store arr of vals[i][q] and W[i]
            List<Tuple<float, float>> arr = new List<Tuple<float, float>>();
            for (int i = 0; i < vals.Count; ++i)
            {
                float[] curr = vals[i] as float[];
                arr.Add(new Tuple<float, float>(curr[q], weights[i]));
            }

            // Sort the list of pr w.r.t.
            // to their arr[] values
            arr.Sort();

            // If N is odd
            if (arr.Count % 2 != 0)
            {
                // Traverse the set pr
                // from left to right
                float sums = 0;
                foreach (Tuple<float, float> element in arr)
                {
                    // Update sums
                    sums += element.Item2;

                    // If sum becomes > 0.5
                    if (sums > BOUND)
                    {
                        //Console.WriteLine("The Weighted Median " +
                        //                "is element " + element.Item1);
                        res[q] = element.Item1;
                        break;
                    }
                }
            }

            // If N is even
            else
            {
                if (lower)
                {
                    // For lower median traverse
                    // the set pr from left
                    float sums = 0;
                    foreach (Tuple<float, float> element in arr)
                    {

                        // Update sums
                        sums += element.Item2;

                        // When sum >= 0.5
                        if (sums >= BOUND)
                        {
                            //Console.WriteLine("Lower Weighted Median " +
                            //                "is element " + element.Item1);
                            res[q] = element.Item1;
                            break;
                        }
                    }
                }
                else
                {
                    // For upper median traverse
                    // the set pr from right
                    float sums = 0;
                    for (int index = arr.Count - 1; index >= 0; --index)
                    {
                        float element = arr[index].Item1;
                        float weight = arr[index].Item2;

                        // Update sums
                        sums += weight;

                        // When sum >= 0.5
                        if (sums >= BOUND)
                        {
                            //Console.Write("Upper Weighted Median " +
                            //            "is element " + element);
                            res[q] = element;
                            break;
                        }
                    }
                }
            }
        }

        return res;
    }
}

public class Pair<F, S>
{
    private readonly F first;
    private readonly S second;

    public Pair(F first, S second)
    {
        this.first = first;
        this.second = second;
    }

    public F GetFirst()
    {
        return first;
    }

    public S GetSecond()
    {
        return second;
    }
}
