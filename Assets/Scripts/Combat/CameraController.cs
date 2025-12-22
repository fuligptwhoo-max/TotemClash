using UnityEngine;
using Mirror;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0f, 25f, -10f);
    public float smoothSpeed = 1000f;
    public float pitchAngle = 53f;

    private Transform target;
    private Camera cam;
    private Quaternion fixedRotation;
    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = 90f;
        }
        fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        transform.rotation = fixedRotation;
    }

    private void Update()
    {
        // Если target не установлен, пытаемся найти локального игрока
        if (target == null && NetworkClient.localPlayer != null)
        {
            target = NetworkClient.localPlayer.transform;
            if (target != null)
            {
                transform.position = target.position + offset;
                Debug.Log("Camera attached to local player: " + target.name);
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / smoothSpeed);
        transform.rotation = fixedRotation;
    }
}