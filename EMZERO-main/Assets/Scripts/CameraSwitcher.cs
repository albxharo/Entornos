using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public Camera mainCamera; // La c�mara principal
    public KeyCode switchKey = KeyCode.C; // Tecla para alternar la vista
    public float transitionSpeed = 2f; // Velocidad de la transici�n
    public float topDownHeight = 20f; // Altura para la vista cenital
    public float topDownRotation = 90f; // Rotaci�n para la vista cenital (mirando hacia abajo)

    private CameraController cameraController; // Referencia al script CameraController
    private Vector3 targetPosition; // Posici�n objetivo para la c�mara
    private Quaternion targetRotation; // Rotaci�n objetivo para la c�mara
    private bool isTopDownView = false; // Estado actual de la vista
    private bool isTransitioning = false; // Indica si la c�mara est� en transici�n

    void Start()
    {
        // Obtiene el componente CameraController de la c�mara principal
        if (mainCamera != null)
        {
            cameraController = mainCamera.GetComponent<CameraController>();

            if (cameraController == null)
            {
                Debug.LogWarning("CameraController no encontrado en la c�mara principal.");
            }
        }
        else
        {
            Debug.LogError("Referencia a la c�mara principal no asignada.");
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null || cameraController == null)
            return;

        // Esperar hasta que la c�mara tenga un jugador asignado
        if (cameraController.player == null)
            return;

        if (Input.GetKeyDown(switchKey) && !isTransitioning)
        {
            ToggleCameraView();
        }

        if (isTransitioning)
        {
            PerformTransition();
        }
    }

    private void ToggleCameraView()
    {
        isTopDownView = !isTopDownView;

        if (isTopDownView)
        {
            Debug.Log("Cambiando a cenital...");

            // Apuntar directamente sobre el jugador
            Vector3 playerPos = cameraController.player.position;
            targetPosition = new Vector3(playerPos.x, playerPos.y + topDownHeight, playerPos.z);
            targetRotation = Quaternion.Euler(topDownRotation, 0f, 0f);

            // Desactiva el script CameraController
            EnableCameraController(false);
        }
        else
        {
            Debug.Log("Volviendo a tercera persona...");

            // Recuperar la posici�n de tercera persona usando offset original
            Vector3 playerPos = cameraController.player.position;
            Quaternion rotation = Quaternion.Euler(cameraController.pitch, cameraController.yaw, 0f);
            Vector3 offsetPos = rotation * cameraController.offset;

            targetPosition = playerPos + offsetPos;
            targetRotation = rotation;

            // Reactiva el script CameraController
            EnableCameraController(true);
        }

        // Inicia la transici�n
        isTransitioning = true;
    }

    private void PerformTransition()
    {
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetPosition,
            Time.deltaTime * transitionSpeed
        );

        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            targetRotation,
            Time.deltaTime * transitionSpeed
        );

        // Comprueba si la c�mara lleg� al objetivo
        if (Vector3.Distance(mainCamera.transform.position, targetPosition) < 0.1f)
        {
            isTransitioning = false; // Finaliza la transici�n
        }
    }

    // Activa o desactiva el script CameraController
    private void EnableCameraController(bool enable)
    {
        if (cameraController != null)
        {
            cameraController.enabled = enable;
        }
    }
}
