using UnityEngine;

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Text;

public class Filter
{
    private int WAIT = 5;
    private int CONSID_ELEMS;
    // private int QUAT_N = 4;
    private int VEC3_N = 3;
    private int ANGLE_N = 1;
    private int K = 10;
    private float EPS = 1e-5f;

    private int cnt = 0;

    private ArrayList rawPositions;
    private ArrayList times;
    private ArrayList filPositions;
    private Vector3 initPos;

    private ArrayList quatList;
    private ArrayList quatTime;
    private ArrayList angleList;
    private Quaternion initRot;

    private ArrayList polyCoef;

    public void Init(Vector3 position, Quaternion rotation)
    {
        initPos = position;
        initRot = rotation;

        CONSID_ELEMS = WAIT * 2 + 1;

        rawPositions = new ArrayList();
        rawPositions.Add(V3ToArr(initPos));
        times = new ArrayList();
        times.Add(0f);
        filPositions = new ArrayList();

        quatList = new ArrayList();
        quatTime = new ArrayList();
        angleList = new ArrayList();

        CalcPolynomialCoef(CONSID_ELEMS);

        // clear all debug outputs
        for (int i = 0; i < VEC3_N; ++i)
        {
            File.WriteAllText("before" + i, string.Empty);
            File.WriteAllText("after" + i, string.Empty);
        }
    }

    // polynomial coefficient for (1 + x + x^2 + ... + x^(m-1))^k
    private void CalcPolynomialCoef(int m)
    {
        polyCoef = new ArrayList();
        polyCoef.Add(new int[1]);

        int[] firstPow = new int[m];
        for (int i = 0; i < m; ++i)
        {
            firstPow[i] = 1;
        }
        polyCoef.Add(firstPow);
        Debug.Log("polyCoef[1]: " + string.Join(", ", firstPow));

        for (int k = 0; k < K; ++k)
        {
            int[] prev = polyCoef[polyCoef.Count - 1] as int[];
            int[] next = new int[prev.Length + m - 1];
            for (int i = 0; i < prev.Length; ++i)
            {
                for (int j = i; j < i + m; ++j)
                {
                    next[j] += prev[i];
                }
            }
            polyCoef.Add(next);
            Debug.Log("polyCoef[" + (k + 2) + "]: " + string.Join(", ", next));
        }
    }

