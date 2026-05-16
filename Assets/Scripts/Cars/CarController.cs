using UnityEngine;
public class CarController : MonoBehaviour
{
    // vehicle config
    public float carMass = 201.0f;
    public float torqueLimit = 33.3f;
    public float gearboxRatio = 12.8f;
    public float maxWheelAngleRad = 0.6f;
    public float steeringAngleDeg = 35.0f;
    public float steeringDamping = 0.1f;
    

    // suspension
    public float frontSpringRate = 13000f;
    public float rearSpringRate = 13000f;
    public float frontDamping = 1000f;
    public float rearDamping = 1000f;
    public float frontWheelTravel = 0.1f;
    public float rearWheelTravel = 0.1f;
    public float antiRollForce = 2000f;

    // tires
    public float tireDiameter = 0.457f;
    public float tireWidth = 0.190f;
    public float tireMass = 3.5f;
    public float rimMass = 0.7f;
    public float tireExtremumSlip   = 0.12f;
    public float tireExtremumValue  = 1.8f;
    public float tireAsymptoteValue = 1.5f;
    public float tireAsymptoteSlip  = 0.5f;
    public float tireConditionScale = 0.8f;
    //aerodynamics
    public float airDensity = 1.225f;
    public float frontWingArea = 0.48f;
    public float frontWingA0   = 0.15f;
    public float frontWingCla  = 4.5f;
    public float frontWingCd0  = 0.08f;
    public float frontWingCda  = 0.4f;
    public float frontWingStall = 0.25f;
    // rear wing
    public float rearWingArea  = 0.36f;
    public float rearWingA0    = 0.35f;
    public float rearWingCla   = 4.5f;
    public float rearWingCd0   = 0.12f;
    public float rearWingCda   = 0.4f;
    public float rearWingStall = 0.25f;
    public float bodyFrontalArea = 0.95f;
    public float bodyCd = 1.0f;
    // trakcking control
    public bool  tcEnabled= true;
    public float tcSlipThreshold= 0.1f;
    public float tcMaxSlip= 0.25f;
    public float tcKp = 1.5f;
    public float tcKi= 0.05f;
    // abs
    public bool  absEnabled = true;
    public float absSlipThreshold= -0.08f;
    public float absMaxSlip = -0.25f;
    public float absKp= 0.5f;
    public float absKi= 0.1f;
    public float maxBrakeForce= 1500f;


    public WheelCollider wheelFL, wheelFR, wheelRL, wheelRR;
    public Transform     meshFL,  meshFR,  meshRL,  meshRR;

    [Header("Wing transforms (optional — for torque application at CoP)")]
    public Transform frontWingTransform;
    public Transform rearWingTransform;
    private Rigidbody rb;

    // TC integrator
    private float tcIntegralFL, tcIntegralFR, tcIntegralRL, tcIntegralRR;

    // ABS integrator
    private float absIntegralFL, absIntegralFR, absIntegralRL, absIntegralRR;

    // debug
    public float debugSpeedKmH;
    public float debugDownforceN;
    public float debugDragN;
    public float debugTcTorqueFactor;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass        = carMass;
        rb.interpolation = RigidbodyInterpolation.Interpolate; 
        rb.centerOfMass  = new Vector3(0f, -0.3f, 0.1f);      
        ApplySuspensionSettings();
        ApplyTireSettings();

        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    void FixedUpdate()
    {
        float throttle = Input.GetAxis("Vertical"); 
        var surface = GetComponent<SurfaceDetector>(); 
        float surfaceTorqueMult = surface != null ? surface.GetTorqueMultiplier() : 1f;
        float steering = Input.GetAxis("Horizontal");
        bool  braking  = Input.GetKey(KeyCode.Space);

        float speedMs = rb.velocity.magnitude;

        ApplyAerodynamics(speedMs);

        float steerAngle = steering * steeringAngleDeg;
        wheelFL.steerAngle = steerAngle;
        wheelFR.steerAngle = steerAngle;

        if (!braking && throttle > 0f)
        {
            float baseTorque = throttle * torqueLimit * gearboxRatio * surfaceTorqueMult;
            float torqueRL   = ApplyTractionControl(wheelRL, baseTorque, speedMs, ref tcIntegralRL);
            float torqueRR   = ApplyTractionControl(wheelRR, baseTorque, speedMs, ref tcIntegralRR);
            wheelRL.motorTorque = torqueRL;
            wheelRR.motorTorque = torqueRR;
            debugTcTorqueFactor = (torqueRL + torqueRR) / (2f * Mathf.Max(baseTorque, 0.01f));
        }
        else
        {
            wheelRL.motorTorque = 0f;
            wheelRR.motorTorque = 0f;
            debugTcTorqueFactor = 1f;
        }

        if (braking)
        {
            wheelFL.brakeTorque = ApplyABS(wheelFL, maxBrakeForce, speedMs, ref absIntegralFL);
            wheelFR.brakeTorque = ApplyABS(wheelFR, maxBrakeForce, speedMs, ref absIntegralFR);
            wheelRL.brakeTorque = ApplyABS(wheelRL, maxBrakeForce * 0.7f, speedMs, ref absIntegralRL); // tylny mniejszy
            wheelRR.brakeTorque = ApplyABS(wheelRR, maxBrakeForce * 0.7f, speedMs, ref absIntegralRR);
        }
        else
        {
            wheelFL.brakeTorque = 0f;
            wheelFR.brakeTorque = 0f;
            wheelRL.brakeTorque = 0f;
            wheelRR.brakeTorque = 0f;
        }

        ApplyAntiRollBar(wheelFL, wheelFR);
        ApplyAntiRollBar(wheelRL, wheelRR);


        debugSpeedKmH = CalculateSpeedKmH();
    }
    void Update()
    {
        UpdateWheelMesh(wheelFL, meshFL);
        UpdateWheelMesh(wheelFR, meshFR);
        UpdateWheelMesh(wheelRL, meshRL);
        UpdateWheelMesh(wheelRR, meshRR);
    }

