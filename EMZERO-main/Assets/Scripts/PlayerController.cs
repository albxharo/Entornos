using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("UI")]
    private TextMeshProUGUI coinText;

    [Header("Stats")]
    public int CoinsCollected = 0;

    [Header("Character settings")]
    public bool isZombie = false;
    public string uniqueID;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float zombieSpeedModifier = 0.8f;
    public Animator animator;

    // Volvemos a exponer cameraTransform para LevelManager u otros
    [HideInInspector]
    public Transform cameraTransform;

    // --- Internals de movimiento ---
    private Vector3 _serverMoveDir = Vector3.zero;
    private float _rotSpeed = 270f;

    private LevelManager _levelManager;

    private void Awake()
    {
        _levelManager = GameObject.Find("LevelManager").GetComponent<LevelManager>();
    }

    private void Start()
    {
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

            GetComponent<PlayerInput>().enabled = true;
        }

        if (IsServer)
        {
            //transform.position = _levelManager.GetSpawnPoint(0);

        }

        Debug.Log($"Player spawned. IsOwner: {IsOwner}, IsServer: {IsServer}");
        base.OnNetworkSpawn();
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (_serverMoveDir.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(_serverMoveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                _rotSpeed * Time.fixedDeltaTime
            );

            float speed = isZombie ? moveSpeed * zombieSpeedModifier : moveSpeed;
            transform.Translate(_serverMoveDir * speed * Time.fixedDeltaTime, Space.World);
            animator.SetFloat("Speed", speed);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (!IsOwner) return;

        Vector2 input2D = ctx.ReadValue<Vector2>();
        if (cameraTransform == null) return;

        Vector3 camF = cameraTransform.forward; camF.y = 0;
        Vector3 camR = cameraTransform.right; camR.y = 0;
        Vector3 dir = (camF * input2D.y + camR * input2D.x).normalized;

        animator.SetFloat("Speed", dir.magnitude * moveSpeed);
        SendMoveDirectionServerRpc(dir);
    }

    [ServerRpc]
    private void SendMoveDirectionServerRpc(Vector3 moveDir)
    {
        _serverMoveDir = moveDir;
    }

    public void CoinCollected()
    {
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