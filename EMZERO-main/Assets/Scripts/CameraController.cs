using UnityEngine;
using Unity.Netcode;

public class CameraController : MonoBehaviour
{
    public Transform player;            // Referencia al jugador
    public Vector3 offset = new Vector3(0f, 2f, -5f);  // Desplazamiento desde el jugador
    public float rotationSpeed = 5f;    // Velocidad de rotación
    public float pitchSpeed = 2f;       // Velocidad de inclinación (eje Y)
    public float minPitch = -20f;       // Ángulo mínimo de inclinación
    public float maxPitch = 50f;        // Ángulo máximo de inclinación

    private float yaw = 0f;             // Rotación alrededor del eje Y
    private float pitch = 2f;           // Inclinación hacia arriba/abajo (eje X)

    private bool playerAssigned = false;

    void Start()
    {

        TryAssignPlayer(); //Intentamos asignar por si ya está

        //Nos suscribimos al evento por si spawnea más tarde
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            TryAssignPlayer();
        }
    }

    void TryAssignPlayer()
    {
        foreach (var playerObject in FindObjectsOfType<PlayerController>())
        {
            if (playerObject.IsOwner)
            {
                player = playerObject.transform;
                playerAssigned = true;
                Debug.Log("? Cámara asignada al jugador local.");
                break;
            }
        }

        if (!playerAssigned)
        {
            Debug.LogWarning("?? No se encontró jugador local para seguir.");
        }
    }
    void LateUpdate()
    {
        // Si aún no hemos asignado al jugador, intentar de nuevo (por si spawneó tarde)
        if (!playerAssigned)
        {
            TryAssignPlayer();
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("Referencia al jugador perdida.");
            return;
        }

        HandleCameraRotation();
        UpdateCameraPosition();
    }

    private void HandleCameraRotation()
    {
        // Obtener la entrada del ratón para la rotación de la cámara
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * pitchSpeed;

        // Modificar los ángulos de rotación (yaw y pitch)
        yaw += mouseX;
        pitch -= mouseY;

        // Limitar la inclinación de la cámara
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void UpdateCameraPosition()
    {
        // Calcular la nueva dirección de la cámara
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 rotatedOffset = rotation * offset;

        // Posicionar la cámara en función del jugador y el nuevo offset
        transform.position = player.position + rotatedOffset;

        // Siempre mirar al jugador
        transform.LookAt(player);
    }
}
