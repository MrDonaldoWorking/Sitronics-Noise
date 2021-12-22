using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public class CameraMovementRepeater : MonoBehaviour
{
    public TextAsset DataAsset;
    public float SpeedFramesPerSecond = 10;

    private Filter _filter;
    private List<RecordedTransform> _recordedData;

    private float _currentPlayedTime = 0f;
    private int _lastSentFrame = -1;

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
    }

    private float ParseData(string s)
    {
        // Debug.Log(s);
        var filteredData = s.Trim();
        return float.Parse(filteredData, CultureInfo.InvariantCulture);
    }

    void Update()
    {
        _currentPlayedTime += Time.deltaTime;

        var currentFrame = (int)(_currentPlayedTime * SpeedFramesPerSecond);
        if (currentFrame == _lastSentFrame)
        {
            transform.position = _filter.FilterPosition(_currentPlayedTime, transform.position, false);
            transform.rotation = _filter.FilterRotation(_currentPlayedTime, transform.rotation, false);
            return;
        }

        while (currentFrame > _lastSentFrame)
        {
            _lastSentFrame++;

            transform.position = _filter.FilterPosition(_currentPlayedTime, _recordedData[_lastSentFrame % _recordedData.Count].Position, true);
            transform.rotation = _filter.FilterRotation(_currentPlayedTime, _recordedData[_lastSentFrame % _recordedData.Count].Rotation, true);
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
