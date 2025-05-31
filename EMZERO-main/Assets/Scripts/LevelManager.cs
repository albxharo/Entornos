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

// Definicion de los modos de juego disponibles
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
    [SerializeField] private GameObject playerPrefab;   // Prefab del jugador humano
    [SerializeField] private GameObject zombiePrefab;   // Prefab del zombi

    [Header("Team Settings")]
    [Tooltip("Numero de jugadores humanos")]
    [SerializeField] private int numberOfHumans = 2;    // Cantidad inicial de humanos

    [Tooltip("Numero de zombis")]
    [SerializeField] private int numberOfZombies = 2;   // Cantidad inicial de zombis

    [Header("Game Mode Settings")]
    [Tooltip("Selecciona el modo de juego")]
    [SerializeField] private GameMode gameMode;         // Modo de juego seleccionado

    // Listas de puntos de aparicion para humanos y zombis
    public List<Vector3> humanSpawnPoints = new List<Vector3>();
    public List<Vector3> zombieSpawnPoints = new List<Vector3>();

    // Variable de red para llevar el conteo total de monedas generadas
    public NetworkVariable<int> totalCoins = new NetworkVariable<int>(0,
        writePerm: NetworkVariableWritePermission.Server);

    // Referencias a elementos de texto en el canvas
    private TextMeshProUGUI humansText;     // Texto que muestra numero de humanos restantes
    private TextMeshProUGUI zombiesText;    // Texto que muestra numero de zombis restantes
    private TextMeshProUGUI timeModeText;   // Texto que muestra tiempo restante o monedas recolectadas
    private TextMeshProUGUI coinValueText;  // Texto que muestra valor actual de monedas
    private TextMeshProUGUI coinLabelText;  // Texto que muestra label de monedas

    // Propiedades para obtener nombres de prefabs
    public string PlayerPrefabName => playerPrefab.name;
    public string ZombiePrefabName => zombiePrefab.name;

    private UniqueIdGenerator uniqueIdGenerator; // Componente para generar IDs unicos
    private LevelBuilder levelBuilder;           // Componente para construir el nivel

    private PlayerController playerController;   // Referencia al controlador del jugador

    public NetworkVariable<float> remainingSeconds = new NetworkVariable<float>(
     0f,
     writePerm: NetworkVariableWritePermission.Server,
     readPerm: NetworkVariableReadPermission.Everyone
 );           // Segundos restantes en modo tiempo

    public bool isGameOver = false;              // Indica si la partida ha terminado
    private ulong? ultimoHumanoId = null;        // ID del ultimo humano antes de conversion

    // Estado local para saber si la partida ha iniciado
    private bool partidaIniciada = false;
    public GameObject gameOverPanel;             // Panel de Game Over (asignado en Inspector)

    public GameObject GO_gameManager;            // Referencia al objeto GameManager
    private GameManager gameManager;             // Componente GameManager

    // Variable de red para llevar las monedas recolectadas por humanos
    public NetworkVariable<int> coinsCollected = new NetworkVariable<int>(0);

    #endregion

    #region Unity game loop methods

    private void Awake()
    {
        Debug.Log("Despertando el nivel");

        // Obtener referencia a UniqueIdGenerator en el mismo GameObject
        uniqueIdGenerator = GetComponent<UniqueIdGenerator>();

        // Obtener referencia a LevelBuilder en el mismo GameObject
        levelBuilder = GetComponent<LevelBuilder>();

        // Obtener referencia al GameManager desde el GameObject asignado
        gameManager = GO_gameManager.GetComponent<GameManager>();

        // Asegurarse de que el tiempo no este pausado
        Time.timeScale = 1f;
    }

    private void Start()
    {
        Debug.Log("Iniciando el nivel");

        // Obtener listas de humanos y zombis desde variables globales de inicio de juego
        numberOfHumans = StartGameVariables.Instance.humanList.Count;
        numberOfZombies = StartGameVariables.Instance.zombieList.Count;

        // Buscar el objeto "CanvasPlayer" en la escena para obtener referencias a UI
        GameObject canvas = GameObject.Find("CanvasPlayer");
        if (canvas != null)
        {
            Debug.Log("Canvas encontrado");

            // Buscar el panel "PanelHud" dentro del Canvas
            Transform panel = canvas.transform.Find("PanelHud");
            if (panel != null)
            {
                // Buscar los objetos de texto dentro del HUD
                Transform humansTextTransform = panel.Find("HumansValue");
                Transform zombiesTextTransform = panel.Find("ZombiesValue");
                Transform gameModeTextTransform = panel.Find("GameModeConditionValue");
                Transform coinsTextTransform = panel.Find("CoinsValue");
                Transform coinscoinTextTransform = panel.Find("Coins");

                // Si existen, obtener componentes TextMeshProUGUI
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

        // Convertir minutos configurados a segundos
        remainingSeconds.Value = StartGameVariables.Instance.minutes * 60;

        // Si existe el LevelBuilder, construir el nivel y obtener puntos de spawn
        if (levelBuilder != null)
        {
            levelBuilder.Build();
            humanSpawnPoints = levelBuilder.GetHumanSpawnPoints();
            zombieSpawnPoints = levelBuilder.GetZombieSpawnPoints();
        }

        // Actualizar la UI de equipos (numero de humanos y zombis)
        UpdateTeamUI();
    }

    private void Update()
    {
        // Si la partida no ha iniciado, no ejecutamos logica de tiempo o monedas
        if (!partidaIniciada)
            return;

        // Dependiendo del modo de juego, llamar a la logica correspondiente
        if (gameMode == GameMode.Tiempo)
        {// Sólo el servidor debe restar al temporizador.
            if (IsServer)
            {
                HandleTimeLimitedGameMode();
            }
            // Independientemente, todos (host y clientes) deben actualizar el texto en pantalla
            UpdateTimeUIFromNetworkVariable();
        }
        else if (gameMode == GameMode.Monedas)
        {
            HandleCoinBasedGameMode();
        }

        // Logica de prueba para convertir jugador a zombi/humano con teclas Z y H
        if (Input.GetKeyDown(KeyCode.Z)) // Presiona Z para convertirte en Zombi
        {
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
        else if (Input.GetKeyDown(KeyCode.H)) // Presiona H para convertirte en Humano
        {
            GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
            if (currentPlayer != null && currentPlayer.name.Contains(zombiePrefab.name))
            {
                ChangeToHuman();
            }
            else
            {
                Debug.Log("El jugador actual no es un zombi.");
            }
        }

        // Actualizar UI de equipos cada fotograma
        UpdateTeamUI();

        // Si no quedan humanos y no se ha terminado la partida, ganan zombies
        if (numberOfHumans == 0 && !isGameOver)
        {
            isGameOver = true;
            ShowGameOverZombiesWin();
        }

        // Si la partida termino, mostrar panel de Game Over
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
            // Inicializar contador de monedas recolectadas en el servidor
            coinsCollected.Value = 0;
        }

        // Suscribirse a cambios de la variable de red coinsCollected para actualizar UI
        coinsCollected.OnValueChanged += OnCoinsChanged;
    }

  

    private void UpdateTimeUIFromNetworkVariable()
    {
        if (timeModeText == null) return;

        // Convertir segundos a mm:ss
        float segundosTotales = remainingSeconds.Value;
        int minutosRestantes = Mathf.FloorToInt(segundosTotales / 60f);
        int segundos = Mathf.FloorToInt(segundosTotales % 60f);
        timeModeText.text = $"{minutosRestantes:D2}:{segundos:D2}";
    }



    #region Team management methods

    private void ChangeToZombie()
    {
        // Obtener el jugador actual y convertirlo
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        ChangeToZombie(currentPlayer, true);
    }

    public void ChangeToZombie(GameObject human, bool enabled)
    {
        if (human == null) return;

        // Obtener NetworkObject y datos de posicion/rotacion del humano
        NetworkObject oldNO = human.GetComponent<NetworkObject>();
        ulong clientId = oldNO.OwnerClientId;
        string uniqueID = human.GetComponent<PlayerController>().uniqueID;
        Vector3 pos = human.transform.position;
        Quaternion rot = human.transform.rotation;

        // Destruir el objeto humano
        Destroy(human);

        // Instanciar prefab de zombi en la misma posicion y rotacion
        GameObject zombie = Instantiate(zombiePrefab, pos, rot);
        var zombieNO = zombie.GetComponent<NetworkObject>();
        zombieNO.SpawnAsPlayerObject(clientId); // Asignar control al mismo jugador

        // Configurar el PlayerController del zombi
        var pc = zombie.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.enabled = enabled;
            pc.isZombie = true;
            pc.uniqueID = uniqueID;

            // Si es el cliente local, actualizar camara y UI
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

            // Ajustar contadores de equipos
            numberOfHumans--;
            numberOfZombies++;
            UpdateTeamUI();
        }
    }

    private void ChangeToHuman()
    {
        Debug.Log("Cambiando a Humano");

        // Obtener referencia al jugador actual
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer != null)
        {
            // Guardar posicion y rotacion del jugador actual
            Vector3 playerPosition = currentPlayer.transform.position;
            Quaternion playerRotation = currentPlayer.transform.rotation;

            // Destruir el zombi o humano actual
            Destroy(currentPlayer);

            // Instanciar el prefab del humano en la misma posicion y rotacion
            GameObject human = Instantiate(playerPrefab, playerPosition, playerRotation);
            human.tag = "Player";

            // Obtener la camara principal
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                // Obtener script CameraController y asignar el jugador
                CameraController cameraController = mainCamera.GetComponent<CameraController>();
                if (cameraController != null)
                {
                    cameraController.player = human.transform;
                }

                // Configurar PlayerController del humano instanciado
                playerController = human.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.enabled = true;
                    playerController.cameraTransform = mainCamera.transform;
                    playerController.isZombie = false; // Cambiar estado a humano
                    numberOfHumans++;   // Incrementar numero de humanos
                    numberOfZombies--;  // Decrementar numero de zombis
                }
                else
                {
                    Debug.LogError("PlayerController no encontrado en el humano instanciado.");
                }
            }
            else
            {
                Debug.LogError("No se encontro la camara principal.");
            }
        }
        else
        {
            Debug.LogError("No se encontro el jugador actual.");
        }
    }

    private void UpdateTeamUI()
    {
        // Actualizar texto de humanos
        if (humansText != null)
        {
            humansText.text = $"{numberOfHumans}";
        }

        // Actualizar texto de zombis
        if (zombiesText != null)
        {
            zombiesText.text = $"{numberOfZombies}";
        }
    }

    #endregion

    #region Modo de juego

    private void HandleTimeLimitedGameMode()
    {
        // Logica para el modo de juego basado en tiempo
        if (isGameOver) return;

        // Reducir remainingSeconds segun tiempo transcurrido
        remainingSeconds.Value -= Time.deltaTime;

        // Comprobar si el tiempo se ha agotado
        if (remainingSeconds.Value <= 0)
        {
            remainingSeconds.Value = 0;
            CheckEndGameCondition();
        }

        // Convertir segundos restantes a minutos y segundos
        int minutesRemaining = Mathf.FloorToInt(remainingSeconds.Value / 60);
        int secondsRemaining = Mathf.FloorToInt(remainingSeconds.Value % 60);

        // Actualizar texto de tiempo en la UI
        if (timeModeText != null)
        {
            timeModeText.text = $"{minutesRemaining:D2}:{secondsRemaining:D2}";
        }
    }

    private void HandleCoinBasedGameMode()
    {
        // Logica para el modo de juego basado en monedas
        if (isGameOver) return;

        if (timeModeText != null && playerController != null)
        {
            timeModeText.text = $"{playerController.CoinsCollected}/{totalCoins.Value}";
            if (playerController.CoinsCollected >= (int)totalCoins.Value)
            {
                isGameOver = true;
            }
        }
    }

    private void OnCoinsChanged(int oldValue, int newValue)
    {
        // Actualizar la UI de monedas en cada cliente cuando cambia
        if (coinValueText != null)
            coinValueText.text = $"{newValue}/{totalCoins.Value}";
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            // Pausar la partida
            Time.timeScale = 0f;
            // Mostrar el panel de Game Over
            gameOverPanel.SetActive(true);

            // Ajustar cursor para permitir interactuar con UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Llamado en tu UI cuando el jugador pulsa "Volver al Menú"
    public void ReturnToMainMenu()
    {
        // Versión LOCAL: (NO nos sirve en red)
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
        // SceneManager.LoadScene("MenuScene");

        // Versión CORRECTA en red:
        if (!IsServer)
        {
            // Si soy un cliente, pido permiso al servidor para cambiar de escena
            AskServerToReturnToMenuServerRpc();
        }
        else
        {
            // Si soy el host/servidor, puedo invocar directamente la carga
            ReturnToMenuAsServer();
        }
    }

    // Este ServerRpc lo podrá invocar cualquier cliente (RequireOwnership = false),
    // para que el servidor cargue la escena en todos.
    [ServerRpc(RequireOwnership = false)]
    private void AskServerToReturnToMenuServerRpc(ServerRpcParams rpcParams = default)
    {
        ReturnToMenuAsServer();
    }

    // Método que realmente hace que el servidor (host) cambie de escena en red
    private void ReturnToMenuAsServer()
    {
        // Opcional: restaurar el cursor y cualquier estado antes de cargar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ESTA es la línea CLAVE: 
        // el servidor invoca NetworkManager.SceneManager.LoadScene para propagar
        // la orden a todos los clientes.
        NetworkManager.SceneManager.LoadScene(
            "MenuScene",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }

    public Vector3 GetHumanSpawnPoint(int index)
    {
        // Devolver punto de spawn valido o Vector3.zero si index fuera de rango
        if (index >= 0 && index < humanSpawnPoints.Count)
        {
            return humanSpawnPoints[index];
        }

        Debug.LogWarning("Indice de spawn fuera de rango. Se usara el primer punto.");
        return Vector3.zero;
    }

    public Vector3 GetZombieSpawnPoint(int index)
    {
        // Devolver punto de spawn valido o Vector3.zero si index fuera de rango
        if (index >= 0 && index < zombieSpawnPoints.Count)
        {
            return zombieSpawnPoints[index];
        }

        Debug.LogWarning("Indice de spawn fuera de rango. Se usara el primer punto.");
        return Vector3.zero;
    }
    #endregion

    // Metodo para iniciar la partida, llamado desde un RPC
    public void StartGame(GameMode mode)
    {
        Debug.Log($"[LevelManager] StartGame() → modo={mode}");
        gameMode = mode;
        partidaIniciada = true;

        if (mode == GameMode.Monedas)
        {
            levelBuilder.SpawnCoins();
            totalCoins.Value = levelBuilder.GetCoinsGenerated();
            coinsCollected.Value = 0;
        }
        else if (mode == GameMode.Tiempo)
        {
            // SOLO el servidor asigna el valor inicial de remainingSeconds
            if (IsServer)
            {
                float segundosIniciales = StartGameVariables.Instance.minutes * 60f;
                remainingSeconds.Value = segundosIniciales;
            }
        }

        SetupUIForMode(mode);
    }


    private void SetupUIForMode(GameMode mode)
    {
        // Ocultar todos los textos relacionados con tiempo y monedas
        timeModeText?.gameObject.SetActive(false);
        coinLabelText?.gameObject.SetActive(false);
        coinValueText?.gameObject.SetActive(false);

        if (mode == GameMode.Tiempo)
        {
            // Mostrar contador de tiempo en modo Tiempo
            timeModeText.gameObject.SetActive(true);
        }
        else if (mode == GameMode.Monedas)
        {
            // Verificar si el jugador local es zombi
            var localPlayerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var pc = localPlayerObj?.GetComponent<PlayerController>();
            bool soyZombie = pc != null && pc.isZombie;

            // Solo los humanos muestran el contador de monedas
            coinLabelText.gameObject.SetActive(!soyZombie);
            coinValueText.gameObject.SetActive(!soyZombie);
        }
    }

    private void ShowGameOverZombiesWin()
    {
        if (!IsServer) return;

        // Enviar mensaje de Game Over a cada cliente segun si eran humanos o zombis
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject;
            if (player == null) continue;

            var pc = player.GetComponent<PlayerController>();
            if (pc == null) continue;

            string msg;

            if (ultimoHumanoId.HasValue && pc.OwnerClientId == ultimoHumanoId.Value)
            {
                msg = "Has sido el ultimo humano. Derrota.";
            }
            else if (pc.isZombie)
            {
                msg = pc.isOriginalZombie ? "¡Victoria total para zombis originales!" : "¡Victoria parcial para zombis convertidos!";
            }
            else
            {
                msg = "Derrota.";
            }

            // Llamar a ClientRpc para mostrar mensaje de Game Over en cada cliente
            ShowGameOverClientRpc(msg, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { pc.OwnerClientId }
                }
            });
        }
    }

    private void ShowGameOverHumansWin()
    {
        if (!IsServer) return;

        // Enviar mensaje de victoria a todos los clientes
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var pc = client.PlayerObject?.GetComponent<PlayerController>();
            if (pc == null) continue;

            string msg = pc.isZombie
                ? "¡Derrota de los zombis!\nAlgunos humanos han sobrevivido."
                : "¡Victoria de los humanos!\nHas sobrevivido hasta el final.";

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
        // Pausar el juego y mostrar panel de Game Over en cliente
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);

        // Ajustar cursor para UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Actualizar texto del Game Over con el mensaje recibido
        var text = gameOverPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (text != null)
            text.text = mensaje;
    }

    public void CheckEndGameCondition()
    {
        if (gameMode == GameMode.Tiempo)
        {
            // Si solo queda un humano, guardar su ID antes de conversion
            if (numberOfHumans == 1)
            {
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

            // Si no quedan humanos, ganar zombies
            if (numberOfHumans == 0 && !isGameOver)
            {
                isGameOver = true;
                ShowGameOverZombiesWin();
            }

            // Si se acabo el tiempo y quedan humanos, ganan humanos
            if (remainingSeconds.Value <= 0 && numberOfHumans > 0 && !isGameOver)
            {
                isGameOver = true;
                ShowGameOverHumansWin();
            }
        }
    }

    #region RPCs para Game Over en modo Monedas

    /// <summary>
    /// Metodo que llama DetectPlayerCollision en el cliente. Corre en el servidor:
    /// despawnea moneda, actualiza contador y, si toca, dispara TriggerGameOverOnAllClients().
    /// </summary>
    /// <param name="rpcParams"></param>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitPickupAndCheckServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[SubmitPickupAndCheckServerRpc] Servidor recibio peticion del cliente {rpcParams.Receive.SenderClientId}");

        // Obtener NetworkObject de la moneda (script en mismo GameObject)
        var thisNO = GetComponent<NetworkObject>();
        if (thisNO != null && thisNO.IsSpawned)
        {
            // Despawnear la moneda en red
            thisNO.Despawn();
            Debug.Log("[SubmitPickupAndCheckServerRpc] Moneda despawneada en red.");
        }
        else
        {
            Debug.LogWarning("[SubmitPickupAndCheckServerRpc] El NetworkObject de la moneda es nulo o ya estaba despawneado.");
        }

        // Incrementar coinsCollected en el servidor
        var lm = FindObjectOfType<LevelManager>();
        if (lm != null)
        {
            int antes = lm.coinsCollected.Value;
            lm.coinsCollected.Value += 1;
            Debug.Log($"[SubmitPickupAndCheckServerRpc] coinsCollected: {antes} → {lm.coinsCollected.Value} (totalCoins = {lm.totalCoins.Value})");

            // Comprobar if fin de juego en modo monedas
            if (!lm.isGameOver && lm.coinsCollected.Value >= lm.totalCoins.Value && lm.coinsCollected.Value > 0)
            {
                Debug.Log("[SubmitPickupAndCheckServerRpc] Condicion Monedas cumplida: Game Over → llamando a TriggerGameOverOnAllClients()");
                lm.isGameOver = true;
                lm.TriggerGameOverOnAllClients();
            }
            else
            {
                Debug.Log("[SubmitPickupAndCheckServerRpc] Aun no toca Game Over (o ya isGameOver=true).");
            }
        }
        else
        {
            Debug.LogError("[SubmitPickupAndCheckServerRpc] NO se encontro LevelManager para actualizar monedas.");
        }
    }

    /// <summary>
    /// Envia a cada cliente un mensaje de Game Over (Monedas). Corre en el servidor.
    /// </summary>
    public void TriggerGameOverOnAllClients()
    {
        Debug.Log("[TriggerGameOverOnAllClients] Servidor va a enviar Game Over a cada cliente.");

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var pc = client.PlayerObject.GetComponent<PlayerController>();
            if (pc == null) continue;

            bool esZombie = pc.isZombie;
            string msg = esZombie
                ? "¡Derrota de los zombis!\nLos humanos han recogido todas las monedas."
                : "¡Victoria de los humanos!\nHas recogido todas las monedas.";

            Debug.Log($"[TriggerGameOverOnAllClients] Enviando a Cliente {client.ClientId}: esZombie={esZombie}, msg=\"{msg}\"");
            ShowGameOverCoinClientRpc(
                msg,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { client.ClientId } }
                }
            );
        }
    }

    /// <summary>
    /// ClientRpc que corre en cada cliente destino. Muestra el panel de Game Over.
    /// </summary>
    [ClientRpc]
    private void ShowGameOverCoinClientRpc(string mensaje, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[ShowGameOverClientRpc] Cliente {NetworkManager.Singleton.LocalClientId} recibio Game Over: \"{mensaje}\"");

        // Pausar la partida local
        Time.timeScale = 0f;

        // Mostrar el panel de Game Over y actualizar texto
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            var textoTMP = gameOverPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textoTMP != null)
            {
                textoTMP.text = mensaje;
            }
            else
            {
                Debug.LogError("[ShowGameOverClientRpc] No se encontro TextMeshProUGUI dentro de gameOverPanel.");
            }
        }
        else
        {
            Debug.LogError("[ShowGameOverClientRpc] gameOverPanel NO esta asignado en el Inspector.");
        }

        // Asegurar que el cursor sea visible y desbloqueado
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    #endregion
}
