using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    int numHumans;
    int numZombies;

    public GameObject humanoPrefab;
    public GameObject zombiePrefab;

    private NetworkList<ulong> humanList = new NetworkList<ulong>();
    private NetworkList<ulong> zombieList = new NetworkList<ulong>();

    private List<ulong> jugadoresPendientes = new List<ulong>();
    private bool partidaIniciada = false;

    private GameObject _GOlevelManager;
    private LevelManager _levelManager;

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

        numHumans = _levelManager.GetNumHumans();
        numZombies = _levelManager.GetNumZombies();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        humanList.Dispose();
        zombieList.Dispose();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.ConnectionApprovalCallback += ApproveConnection;
            NetworkManager.OnClientConnectedCallback += OnClientConnected;

            // Suscribimos un listener para cuando el servidor cambie el modo
            SelectedGameMode.OnValueChanged += (oldMode, newMode) =>
            {
                gameModeChosen = true;
                Debug.Log($"Modo de juego seleccionado: {newMode}");
                TryStartMatchIfReady();

            };
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
            Debug.Log($"Jugador {clientId} rechazado: equipos llenos");
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
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
            && humanList.Count == numHumans
            && zombieList.Count == numZombies
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
            GameObject player = Instantiate(prefab, GetSpawnPoint(equipo), Quaternion.identity);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

            Debug.Log($"Jugador {clientId} spawneado como {equipo}");

            EnviarEquipoClientRpc(equipo, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
        }

        jugadoresPendientes.Clear();
    }

    private string AsignarEquipo(ulong clientId)
    {
        if (humanList.Count >= numHumans && zombieList.Count >= numZombies)
        {
            Debug.Log("Equipos completados");
            return "ninguno";
        }

        if (humanList.Count >= numHumans)
        {
            zombieList.Add(clientId);
            return "zombie";
        }

        if (zombieList.Count >= numZombies)
        {
            humanList.Add(clientId);
            return "human";
        }

        if (UnityEngine.Random.value < 0.5f)
        {
            humanList.Add(clientId);
            return "human";
        }
        else
        {
            zombieList.Add(clientId);
            return "zombie";
        }
    }

    private Vector3 GetSpawnPoint(string equipo)
    {
        return _levelManager.GetSpawnPoint(0);
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
}
