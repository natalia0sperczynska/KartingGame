using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class SurfaceDetector : MonoBehaviour
{

    public RaceManager raceManager;  
    [System.Serializable]
    
    public struct SurfaceProfile
    {
        public string tag;
        public float airDrag;                
        public float frictionMultiplier;
        public float torqueMultiplier;
        public ParticleSystem dustEffect;   
        
    }

    public SurfaceProfile road = new SurfaceProfile
    {
        tag              = "Road",
        airDrag          = 0.01f,
        frictionMultiplier = 1.0f,
        torqueMultiplier   = 1.0f,
    };

    public SurfaceProfile grass = new SurfaceProfile
    {
        tag              = "Grass",
        airDrag          = 0.15f,     
        frictionMultiplier = 0.45f,   
        torqueMultiplier   = 0.65f,   
    };

    public SurfaceProfile dirt = new SurfaceProfile
    {
        tag              = "Dirt",
        airDrag          = 0.08f,
        frictionMultiplier = 0.65f,
        torqueMultiplier   = 0.80f,
    };

    public SurfaceProfile defaultSurface;

    public float raycastDistance = 0.8f;
    public LayerMask groundLayers = ~0;         

    public float transitionSpeed = 4f;

    private Rigidbody         _rb;
    private CarController _car;
    private WheelCollider[]   _wheels;

    private SurfaceProfile _currentProfile;
    private SurfaceProfile _targetProfile;
    private float          _currentFriction = 1f;

    public string CurrentSurfaceTag { get; private set; } = "Road";
    public bool   IsOnRoad          => CurrentSurfaceTag == "Road";

    void Awake()
    {
        _rb  = GetComponent<Rigidbody>();
        _car = GetComponent<CarController>();

        defaultSurface = road; 

        if (_car != null)
        {
            _wheels = new WheelCollider[]
            {
                _car.wheelFL, _car.wheelFR, _car.wheelRL, _car.wheelRR
            };
        }

        _currentProfile = road;
        _targetProfile  = road;
    }

    void FixedUpdate()
    {
        DetectSurface();
        ApplySurfacePhysics();
    }

    void DetectSurface()
    {
        int roadCount  = 0;
        int grassCount = 0;
        int dirtCount  = 0;

        Vector3[] rayOrigins = _car != null
            ? new[]
            {
                _car.wheelFL.transform.position,
                _car.wheelFR.transform.position,
                _car.wheelRL.transform.position,
                _car.wheelRR.transform.position,
            }
            : new[] { transform.position };

        foreach (var origin in rayOrigins)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin + Vector3.up * 0.1f, Vector3.down,
                                out hit, raycastDistance + _car?.tireDiameter * 0.5f ?? 0.3f,
                                groundLayers))
            {
                string t = hit.collider.tag;
                if      (t == road.tag)  roadCount++;
                else if (t == grass.tag) grassCount++;
                else if (t == dirt.tag)  dirtCount++;
            }
        }
        if (grassCount > roadCount && grassCount >= dirtCount)
        {
            _targetProfile   = grass;
            CurrentSurfaceTag = grass.tag;
        }
        else if (dirtCount > roadCount)
        {
            _targetProfile   = dirt;
            CurrentSurfaceTag = dirt.tag;
        }
        else
        {
            _targetProfile   = road;
            CurrentSurfaceTag = road.tag;
        }
    }
    void ApplySurfacePhysics()
    {
        _currentFriction = Mathf.Lerp(
            _currentFriction,
            _targetProfile.frictionMultiplier,
            transitionSpeed * Time.fixedDeltaTime
        );

        _rb.drag = Mathf.Lerp(
            _rb.drag,
            _targetProfile.airDrag,
            transitionSpeed * Time.fixedDeltaTime
        );


        if (_wheels == null || _car == null) return;

        foreach (var wheel in _wheels)
        {
            if (wheel == null) continue;
            WheelFrictionCurve fwd = wheel.forwardFriction;
            fwd.extremumValue  = _car.tireExtremumValue  * _car.tireConditionScale * _currentFriction;
            fwd.asymptoteValue = _car.tireAsymptoteValue * _car.tireConditionScale * _currentFriction;
            wheel.forwardFriction = fwd;

            WheelFrictionCurve side = wheel.sidewaysFriction;
            side.extremumValue  = _car.tireExtremumValue  * 0.88f * _car.tireConditionScale * _currentFriction;
            side.asymptoteValue = _car.tireAsymptoteValue * 0.85f * _car.tireConditionScale * _currentFriction;
            wheel.sidewaysFriction = side;
        }

        _currentProfile = _targetProfile;
    }
    public float GetTorqueMultiplier() => _targetProfile.torqueMultiplier;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = IsOnRoad ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.1f,
                       Vector3.down * raycastDistance);
    }
}
