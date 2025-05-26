using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class GameManager : NetworkBehaviour
{
    // Start is called before the first frame update
    int numHumans;
    int numZombies;

    public GameObject humanoPrefab;
    public GameObject zombiePrefab;

    NetworkList<ulong> humanList = new NetworkList<ulong>();
    NetworkList<ulong> zombieList = new NetworkList<ulong>();

    GameObject _GOlevelManager;
    LevelManager _levelManager;
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
        }
    }
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        string equipo = AsignarEquipo(clientId);

        if (equipo == "ninguno")
        {
            Debug.Log($"Jugador {clientId} rechazado: equipos llenos");
            return;
        }

        GameObject prefab = equipo == "zombie" ? zombiePrefab : humanoPrefab;

        GameObject player = Instantiate(prefab, GetSpawnPoint(equipo), Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        Debug.Log($"Jugador {clientId} spawneado como {equipo}");

        EnviarEquipoClientRpc(equipo, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        });

        if (equipo == "ninguno")
        {
            Debug.Log($"Jugador {clientId} rechazado: equipos llenos");

            NetworkManager.Singleton.DisconnectClient(clientId); // 👈 Desconecta al jugador
            return;
        }
    }
    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Verificar si hay hueco en los equipos
        bool hayHueco = humanList.Count < numHumans || zombieList.Count < numZombies;

        response.Approved = hayHueco;
        response.CreatePlayerObject = false; // Porque hacemos spawn manual luego
        response.Pending = false;

        if (!hayHueco)
        {
            Debug.Log("Un cliente fue rechazado porque los equipos están completos.");
        }
    }
    private string AsignarEquipo(ulong clientId)
    {
        if(humanList.Count >= numHumans && zombieList.Count >= numZombies)
        {
            Debug.Log("Equipos Completados");
            return "ninguno";
        }
        if(humanList.Count >= numHumans)
        {
            zombieList.Add(clientId);
            return "zombie";
        }
        if (zombieList.Count >= numZombies)
        {
            humanList.Add(clientId);
            return "human";
        }
        if(UnityEngine.Random.value < 0.5f)
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
        // Cambia esto según cómo gestiones los puntos de spawn
        return _levelManager.GetSpawnPoint(0);
    }

    [ClientRpc]
    private void EnviarEquipoClientRpc(string equipo, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"Me han asignado al equipo: {equipo}");

        // Aquí puedes guardar el equipo local, activar modelos, UI, etc.
        // Por ejemplo:
        // if (equipo == "zombie") ActivarZombie();
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
