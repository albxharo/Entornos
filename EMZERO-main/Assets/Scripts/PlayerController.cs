using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("UI")]
    private TextMeshProUGUI coinText;       // Texto de monedas en el HUD

    [Header("Stats")]
    public int CoinsCollected = 0;          // Contador de monedas

    [Header("Character settings")]
    public bool isZombie = false;            // Está infectado?
    public string uniqueID;                  // ID único del jugador
    public bool isOriginalZombie = false;    //Para diferenciar los zombies del principio de la partida de los humanos convertidos

    [Header("Movement Settings")]
    public float moveSpeed = 5f;             // Velocidad normal
    public float zombieSpeedModifier = 0.8f; // Reducción de velocidad si es zombie
    public Animator animator;                // Animador del personaje

    // Volvemos a exponer cameraTransform para LevelManager u otros
    [HideInInspector]
    public Transform cameraTransform;

    // --- Internals de movimiento ---
    private Vector3 _serverMoveDir = Vector3.zero;
    private float _rotSpeed = 270f;

    private LevelManager _levelManager;

    // NetworkVariable para sincronizar la velocidad del Animator
    public NetworkVariable<float> networkedSpeed = new NetworkVariable<float>(0f);

    private void Awake()
    {
        // Buscar el LevelManager
       // _levelManager = GameObject.Find("LevelManager").GetComponent<LevelManager>();
    }

    private void Start()
    {
        // Obtener referencia al texto de monedas en la UI
        var canvas = GameObject.Find("CanvasPlayer");
        if (canvas != null)
        {
            var panel = canvas.transform.Find("PanelHud");
            var coinTextTransform = panel?.Find("CoinsValue");
            coinText = coinTextTransform?.GetComponent<TextMeshProUGUI>();
        }
        UpdateCoinUI();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Escuchar cambios de velocidad para animación
        networkedSpeed.OnValueChanged += OnSpeedChanged;

        if (IsOwner)
        {
            // Asignamos la cámara principal al cameraTransform
            var cam = Camera.main;
            if (cam != null)
            {
                cameraTransform = cam.transform;

                // También decimos al CameraController quién es su target
                var cc = cam.GetComponent<CameraController>();
                if (cc != null)
                    cc.player = transform;
            }

            // Activar input solo en el propietario
            GetComponent<PlayerInput>().enabled = true;
        }

        Debug.Log($"Player spawned. IsOwner: {IsOwner}, IsServer: {IsServer}");
    }

    public override void OnNetworkDespawn()
    {
        networkedSpeed.OnValueChanged -= OnSpeedChanged;
        base.OnNetworkDespawn();
    }

    private void OnSpeedChanged(float oldSpeed, float newSpeed)
    {
        animator.SetFloat("Speed", newSpeed);
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (_serverMoveDir.sqrMagnitude > 0.01f)
        {
            // Rotar hacia la dirección del movimiento
            var targetRot = Quaternion.LookRotation(_serverMoveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                _rotSpeed * Time.fixedDeltaTime
            );

            // Mover al personaje
            float speed = isZombie ? moveSpeed * zombieSpeedModifier : moveSpeed;
            transform.Translate(_serverMoveDir * speed * Time.fixedDeltaTime, Space.World);

            // Actualizar la variable de red que sincroniza la animación
            networkedSpeed.Value = speed;
        }
        else
        {
            networkedSpeed.Value = 0f;
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;

        Vector2 input2D = ctx.ReadValue<Vector2>();
        if (cameraTransform == null) return;

        // Calcular dirección en base a la orientación de la cámara
        Vector3 camF = cameraTransform.forward; camF.y = 0;
        Vector3 camR = cameraTransform.right; camR.y = 0;
        Vector3 dir = (camF * input2D.y + camR * input2D.x).normalized;

        // No seteamos animator aquí para evitar conflicto con sincronización

        // Enviar dirección al servidor
        SendMoveDirectionServerRpc(dir);
    }

    [ServerRpc]
    private void SendMoveDirectionServerRpc(Vector3 moveDir)
    {
        _serverMoveDir = moveDir;
    }

    public void CoinCollected()
    {
        // Solo sumar monedas si no es zombie
        if (!isZombie)
        {
            CoinsCollected++;
            UpdateCoinUI();
        }
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = CoinsCollected.ToString();
    }


}
