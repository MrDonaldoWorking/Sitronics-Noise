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
    private ArrayList quatNois;
    private ArrayList quatFilt;

    private readonly string statsFileName = "allStats";
    private readonly string statsDirName = "stats";
    private readonly string csvDirName = "csv";

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

        genVec = new NoiseGenerator(0.05f, 0f);
        genQuat = new NoiseGenerator(10f, 0f);

        vecTime = new ArrayList();
        quatTime = new ArrayList();
        vecDist = new ArrayList();
        quatDist = new ArrayList();

        compared = false;

        vecNois = new ArrayList();
        vecFilt = new ArrayList();
        quatNois = new ArrayList();
        quatFilt = new ArrayList();

        System.IO.File.WriteAllText($"{statsDirName}/{statsFileName}", string.Empty);
        // Toggle this to start new research
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

    private Vector3 NoiseVec3(Vector3 vec)
    {
        Vector3 res = new Vector3(vec.x, vec.y, vec.z);
        for (int i = 0; i < 3; ++i)
        {
            res[i] += genVec.Noise();
        }
        return res;
    }

    private Quaternion NoiseQuat(Quaternion quat)
    {
        Quaternion res = new Quaternion(quat.x, quat.y, quat.z, quat.w);
        Vector3 noiseAngle = new Vector3(0f, 0f, 0f);
        for (int i = 0; i < 3; ++i)
        {
            noiseAngle[i] += genQuat.Noise();
        }

        return res * Quaternion.Euler(noiseAngle);
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
        return casted[casted.Count / 2];
    }

    private float readAndGetMean(string fileName)
    {
        return getMean(System.IO.File.ReadAllText(fileName));
    }

    private float readAndGetMedian(string fileName)
    {
        return getMedian(System.IO.File.ReadAllText(fileName));
    }

    private void WriteArrayListFloat(ref ArrayList times, string fileName, string typeName)
    {
        if (!System.IO.Directory.Exists(statsDirName))
        {
            System.IO.Directory.CreateDirectory(statsDirName);
        }
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
        if (!System.IO.Directory.Exists(csvDirName))
        {
            System.IO.Directory.CreateDirectory(csvDirName);
        }
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

    void Update()
    {
        if (_lastSentFrame >= _recordedData.Count)
        {
            if (compared)
            {
                return;
            }
            WriteArrayListFloat(ref vecTime, "vecTime", "s");
            WriteArrayListFloat(ref quatTime, "quatTime", "s");
            WriteArrayListFloat(ref vecDist, "vecDist", "mm");
            WriteArrayListFloat(ref quatDist, "quatDist", "deg");
            WriteFullInfo(ref vecNois, ref quatNois, "noisedFull");
            WriteFullInfo(ref vecFilt, ref quatFilt, "filteredFull");
            compared = true;
            return;
        }

        _currentPlayedTime += Time.deltaTime;

        var currentFrame = (int)(_currentPlayedTime * SpeedFramesPerSecond);
        if (currentFrame == _lastSentFrame)
        {
            // Stopwatch timer = Stopwatch.StartNew();
            transform.position = _filter.FilterPosition(_currentPlayedTime, transform.position, false);
            // timer.Stop();
            // long posMillis = timer.ElapsedMillis;

            // timer = Stopwatch.StartNew();
            transform.rotation = _filter.FilterRotation(_currentPlayedTime, transform.rotation, false);
            // timer.Stop(); 
            // long rotMillis = timer.ElapsedMillis;
            return;
        }

        while (currentFrame > _lastSentFrame)
        {
            _lastSentFrame++;

            Vector3 vecB = _recordedData[_lastSentFrame % _recordedData.Count].Position;
            Vector3 vecN = NoiseVec3(vecB);
            vecNois.Add(vecN);
            Stopwatch timer = Stopwatch.StartNew();
            transform.position = _filter.FilterPosition(_currentPlayedTime, vecN, true);
            timer.Stop();
            vecTime.Add(timer.Elapsed.Milliseconds / 1000f);
            Vector3 vecA = transform.position;
            vecFilt.Add(vecA);
            vecDist.Add(DistVec3(vecA, vecB));

            Quaternion quatB = _recordedData[_lastSentFrame % _recordedData.Count].Rotation;
            Quaternion quatN = NoiseQuat(quatB);
            quatNois.Add(quatN);
            timer = Stopwatch.StartNew();
            transform.rotation = _filter.FilterRotation(_currentPlayedTime, quatN, true);
            timer.Stop();
            quatTime.Add(timer.Elapsed.Milliseconds / 1000f);
            Quaternion quatA = transform.rotation;
            quatFilt.Add(quatA);
            quatDist.Add(DistQuat(quatA.normalized, quatB.normalized));
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
