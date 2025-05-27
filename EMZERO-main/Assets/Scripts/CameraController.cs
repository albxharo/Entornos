using UnityEngine;
using Unity.Netcode;   // ← necesario para NetworkObject y NetworkManager

public class CameraController : MonoBehaviour
{
    [Tooltip("El transform del jugador que queremos seguir")]
    public Transform player;
    public Vector3 offset = new Vector3(0f, 2f, -5f);
    public float rotationSpeed = 5f;
    public float pitchSpeed = 2f;
    public float minPitch = -20f;
    public float maxPitch = 50f;

    private float yaw = 0f;
    private float pitch = 2f;

    void LateUpdate()
    {
        if (player == null) return;

        // → Solo sigo al jugador si este NetworkObject ¡es mío!
        var netObj = player.GetComponent<NetworkObject>();
        if (netObj == null) return;

        // Opción A: con IsOwner
        if (!netObj.IsOwner) return;

        // Opción B (equivalente):    
        // if (netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId) return;

        HandleCameraRotation();
        UpdateCameraPosition();
    }

    private void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * pitchSpeed;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void UpdateCameraPosition()
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pos = player.position + rot * offset;
        transform.position = pos;
        transform.LookAt(player);
    }
}
