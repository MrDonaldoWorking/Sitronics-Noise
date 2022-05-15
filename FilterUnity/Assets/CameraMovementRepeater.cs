using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System;
using UnityEngine;

public class CameraMovementRepeater : MonoBehaviour
{
    public TextAsset DataAsset;
    public float SpeedFramesPerSecond = 10;

    private Filter _filter;
    private List<RecordedTransform> _recordedData;

    private float _currentPlayedTime = 0f;
    private int _lastSentFrame = -1;

    private NoiseGenerator genVec;
    private NoiseGenerator genQuat;

    private ArrayList vecTime;
    private ArrayList quatTime;
    private ArrayList vecDist;
    private ArrayList quatDist;
    private bool compared;

    private ArrayList vecNois;
    private ArrayList vecFilt;
    private ArrayList vecReal;
    private ArrayList quatNois;
    private ArrayList quatFilt;
    private ArrayList quatReal;
    private ArrayList times;

    private readonly string statsFileName = "allStats";
    private readonly string statsDirName = "stats";
    private readonly string csvDirName = "csv";
    private readonly string bestDirName = "best";
    private string algoName;

    private ArrayList qnDiff;

    void Start()
    {
        var text = DataAsset.text;
        var textLines = text.Replace("\r\n", "\n").Split('\n');

        _recordedData = textLines
            .Select(tl => tl.Split(','))
            .Skip(1)
            .Where(s => s.Length > 1)
            .Select(tl => tl.Select(ParseData).ToArray())
            .Select(dataElement => new RecordedTransform(dataElement))
            .ToList();

        transform.position = _recordedData.First().Position;
        transform.rotation = _recordedData.First().Rotation;

        _filter = new Filter();
        _filter.Init(transform.position, transform.rotation);

        genVec = new NoiseGenerator(0f, 0.9f, 100);
        genQuat = new NoiseGenerator(0f, 10f, 100);

        vecTime = new ArrayList();
        quatTime = new ArrayList();
        vecDist = new ArrayList();
        quatDist = new ArrayList();

        compared = false;

        vecNois = new ArrayList();
        vecFilt = new ArrayList();
        vecReal = new ArrayList();
        quatNois = new ArrayList();
        quatFilt = new ArrayList();
        quatReal = new ArrayList();
        times = new ArrayList();

        qnDiff = new ArrayList();

        System.IO.File.WriteAllText($"{statsDirName}/{statsFileName}", string.Empty);
        System.IO.File.WriteAllText("defect", string.Empty);
        System.IO.File.WriteAllText("sysdefect", string.Empty);
        // Toggle this to start new research
        algoName = "WMedian Gauss neig=6 Wait=7";
        bool newResearch = true;
        if (newResearch)
        {
            string[] types = { "vec", "quat" };
            string[] researches = { "Time", "Dist" };
            string[] results = { "mean", "max" };
            foreach (string type in types)
            {
                foreach (string research in researches)
                {
                    foreach (string result in results)
                    {
                        System.IO.File.WriteAllText($"{statsDirName}/{type}{research}{result}", string.Empty);
                    }
                }
            }
        }
    }

    private float ParseData(string s)
    {
        // Debug.Log(s);
        var filteredData = s.Trim();
        return float.Parse(filteredData, CultureInfo.InvariantCulture);
    }

    private Vector3 NoiseVec3(Vector3 vec, int ind)
    {
        Vector3 res = new Vector3(vec.x, vec.y, vec.z);
        for (int i = 0; i < 3; ++i)
        {
            res[i] += genVec.Noise();
            if (genVec.GetHappened())
            {
                System.IO.File.AppendAllText("sysdefect", $"{ind}\n");
            }
        }
        return res;
    }

    private Quaternion NoiseQuat(Quaternion quat, int ind)
    {
        Quaternion res = new Quaternion(quat.x, quat.y, quat.z, quat.w);
        Vector3 noiseAngle = new Vector3(0f, 0f, 0f);
        for (int i = 0; i < 3; ++i)
        {
            noiseAngle[i] += genQuat.Noise();
            if (genQuat.GetHappened())
            {
                System.IO.File.AppendAllText("sysdefect", $"{ind}\n");
            }
        }

        return Quaternion.Normalize(res * Quaternion.Euler(noiseAngle));
    }

