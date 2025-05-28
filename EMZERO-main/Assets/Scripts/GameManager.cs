using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    int numHumans;
    int numZombies;

    public GameObject humanoPrefab;
    public GameObject zombiePrefab;

    private NetworkList<ulong> humanList = new NetworkList<ulong>();
    private NetworkList<ulong> zombieList = new NetworkList<ulong>();

    private NetworkList<ulong> readyPlayersList = new NetworkList<ulong>();


    private List<ulong> jugadoresPendientes = new List<ulong>();
    private bool partidaIniciada = false;

    private GameObject _GOlevelManager;
    private LevelManager _levelManager;

    private GameObject canvas;
    public GameObject readycanvas;

    [SerializeField] private GameObject panelNombre;

    public NetworkVariable<int> lastTypePlayer =
        new NetworkVariable<int>(
             1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    [SerializeField] private TMP_InputField inputFieldNumJugadores;


    // Esto sincroniza el modo de juego a todos los clientes.
    public NetworkVariable<GameMode> SelectedGameMode =
        new NetworkVariable<GameMode>(
             GameMode.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    private bool gameModeChosen = false;

    private void Awake()
    {
        _GOlevelManager = GameObject.Find("LevelManager");
        _levelManager = _GOlevelManager.GetComponent<LevelManager>();

        canvas = GameObject.Find("CanvasPlayer");

        //Asegurarse de que el panel del nombre esté activo al inicio
        if (panelNombre != null)
            panelNombre.SetActive(true);

        numHumans = _levelManager.GetNumHumans();
        numZombies = _levelManager.GetNumZombies();
    }

    //Llamar a esto al confirmar el nombre desde tu UI
    public void ConfirmarNombreJugador()
    {
        if (panelNombre != null)
            panelNombre.SetActive(false);
    }

    public void OnNumPlayersChange(int numero)
    {
        numHumans = numero;
        numZombies = numero;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        humanList.Dispose();
        zombieList.Dispose();
        readyPlayersList.Dispose();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            //NetworkManager.ConnectionApprovalCallback += ApproveConnection;
            NetworkManager.OnClientConnectedCallback += OnClientConnected;

            // Suscribimos un listener para cuando el servidor cambie el modo
            SelectedGameMode.OnValueChanged += (oldMode, newMode) =>
            {
                gameModeChosen = true;
                Debug.Log($"Modo de juego seleccionado: {newMode}");
                TryStartMatchIfReady();

            };

            //////////////////////////
        }
    }

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        bool hayHueco = humanList.Count < numHumans || zombieList.Count < numZombies;

        response.Approved = hayHueco;
        response.CreatePlayerObject = false;
        response.Pending = false;

        if (!hayHueco)
        {
            Debug.Log("Conexión rechazada: equipos completos.");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer || partidaIniciada)
            return;

        string equipo = AsignarEquipo(clientId);

        if (equipo == "ninguno")
        {
            if(lastTypePlayer.Value == 0)
            {
                Debug.Log("Ultimo fue: zombie");
                lastTypePlayer.Value = 1;
                equipo = "human";
            }
            else
            {
                equipo = "zombie";
                lastTypePlayer.Value = 0;

                Debug.Log("Ultimo fue: humano");

            }
        }

        Debug.Log($"Jugador {clientId} asignado a {equipo}, esperando inicio de partida");

        jugadoresPendientes.Add(clientId);

        // Tras asignar equipos, intentamos arrancar
        TryStartMatchIfReady();
    }

    private void TryStartMatchIfReady()
    {
        // Sólo lanzar IniciarPartida cuando
        //  1) Teams completos, y
        //  2) El Host YA ha elegido modo
        if (gameModeChosen
            && readyPlayersList.Count > ((humanList.Count + zombieList.Count) / 2)
            && !partidaIniciada)
        {
            IniciarPartida();
        }
    }

    // ServerRpc público para que el Host lo invoque al elegir en UI
    [ServerRpc(RequireOwnership = false)]
    public void SelectGameModeServerRpc(GameMode mode)
    {
        SelectedGameMode.Value = mode;
        // No llamamos aquí directamente a IniciarPartida: 
        // lo hará TryStartMatchIfReady si ya están todos conectados.
    }
    private void IniciarPartida()
    {
        partidaIniciada = true;
        Debug.Log("Todos los jugadores listos. Iniciando partida...");


        foreach (ulong clientId in jugadoresPendientes)
        {
            string equipo = humanList.Contains(clientId) ? "human" : "zombie";

            GameObject prefab = equipo == "zombie" ? zombiePrefab : humanoPrefab;
            GameObject player = Instantiate(prefab, GetSpawnPoint(equipo, clientId), Quaternion.identity);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

            Debug.Log($"Jugador {clientId} spawneado como {equipo}");

            EnviarEquipoClientRpc(equipo, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
        }

        jugadoresPendientes.Clear();
        // avisamos a todos los clientes de que empiece el juego!
        StartGameClientRpc(SelectedGameMode.Value);
    }

    [ClientRpc]
    private void StartGameClientRpc(GameMode mode, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[GameManager] StartGameClientRpc recibido. Modo={mode}");
        var lm = FindObjectOfType<LevelManager>();
        if (lm != null)
            lm.StartGame(mode);
    }

    private string AsignarEquipo(ulong clientId)
    {
        if (humanList.Count >= numHumans && zombieList.Count >= numZombies)
        {
            if(humanList.Count == numHumans && zombieList.Count == numZombies)
            {
                Debug.Log("Equipos recién completados");
                lastTypePlayer.Value = 1; //para que si o si el siguiente sea zombie
                return "ninguno";

            }
            Debug.Log("Equipos completados");
            return "ninguno";
        }

        if (humanList.Count >= numHumans)
        {
            zombieList.Add(clientId);
            lastTypePlayer.Value = 0;
            return "zombie";
        }

        if (zombieList.Count >= numZombies)
        {
            humanList.Add(clientId);
            lastTypePlayer.Value = 1;

            return "human";
        }

        if (UnityEngine.Random.value < 0.5f)
        {
            humanList.Add(clientId);
            lastTypePlayer.Value = 1;

            return "human";
        }
        else
        {
            zombieList.Add(clientId);
            lastTypePlayer.Value = 0;

            return "zombie";
        }
    }

    private Vector3 GetSpawnPoint(string equipo, ulong clientId)
    {
        int index = 0;

        if(equipo == "zombie")
        {
            index = zombieList.IndexOf(clientId);
            return _levelManager.GetZombieSpawnPoint(index);
        }
        else
        {
            index = humanList.IndexOf(clientId);
            return _levelManager.GetHumanSpawnPoint(index);

        }
    }

    [ClientRpc]
    private void EnviarEquipoClientRpc(string equipo, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"Me han asignado al equipo: {equipo}");
    }

    private void OnDisable()
    {
        if (IsServer)
        {
            NetworkManager.ConnectionApprovalCallback -= ApproveConnection;
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    public void readyButtonPressed()
    {

        
            readyButtonpressedServerRpc(OwnerClientId);
            readycanvas.SetActive(false);
        
    }

    [ServerRpc(RequireOwnership = false)]
    public void readyButtonpressedServerRpc(ulong clientId)
    {
             readyPlayersList.Add(clientId);
            Debug.Log($"[Server] Jugador {clientId} está listo.");
            TryStartMatchIfReady();
        
    }
}
