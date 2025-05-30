using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using Unity.Netcode;

public enum GameMode
{
    None,
    Tiempo,
    Monedas
}


public class LevelManager : NetworkBehaviour
{
    #region Properties

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject zombiePrefab;

    [Header("Team Settings")]
    [Tooltip("Número de jugadores humanos")]
    [SerializeField] private int numberOfHumans = 2;

    [Tooltip("Número de zombis")]
    [SerializeField] private int numberOfZombies = 2;

    [Header("Game Mode Settings")]
    [Tooltip("Selecciona el modo de juego")]
    [SerializeField] private GameMode gameMode;

    [Tooltip("Tiempo de partida en minutos para el modo tiempo")]
    [SerializeField] private int minutes = 5;

    public List<Vector3> humanSpawnPoints = new List<Vector3>();
    public List<Vector3> zombieSpawnPoints = new List<Vector3>();



    // Referencias a los elementos de texto en el canvas
    private TextMeshProUGUI humansText;
    private TextMeshProUGUI zombiesText;
    private TextMeshProUGUI timeModeText;
    private TextMeshProUGUI coinValueText;
    private TextMeshProUGUI coinLabelText;


    private int CoinsGenerated = 0;

    public string PlayerPrefabName => playerPrefab.name;
    public string ZombiePrefabName => zombiePrefab.name;

    private UniqueIdGenerator uniqueIdGenerator;
    private LevelBuilder levelBuilder;

    private PlayerController playerController;

    private float remainingSeconds;
    private bool isGameOver = false;
    private ulong? ultimoHumanoId = null;

    // Estado local
    private bool partidaIniciada = false;
    public GameObject gameOverPanel; // Asigna el panel desde el inspector

    public GameObject GO_gameManager;
    private GameManager gameManager;


    public NetworkVariable<int> coinsCollected = new NetworkVariable<int>(0);



    #endregion

    #region Unity game loop methods

    private void Awake()
    {
        Debug.Log("Despertando el nivel");

        // Obtener la referencia al UniqueIDGenerator
        uniqueIdGenerator = GetComponent<UniqueIdGenerator>();

        // Obtener la referencia al LevelBuilder
        levelBuilder = GetComponent<LevelBuilder>();

        gameManager = GO_gameManager.GetComponent<GameManager>();

        Time.timeScale = 1f; // Asegurarse de que el tiempo no esté detenido
    }