    private float DistVec3(Vector3 a, Vector3 b)
    {
        float ans = 0;
        for (int i = 0; i < 3; ++i)
        {
            ans += (float)Math.Pow(a[i] - b[i], 2);
        }
        return (float)Math.Pow(ans, 0.5);
    }

    private float DistQuat(Quaternion a, Quaternion b)
    {
        Vector3 forwardA = a * Vector3.forward;
        Vector3 forwardB = b * Vector3.forward;
        return Vector3.Angle(forwardA, forwardB);
    }

    // line divided by whitespaces
    private float getMean(string line)
    {
        string[] vals = line.Split(' ');
        int cnt = 0;
        float sum = 0;
        for (int i = 0; i < vals.Length; ++i)
        {
            try
            {
                sum += float.Parse(vals[i]);
                ++cnt;
            }
            catch (FormatException e)
            { // ignored
                UnityEngine.Debug.LogWarning($"FormatException: {vals[i]}, message: {e.Data}");
            }
        }
        return sum / cnt;
    }

    private float getMedian(string line)
    {
        string[] vals = line.Split(' ');
        List<float> casted = new List<float>();
        for (int i = 0; i < vals.Length; ++i)
        {
            try
            {
                casted.Add(float.Parse(vals[i]));
            }
            catch (FormatException e)
            {
                UnityEngine.Debug.LogWarning($"FormatException: {e}");
            }
        }
        casted.Sort();
        return casted[0];
    }

    private float readAndGetMean(string fileName)
    {
        return getMean(System.IO.File.ReadAllText(fileName));
    }

    private float readAndGetMedian(string fileName)
    {
        return getMedian(System.IO.File.ReadAllText(fileName));
    }

    private void CreateDirIfNotExists(string name)
    {
        if (!System.IO.Directory.Exists(name))
        {
            System.IO.Directory.CreateDirectory(name);
        }
    }

    private void WriteArrayListFloat(ref ArrayList times, string fileName, string typeName)
    {
        CreateDirIfNotExists(statsDirName);
        using (StreamWriter writer = new StreamWriter($"{statsDirName}/{fileName}"))
        {
            times.RemoveAt(times.Count - 1);
            float sum = 0, maxTime = 0;
            foreach (float time in times)
            {
                float curTime = typeName == "mm" ? time * 1000 : time;
                sum += curTime;
                maxTime = Math.Max(maxTime, curTime);
                writer.WriteLine(curTime);
            }
            using (StreamWriter all = new StreamWriter($"{statsDirName}/{statsFileName}", true))
            {
                float currMean = sum / times.Count;
                writer.WriteLine($"mean: {currMean} {typeName}");
                string meanPath = $"{statsDirName}/{fileName}mean";
                string maxPath = $"{statsDirName}/{fileName}max";
                System.IO.File.AppendAllText(meanPath, currMean + " ");
                writer.WriteLine($"max: {maxTime} {typeName}");
                System.IO.File.AppendAllText(maxPath, maxTime + " ");
                writer.WriteLine($"Generated at: {DateTime.Now}");

                all.WriteLine(fileName);
                all.WriteLine($"mean: {readAndGetMean(meanPath).ToString("f7")} {typeName}");
                all.WriteLine($"max: {readAndGetMedian(maxPath).ToString("f7")} {typeName}");
            }
        }
    }

    private void WriteFullInfo(ref ArrayList vec, ref ArrayList quat, string fileName)
    {
        CreateDirIfNotExists(csvDirName);
        using (StreamWriter writer = new StreamWriter($"{csvDirName}/{fileName}.csv"))
        {
            writer.WriteLine("PosX,PosY,PosZ,RotX,RotY,RotZ,RotW,RotA");
            // RotA - rotation angle, quaternion difference
            for (int i = 0; i < vec.Count; ++i)
            {
                Vector3 v3 = (Vector3)vec[i];
                Quaternion qt = (Quaternion)quat[i];
                Quaternion prev = (i == 0 ? new Quaternion(0, 0, 0, 1) : (Quaternion)quat[i - 1]);
                float angle = (i == 0 ? 0f : Quaternion.Angle(prev, qt));
                writer.WriteLine($"{v3.x},{v3.y},{v3.z},{qt.x},{qt.y},{qt.z},{qt.w},{angle}");
            }
            // writer.WriteLine($"Generated at: {DateTime.Now}");
        }
    }

