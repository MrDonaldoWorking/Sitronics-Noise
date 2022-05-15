using UnityEngine;

using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System.Text;

public class Filter
{
    private readonly int WAIT = 5;
    private int CONSID_ELEMS;
    public static readonly int QUAT_N = 4;
    public static readonly int VEC3_N = 3;
    public static readonly int ANGLE_N = 1;
    private readonly float EPS = 1e-5f;

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

    private KolZurFilter kz;
    private SavGolFilter sg;

    public void Init(Vector3 position, Quaternion rotation)
    {
        initPos = position;
        initRot = rotation;

        CONSID_ELEMS = WAIT * 2 + 1;

        rawPositions = new ArrayList { Util.V3ToArr(initPos) };
        rawPosTime = new ArrayList { 0f };
        filPositions = new ArrayList { Util.V3ToArr(initPos) };
        filPosTime = new ArrayList { 0f };

        rawQuats = new ArrayList { rotation };
        rawQuatTime = new ArrayList { 0f };
        filQuats = new ArrayList { rotation };
        filQuatTime = new ArrayList { 0f };
        rawAngles = new ArrayList();
        filAngles = new ArrayList();

        kz = new KolZurFilter(WAIT);
        sg = new SavGolFilter(WAIT, 1);

        // clear all debug outputs
        for (int i = 0; i < VEC3_N; ++i)
        {
            File.WriteAllText("before" + i, string.Empty);
            File.WriteAllText("after" + i, string.Empty);
        }
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
            rawPositions[rawPositions.Count - 1] = Util.V3ToArr(position);
            rawPrevTime = rawPosTime.Count >= 2 ? (float)rawPosTime[rawPosTime.Count - 2] : 0;
        }
        else if (positionChanged)
        {
            rawPositions.Add(Util.V3ToArr(position));
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
                filPositions.Add(Util.V3ToArr(initPos));
                filPosTime.Add(time);
            }
            else
            {
                filPositions[filPositions.Count - 1] = Util.V3ToArr(initPos);
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
            float[] predict = ABG.Predict(filPositions.GetRange(startIndex, elemsCnt), filPosTime.GetRange(startIndex, elemsCnt), VEC3_N);
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
            //return ArrToV3(ref laggedPos);
            return position;
        }

        ArrayList posWindow = rawPositions.GetRange(rawPositions.Count - CONSID_ELEMS, CONSID_ELEMS);
        float[] filtered = kz.Filter(ref posWindow, 2, VEC3_N);
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

        return Util.ArrToV3(ref filtered);
    }

    public static Quaternion ExtrapolateRotation(Quaternion from, Quaternion to, float factor)
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

    public static float[] QuatsToAngle(Quaternion from, Quaternion to)
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
        // Debug.Log($"time: {rawPrevTime} -> {time} = {time - rawPrevTime}; rotationChanged = {rotationChanged}; quat: {prev.ToString("f5")} -> {rotation.ToString("f5")}");
        // Debug.Log($"time: {time}, rotationChanged={rotationChanged}; multiple={multipleFrames}");
        if (multipleFrames)
        {
            rawQuats[rawQuats.Count - 1] = rotation;
            rawPrevTime = rawQuatTime.Count >= 2 ? (float)rawQuatTime[rawQuatTime.Count - 2] : 0;
            prev = rawQuats.Count >= 2 ? (Quaternion)rawQuats[rawQuats.Count - 2] : initRot;
            rawAngles[rawAngles.Count - 1] = QuatsToAngle(prev, rotation);
            // Debug.Log($"mul raw {Util.ObjectArrsToString(ref rawAngles, ANGLE_N)}");
        }
        else if (rotationChanged)
        {
            rawQuats.Add(rotation);
            rawQuatTime.Add(time);
            rawAngles.Add(QuatsToAngle(prev, rotation));
            // Debug.Log($"raw {Util.ObjectArrsToString(ref rawAngles, ANGLE_N)}");
        }
        if (rawAngles.Count < CONSID_ELEMS)
        {
            if (!multipleFrames)
            {
                filQuats.Add(initRot);
                filAngles.Add(QuatsToAngle(initRot, initRot));
                filQuatTime.Add(time);
                // Debug.Log($"fil {Util.ObjectArrsToString(ref filAngles, ANGLE_N)}");
            }
            else
            {
                filQuats[filQuats.Count - 1] = initRot;
                // filAngles.Count > 0 guaranteed
                filAngles[filAngles.Count - 1] = QuatsToAngle(initRot, initRot);
                // time is identical to last element
                // Debug.Log($"mul fil {Util.ObjectArrsToString(ref filAngles, ANGLE_N)}");
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
            float[] predict = ABG.Predict(filAngles.GetRange(startIndex, elemsCnt), filQuatTime.GetRange(startIndex + 1, elemsCnt), ANGLE_N);
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
            return rotation;
        }

        ArrayList fixedWindow = rawAngles.GetRange(rawAngles.Count - CONSID_ELEMS, CONSID_ELEMS);
        float filteredAngle = sg.Filter(ref fixedWindow, ANGLE_N)[0];
        float angle = (rawAngles[rawAngles.Count - 1 - WAIT] as float[])[0];
        float factor = Math.Abs(angle) < EPS ? 0 : filteredAngle / angle;
        // Debug.Log("Res: " + filteredAngle + " / " + angle + " = " + factor);
        Quaternion result = ExtrapolateRotation((Quaternion)rawQuats[rawQuats.Count - WAIT - 2], (Quaternion)rawQuats[rawQuats.Count - WAIT - 1], factor);
        if (!multipleFrames)
        {
            filQuats.Add(result);
            filAngles.Add(filteredAngle);
            filQuatTime.Add(time);
            // Debug.Log($"fil {Util.ObjectArrsToString(ref filAngles, ANGLE_N)}");
            // Debug.Log($"raws: {Util.ObjectArrsToString(ref fixedWindow, ANGLE_N)}");
            // Debug.Log($"time: {time}: {(rawAngles[rawAngles.Count - 1 - WAIT] as float[])[0].ToString("f7")} -> {(filAngles[filAngles.Count - 1 - WAIT] as float[])[0].ToString("f7")}");
        }
        else
        {
            filQuats[filQuats.Count - 1] = result;
            filAngles[filAngles.Count - 1] = filteredAngle;
            // Debug.Log($"mul fil {Util.ObjectArrsToString(ref filAngles, ANGLE_N)}");
            // time is identical to last element in ArrayList
        }
        return result;
    }
}