    // Gazebo (lifting_surface_model):
    //   CL = cla * (alpha - a0)
    //   CD = cd0 + cda * alpha²
    //   Lift = 0.5 * rho * v² * A * CL
    //   Drag = 0.5 * rho * v² * A * CD
    //
    // Rear wing (negative lift).
    // downforce = -transform.up
    //force oppposite to the velocity vector (not transform.forward!)

    void ApplyAerodynamics(float speedMs)
    {
        float qDyn = 0.5f * airDensity * speedMs * speedMs; // dynamic pressure

        float pitchAngle = Vector3.SignedAngle(
            Vector3.ProjectOnPlane(transform.forward, Vector3.up),
            transform.forward,
            transform.right
        ) * Mathf.Deg2Rad;

        float alphaFront   = Mathf.Clamp(pitchAngle, -frontWingStall, frontWingStall);
        float clFront      = frontWingCla * (alphaFront - frontWingA0); // negative = downforce
        float cdFront      = frontWingCd0 + frontWingCda * alphaFront * alphaFront;

        float liftFront    = qDyn * frontWingArea * clFront; 
        float dragFront    = qDyn * frontWingArea * cdFront; 

        
        float alphaRear    = Mathf.Clamp(pitchAngle, -rearWingStall, rearWingStall);
        float clRear       = rearWingCla * (alphaRear - rearWingA0);
        float cdRear       = rearWingCd0 + rearWingCda * alphaRear * alphaRear;

        float liftRear     = qDyn * rearWingArea * clRear;
        float dragRear     = qDyn * rearWingArea * cdRear;

   
        float dragBody     = qDyn * bodyFrontalArea * bodyCd;

      
        float totalDownforce = -(liftFront + liftRear);   
        float totalDrag      = dragFront + dragRear + dragBody;

        rb.AddForce(-transform.up * totalDownforce);

        if (speedMs > 0.1f)
            rb.AddForce(-rb.velocity.normalized * totalDrag);

   
        debugDownforceN = totalDownforce;
        debugDragN      = totalDrag;
    }


    // config: traction_control_slip_threshold, _max_slip, Kp, Ki
    //
    //   slip = (v_wheel - v_car) / max(v_car, epsilon)
    //   > 0 = spin wheel, < 0 
    float ApplyTractionControl(WheelCollider wheel, float requestedTorque,
                                float vehicleSpeedMs, ref float integral)
    {
        if (!tcEnabled || vehicleSpeedMs < 0.5f)
            return requestedTorque;

        float wheelSpeedMs = wheel.rpm * (Mathf.PI * tireDiameter) / 60f;

        float slip = (wheelSpeedMs - vehicleSpeedMs) / Mathf.Max(vehicleSpeedMs, 0.5f);

        if (slip <= tcSlipThreshold)
        {
            integral = Mathf.Max(0f, integral - Time.fixedDeltaTime * 0.5f); 
            return requestedTorque;
        }

        float error = slip - tcSlipThreshold;
        integral   += error * Time.fixedDeltaTime;
        integral    = Mathf.Clamp(integral, 0f, 1f);

        float reduction = Mathf.Clamp01(tcKp * error + tcKi * integral);

        if (slip > tcMaxSlip)
            reduction = 1.0f;

        return requestedTorque * (1f - reduction);
    }
    float ApplyABS(WheelCollider wheel, float requestedBrake,
                   float vehicleSpeedMs, ref float integral)
    {
        if (!absEnabled || vehicleSpeedMs < 0.5f)
            return requestedBrake;

        float wheelSpeedMs = wheel.rpm * (Mathf.PI * tireDiameter) / 60f;
        float slip         = (wheelSpeedMs - vehicleSpeedMs) / Mathf.Max(vehicleSpeedMs, 0.5f);

        if (slip >= absSlipThreshold)
        {
            integral = Mathf.Max(0f, integral - Time.fixedDeltaTime * 0.5f);
            return requestedBrake;
        }

        float error  = absSlipThreshold - slip; 
        integral    += error * Time.fixedDeltaTime;
        integral     = Mathf.Clamp(integral, 0f, 1f);

        float reduction = Mathf.Clamp01(absKp * error + absKi * integral);

        if (slip < absMaxSlip)
            reduction = 1.0f;

        return requestedBrake * (1f - reduction);
    }

