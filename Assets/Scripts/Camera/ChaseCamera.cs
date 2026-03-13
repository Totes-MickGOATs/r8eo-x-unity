using UnityEngine;

/// <summary>
/// Simple chase camera that follows the RC buggy with smooth interpolation.
/// </summary>
public class ChaseCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow")]
    public float followDistance = 3f;
    public float followHeight = 1.5f;
    public float lookHeight = 0.3f;
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        float dt = Time.deltaTime;
        Vector3 targetPos = target.position - target.forward * followDistance + Vector3.up * followHeight;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * dt);
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
