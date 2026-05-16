using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TelemetryHUD : MonoBehaviour
{
    public CarController car;
    public TextMeshProUGUI speedValue;         
    public TextMeshProUGUI gearValue;        
 
    public TextMeshProUGUI downforceValue;     
    public TextMeshProUGUI dragValue;         
    public TextMeshProUGUI slipValue;          
    public TextMeshProUGUI tcValue;            
    public TextMeshProUGUI absValue;           

    public TelemetryLineGraph speedGraph;
    public TelemetryLineGraph slipGraph;
    public TelemetryLineGraph downforceGraph;
 
    public float sampleRate = 10f;
    public float historySeconds = 12f;
    public float rangeRecalcInterval = 2f;

    static readonly Color COL_NORMAL  = new Color(0.80f, 0.80f, 0.80f);
    static readonly Color COL_GOOD    = new Color(0.29f, 0.87f, 0.50f); 
    static readonly Color COL_WARN    = new Color(0.98f, 0.57f, 0.23f);
    static readonly Color COL_ALERT   = new Color(0.95f, 0.30f, 0.30f); 

    private TelemetryRingBuffer _speedBuf;
    private TelemetryRingBuffer _slipBuf;
    private TelemetryRingBuffer _downforceBuf;
 
    private float _sampleTimer;
    private float _rangeTimer;
    private float _sampleInterval;
    private float _displayTimer; 
    public float displayUpdateInterval = 0.2f;
 
    private float _lastSpeedKmH    = -1f;
    private float _lastDownforce   = -1f;
    private float _lastDrag        = -1f;
    private float _lastSlip        = -1f;
    private float _lastTcFactor    = -1f;

    public void Initialize(CarController targetCar)
    {
        car = targetCar; 

        _sampleInterval = 1f / sampleRate;
        int cap = Mathf.RoundToInt(historySeconds * sampleRate);
 
        _speedBuf     = new TelemetryRingBuffer(cap);
        _slipBuf      = new TelemetryRingBuffer(cap);
        _downforceBuf = new TelemetryRingBuffer(cap);
 
        if (speedGraph)
        {
            speedGraph.SetBuffer(_speedBuf);
            speedGraph.autoRange = false;
            speedGraph.manualMin = 0f;
            speedGraph.manualMax = 150f;   
        }
        if (slipGraph)
        {
            slipGraph.SetBuffer(_slipBuf);
            slipGraph.autoRange = false;
            slipGraph.manualMin = 0f;
            slipGraph.manualMax = 30f;     
        }
        if (downforceGraph)
        {
            downforceGraph.SetBuffer(_downforceBuf);
            downforceGraph.autoRange = true;
        }
    }
 
    void Update()
    {
        if (car == null || _speedBuf == null) return;

        _sampleTimer += Time.deltaTime;
        if (_sampleTimer >= _sampleInterval)
        {
            _sampleTimer -= _sampleInterval;
            Sample();
        }

        _displayTimer += Time.deltaTime;
        if (_displayTimer >= displayUpdateInterval)
        {
            _displayTimer = 0f;
            UpdateDisplays(); 
        }

        _rangeTimer += Time.deltaTime;
        if (_rangeTimer >= rangeRecalcInterval)
        {
            _rangeTimer = 0f;
            _speedBuf?.RecalculateMinMax();
            _slipBuf?.RecalculateMinMax();
            _downforceBuf?.RecalculateMinMax();
        }
    }
 
    void Sample()
    {
        float speedKmH  = car.debugSpeedKmH;
        float downforce = car.debugDownforceN;
        float drag      = car.debugDragN;
 
        float vCar   = car.debugSpeedKmH / 3.6f;
        float vWheel = ((car.wheelRL.rpm + car.wheelRR.rpm) * 0.5f)
                       * (Mathf.PI * car.tireDiameter) / 60f;
        float slip   = vCar > 0.5f
            ? Mathf.Abs((vWheel - vCar) / vCar) * 100f
            : 0f;
 
        _speedBuf.Push(speedKmH);
        _slipBuf.Push(slip);
        _downforceBuf.Push(downforce);

        speedGraph?.Refresh();
        slipGraph?.Refresh();
        downforceGraph?.Refresh();
    }

    void UpdateDisplays()
    {
        float spd = car.debugSpeedKmH;
        float df  = car.debugDownforceN;
        float dr  = car.debugDragN;
        float tc  = car.debugTcTorqueFactor;
 
        if (speedValue != null && Mathf.Abs(spd - _lastSpeedKmH) > 0.5f)
        {
            speedValue.text = Mathf.RoundToInt(spd).ToString();
            _lastSpeedKmH = spd;
        }
 
        if (gearValue != null)
            gearValue.text = EstimateGear(spd);
    
        if (downforceValue != null && Mathf.Abs(df - _lastDownforce) > 1f)
        {
            downforceValue.text  = $"{df:F0} N";
            downforceValue.color = df > 100f ? COL_GOOD : COL_NORMAL;
            _lastDownforce = df;
        }
 
        if (dragValue != null && Mathf.Abs(dr - _lastDrag) > 1f)
        {
            dragValue.text  = $"{dr:F0} N";
            dragValue.color = dr > 250f ? COL_WARN : COL_NORMAL;
            _lastDrag = dr;
        }
   
        float vCar   = spd / 3.6f;
        float vWheel = ((car.wheelRL.rpm + car.wheelRR.rpm) * 0.5f)
                       * (Mathf.PI * car.tireDiameter) / 60f;
        float slip   = vCar > 0.5f
            ? Mathf.Abs((vWheel - vCar) / vCar) * 100f : 0f;
 
        if (slipValue != null && Mathf.Abs(slip - _lastSlip) > 0.2f)
        {
            slipValue.text  = $"{slip:F1}%";
            slipValue.color = slip > car.tcSlipThreshold * 100f ? COL_WARN
                            : slip > car.tcMaxSlip * 100f       ? COL_ALERT
                            : COL_NORMAL;
            _lastSlip = slip;
        }
 
        if (tcValue != null && Mathf.Abs(tc - _lastTcFactor) > 0.01f)
        {
            float reductPct = (1f - tc) * 100f;
            bool  cutting   = reductPct > 2f;
            tcValue.text  = car.tcEnabled
                ? (cutting ? $"TC ▼{reductPct:F0}%" : "TC  OK")
                : "TC  ---";
            tcValue.color = !car.tcEnabled ? COL_NORMAL
                           : cutting       ? COL_WARN
                           :                 COL_GOOD;
            _lastTcFactor = tc;
        }
    
        if (absValue != null)
        {
            absValue.text  = car.absEnabled ? "ABS  ON" : "ABS ---";
            absValue.color = car.absEnabled ? COL_GOOD : COL_NORMAL;
        }
    }
 
    static string EstimateGear(float kmh) =>
        kmh < 15f  ? "1" :
        kmh < 35f  ? "2" :
        kmh < 60f  ? "3" :
        kmh < 90f  ? "4" : "5";
}