    // anit-roll bar applies force to reduce body roll during cornering
    void ApplyAntiRollBar(WheelCollider wheelL, WheelCollider wheelR)
    {
        WheelHit hitL, hitR;
        bool groundL = wheelL.GetGroundHit(out hitL);
        bool groundR = wheelR.GetGroundHit(out hitR);

       
        float travelL = groundL
            ? (-wheelL.transform.InverseTransformPoint(hitL.point).y - wheelL.radius)
              / wheelL.suspensionDistance
            : 1f;

        float travelR = groundR
            ? (-wheelR.transform.InverseTransformPoint(hitR.point).y - wheelR.radius)
              / wheelR.suspensionDistance
            : 1f;

        float antiRollForceValue = (travelL - travelR) * antiRollForce;

        if (groundL)
            rb.AddForceAtPosition(wheelL.transform.up * -antiRollForceValue,
                                  wheelL.transform.position);
        if (groundR)
            rb.AddForceAtPosition(wheelR.transform.up *  antiRollForceValue,
                                  wheelR.transform.position);
    }

    void ApplySuspensionSettings()
    {
     
        ApplySuspensionToWheel(wheelFL, frontSpringRate, frontDamping, frontWheelTravel);
        ApplySuspensionToWheel(wheelFR, frontSpringRate, frontDamping, frontWheelTravel);
    
        ApplySuspensionToWheel(wheelRL, rearSpringRate, rearDamping, rearWheelTravel);
        ApplySuspensionToWheel(wheelRR, rearSpringRate, rearDamping, rearWheelTravel);
    }

    void ApplySuspensionToWheel(WheelCollider w, float spring, float damper, float travel)
    {
        JointSpring js = w.suspensionSpring;
        js.spring         = spring;
        js.damper         = damper;
        js.targetPosition = 0.5f; 
        w.suspensionSpring    = js;
        w.suspensionDistance  = travel;
    }

    void ApplyTireSettings()
    {
        WheelCollider[] wheels = { wheelFL, wheelFR, wheelRL, wheelRR };
        foreach (var w in wheels)
        {
            w.mass   = tireMass + rimMass;
            w.radius = tireDiameter * 0.5f;

            WheelFrictionCurve fwd = w.forwardFriction;
            fwd.extremumSlip   = tireExtremumSlip;
            fwd.extremumValue  = tireExtremumValue  * tireConditionScale;
            fwd.asymptoteSlip  = tireAsymptoteSlip;
            fwd.asymptoteValue = tireAsymptoteValue * tireConditionScale;
            fwd.stiffness      = 1.0f;
            w.forwardFriction  = fwd;
            WheelFrictionCurve side = w.sidewaysFriction;
            side.extremumSlip   = tireExtremumSlip  * 1.2f;    
            side.extremumValue  = tireExtremumValue * 0.88f * tireConditionScale;
            side.asymptoteSlip  = tireAsymptoteSlip;
            side.asymptoteValue = tireAsymptoteValue * 0.85f * tireConditionScale;
            side.stiffness      = 1.0f;
            w.sidewaysFriction  = side;
        }
    }

    void UpdateWheelMesh(WheelCollider col, Transform mesh)
    {
        if (mesh == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.SetPositionAndRotation(pos, rot);
    }

    float CalculateSpeedKmH()
    {
        float avgRpm       = (wheelRL.rpm + wheelRR.rpm) * 0.5f;
        float circumference = Mathf.PI * tireDiameter;
        return Mathf.Abs(avgRpm * circumference * 60f / 1000f);
    }
}