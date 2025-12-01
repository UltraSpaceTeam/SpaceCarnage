using UnityEngine;

public class LoginSceneCamera : MonoBehaviour
{

    [Header("Settings")]
    public Transform target;
    public float speed = 10f;
    public float height = 5f;
    public float distance = 10f;

    void Update()
    {
        if (target != null)
        {
            transform.RotateAround(target.position, Vector3.up, speed * Time.deltaTime);

            Vector3 targetPosition = target.position;
            targetPosition.y += height;

            transform.LookAt(targetPosition);
            Vector3 direction = (transform.position - targetPosition).normalized;
            transform.position = targetPosition + direction * distance;

        }
    }
}
