using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.8f, -5.5f);
    public float positionSmoothTime = 0.12f;

    public float verticalSmoothTime = 0.28f;

    public float rotationSmoothSpeed = 6f;

    private Vector3  _posVelocity = Vector3.zero;
    private float    _vertVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + target.rotation * offset;

        float smoothY = Mathf.SmoothDamp(
            transform.position.y,
            desired.y,
            ref _vertVelocity,
            verticalSmoothTime
        );

        Vector3 smoothXZ = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref _posVelocity,
            positionSmoothTime
        );

        transform.position = new Vector3(smoothXZ.x, smoothY, smoothXZ.z);

        Quaternion targetRot = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSmoothSpeed * Time.deltaTime
        );
    }
}