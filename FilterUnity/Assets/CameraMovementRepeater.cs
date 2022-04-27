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

        genVec = new NoiseGenerator(0f, 0f);
        genQuat = new NoiseGenerator(0f, 0f);

        vecTime = new ArrayList();
        quatTime = new ArrayList();
        vecDist = new ArrayList();
        quatDist = new ArrayList();
        compared = false;
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
        for (int i = 0; i < 4; ++i)
        {
            res[i] += genQuat.Noise();
        }
        return res;
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

    private void WriteArrayListFloat(StreamWriter writer, ArrayList times)
    {
        times.RemoveAt(times.Count - 1);
        float sum = 0, maxTime = 0;
        foreach (float time in times)
        {
            sum += time;
            maxTime = Math.Max(maxTime, time);
            writer.WriteLine(time);
        }
        writer.WriteLine($"mean: {sum / times.Count}");
        writer.WriteLine($"max: {maxTime}");
    }

    void Update()
    {
        if (_lastSentFrame >= _recordedData.Count)
        {
            if (compared)
            {
                return;
            }
            using (StreamWriter writer = new StreamWriter("vecTime", true))
            {
                WriteArrayListFloat(writer, vecTime);
            }
            using (StreamWriter writer = new StreamWriter("quatTime", true))
            {
                WriteArrayListFloat(writer, quatTime);
            }
            using (StreamWriter writer = new StreamWriter("vecDist", true))
            {
                WriteArrayListFloat(writer, vecDist);
            }
            using (StreamWriter writer = new StreamWriter("quatDist", true))
            {
                WriteArrayListFloat(writer, quatDist);
            }
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
            Stopwatch timer = Stopwatch.StartNew();
            transform.position = _filter.FilterPosition(_currentPlayedTime, vecN, true);
            timer.Stop();
            vecTime.Add(timer.Elapsed.Milliseconds / 1000f);
            Vector3 vecA = transform.position;
            vecDist.Add(DistVec3(vecA, vecB));

            Quaternion quatB = _recordedData[_lastSentFrame % _recordedData.Count].Rotation;
            Quaternion quatN = NoiseQuat(quatB);
            timer = Stopwatch.StartNew();
            transform.rotation = _filter.FilterRotation(_currentPlayedTime, quatN, true);
            timer.Stop();
            quatTime.Add(timer.Elapsed.Milliseconds / 1000f);
            Quaternion quatA = transform.rotation;
            quatDist.Add(DistQuat(quatA, quatB));
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