    public class Stats
    {
        public float mean { get; set; }
        public float max { get; set; }
        public string algo { get; set; }
    }

    private void CollectResults()
    {
        string[] lines = System.IO.File.ReadAllLines($"{statsDirName}/{statsFileName}");
        // time, vec, quat; mean, max
        float[,] results = new float[3, 2];
        for (int j = 0; j < 2; ++j)
        {
            results[0, j] = float.Parse(lines[1 + j].Split(' ')[1]) + float.Parse(lines[4 + j].Split(' ')[1]);
        }
        for (int i = 1; i < 3; ++i)
        {
            for (int j = 0; j < 2; ++j)
            {
                results[i, j] = float.Parse(lines[7 + (i - 1) * 3 + j].Split(' ')[1]);
            }
        }
        Stats[] curr = new Stats[3];
        for (int q = 0; q < 3; ++q)
        {
            UnityEngine.Debug.LogWarning($"stats[{q}]: {results[q, 0]}; {results[q, 1]}");
            curr[q] = new Stats();
            curr[q].mean = results[q, 0];
            curr[q].max = results[q, 1];
            curr[q].algo = algoName;
            UnityEngine.Debug.Log($"{curr[q].algo}: {curr[q].mean}; {curr[q].max}");
        }

        CreateDirIfNotExists(bestDirName);
        string[] types = { "Time", "Vec", "Quat" };
        string[] measures = { "ms", "mm", "deg" };
        for (int q = 0; q < 3; ++q)
        {
            string currBestPath = $"{bestDirName}/best{types[q]}";
            string[] bests = System.IO.File.Exists(currBestPath) ? System.IO.File.ReadAllLines(currBestPath) : new string[0];
            List<Stats> statList = new List<Stats>();
            foreach (string line in bests)
            {
                Stats stat = new Stats();
                // <Algo name>: <mean> <measure>; <max> <measure>
                string[] statStr = line.Split(':')[1].Split(';');
                stat.mean = float.Parse(statStr[0].Split(' ')[1]);
                stat.max = float.Parse(statStr[1].Split(' ')[1]);
                stat.algo = line.Split(':')[0];
                if (stat.algo == algoName)
                {
                    if (stat.mean < curr[q].mean)
                    {
                        curr[q] = stat;
                    }
                }
                else
                {
                    statList.Add(stat);
                }
            }
            statList.Add(curr[q]);
            statList.Sort(delegate (Stats a, Stats b) { return a.mean.CompareTo(b.mean); });

            using (StreamWriter writer = new StreamWriter(currBestPath))
            {
                foreach (Stats stat in statList)
                {
                    writer.WriteLine($"{stat.algo}: {stat.mean} {measures[q]}; {stat.max} {measures[q]}");
                }
            }
        }
    }

    private void WriteErrorsInfo()
    {
        using (StreamWriter writer = new StreamWriter($"{csvDirName}/errors.csv"))
        {
            writer.WriteLine("Pos,Rot");
            for (int i = 0; i < vecDist.Count; ++i)
            {
                writer.WriteLine($"{vecDist[i]},{quatDist[i]}");
            }
        }
    }

