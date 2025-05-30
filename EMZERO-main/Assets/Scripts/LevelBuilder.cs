using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// Clase para generar el nivel del juego de forma determinista usando un seed.
/// Suelos, muros, ítems, puertas y monedas se generan idénticamente en servidor y clientes.
/// </summary>
public class LevelBuilder : NetworkBehaviour
{
    #region Network y Seed

    // Semilla compartida por Netcode
    private NetworkVariable<int> levelSeed = new NetworkVariable<int>(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone
    );

    #endregion

    #region Properties

    [Header("Prefabs")]
    [SerializeField] private GameObject[] floorPrefabs;
    [SerializeField] private GameObject[] obstaclesPrefabs;
    [SerializeField] private GameObject cornerPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private GameObject doorHolePrefab;
    [SerializeField] private GameObject exteriorPrefab;
    [SerializeField] public GameObject coinPrefab;

    [Header("Room Settings")]
    private int numberOfRooms = 4;
    [SerializeField] private int roomWidth = 5;
    [SerializeField] private int roomLength = 5;
    [SerializeField] private float ítemsDensity = 20f;
    [SerializeField] private float coinsDensity = 1f;

    private readonly float tileSize = 1.0f;
    private Transform roomParent;

    private int CoinsGenerated = 0;
    private List<Vector3> humanSpawnPoints = new List<Vector3>();
    private List<Vector3> zombieSpawnPoints = new List<Vector3>();
    private List<Vector3> coinPositions = new List<Vector3>();

    #endregion

    #region Unity callbacks

