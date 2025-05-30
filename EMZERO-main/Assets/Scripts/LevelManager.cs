using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

    public NetworkVariable<int> currentHumans = new NetworkVariable<int>();
    public NetworkVariable<int> currentZombies = new NetworkVariable<int>();

    [Header("Game Mode Settings")]
    [Tooltip("Selecciona el modo de juego")]
    [SerializeField] private GameMode gameMode;

    [Tooltip("Tiempo de partida en minutos para el modo tiempo")]
    [SerializeField] private int minutes = 5;

    private List<Vector3> humanSpawnPoints = new List<Vector3>();
    private List<Vector3> zombieSpawnPoints = new List<Vector3>();

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

    private bool partidaIniciada = false;
    public GameObject gameOverPanel;

    public GameObject GO_gameManager;
    private GameManager gameManager;

    #endregion

    #region Unity game loop methods

    private void Awake()
    {
        Debug.Log("Despertando el nivel");

        uniqueIdGenerator = GetComponent<UniqueIdGenerator>();
        levelBuilder = GetComponent<LevelBuilder>();
        gameManager = GO_gameManager.GetComponent<GameManager>();

        Time.timeScale = 1f;
    }

    private void Start()
    {
        Debug.Log("Iniciando el nivel");

        GameObject canvas = GameObject.Find("CanvasPlayer");
        if (canvas != null)
        {
            Debug.Log("Canvas encontrado");

            Transform panel = canvas.transform.Find("PanelHud");
            if (panel != null)
            {
                Debug.Log("PanelHud encontrado");
                humansText = panel.Find("HumansValue")?.GetComponent<TextMeshProUGUI>();
                if (humansText == null) Debug.LogError("HumansValue TextMeshProUGUI no encontrado.");

                zombiesText = panel.Find("ZombiesValue")?.GetComponent<TextMeshProUGUI>();
                if (zombiesText == null) Debug.LogError("ZombiesValue TextMeshProUGUI no encontrado.");

                timeModeText = panel.Find("GameModeConditionValue")?.GetComponent<TextMeshProUGUI>();
                if (timeModeText == null) Debug.LogError("GameModeConditionValue TextMeshProUGUI no encontrado.");

                coinValueText = panel.Find("CoinsValue")?.GetComponent<TextMeshProUGUI>();
                if (coinValueText == null) Debug.LogError("CoinsValue TextMeshProUGUI no encontrado.");

                coinLabelText = panel.Find("Coins")?.GetComponent<TextMeshProUGUI>();
                if (coinLabelText == null) Debug.LogError("Coins TextMeshProUGUI no encontrado.");
            }
            else
            {
                Debug.LogError("PanelHud no encontrado dentro de CanvasPlayer.");
            }
        }
        else
        {
            Debug.LogError("CanvasPlayer no encontrado en la escena.");
        }

        remainingSeconds = minutes * 60;

        if (levelBuilder != null)
        {
            levelBuilder.Build();
            humanSpawnPoints = levelBuilder.GetHumanSpawnPoints();
            zombieSpawnPoints = levelBuilder.GetZombieSpawnPoints();
            CoinsGenerated = levelBuilder.GetCoinsGenerated();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Debug para verificar los valores de StartGameVariables antes de la asignación
            Debug.Log($"[Server] StartGameVariables - HumanList Count: {StartGameVariables.Instance.humanList.Count}");
            Debug.Log($"[Server] StartGameVariables - ZombieList Count: {StartGameVariables.Instance.zombieList.Count}");

            currentHumans.Value = StartGameVariables.Instance.humanList.Count;
            currentZombies.Value = StartGameVariables.Instance.zombieList.Count;
            Debug.Log($"[Server] Initializing currentHumans: {currentHumans.Value}, currentZombies: {currentZombies.Value}");

            SpawnTeams();
        }

        currentHumans.OnValueChanged += OnHumanCountChanged;
        currentZombies.OnValueChanged += OnZombieCountChanged;

        // Esta llamada debería actualizar el UI con los valores iniciales.
        // Si no se actualiza, los valores de currentHumans.Value o currentZombies.Value
        // podrían ser 0 en ese momento debido a StartGameVariables.
        UpdateTeamUI();
    }

    public override void OnNetworkDespawn()
    {
        currentHumans.OnValueChanged -= OnHumanCountChanged;
        currentZombies.OnValueChanged -= OnZombieCountChanged;
    }

    private void Update()
    {
        if (!partidaIniciada)
            return;

        if (gameMode == GameMode.Tiempo)
        {
            HandleTimeLimitedGameMode();
        }
        else if (gameMode == GameMode.Monedas)
        {
            HandleCoinBasedGameMode();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (IsClient && IsOwner)
            {
                GameObject localPlayer = GameObject.FindGameObjectWithTag("Player");
                if (localPlayer != null)
                {
                    if (localPlayer.name.Contains(playerPrefab.name))
                    {
                        RequestChangeToZombieServerRpc(localPlayer.GetComponent<NetworkObject>().NetworkObjectId);
                    }
                    else
                    {
                        Debug.Log("El jugador actual no es un humano.");
                    }
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            if (IsClient && IsOwner)
            {
                GameObject localPlayer = GameObject.FindGameObjectWithTag("Player");
                if (localPlayer != null)
                {
                    if (localPlayer.name.Contains(zombiePrefab.name))
                    {
                        RequestChangeToHumanServerRpc(localPlayer.GetComponent<NetworkObject>().NetworkObjectId);
                    }
                    else
                    {
                        Debug.Log("El jugador actual no es un zombie.");
                    }
                }
            }
        }

        if (isGameOver)
        {
            ShowGameOverPanel();
        }
    }

    #endregion

    #region Team management methods

    [ServerRpc(RequireOwnership = false)]
    public void RequestChangeToZombieServerRpc(ulong humanNetworkObjectId)
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(humanNetworkObjectId, out NetworkObject humanNetworkObject))
        {
            ChangeCharacterOnServer(humanNetworkObject.gameObject, zombiePrefab, true);
        }
        else
        {
            Debug.LogWarning($"[Server] No se encontró NetworkObject con ID: {humanNetworkObjectId}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestChangeToHumanServerRpc(ulong zombieNetworkObjectId)
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(zombieNetworkObjectId, out NetworkObject zombieNetworkObject))
        {
            ChangeCharacterOnServer(zombieNetworkObject.gameObject, playerPrefab, false);
        }
        else
        {
            Debug.LogWarning($"[Server] No se encontró NetworkObject con ID: {zombieNetworkObjectId}");
        }
    }

    private void ChangeCharacterOnServer(GameObject oldCharacter, GameObject newPrefab, bool isBecomingZombie)
    {
        if (oldCharacter == null) return;

        NetworkObject oldNO = oldCharacter.GetComponent<NetworkObject>();
        ulong clientId = oldNO.OwnerClientId;
        string uniqueID = oldCharacter.GetComponent<PlayerController>().uniqueID;
        Vector3 pos = oldCharacter.transform.position;
        Quaternion rot = oldCharacter.transform.rotation;

        oldNO.Despawn(true);

        GameObject newCharacter = Instantiate(newPrefab, pos, rot);
        NetworkObject newNO = newCharacter.GetComponent<NetworkObject>();

        newNO.SpawnAsPlayerObject(clientId);

        PlayerController pc = newCharacter.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.uniqueID = uniqueID;
            pc.isZombie = isBecomingZombie;
        }

        if (isBecomingZombie)
        {
            currentHumans.Value--;
            currentZombies.Value++;
            Debug.Log($"[Server] Converted to Zombie. Humans: {currentHumans.Value}, Zombies: {currentZombies.Value}");
        }
        else
        {
            currentHumans.Value++;
            currentZombies.Value--;
            Debug.Log($"[Server] Converted to Human. Humans: {currentHumans.Value}, Zombies: {currentZombies.Value}");
        }
    }

    private void OnHumanCountChanged(int oldVal, int newVal)
    {
        UpdateTeamUI();
        Debug.Log($"[Client/Server] Human count changed from {oldVal} to {newVal}");
    }

    private void OnZombieCountChanged(int oldVal, int newVal)
    {
        UpdateTeamUI();
        Debug.Log($"[Client/Server] Zombie count changed from {oldVal} to {newVal}");
    }

    private void UpdateTeamUI()
    {
        if (humansText != null)
        {
            humansText.text = $"{currentHumans.Value}";
        }

        if (zombiesText != null)
        {
            zombiesText.text = $"{currentZombies.Value}";
        }
    }

    private void SpawnPlayer(Vector3 spawnPosition, GameObject prefab)
    {
        if (!IsServer) return;

        Debug.Log($"[Server] Instanciando jugador en {spawnPosition}");
        if (prefab != null)
        {
            GameObject player = Instantiate(prefab, spawnPosition, Quaternion.identity);
            player.tag = "Player";

            NetworkObject playerNO = player.GetComponent<NetworkObject>();
            if (playerNO != null)
            {
                playerNO.Spawn();
            }
            else
            {
                Debug.LogError("El prefab del jugador no tiene un NetworkObject.");
            }
        }
        else
        {
            Debug.LogError("Faltan referencias al prefab o al punto de aparición.");
        }
    }

    private void SpawnTeams()
    {
        if (!IsServer) return;

        Debug.Log("[Server] Instanciando equipos");

        int humanIndex = 0;
        int zombieIndex = 0;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (StartGameVariables.Instance.humanList.Contains(clientId))
            {
                if (humanIndex < humanSpawnPoints.Count)
                {
                    GameObject humanInstance = Instantiate(playerPrefab, humanSpawnPoints[humanIndex], Quaternion.identity);
                    NetworkObject humanNO = humanInstance.GetComponent<NetworkObject>();
                    humanNO.SpawnAsPlayerObject(clientId);
                    humanIndex++;
                }
            }
            else if (StartGameVariables.Instance.zombieList.Contains(clientId))
            {
                if (zombieIndex < zombieSpawnPoints.Count)
                {
                    GameObject zombieInstance = Instantiate(zombiePrefab, zombieSpawnPoints[zombieIndex], Quaternion.identity);
                    NetworkObject zombieNO = zombieInstance.GetComponent<NetworkObject>();
                    zombieNO.SpawnAsPlayerObject(clientId);
                    PlayerController pc = zombieInstance.GetComponent<PlayerController>();
                    if (pc != null) pc.isZombie = true;
                    zombieIndex++;
                }
            }
        }
    }

    private void SpawnNonPlayableCharacter(GameObject prefab, Vector3 spawnPosition)
    {
        if (!IsServer) return;

        if (prefab != null)
        {
            GameObject npc = Instantiate(prefab, spawnPosition, Quaternion.identity);
            NetworkObject npcNO = npc.GetComponent<NetworkObject>();
            if (npcNO != null)
            {
                npcNO.Spawn();
            }
            else
            {
                Debug.LogError("El prefab del NPC no tiene un NetworkObject.");
            }

            var playerController = npc.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
                playerController.uniqueID = uniqueIdGenerator.GenerateUniqueID();
            }
            Debug.Log($"[Server] Personaje no jugable instanciado en {spawnPosition}");
        }
    }

    #endregion

    #region Modo de juego

    private void HandleTimeLimitedGameMode()
    {
        if (isGameOver) return;

        remainingSeconds -= Time.deltaTime;

        if (remainingSeconds <= 0)
        {
            isGameOver = true;
            remainingSeconds = 0;
        }

        int minutesRemaining = Mathf.FloorToInt(remainingSeconds / 60);
        int secondsRemaining = Mathf.FloorToInt(remainingSeconds % 60);

        if (timeModeText != null)
        {
            timeModeText.text = $"{minutesRemaining:D2}:{secondsRemaining:D2}";
        }
    }

    private void HandleCoinBasedGameMode()
    {
        if (isGameOver) return;

        if (timeModeText != null && playerController != null)
        {
            timeModeText.text = $"{playerController.CoinsCollected}/{CoinsGenerated}";
            if (playerController.CoinsCollected == CoinsGenerated)
            {
                isGameOver = true;
            }
        }
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            Time.timeScale = 0f;
            gameOverPanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ReturnToMainMenu()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene("MenuScene");
    }

    public Vector3 GetHumanSpawnPoint(int index)
    {
        if (index >= 0 && index < humanSpawnPoints.Count)
        {
            return humanSpawnPoints[index];
        }

        Debug.LogWarning("Índice de spawn fuera de rango. Se usará el primer punto.");
        return humanSpawnPoints[0];
    }

    public Vector3 GetZombieSpawnPoint(int index)
    {
        if (index >= 0 && index < zombieSpawnPoints.Count)
        {
            return zombieSpawnPoints[index];
        }

        Debug.LogWarning("Índice de spawn fuera de rango. Se usará el primer punto.");
        return zombieSpawnPoints[0];
    }

    #endregion

    public void StartGame(GameMode mode)
    {
        if (IsServer)
        {
            Debug.Log($"[LevelManager] StartGame() → modo={mode}");
            gameMode = mode;
            partidaIniciada = true;
            SetupUIForMode(mode);
        }
        else if (IsClient)
        {
            SetupUIForMode(mode);
        }
    }

    private void SetupUIForMode(GameMode mode)
    {
        timeModeText?.gameObject.SetActive(false);
        coinLabelText?.gameObject.SetActive(false);
        coinValueText?.gameObject.SetActive(false);

        if (mode == GameMode.Tiempo)
        {
            timeModeText.gameObject.SetActive(true);
        }
        else if (mode == GameMode.Monedas)
        {
            coinLabelText.gameObject.SetActive(true);
            coinValueText.gameObject.SetActive(true);
        }
    }
}