    void Update()
    {
        if (_lastSentFrame >= _recordedData.Count)
        {
            if (compared)
            {
                return;
            }
            WriteArrayListFloat(ref vecTime, "vecTime", "ms");
            WriteArrayListFloat(ref quatTime, "quatTime", "ms");
            WriteArrayListFloat(ref vecDist, "vecDist", "mm");
            WriteArrayListFloat(ref quatDist, "quatDist", "deg");
            WriteFullInfo(ref vecNois, ref quatNois, "noisedFull");
            WriteFullInfo(ref vecFilt, ref quatFilt, "filteredFull");
            WriteErrorsInfo();
            CollectResults();
            compared = true;
            return;
        }

        _currentPlayedTime += Time.deltaTime;

        var currentFrame = (int)(_currentPlayedTime * SpeedFramesPerSecond);
        if (currentFrame == _lastSentFrame)
        {
            // Stopwatch timer = Stopwatch.StartNew();
            // transform.position = _filter.FilterPosition(_currentPlayedTime, transform.position, false);
            // timer.Stop();
            // long posMillis = timer.ElapsedMillis;

            // timer = Stopwatch.StartNew();
            // transform.rotation = _filter.FilterRotation(_currentPlayedTime, transform.rotation, false);
            // timer.Stop(); 
            // long rotMillis = timer.ElapsedMillis;
            return;
        }

        while (currentFrame > _lastSentFrame)
        {
            _lastSentFrame++;

            times.Add(_currentPlayedTime);
            Vector3 vecB = _recordedData[_lastSentFrame % _recordedData.Count].Position;
            vecReal.Add(vecB);
            Vector3 vecN = NoiseVec3(vecB, vecReal.Count);
            vecNois.Add(vecN);
            Stopwatch timer = Stopwatch.StartNew();
            transform.position = _filter.FilterPosition(_currentPlayedTime, vecN, true);
            timer.Stop();
            // Accroding to https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch.elapsed?view=net-6.0
            vecTime.Add(timer.Elapsed.Milliseconds / 10f);
            Vector3 vecA = transform.position;
            vecFilt.Add(vecA);
            if (vecNois.Count >= _filter.CONSID_ELEMS)
            {
                vecDist.Add(DistVec3(vecA, (Vector3)vecReal[vecReal.Count - 1 - _filter.WAIT]));
            }

            Quaternion quatB = _recordedData[_lastSentFrame % _recordedData.Count].Rotation;
            quatReal.Add(quatB);
            Quaternion quatN = NoiseQuat(quatB, quatReal.Count);
            quatNois.Add(quatN);
            timer = Stopwatch.StartNew();
            transform.rotation = _filter.FilterRotation(_currentPlayedTime, quatN, true);
            /** Experimental Code
            qnDiff.Add(quatNois.Count <= 1 ? Filter.QuatsToAngle(_recordedData.First().Rotation, quatN) : Filter.QuatsToAngle((Quaternion)quatNois[quatNois.Count - 2], quatN));
            if (qnDiff.Count < CONSID_ELEMS)
            {
                // transform.rotation = _recordedData.First().Rotation;
            }
            else
            {
                ArrayList angleWindow = qnDiff.GetRange(qnDiff.Count - CONSID_ELEMS, CONSID_ELEMS);
                ArrayList timeWindow = times.GetRange(times.Count - CONSID_ELEMS, CONSID_ELEMS);
                float filAngle = KernelFilter.Gaussian(ref angleWindow, ref timeWindow, Util.MeanDiff(ref times, 3), Filter.ANGLE_N)[0];
                float rawAngle = (qnDiff[qnDiff.Count - WAIT - 1] as float[])[0];
                UnityEngine.Debug.Log($"time: {_currentPlayedTime}, Angles: {Util.ObjectArrsToString(ref angleWindow, Filter.ANGLE_N)}");
                UnityEngine.Debug.Log($"res: {filAngle}; / {rawAngle} = {filAngle / rawAngle}");
                // transform.rotation = Filter.ExtrapolateRotation((Quaternion)quatNois[quatNois.Count - WAIT - 2], (Quaternion)quatNois[quatNois.Count - WAIT - 1], filAngle / rawAngle);
                UnityEngine.Debug.Log($"was: {Quaternion.Angle((Quaternion)quatNois[quatNois.Count - WAIT - 2], (Quaternion)quatNois[quatNois.Count - WAIT - 1])}, now: {Quaternion.Angle((Quaternion)quatFilt[quatFilt.Count - WAIT - 2], transform.rotation)}");
            } */
            timer.Stop();
            quatTime.Add(timer.Elapsed.Milliseconds / 10f);
            Quaternion quatA = transform.rotation;
            quatFilt.Add(quatA);
            if (quatNois.Count >= _filter.CONSID_ELEMS)
            {
                quatDist.Add(DistQuat(quatA.normalized, ((Quaternion)quatReal[quatReal.Count - 1 - _filter.WAIT]).normalized));
            }
        }
    }
}

public class RecordedTransform
{
    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; }

    public RecordedTransform(float px, float py, float pz, float rx, float ry, float rz, float rw)
    {
        Position = new Vector3(px, py, pz);
        Rotation = new Quaternion(rx, ry, rz, rw);
    }

    public RecordedTransform(float[] data) : this(data[0], data[1], data[2], data[3], data[4], data[5], data[6])
    {

    }
}
