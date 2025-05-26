using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    private TextMeshProUGUI coinText;

    [Header("Stats")]
    public int CoinsCollected = 0;

    [Header("Character settings")]
    public bool isZombie = false; // Añadir una propiedad para el estado del jugador
    public string uniqueID; // Añadir una propiedad para el identificador único

    [Header("Movement Settings")]
    public float moveSpeed = 5f;           // Velocidad de movimiento
    public float zombieSpeedModifier = 0.8f; // Modificador de velocidad para zombies
    public Animator animator;              // Referencia al Animator
    public Transform cameraTransform;      // Referencia a la cámara


    private float horizontalInput;         // Entrada horizontal (A/D o flechas)
    private float verticalInput;           // Entrada vertical (W/S o flechas)


    //------------------------
    Transform _playerTransform;
    Vector2 _input;
    float _rotSpeed = 270f;

    GameObject _GOlevelManager;

    LevelManager _levelManager;


    private void Awake()
    {
        _playerTransform = transform;

        _GOlevelManager = GameObject.Find("LevelManager");


        _levelManager = _GOlevelManager.GetComponent<LevelManager>();
    }
    void Start()
    {
        // Buscar el objeto "CanvasPlayer" en la escena
        GameObject canvas = GameObject.Find("CanvasPlayer");

        if (canvas != null)
        {
            Debug.Log("Canvas encontrado");

            // Buscar el Panel dentro del CanvasHud
            Transform panel = canvas.transform.Find("PanelHud");
            if (panel != null)
            {
                // Buscar el TextMeshProUGUI llamado "CoinsValue" dentro del Panel
                Transform coinTextTransform = panel.Find("CoinsValue");
                if (coinTextTransform != null)
                {
                    coinText = coinTextTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        UpdateCoinUI();


    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            InitializeOwner() ;

        }
        if (IsServer)
        {
            Vector3 _spawnPos = _levelManager.GetSpawnPoint(0);
            transform.position = _spawnPos;
        }

        Debug.Log($"Player spawned. IsOwner: {IsOwner}, IsClient: {IsClient}, IsServer: {IsServer}");


        base.OnNetworkSpawn();
    }

    private void InitializeOwner()
    {
        GetComponent<PlayerInput>().enabled = true;
    }

    void FixedUpdate()
    {
        if (!IsServer)
            return;
        // Leer entrada del teclado
        horizontalInput = _input.x;
        verticalInput = _input.y;


        // Mover el jugador
        MovePlayer();

        // Manejar las animaciones del jugador
        HandleAnimations();
    }
    public void OnMove(InputAction.CallbackContext ctx)
    {
        OnMoveRpc(ctx.ReadValue<Vector2>());    


    }


    void MovePlayer()
    {
        /*if (cameraTransform == null) { return; }

        // Calcular la dirección de movimiento en relación a la cámara
        Vector3 moveDirection = (cameraTransform.forward * _input.y + cameraTransform.right * _input.x).normalized;
        moveDirection.y = 0f; // Asegurarnos de que el movimiento es horizontal (sin componente Y)

        // Mover el jugador usando el Transform
        if (moveDirection != Vector3.zero)
        {
            // Calcular la rotación en Y basada en la dirección del movimiento
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);

            // Ajustar la velocidad si es zombie
            float adjustedSpeed = isZombie ? moveSpeed * zombieSpeedModifier : moveSpeed;

            // Mover al jugador en la dirección deseada
            Vector3 newPosition = moveDirection * adjustedSpeed * Time.fixedDeltaTime;
            transform.Translate(moveDirection * adjustedSpeed * Time.fixedDeltaTime, Space.World);
            OnMoveRpc(newPosition);
        }*/
        _playerTransform.Translate(Vector3.forward *(_input.y * moveSpeed* Time.fixedDeltaTime));
        _playerTransform.Rotate(Vector3.up *(_input.x * _rotSpeed * Time.fixedDeltaTime));


    }


    //---------
    [Rpc(SendTo.Server)]
    void OnMoveRpc(Vector2 input)
    {

        _input = input;
    }

    void HandleAnimations()
    {
        // Animaciones basadas en la dirección del movimiento
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));  // Controla el movimiento (caminar/correr)
    }

    public void CoinCollected()
    {
        if (!isZombie) // Solo los humanos pueden recoger monedas
        {
            this.CoinsCollected++;
            UpdateCoinUI();
        }
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = $"{CoinsCollected}";
        }
    }

}

