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

    private GameObject _GOlevelManager;
    private LevelManager _levelManager;

    private GameObject canvas;
    public GameObject readycanvas;

    public GameObject nicknameManager;
    public NicknameSelector nicknameSelector;

    [SerializeField] private TMP_InputField inputFieldNumJugadores;


    private void Awake()
    {
        _GOlevelManager = GameObject.Find("LevelManager");
        _levelManager = _GOlevelManager.GetComponent<LevelManager>();

        canvas = GameObject.Find("CanvasPlayer");

    }

    private void IniciarPartida()
    {
        Debug.Log("Todos los jugadores listos. Iniciando partida...");


        foreach (ulong clientId in StartGameVariables.Instance.jugadoresPendientes)
        {
            string equipo = StartGameVariables.Instance.humanList.Contains(clientId) ? "human" : "zombie";

            GameObject prefab = equipo == "zombie" ? zombiePrefab : humanoPrefab;
            GameObject player = Instantiate(prefab, GetSpawnPoint(equipo, clientId), Quaternion.identity);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

            if (equipo == "zombie")
                player.GetComponent<PlayerController>().isOriginalZombie = true;

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
            if (NetworkManager.ConnectedClientsIds.Count == StartGameVariables.Instance.jugadoresPendientes.Count && StartGameVariables.Instance.jugadoresPendientes.Count!=0 && StartGameVariables.Instance.SelectedGameMode.Value != GameMode.None  )
            {
                // En lugar de lanzar IniciarPartida() directamente...
                StartCoroutine(WaitAndStartMatch());
            }
        }
    }

    private IEnumerator WaitAndStartMatch()
    {
        // Buscamos el LevelManager de la escena
        var lm = FindObjectOfType<LevelManager>();
        // Esperamos hasta que LevelManager.Build() haya corrido y las listas estén llenas
        yield return new WaitUntil(() =>
            lm != null
            && lm.zombieSpawnPoints.Count > 0
            && lm.humanSpawnPoints.Count > 0
        );

        // Ahora comprobamos de nuevo que todos los clientes están listos
        int connected = NetworkManager.ConnectedClientsIds.Count;
        int pending = StartGameVariables.Instance.jugadoresPendientes.Count;
        var mode = StartGameVariables.Instance.SelectedGameMode.Value;

        if (pending != 0 && connected == pending && mode != GameMode.None)
        {
            IniciarPartida();
        }
    }

    

    [ClientRpc]
    private void EnviarEquipoClientRpc(string equipo, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"Me han asignado al equipo: {equipo}");
    }

    

}
