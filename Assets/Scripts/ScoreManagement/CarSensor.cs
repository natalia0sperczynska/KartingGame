using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSensor : MonoBehaviour
{
    public RaceManager raceManager;
 
    public float gracePeriod = 0.4f;     
 
    public float rayLength = 0.6f;
 
    private SurfaceDetector _surface;
    private float           _offRoadTimer = 0f;
    private bool            _wasOffRoad   = false;
 
    void Start()
    {
        _surface = GetComponentInParent<SurfaceDetector>();

        if (raceManager == null)
            raceManager = FindObjectOfType<RaceManager>();
    }
 
    void FixedUpdate()
    {
         if (raceManager == null)
        raceManager = FindObjectOfType<RaceManager>();
    if (raceManager == null) return;
        bool offRoad;
 
        if (_surface != null)
        {
            offRoad = !_surface.IsOnRoad;
        }
        else
        {
            RaycastHit hit;
            offRoad = true;
            if (Physics.Raycast(transform.position + Vector3.up * 0.3f,
                                Vector3.down, out hit, rayLength))
            {
                offRoad = !hit.collider.CompareTag("Road");
            }
        }
 
        if (offRoad)
        {
            _offRoadTimer += Time.fixedDeltaTime;
            if (_offRoadTimer >= gracePeriod && !_wasOffRoad)
            {
                _wasOffRoad = true;
                raceManager?.ResetMultiplier();
            }
        }
        else
        {
            _offRoadTimer = 0f;
            _wasOffRoad   = false;
        }
    }
     void OnDrawGizmosSelected()
    {
        Gizmos.color = _wasOffRoad ? Color.red : Color.green;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.3f,
                       Vector3.down * rayLength);
    }
}
 