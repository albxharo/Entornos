using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{

    public GameObject humanoPrefab;
    public GameObject zombiePrefab;


    // Lista local para jugadores aún no spawneados
    //private bool partidaIniciada = false;

    private GameObject _GOlevelManager;
    private LevelManager _levelManager;

    private GameObject canvas;
    public GameObject readycanvas;

    public GameObject nicknameManager;
    public NicknameSelector nicknameSelector;




    [SerializeField] private TMP_InputField inputFieldNumJugadores;




   // private bool gameModeChosen = false;

    private void Awake()
    {
        _GOlevelManager = GameObject.Find("LevelManager");
        _levelManager = _GOlevelManager.GetComponent<LevelManager>();

        canvas = GameObject.Find("CanvasPlayer");

    }



    public void Start()
    {
        //NetworkManager.SceneManager.OnLoad += IniciarPartida();
        IniciarPartida();
    }


    /*
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
    }*/

    

    // ServerRpc público para que el Host lo invoque al elegir en UI
    /*[ServerRpc(RequireOwnership = false)]
    public void SelectGameModeServerRpc(GameMode mode)
    {
        SelectedGameMode.Value = mode;
        // No llamamos aquí directamente a IniciarPartida: 
        // lo hará TryStartMatchIfReady si ya están todos conectados.
    }*/
    private void IniciarPartida()
    {
        //partidaIniciada = true;
        Debug.Log("Todos los jugadores listos. Iniciando partida...");


        foreach (ulong clientId in StartGameVariables.Instance.jugadoresPendientes)
        {
            string equipo = StartGameVariables.Instance.humanList.Contains(clientId) ? "human" : "zombie";

            GameObject prefab = equipo == "zombie" ? zombiePrefab : humanoPrefab;
            GameObject player = Instantiate(prefab, GetSpawnPoint(equipo, clientId), Quaternion.identity);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

            Debug.Log($"Jugador {clientId} spawneado como {equipo}");

            EnviarEquipoClientRpc(equipo, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });
        }

        StartGameVariables.Instance.jugadoresPendientes.Clear();
        StartGameClientRpc(StartGameVariables.Instance.SelectedGameMode.Value);
    }

    [ClientRpc]
    private void StartGameClientRpc(GameMode mode, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[GameManager] StartGameClientRpc recibido. Modo={mode}");
        var lm = FindObjectOfType<LevelManager>();
        if (lm != null)
            lm.StartGame(mode);
    }

   

    // Devuelve el punto de aparición según el equipo y el índice
    private Vector3 GetSpawnPoint(string equipo, ulong clientId)
    {
        int index = 0;

        if(equipo == "zombie")
        {
            index = StartGameVariables.Instance.zombieList.IndexOf(clientId);
            return _levelManager.GetZombieSpawnPoint(index);
        }
        else
        {
            index = StartGameVariables.Instance.humanList.IndexOf(clientId);
            return _levelManager.GetHumanSpawnPoint(index);

        }
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.SceneManager.OnLoadComplete += OnSceneLoadComplete;
        }
    }
    private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (sceneName == "GameScene") // Asegúrate de usar el nombre correcto
        {
            // Verificamos si todos han cargado
            if (NetworkManager.ConnectedClientsIds.Count == StartGameVariables.Instance.jugadoresPendientes.Count)
            {
                IniciarPartida(); // Aquí sí puedes spawnear a todos
            }
        }
    }

   /* private  void OnDestroy()
    {
        if (IsServer)
        {
            NetworkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
        }
    }*/

    [ClientRpc]
    private void EnviarEquipoClientRpc(string equipo, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"Me han asignado al equipo: {equipo}");
    }

    // Limpieza de suscripciones

}
