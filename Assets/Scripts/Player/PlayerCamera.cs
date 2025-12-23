using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 4, -10);

    [Header("Movement")]
    public float positionSmoothTime = 0.15f;
    public float maxLagDistance = 3.0f;

    [Header("Rotation")]
    public float rotationSmoothSpeed = 5f;
    [Range(0, 1)]
    public float lookAtTargetStrength = 0.5f;

    private Vector3 _currentVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 idealPosition = target.TransformPoint(offset);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            idealPosition,
            ref _currentVelocity,
            positionSmoothTime
        );

        Vector3 lagVector = transform.position - idealPosition;
        if (lagVector.magnitude > maxLagDistance)
        {
            transform.position = idealPosition + lagVector.normalized * maxLagDistance;
            _currentVelocity = Vector3.Project(_currentVelocity, lagVector.normalized);
        }

        Vector3 lookAtPoint = target.position + (target.forward * 1000f);

        Vector3 direction = lookAtPoint - transform.position;

        Quaternion lookRotation = Quaternion.LookRotation(direction, target.up);


        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation,
            Time.deltaTime * rotationSmoothSpeed
        );
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        Vector3 startPos = target.TransformPoint(offset);
        transform.SetPositionAndRotation(startPos, target.rotation);
        _currentVelocity = Vector3.zero;
    }
}