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
    private ArrayList rawPosTime;
    private ArrayList filPositions;
    private ArrayList filPosTime;
    private Vector3 initPos;

    private ArrayList rawQuats;
    private ArrayList rawQuatTime;
    private ArrayList filQuats;
    private ArrayList filQuatTime;
    private ArrayList rawAngles;
    private ArrayList filAngles;
    private Quaternion initRot;

    private ArrayList polyCoef;

    public void Init(Vector3 position, Quaternion rotation)
    {
        initPos = position;
        initRot = rotation;

        CONSID_ELEMS = WAIT * 2 + 1;

        rawPositions = new ArrayList();
        rawPositions.Add(V3ToArr(initPos));
        rawPosTime = new ArrayList();
        rawPosTime.Add(0f);
        filPositions = new ArrayList();
        filPositions.Add(V3ToArr(initPos));
        filPosTime = new ArrayList();
        filPosTime.Add(0f);

        rawQuats = new ArrayList();
        rawQuats.Add(rotation);
        rawQuatTime = new ArrayList();
        rawQuatTime.Add(0f);
        filQuats = new ArrayList();
        filQuats.Add(rotation);
        filQuatTime = new ArrayList();
        filQuatTime.Add(0f);
        rawAngles = new ArrayList();
        filAngles = new ArrayList();

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

    // b - kernel width, len - Vector3 or Quaternion
    private float[] GaussianKernel(ref ArrayList vals, ref ArrayList time, float b, int len)
    {
        float midTime = (float)time[time.Count / 2];
        float[] kernel = new float[vals.Count];
        float sum = 0;
        for (int i = 0; i < vals.Count; ++i)
        {
            kernel[i] = (float)Math.Exp(-(Math.Pow((float)time[i] - midTime, 2)) / (2 * b * b));
            sum += kernel[i];
        }
        for (int i = 0; i < vals.Count; ++i)
        {
            kernel[i] /= sum;
        }
        float[] result = new float[len];
        for (int q = 0; q < len; ++q)
        {
            float val = 0;
            for (int i = 0; i < vals.Count; ++i)
            {
                float[] valArr = vals[i] as float[];
                val += valArr[q] * kernel[i];
            }
            result[q] = val;
        }
        return result;
    }

    public Vector3 FilterPosition(float time, Vector3 position, bool positionChanged)
    {
        float rawPrevTime = (float)rawPosTime[rawPosTime.Count - 1];
        float filPrevTime = (float)filPosTime[filPosTime.Count - 1];
        // Issue with sending multiple positions in one frame
        // Why: Machine draws 15 frames in second, but someone set 60 fps
        // Solve: delete previous data and proceed filtering
        bool multipleFrames = Math.Abs(time - rawPrevTime) < EPS || Math.Abs(time - filPrevTime) < EPS;
        if (multipleFrames)
        {
            rawPositions[rawPositions.Count - 1] = V3ToArr(position);
            rawPrevTime = rawPosTime.Count >= 2 ? (float)rawPosTime[rawPosTime.Count - 2] : 0;
        }
        else if (positionChanged)
        {
            rawPositions.Add(V3ToArr(position));
            rawPosTime.Add(time);
            // using (StreamWriter writer = new StreamWriter("Pos_ArrayList_vals", true))
            // {
            //     writer.Write($"{time.ToString("f7")}: {position.ToString("f7")}\t&\t");
            // }
        }
        // Debug.Log($"time: {prevTime} -> {time} = {time - prevTime}; positionChanged = {positionChanged}; position: {string.Join(",", rawPositions[rawPositions.Count - 2] as float[])} -> {position.ToString("f7")}");
        if (rawPositions.Count < CONSID_ELEMS)
        {
            // if (!multipleFrames)
            // {
            //     using (StreamWriter writer = new StreamWriter("Pos_ArrayList_vals", true))
            //     {
            //         writer.WriteLine($"{initPos.ToString("f7")}");
            //     }
            // }
            if (!multipleFrames)
            {
                filPositions.Add(V3ToArr(initPos));
                filPosTime.Add(time);
            }
            else
            {
                filPositions[filPositions.Count - 1] = V3ToArr(initPos);
                // time in last ArrayList element is identical
            }
            return initPos;
        }
        // Lost connection
        if (!positionChanged)
        {
            int startIndex = Math.Max(0, filPositions.Count - WAIT);
            int elemsCnt = Math.Min(WAIT, filPositions.Count - startIndex);
            // Debug.Log($"vectorList size = {filPositions.Count}, vectTime size = {times.Count}, GetRange({startIndex}, {elemsCnt})");
            float[] predict = PredictABG(filPositions.GetRange(startIndex, elemsCnt), filPosTime.GetRange(startIndex, elemsCnt), VEC3_N);
            // Debug.Log($"Predicted Pos = {string.Join(",", predict)}");
            // if (!multipleFrames)
            // {
            //     using (StreamWriter writer = new StreamWriter("Pos_ArrayList_vals", true))
            //     {
            //         writer.Write($"{time.ToString("f7")}: ({string.Join(", ", predict)})\t!\t");
            //     }
            // }
            filPositions.Add(predict);
            filPosTime.Add(time);
            // WAIT lag
            float[] laggedPos = filPositions[filPositions.Count - WAIT] as float[];
            return ArrToV3(ref laggedPos);
        }

        ArrayList posWindow = rawPositions.GetRange(rawPositions.Count - CONSID_ELEMS, CONSID_ELEMS);
        float[] filtered = KolZur(ref posWindow, 2, VEC3_N);
        if (!multipleFrames)
        {
            filPositions.Add(filtered);
            filPosTime.Add(time);
        }
        else
        {
            filPositions[filPositions.Count - 1] = filtered;
        }
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

    private float[] QuatsToAngle(Quaternion from, Quaternion to)
    {
        float[] angle = { (float)Quaternion.Angle(from, to) };
        return angle;
    }

    public Quaternion FilterRotation(float time, Quaternion rotation, bool rotationChanged)
    {
        float rawPrevTime = (float)rawQuatTime[rawQuatTime.Count - 1];
        float filPrevTime = (float)filQuatTime[filQuatTime.Count - 1];
        Quaternion prev = (Quaternion)rawQuats[rawQuats.Count - 1];
        // Issue with sending multiple positions in one frame
        // Why: Machine draws 15 frames in second, but someone set 60 fps
        // Solve: delete previous data and proceed filtering
        bool multipleFrames = Math.Abs(time - rawPrevTime) < EPS || Math.Abs(time - filPrevTime) < EPS;
        Debug.Log($"time: {rawPrevTime} -> {time} = {time - rawPrevTime}; rotationChanged = {rotationChanged}; quat: {prev.ToString("f5")} -> {rotation.ToString("f5")}");
        if (multipleFrames)
        {
            rawQuats[rawQuats.Count - 1] = rotation;
            rawPrevTime = rawQuatTime.Count >= 2 ? (float)rawQuatTime[rawQuatTime.Count - 2] : 0;
            prev = rawQuats.Count >= 2 ? (Quaternion)rawQuats[rawQuats.Count - 2] : initRot;
            rawAngles[rawAngles.Count - 1] = QuatsToAngle(prev, rotation);
        }
        else if (rotationChanged)
        {
            rawQuats.Add(rotation);
            rawQuatTime.Add(time);
            rawAngles.Add(QuatsToAngle(prev, rotation));
        }
        if (rawAngles.Count < CONSID_ELEMS)
        {
            if (!multipleFrames)
            {
                filQuats.Add(initRot);
                filAngles.Add(QuatsToAngle(prev, initRot));
                filQuatTime.Add(time);
            }
            else
            {
                filQuats[filQuats.Count - 1] = initRot;
                // filAngles.Count > 0 guaranteed
                filAngles[filAngles.Count - 1] = QuatsToAngle(prev, initRot);
                // time is identical to last element
            }
            return initRot;
        }

        // Lost connection
        if (!rotationChanged)
        {
            int startIndex = Math.Max(0, filAngles.Count - WAIT);
            int elemsCnt = Math.Min(WAIT, filAngles.Count - startIndex);
            // Neighbour differences are less in 1 than all values
            // Debug.Log($"filAngles size = {filAngles.Count}, filQuatTime size = {filQuatTime.Count}, filAngles.GetRange({startIndex}, {elemsCnt})");
            float[] predict = PredictABG(filAngles.GetRange(startIndex, elemsCnt), filQuatTime.GetRange(startIndex + 1, elemsCnt), ANGLE_N);
            // Debug.Log($"Predicted Pos = {string.Join(",", predict)}");
            float predictedAngle = predict[0];
            // Assuming rotation will be as same as two previous Quaternions
            Quaternion filPrev2 = (Quaternion)filQuats[filQuats.Count - 2];
            Quaternion filPrev = (Quaternion)filQuats[filQuats.Count - 1];
            float prevAngle = (filAngles[filAngles.Count - 1] as float[])[0];
            // predictedAngle = prevAngle * (time - (float)filQuatTime[filQuatTime.Count - 1]) / ((float)filQuatTime[filQuatTime.Count - 1] - (float)filQuatTime[filQuatTime.Count - 2]);
            float predictFactor = Math.Abs(prevAngle) < EPS ? 1 : (prevAngle + predictedAngle) / prevAngle;
            Quaternion predictQuat = ExtrapolateRotation(filPrev2, filPrev, predictFactor);
            // Debug.Log($"angle: {predictedAngle}, {prevAngle} = {predictFactor}; quats: {filPrev2.ToString("f5")} -> {filPrev.ToString("f5")} -> {predictQuat.ToString("f5")}");
            filQuats.Add(predictQuat);
            filAngles.Add(predict);
            filQuatTime.Add(time);
            // WAIT lag
            // return (Quaternion)filQuats[filQuats.Count - WAIT];
        }

        ArrayList fixedWindow = rawAngles.GetRange(rawAngles.Count - CONSID_ELEMS, CONSID_ELEMS);
        float filteredAngle = KolZur(ref fixedWindow, 3, ANGLE_N)[0];
        float angle = (rawAngles[rawAngles.Count - WAIT] as float[])[0];
        float factor = Math.Abs(angle) < EPS ? 0 : filteredAngle / angle;
        // Debug.Log("Res: " + filteredAngle + " / " + angle + " = " + factor);
        Quaternion result = ExtrapolateRotation(prev, rotation, factor);
        if (!multipleFrames)
        {
            filQuats.Add(result);
            filAngles.Add(QuatsToAngle(prev, result));
            filQuatTime.Add(time);
        }
        else
        {
            filQuats[filQuats.Count - 1] = result;
            filAngles[filAngles.Count - 1] = QuatsToAngle(prev, result);
            // time is identical to last element in ArrayList
        }
        return result;
    }
}
