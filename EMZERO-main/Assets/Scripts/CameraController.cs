using UnityEngine;
using Unity.Netcode;

public class CameraController : MonoBehaviour
{
    public Transform player;            // Referencia al jugador
    public Vector3 offset = new Vector3(0f, 2f, -5f);  // Desplazamiento desde el jugador
    public float rotationSpeed = 5f;    // Velocidad de rotaci�n
    public float pitchSpeed = 2f;       // Velocidad de inclinaci�n (eje Y)
    public float minPitch = -20f;       // �ngulo m�nimo de inclinaci�n
    public float maxPitch = 50f;        // �ngulo m�ximo de inclinaci�n

    private float yaw = 0f;             // Rotaci�n alrededor del eje Y
    private float pitch = 2f;           // Inclinaci�n hacia arriba/abajo (eje X)

    private bool playerAssigned = false;

    void Start()
    {

        TryAssignPlayer(); //Intentamos asignar por si ya est�

        //Nos suscribimos al evento por si spawnea m�s tarde
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
                Debug.Log("? C�mara asignada al jugador local.");
                break;
            }
        }

        if (!playerAssigned)
        {
            Debug.LogWarning("?? No se encontr� jugador local para seguir.");
        }
    }
    void LateUpdate()
    {
        // Si a�n no hemos asignado al jugador, intentar de nuevo (por si spawne� tarde)
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
        // Obtener la entrada del rat�n para la rotaci�n de la c�mara
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * pitchSpeed;

        // Modificar los �ngulos de rotaci�n (yaw y pitch)
        yaw += mouseX;
        pitch -= mouseY;

        // Limitar la inclinaci�n de la c�mara
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void UpdateCameraPosition()
    {
        // Calcular la nueva direcci�n de la c�mara
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 rotatedOffset = rotation * offset;

        // Posicionar la c�mara en funci�n del jugador y el nuevo offset
        transform.position = player.position + rotatedOffset;

        // Siempre mirar al jugador
        transform.LookAt(player);
    }
}