    private void Start()
    {
        Debug.Log("Iniciando el nivel");
        numberOfHumans = StartGameVariables.Instance.humanList.Count;
        numberOfZombies = StartGameVariables.Instance.zombieList.Count;
        // Buscar el objeto "CanvasPlayer" en la escena
        GameObject canvas = GameObject.Find("CanvasPlayer");
        if (canvas != null)
        {
            Debug.Log("Canvas encontrado");

            // Buscar el Panel dentro del CanvasHud
            Transform panel = canvas.transform.Find("PanelHud");
            if (panel != null)
            {
                // Buscar los TextMeshProUGUI llamados "HumansValue" y "ZombiesValue" dentro del Panel
                Transform humansTextTransform = panel.Find("HumansValue");
                Transform zombiesTextTransform = panel.Find("ZombiesValue");
                Transform gameModeTextTransform = panel.Find("GameModeConditionValue");
                Transform coinsTextTransform = panel.Find("CoinsValue");
                Transform coinscoinTextTransform = panel.Find("Coins");

                if (humansTextTransform != null)
                {
                    humansText = humansTextTransform.GetComponent<TextMeshProUGUI>();
                }

                if (zombiesTextTransform != null)
                {
                    zombiesText = zombiesTextTransform.GetComponent<TextMeshProUGUI>();
                }

                if (gameModeTextTransform != null)
                {
                    timeModeText = gameModeTextTransform.GetComponent<TextMeshProUGUI>();
                }
                if (coinsTextTransform != null)
                {
                    coinValueText = coinsTextTransform.GetComponent<TextMeshProUGUI>();
                }
                if (coinscoinTextTransform != null)
                {
                    coinLabelText = coinscoinTextTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        remainingSeconds = minutes * 60;

        // Obtener los puntos de aparición y el número de monedas generadas desde LevelBuilder
        if (levelBuilder != null)
        {
            
            levelBuilder.Build();
            humanSpawnPoints = levelBuilder.GetHumanSpawnPoints();
            zombieSpawnPoints = levelBuilder.GetZombieSpawnPoints();
        }


        UpdateTeamUI();
    }

    private void Update()
    {

        if (!partidaIniciada)
            return;    // hasta que no arranque, no entramos a lógica de tiempo/monedas
        if (gameMode == GameMode.Tiempo)
        {
            // Lógica para el modo de juego basado en tiempo
            HandleTimeLimitedGameMode();
        }
        else if (gameMode == GameMode.Monedas)
        {
            // Lógica para el modo de juego basado en monedas
            HandleCoinBasedGameMode();
        }

        //borrar luego
        if (Input.GetKeyDown(KeyCode.Z)) // Presiona "Z" para convertirte en Zombie
        {
            // Comprobar si el jugador actual está usando el prefab de humano
            GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
            if (currentPlayer != null && currentPlayer.name.Contains(playerPrefab.name))
            {
                ChangeToZombie();
            }
            else
            {
                Debug.Log("El jugador actual no es un humano.");
            }
        }
        else if (Input.GetKeyDown(KeyCode.H)) // Presiona "H" para convertirte en Humano
        {
        
            // Comprobar si el jugador actual está usando el prefab de zombie
            GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
            if (currentPlayer != null && currentPlayer.name.Contains(zombiePrefab.name))
            {
                ChangeToHuman();
            }
            else
            {
                Debug.Log("El jugador actual no es un zombie.");
            }
        }
        UpdateTeamUI();

        if (numberOfHumans == 0 && !isGameOver)
        {
            isGameOver = true;
            ShowGameOverPanelCustom();
        }

        if (isGameOver)
        {
            ShowGameOverPanel();
        }
    }

    #endregion


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // inicializa coinsCollected.Value = 0 si quieres
            coinsCollected.Value = 0;
        }

        // Todos: nos suscribimos a cambios
        coinsCollected.OnValueChanged += OnCoinsChanged;
    }


    #region Team management methods

    private void ChangeToZombie()
    {
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        ChangeToZombie(currentPlayer, true);
    }

   public void ChangeToZombie(GameObject human, bool enabled)
{
    if (human == null) return;

    NetworkObject oldNO = human.GetComponent<NetworkObject>();
    ulong clientId = oldNO.OwnerClientId;
    string uniqueID = human.GetComponent<PlayerController>().uniqueID;
    Vector3 pos = human.transform.position;
    Quaternion rot = human.transform.rotation;

    Destroy(human);

    GameObject zombie = Instantiate(zombiePrefab, pos, rot);
    var zombieNO = zombie.GetComponent<NetworkObject>();
    zombieNO.SpawnAsPlayerObject(clientId); // ¡le asigna el control al mismo jugador!

    var pc = zombie.GetComponent<PlayerController>();
    if (pc != null)
    {
        pc.enabled = enabled;
        pc.isZombie = true;
        pc.uniqueID = uniqueID;

            // UI y cámara
            if (enabled && clientId == NetworkManager.Singleton.LocalClientId)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    var cc = cam.GetComponent<CameraController>();
                    if (cc != null) cc.player = zombie.transform;

                    pc.cameraTransform = cam.transform;
                }
            }

            numberOfHumans--;
        numberOfZombies++;
        UpdateTeamUI();
    }
}


    private void ChangeToHuman()
    {
        Debug.Log("Cambiando a Humano");

        // Obtener la referencia al jugador actual
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");

        if (currentPlayer != null)
        {
            // Guardar la posición y rotación del jugador actual
            Vector3 playerPosition = currentPlayer.transform.position;
            Quaternion playerRotation = currentPlayer.transform.rotation;

            // Destruir el jugador actual
            Destroy(currentPlayer);

            // Instanciar el prefab del humano en la misma posición y rotación
            GameObject human = Instantiate(playerPrefab, playerPosition, playerRotation);
            human.tag = "Player";

            // Obtener la referencia a la cámara principal
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                // Obtener el script CameraController de la cámara principal
                CameraController cameraController = mainCamera.GetComponent<CameraController>();

                if (cameraController != null)
                {
                    // Asignar el humano al script CameraController
                    cameraController.player = human.transform;
                }

                // Obtener el componente PlayerController del humano instanciado
                playerController = human.GetComponent<PlayerController>();
                // Asignar el transform de la cámara al PlayerController
                if (playerController != null)
                {
                    playerController.enabled = true;
                    playerController.cameraTransform = mainCamera.transform;
                    playerController.isZombie = false; // Cambiar el estado a humano
                    numberOfHumans++; // Aumentar el número de humanos
                    numberOfZombies--; // Reducir el número de zombis
                }
                else
                {
                    Debug.LogError("PlayerController no encontrado en el humano instanciado.");
                }
            }
            else
            {
                Debug.LogError("No se encontró la cámara principal.");
            }
        }
        else
        {
            Debug.LogError("No se encontró el jugador actual.");
        }
    }

   

    private void UpdateTeamUI()
    {
        if (humansText != null)
        {
            humansText.text = $"{numberOfHumans}";
        }

        if (zombiesText != null)
        {
            zombiesText.text = $"{numberOfZombies}";
        }
    }

    #endregion

    #region Modo de juego

    private void HandleTimeLimitedGameMode()
    {
        // Implementar la lógica para el modo de juego basado en tiempo
        if (isGameOver) return;

        // Decrementar remainingSeconds basado en Time.deltaTime
        remainingSeconds -= Time.deltaTime;

        // Comprobar si el tiempo ha llegado a cero
        if (remainingSeconds <= 0)
        {
            isGameOver = true;
            remainingSeconds = 0;
        }

        // Convertir remainingSeconds a minutos y segundos
        int minutesRemaining = Mathf.FloorToInt(remainingSeconds / 60);
        int secondsRemaining = Mathf.FloorToInt(remainingSeconds % 60);

        // Actualizar el texto de la interfaz de usuario
        if (timeModeText != null)
        {
            timeModeText.text = $"{minutesRemaining:D2}:{secondsRemaining:D2}";
        }

    }

    private void HandleCoinBasedGameMode()
    {
        if (isGameOver) return;

        CoinsGenerated = levelBuilder.GetCoinsGenerated();

        // Implementar la lógica para el modo de juego basado en monedas
        if (timeModeText != null && playerController != null)
        {
            timeModeText.text = $"{playerController.CoinsCollected}/{CoinsGenerated}";
            if (playerController.CoinsCollected >= CoinsGenerated)
            {
                isGameOver = true;
            }
        }
    }

    private void OnCoinsChanged(int oldValue, int newValue)
    {
        // Actualiza la UI en cada cliente
        if (coinValueText != null)
            coinValueText.text = $"{newValue}/{CoinsGenerated}";
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            Time.timeScale = 0f;
            gameOverPanel.SetActive(true); // Muestra el panel de pausa

            // Gestión del cursor
            Cursor.lockState = CursorLockMode.None; // Desbloquea el cursor
            Cursor.visible = true; // Hace visible el cursor
        }
    }

    public void ReturnToMainMenu()
    {
        // Gestión del cursor
        Cursor.lockState = CursorLockMode.Locked; // Bloquea el cursor
        Cursor.visible = false; // Oculta el cursor

        // Cargar la escena del menú principal
        SceneManager.LoadScene("MenuScene"); // Cambia "MenuScene" por el nombre de tu escena principal
    }

    public Vector3 GetHumanSpawnPoint(int index)
    {
        if (index >= 0 && index < humanSpawnPoints.Count)
        {
            return humanSpawnPoints[index];
        }

        Debug.LogWarning("Índice de spawn fuera de rango. Se usará el primer punto.");
        return Vector3.zero;


    }

    public Vector3 GetZombieSpawnPoint(int index)
    {
        if (index >= 0 && index < zombieSpawnPoints.Count)
        {
            return zombieSpawnPoints[index];
        }

        Debug.LogWarning("Índice de spawn fuera de rango. Se usará el primer punto.");
        return Vector3.zero;

        #endregion

    }

    // Exponer para el RPC
    // Exponer para el RPC
    public void StartGame(GameMode mode)
    {
        Debug.Log($"[LevelManager] StartGame() → modo={mode}");
        gameMode = mode;
        partidaIniciada = true;


        if (mode == GameMode.Monedas)
        {
            levelBuilder.SpawnCoins();
            CoinsGenerated = levelBuilder.GetCoinsGenerated();
        }


        SetupUIForMode(mode);
    }



    private void SetupUIForMode(GameMode mode)
    {
        // Ocultamos siempre todos los textos:
        timeModeText?.gameObject.SetActive(false);
        coinLabelText?.gameObject.SetActive(false);
        coinValueText?.gameObject.SetActive(false);

        if (mode == GameMode.Tiempo)
        {
            timeModeText.gameObject.SetActive(true);
        }
        else if (mode == GameMode.Monedas)
        {
            // Averiguamos si yo soy zombi
            var localPlayerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var pc = localPlayerObj?.GetComponent<PlayerController>();
            bool soyZombie = pc != null && pc.isZombie;

            // Solo los humanos muestran el contador de monedas
            coinLabelText.gameObject.SetActive(!soyZombie);
            coinValueText.gameObject.SetActive(!soyZombie);
        }
    }




    private void ShowGameOverPanelCustom()
    {
        if (!IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject;
            if (player == null) continue;

            var pc = player.GetComponent<PlayerController>();
            if (pc == null) continue;

            string msg;

            if (ultimoHumanoId.HasValue && pc.OwnerClientId == ultimoHumanoId.Value)
            {
                msg = "Has sido el último humano. Derrota.";
            }
            else if (pc.isZombie)
            {
                msg = pc.isOriginalZombie ? "¡Victoria total para zombis originales!" : "¡Victoria parcial para zombis convertidos!";
            }
            
            else
            {
                msg = "Derrota.";
            }

            ShowGameOverClientRpc(msg, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { pc.OwnerClientId }
                }
            });
        }
    }

    [ClientRpc]
    private void ShowGameOverClientRpc(string mensaje, ClientRpcParams rpcParams = default)
    {
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var text = gameOverPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (text != null)
            text.text = mensaje;
    }


    public void CheckEndGameCondition()
    {
        if ( gameMode== GameMode.Tiempo)
        {
            if (numberOfHumans == 1)
            {
                // Busca y guarda el último humano antes de que se convierta
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var pc = client.PlayerObject?.GetComponent<PlayerController>();
                    if (pc != null && !pc.isZombie)
                    {
                        ultimoHumanoId = client.ClientId;
                        break;
                    }
                }
            }

            if (numberOfHumans == 0 && !isGameOver)
            {
                isGameOver = true;
                ShowGameOverPanelCustom();
            }
        }
        else if(gameMode == GameMode.Monedas)
        {
            

            if (coinsCollected.Value >= CoinsGenerated && !isGameOver)
            {
                isGameOver = true;
                // Recorremos todos los clientes para notificarles
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    var pc = client.PlayerObject.GetComponent<PlayerController>();
                    bool esZombie = pc.isZombie;

                    // Mensaje diferente según equipo
                    string msg = esZombie
                        ? "¡Derrota de los zombis!\nLos humanos han recogido todas las monedas."
                        : "¡Victoria de los humanos!\nHas recogido todas las monedas.";

                    // Enviamos el RPC a cada cliente individualmente
                    ShowGameOverClientRpc(
                        msg,
                        new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams
                            {
                                TargetClientIds = new[] { client.ClientId }
                            }
                        }
                    );
                }

            }

                
        
    }



    }


}