    private readonly struct SparseTableMin
    {
        private readonly ArrayList table;

        public SparseTableMin(ref float[] arr)
        {
            int n = arr.Length;
            int h = (int)Math.Log(n, 2);

            table = new ArrayList();
            table.Add(arr);

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
    }

    private readonly struct SparseTableMax
    {
        private readonly ArrayList table;

        public SparseTableMax(ref float[] arr)
        {
            int n = arr.Length;
            int h = (int)Math.Log(n, 2);

            table = new ArrayList();
            table.Add(arr);

            for (int i = 1; i <= h; ++i)
            {
                float[] prev = table[table.Count - 1] as float[];
                float[] next = new float[prev.Length - (1 << i - 1)];
                for (int j = 0; j < next.Length; ++j)
                {
                    next[j] = Math.Max(prev[j], prev[j + (1 << (i - 1))]);
                }
                table.Add(next);
            }
        }

        public float Get(int left, int right)
        {
            int lg = (int)Math.Log(right - left, 2);
            int len = 1 << lg;
            float[] arr = table[lg] as float[];
            return Math.Max(arr[left], arr[right - len]);
        }
    }

    // Original Alpha-Beta-Gamma Filter
    private readonly struct ABG // Assuming it's movement with changing accelaration
    {
        private static readonly int ARGS_N = 3;
        private readonly float[] factors;
        private readonly float[] currents;
        private readonly float[] predictions;

        public ABG(float a, float b, float c, ref float[] init)
        {
            factors = new float[ARGS_N];
            currents = new float[ARGS_N];
            predictions = new float[ARGS_N];
            Init(a, b, c, ref init);
        }

        public ABG(ref float[] init)
        {
            factors = new float[ARGS_N];
            currents = new float[ARGS_N];
            predictions = new float[ARGS_N];
            Init(0.5f, 0.4f, 0.1f, ref init);
        }

        private void Init(float a, float b, float c, ref float[] init)
        {
            factors[0] = a;
            factors[1] = b;
            factors[2] = c;
            for (int i = 0; i < Math.Min(ARGS_N, init.Length); ++i)
            {
                currents[i] = init[i];
            }
        }

        public void Update(float time, float position) // measure current position since time from previous Measure
        {
            Predict(time);
            Estimate(time, position);
        }

        public void Update(float time) // If there is no measure
        {
            Predict(time);
            Estimate(time, predictions[0]);
        }

        public float[] GetCurrent()
        {
            return currents;
        }

        public float[] GetPrediction()
        {
            return predictions;
        }

        private void Predict(float time)
        {
            predictions[0] = currents[0] + currents[1] * time + currents[2] * time * time / 2;
            predictions[1] = currents[1] + currents[2] * time;
            predictions[2] = currents[2];
        }

        private void Estimate(float time, float position)
        {
            float deviation = position - predictions[0];
            currents[0] = predictions[0] + factors[0] * deviation;
            currents[1] = predictions[1] + factors[1] * deviation / time;
            currents[2] = predictions[2] + factors[2] * deviation / (0.5f * time * time);
        }
    }

    private ArrayList ToVec3ArrayList(ref ArrayList arr)
    {
        ArrayList result = new ArrayList();
        int len = (arr[0] as float[]).Length;
        // string strvec = "";
        for (int i = 0; i < len; ++i)
        {
            Vector3 vec = new Vector3(0f, 0f, 0f);
            for (int q = 0; q < VEC3_N; ++q)
            {
                float[] cur = arr[q] as float[];
                vec[q] = cur[i];
            }
            // strvec += vec + " ";
            result.Add(vec);
        }
        // Debug.Log("res: " + strvec);

        return result;
    }

    // https://en.wikipedia.org/wiki/Lulu_smoothing
    // L operator of LULU filter
    private ArrayList L(ref ArrayList vals, int n)
    {
        // string valstr = "";
        // foreach (Vector3 vec in vals)
        // {
        //     valstr += vec + " ";
        // }
        // Debug.Log("L vals = " + valstr);
        ArrayList seqs = new ArrayList();
        for (int q = 0; q < VEC3_N; ++q)
        {
            float[] arr = new float[vals.Count];
            for (int i = 0; i < vals.Count; ++i)
            {
                Vector3 curVec = (Vector3)vals[i];
                arr[i] = curVec[q];
            }

            SparseTableMin st = new SparseTableMin(ref arr);
            float[] mins = new float[vals.Count - n];
            for (int i = 0; i + n < vals.Count; ++i)
            {
                mins[i] = st.Get(i, i + n);
            }
            // Debug.Log("mins: " + string.Join(", ", mins));
            seqs.Add(mins);
        }

        ArrayList arrL = new ArrayList();
        for (int q = 0; q < VEC3_N; ++q)
        {
            float[] mins = seqs[q] as float[];
            SparseTableMax st = new SparseTableMax(ref mins);
            float[] curL = new float[mins.Length - n];
            for (int i = 0; i + n < mins.Length; ++i)
            {
                curL[i] = st.Get(i, i + n);
            }
            // Debug.Log("curL: " + string.Join(", ", curL));
            arrL.Add(curL);
        }

        return ToVec3ArrayList(ref arrL);
    }

    // U operator of LULU filter
    private ArrayList U(ref ArrayList vals, int n)
    {
        // string valstr = "";
        // foreach (Vector3 vec in vals)
        // {
        //     valstr += vec + " ";
        // }
        // Debug.Log("U vals = " + valstr);
        ArrayList seqs = new ArrayList();
        for (int q = 0; q < VEC3_N; ++q)
        {
            float[] arr = new float[vals.Count];
            for (int i = 0; i < vals.Count; ++i)
            {
                Vector3 curVec = (Vector3)vals[i];
                arr[i] = curVec[q];
            }

            SparseTableMax st = new SparseTableMax(ref arr);
            float[] maxs = new float[vals.Count - n];
            for (int i = 0; i + n < vals.Count; ++i)
            {
                maxs[i] = st.Get(i, i + n);
            }
            // Debug.Log("maxs: " + string.Join(", ", maxs));
            seqs.Add(maxs);
        }

        ArrayList arrU = new ArrayList();
        for (int q = 0; q < VEC3_N; ++q)
        {
            float[] maxs = seqs[q] as float[];
            SparseTableMin st = new SparseTableMin(ref maxs);
            float[] curU = new float[maxs.Length - n];
            for (int i = 0; i + n < maxs.Length; ++i)
            {
                curU[i] = st.Get(i, i + n);
            }
            // Debug.Log("curU: " + string.Join(", ", curU));
            arrU.Add(curU);
        }

        return ToVec3ArrayList(ref arrU);
    }

    // https://en.wikipedia.org/wiki/Exponential_smoothing
    private struct SingleExp
    {
        private Vector3 s;
        private readonly float alpha;

        public SingleExp(ref Vector3 init, float a)
        {
            s = init;
            alpha = a;
        }

        public Vector3 GetNext(ref Vector3 measure)
        {
            s = alpha * measure + (1 - alpha) * s;
            return s;
        }
    }

    private struct DoubleExp
    {
        private Vector3 s;
        private readonly float alpha;
        private Vector3 b;
        private readonly float beta;
        private bool firstTime;

        public DoubleExp(ref Vector3 init, float a, float b)
        {
            s = init;
            this.b = init;
            alpha = a;
            beta = b;
            firstTime = true;
        }

        public Vector3 GetNext(ref Vector3 measure)
        {
            if (firstTime)
            {
                firstTime = false;
                // b0 = x1 - x0
                b = measure - s;
            }

            Vector3 prevS = s;
            Vector3 prevB = b;
            s = alpha * measure + (1 - alpha) * (prevS + prevB);
            b = beta * (s - prevS) + (1 - beta) * prevB;

            return s;
        }
    }

    // ArrayList of Vector3
    // m = CONSID_ELEMS, - window size
    // k - iterations count
    // https://wires.onlinelibrary.wiley.com/doi/pdf/10.1002/wics.71
    private float[] KolZur(ref ArrayList vals, int k, int len)
    {
        // vals.Count = CONSID_ELEMS = 2 * WAIT + 1
        int t = WAIT;
        int m = CONSID_ELEMS;
        float[] result = new float[len];
        // string calcstr = "";
        for (int q = 0; q < len; ++q)
        {
            for (int s = k * -WAIT; s <= k * WAIT; ++s)
            {
                float[] val = vals[Math.Min(vals.Count - 1, Math.Max(0, t + s))] as float[];
                int[] coef = polyCoef[k] as int[];
                float dividend = val[q] * (float)coef[s + k * WAIT];
                result[q] += (float)(dividend / Math.Pow(m, k));
            }
        }

        return result;
    }

    // each element of ArrayList is Vector3 with floats
    private Vector3 Median(ref ArrayList vals)
    {
        float[][] coords = new float[VEC3_N][];
        for (int q = 0; q < VEC3_N; ++q)
        {
            float[] curCoord = new float[vals.Count];
            for (int i = 0; i < vals.Count; ++i)
            {
                Vector3 val = (Vector3)vals[i];
                curCoord[i] = val[q];
            }
            Array.Sort(curCoord);
            coords[q] = curCoord;
        }

        return new Vector3(coords[0][vals.Count / 2], coords[1][vals.Count / 2], coords[2][vals.Count / 2]);
    }

    private float[] V3ToArr(Vector3 vector)
    {
        float[] values = new float[VEC3_N];
        for (int q = 0; q < VEC3_N; ++q)
        {
            values[q] = vector[q];
        }
        return values;
    }

    private Vector3 ArrToV3(ref float[] arr)
    {
        return new Vector3(arr[0], arr[1], arr[2]);
    }

    private float[] PredictABG(ArrayList vals, ArrayList time, int len)
    {
        ArrayList ABGs = new ArrayList();
        // init ABG
        for (int q = 0; q < len; ++q)
        {
            float[] startPos = vals[0] as float[];
            float[] init = { startPos[q] };
            ABGs.Add(new ABG(ref init));
        }
        // Prepare ABG for prediction
        for (int i = 1; i < vals.Count; ++i)
        {
            for (int q = 0; q < len; ++q)
            {
                ABG axis = (ABG)ABGs[q];
                axis.Update((float)time[i] - (float)time[i - 1], (vals[i] as float[])[q]);
            }
        }
        // Predict next value
        float[] predict = new float[len];
        for (int q = 0; q < len; ++q)
        {
            ABG axis = (ABG)ABGs[q];
            predict[q] = axis.GetCurrent()[0];
        }
        return predict;
    }

    public Vector3 FilterPosition(float time, Vector3 position, bool positionChanged)
    {
        float prevTime = times.Count > 0 ? (float)times[times.Count - 1] : 0;
        // Issue with sending multiple positions in one frame
        // Why: Machine draws 15 frames in second, but someone set 60 fps
        // Solve: delete previous data and proceed filtering
        bool multipleFrames = Math.Abs(time - prevTime) < EPS;
        if (multipleFrames)
        {
            rawPositions[rawPositions.Count - 1] = V3ToArr(position);
            prevTime = times.Count > 2 ? (float)times[times.Count - 2] : 0;
        }
        else if (positionChanged)
        {
            rawPositions.Add(V3ToArr(position));
            times.Add(time);
            // using (StreamWriter writer = new StreamWriter("Pos_ArrayList_vals", true))
            // {
            //     writer.Write($"{time.ToString("f7")}: {position.ToString("f7")}\t&\t");
            // }
        }
        Debug.Log($"time: {prevTime} -> {time} = {time - prevTime}; positionChanged = {positionChanged}; position: {string.Join(",", rawPositions[rawPositions.Count - 2] as float[])} -> {position.ToString("f7")}");
        // Lost connection
        if (!positionChanged)
        {
            int startIndex = Math.Max(0, filPositions.Count - WAIT);
            int elemsCnt = Math.Min(WAIT, filPositions.Count - startIndex);
            // Debug.Log($"vectorList size = {vectorList.Count}, vectTime size = {vectorTime.Count}, GetRange({startIndex}, {elemsCnt})");
            float[] predict = PredictABG(filPositions.GetRange(startIndex, elemsCnt), times.GetRange(startIndex, elemsCnt), VEC3_N);
            // Debug.Log($"Predicted Pos = {string.Join(",", predict)}");
            // if (!multipleFrames)
            // {
            //     using (StreamWriter writer = new StreamWriter("Pos_ArrayList_vals", true))
            //     {
            //         writer.Write($"{time.ToString("f7")}: ({string.Join(", ", predict)})\t!\t");
            //     }
            // }
            rawPositions.Add(predict);
            times.Add(time);
        }
        if (rawPositions.Count < CONSID_ELEMS)
        {
            // if (!multipleFrames)
            // {
            //     using (StreamWriter writer = new StreamWriter("Pos_ArrayList_vals", true))
            //     {
            //         writer.WriteLine($"{initPos.ToString("f7")}");
            //     }
            // }
            return initPos;
        }

        ArrayList posWindow = rawPositions.GetRange(rawPositions.Count - CONSID_ELEMS, CONSID_ELEMS);
        float[] filtered = KolZur(ref posWindow, 2, VEC3_N);
        filPositions.Add(filtered);
        // Debug.Log("Filtered position: " + ArrToV3(filtered));
        // if (!multipleFrames)
        // {
        //     using (StreamWriter writer = new StreamWriter("Pos_ArrayList_vals", true))
        //     {
        //         writer.WriteLine($"({string.Join(", ", filtered)})");
        //     }
        // }

        return ArrToV3(ref filtered);
    }

    private void Log2File(Vector3 bfr, Vector3 aft)
    {
        for (int q = 0; q < VEC3_N; ++q)
        {
            using (StreamWriter before = new StreamWriter("before" + q, true))
            {
                before.Write(bfr[q] + " ");
            }

            using (StreamWriter after = new StreamWriter("after" + q, true))
            {
                after.Write(aft[q] + " ");
            }
        }
    }

    private Quaternion ExtrapolateRotation(Quaternion from, Quaternion to, float factor)
    {
        Quaternion rot = to * Quaternion.Inverse(from); // rot is the rotation from from to to
        float ang;
        Vector3 axis;
        rot.ToAngleAxis(out ang, out axis); // find axis-angle representation
        if (ang > 180) // assume the shortest path
        {
            ang -= 360;
        }
        ang = ang * factor % 360; // multiply angle by the factor
        return Quaternion.AngleAxis(ang, axis) * from; // combine with first rotation
    }

    public Quaternion FilterRotation(float time, Quaternion rotation, bool rotationChanged)
    {
        quatList.Add(rotation);
        quatTime.Add(time);
        Quaternion prev = quatList.Count > 1 ? (Quaternion)quatList[quatList.Count - 2] : initRot;
        float[] angleArr = new float[1];
        angleArr[0] = (float)Quaternion.Angle(prev, rotation);
        angleList.Add(angleArr);
        if (quatList.Count < CONSID_ELEMS)
        {
            return initRot;
        }

        ArrayList fixedWindow = angleList.GetRange(angleList.Count - CONSID_ELEMS, CONSID_ELEMS);
        string str = "(";
        foreach (float[] flt in fixedWindow)
        {
            str += string.Join(",", flt) + " ";
        }
        // Debug.Log("Rotations: " + str + ")");
        float filteredAngle = KolZur(ref fixedWindow, 3, ANGLE_N)[0];
        float angle = (angleList[angleList.Count - 1] as float[])[0];
        float factor = Math.Abs(angle) < EPS ? 0 : filteredAngle / angle;
        // Debug.Log("Res: " + filteredAngle + " / " + angle + " = " + factor);
        return ExtrapolateRotation(prev, rotation, factor);
    }
}
