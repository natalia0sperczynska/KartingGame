using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TelemetryRingBuffer 
{
    private readonly float[] _data;
    private int   _head;    
    private int   _count;       
 
    public int   Capacity => _data.Length;
    public int   Count    => _count;
    public float Min      { get; private set; } =  float.MaxValue;
    public float Max      { get; private set; } = -float.MaxValue;
 
    public TelemetryRingBuffer(int capacity)
    {
        _data = new float[capacity];
    }
 
    public void Push(float value)
    {
        _data[_head] = value;
        _head = (_head + 1) % Capacity;
        if (_count < Capacity) _count++;
 
        if (value < Min) Min = value;
        if (value > Max) Max = value;
    }
 

    public float Get(int chronoIndex)
    {
        int offset = (_head - _count + chronoIndex + Capacity * 2) % Capacity;
        return _data[offset];
    }
 
    public void RecalculateMinMax()
    {
        Min =  float.MaxValue;
        Max = -float.MaxValue;
        for (int i = 0; i < _count; i++)
        {
            float v = Get(i);
            if (v < Min) Min = v;
            if (v > Max) Max = v;
        }
    }
 
    public void Clear()
    {
        _head  = 0;
        _count = 0;
        Min    =  float.MaxValue;
        Max    = -float.MaxValue;
    }
}
