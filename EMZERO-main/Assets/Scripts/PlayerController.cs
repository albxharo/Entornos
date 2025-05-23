using System.Globalization;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private TextMeshProUGUI coinText;

    [Header("Stats")]
    public NetworkVariable<int> CoinsCollected = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Character settings")]
    public NetworkVariable<bool> isZombie = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public string uniqueID;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float zombieSpeedModifier = 0.8f;
    public Animator animator;
    public Transform cameraTransform;

    private float horizontalInput;
    private float verticalInput;

    void Start()
    {
        if (IsLocalPlayer)
        {
            GameObject canvas = GameObject.Find("CanvasPlayer");
            if (canvas != null)
            {
                Transform panel = canvas.transform.Find("PanelHud");
                if (panel != null)
                {
                    Transform coinTextTransform = panel.Find("CoinsValue");
                    if (coinTextTransform != null)
                    {
                        coinText = coinTextTransform.GetComponent<TextMeshProUGUI>();
                    }
                }
            }

            CoinsCollected.OnValueChanged += OnCoinsChanged;
            UpdateCoinUI(); // inicializa con el valor actual
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        MovePlayer();
        HandleAnimations();
    }

    void MovePlayer()
    {
        if (cameraTransform == null) return;

        Vector3 moveDirection = (cameraTransform.forward * verticalInput + cameraTransform.right * horizontalInput).normalized;
        moveDirection.y = 0f;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);

            float adjustedSpeed = isZombie.Value ? moveSpeed * zombieSpeedModifier : moveSpeed;
            transform.Translate(moveDirection * adjustedSpeed * Time.deltaTime, Space.World);
        }
    }

    void HandleAnimations()
    {
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));
    }

    // Se llama cuando el jugador colisiona con una moneda
    public void CoinCollected()
    {
        if (!IsOwner) return; // solo el dueño lo puede pedir
        RequestCoinCollectedServerRpc();
    }

    [ServerRpc]
    void RequestCoinCollectedServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!isZombie.Value) // solo humanos pueden recoger
        {
            CoinsCollected.Value++; // el servidor actualiza el contador
        }
    }

    // Se actualiza el UI cuando cambian las monedas
    void OnCoinsChanged(int oldValue, int newValue)
    {
        UpdateCoinUI();
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = $"{CoinsCollected.Value}";
        }
    }
}