    private void Awake()
    {
        GameObject parentObject = new GameObject("RoomsParent");
        roomParent = parentObject.transform;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // El servidor elige y comparte el seed
            int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            levelSeed.Value = seed;
            // Construye localmente tras fijar el seed
            Build();
        }
        else
        {
            // Clientes esperan al seed y luego construyen
            levelSeed.OnValueChanged += (_, newSeed) => Build();
        }
    }

    #endregion

    #region World building

    /// <summary>
    /// Genera la estructura del nivel (sin monedas) usando el seed compartido.
    /// </summary>
    public void Build()
    {
        // Reiniciamos PRNG con seed
        UnityEngine.Random.InitState(levelSeed.Value);

        CoinsGenerated = 0;
        humanSpawnPoints.Clear();
        zombieSpawnPoints.Clear();
        coinPositions.Clear();

        CreateRooms(roomWidth, roomLength, numberOfRooms);
    }

    private void CreateRooms(int width, int length, int count)
    {
        int rows = Mathf.CeilToInt(Mathf.Sqrt(count));
        int cols = Mathf.CeilToInt((float)count / rows);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                float x = j * width;
                float z = i * length;

                CreateRoom(width, length, x, z);

                var spawnPoint = new Vector3(x + width / 2f, 2f, z + length / 2f);
                if ((i * cols + j) % 2 == 0)
                    humanSpawnPoints.Add(spawnPoint);
                else
                    zombieSpawnPoints.Add(spawnPoint);
            }
        }

        // Fallbacks si faltan spawn points
        if (zombieSpawnPoints.Count == 0)
            zombieSpawnPoints.Add(humanSpawnPoints[0] + Vector3.right);
        if (humanSpawnPoints.Count == 0)
            humanSpawnPoints.Add(zombieSpawnPoints[0] + Vector3.right * 2);

        CreateExterior(rows, cols, width, length);
    }

    private void CreateRoom(int width, int length, float offsetX, float offsetZ)
    {
        CreateFloor(width, length, offsetX, offsetZ);
        CreateWalls(width, length, offsetX, offsetZ);
    }

    private void CreateFloor(int width, int length, float offsetX, float offsetZ)
    {
        for (int x = 0; x < width; x++)
            for (int z = 0; z < length; z++)
            {
                var pos = new Vector3(x * tileSize + offsetX, 0, z * tileSize + offsetZ);

                // Suelo
                var floorPrefab = floorPrefabs[UnityEngine.Random.Range(0, floorPrefabs.Length)];
                InstantiateNetworked(floorPrefab, pos);

                // Obstáculo
                if (ShouldPlaceItem(x, z, width, length))
                {
                    var obs = InstantiateNetworked(
                        obstaclesPrefabs[UnityEngine.Random.Range(0, obstaclesPrefabs.Length)],
                        pos
                    );
                    obs.tag = "Obstacle";
                }
                else if (ShouldPlaceCoin(x, z, width, length))
                {
                    // Guardamos la posición fija en y = 0.1
                    coinPositions.Add(new Vector3(pos.x, 0.1f, pos.z));
                }
            }
    }

    private void CreateWalls(int width, int length, float offsetX, float offsetZ)
    {
        // Esquinas
        Place(cornerPrefab, offsetX, offsetZ);
        Place(cornerPrefab, (width - 1) * tileSize + offsetX, offsetZ);
        Place(cornerPrefab, offsetX, (length - 1) * tileSize + offsetZ);
        Place(cornerPrefab, (width - 1) * tileSize + offsetX, (length - 1) * tileSize + offsetZ);

        // Paredes y puertas
        int dpX = (width - 1) / 2, dpZ = (length - 1) / 2;
        for (int i = 1; i < width - 1; i++)
        {
            // Inferior
            if (i == dpX)
                Place(doorPrefab, i * tileSize + offsetX, offsetZ);
            else
                Place(wallPrefab, i * tileSize + offsetX, offsetZ);

            // Superior
            if (i == dpX)
                Place(doorPrefab, i * tileSize + offsetX, (length - 1) * tileSize + offsetZ, Quaternion.Euler(0, 180, 0));
            else
                Place(wallPrefab, i * tileSize + offsetX, (length - 1) * tileSize + offsetZ);
        }
        for (int i = 1; i < length - 1; i++)
        {
            // Izquierda
            if (i == dpZ)
                Place(doorPrefab, offsetX, i * tileSize + offsetZ, Quaternion.Euler(0, 90, 0));
            else
                Place(wallPrefab, offsetX, i * tileSize + offsetZ, Quaternion.Euler(0, 90, 0));

            // Derecha
            if (i == dpZ)
                Place(doorPrefab, (width - 1) * tileSize + offsetX, i * tileSize + offsetZ, Quaternion.Euler(0, 270, 0));
            else
                Place(wallPrefab, (width - 1) * tileSize + offsetX, i * tileSize + offsetZ, Quaternion.Euler(0, 270, 0));
        }
    }

    private void CreateExterior(int rows, int cols, int width, int length)
    {
        float minX = -tileSize, maxX = cols * width, minZ = -tileSize, maxZ = rows * length;
        for (float x = minX; x <= maxX; x += tileSize)
        {
            Place(exteriorPrefab, x, minZ);
            Place(exteriorPrefab, x, maxZ);
        }
        for (float z = minZ; z < maxZ; z += tileSize)
        {
            Place(exteriorPrefab, minX, z);
            Place(exteriorPrefab, maxX, z);
        }
    }

    #endregion

    #region Spawn de monedas

    public void SpawnCoins()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Solo el servidor debe ejecutar SpawnCoins()");
            return;
        }

        int totalSlots = coinPositions.Count;
        int toSpawn = Mathf.CeilToInt(totalSlots * (coinsDensity / 100f));
        toSpawn = Mathf.Clamp(toSpawn, 1, totalSlots);

        var indices = Enumerable.Range(0, totalSlots)
                                .OrderBy(_ => UnityEngine.Random.value)
                                .Take(toSpawn);
        foreach (int i in indices)
        {
            // Usamos la posición guardada, ya con y = -0.8
            Vector3 pos = coinPositions[i];
            var coin = InstantiateNetworked(coinPrefab, pos);
                        if (coin.GetComponent<NetworkObject>() == null)
                            {
                Debug.LogError("coinPrefab NO tiene NetworkObject!");
                                continue;
                            }
            CoinsGenerated++;
        }

        Debug.Log($"Monedas instanciadas en red: {CoinsGenerated}");
    }

    #endregion

    #region Helpers

    private GameObject InstantiateNetworked(GameObject prefab, Vector3 pos, Quaternion rot = default)
    {
        var go = Instantiate(prefab, pos, rot, roomParent);
        var netObj = go.GetComponent<NetworkObject>();
        if (IsServer && netObj != null)
            netObj.Spawn();
        return go;
    }

    private void Place(GameObject prefab, float x, float z, Quaternion rot = default)
    {
        InstantiateNetworked(prefab, new Vector3(x, 0, z), rot);
    }

    private bool ShouldPlaceItem(int x, int z, int w, int l)
    {
        bool inside = x > 0 && x < w - 1 && z > 0 && z < l - 1;
        return inside && UnityEngine.Random.Range(0f, 100f) < ítemsDensity;
    }

    private bool ShouldPlaceCoin(int x, int z, int w, int l)
    {
        bool inside = x > 0 && x < w - 1 && z > 0 && z < l - 1;
        return inside && UnityEngine.Random.Range(0f, 100f) < coinsDensity;
    }

    #endregion

    #region Accesores públicos

    public List<Vector3> GetHumanSpawnPoints() => humanSpawnPoints.ToList();
    public List<Vector3> GetZombieSpawnPoints() => zombieSpawnPoints.ToList();
    public int GetCoinsGenerated() => CoinsGenerated;

    #endregion
}
