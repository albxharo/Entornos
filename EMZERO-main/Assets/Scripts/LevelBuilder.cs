using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Clase para generar el nivel del juego, incluyendo suelos, paredes, ítems decorativos y el borde exterior.
/// Las monedas se generan por separado llamando a SpawnCoins().
/// </summary>
public class LevelBuilder : MonoBehaviour
{
    #region Properties

    [Header("Prefabs")]
    [Tooltip("Array con los prefabs de suelo")]
    [SerializeField] private GameObject[] floorPrefabs;
    [Tooltip("Array con los prefabs de ítems decorativos")]
    [SerializeField] private GameObject[] obstaclesPrefabs;
    [Tooltip("Prefab para las esquinas")]
    [SerializeField] private GameObject cornerPrefab;
    [Tooltip("Prefab para los muros")]
    [SerializeField] private GameObject wallPrefab;
    [Tooltip("Prefab para las puertas")]
    [SerializeField] private GameObject doorPrefab;
    [Tooltip("Prefab para el trozo de muro que incluye puerta")]
    [SerializeField] private GameObject doorHolePrefab;
    [Tooltip("Prefab para el borde exterior")]
    [SerializeField] private GameObject exteriorPrefab;
    [Tooltip("Prefab para las monedas")]
    [SerializeField] public GameObject coinPrefab;

    [Header("Room Settings")]
    [Tooltip("Número total de salas")]
    [SerializeField] private int numberOfRooms = 1;
    [Tooltip("Ancho de cada sala")]
    [SerializeField] private int roomWidth = 5;
    [Tooltip("Largo de cada sala")]
    [SerializeField] private int roomLength = 5;
    [Tooltip("Densidad de elementos decorativos [%]")]
    [SerializeField] private float ítemsDensity = 20f;
    [Tooltip("Densidad de monedas [%]")]
    [SerializeField] private float coinsDensity = 20f;

    private readonly float tileSize = 1.0f;
    private Transform roomParent;

    private int CoinsGenerated = 0;

    private HashSet<Vector3> humanSpawnPoints = new HashSet<Vector3>();
    private HashSet<Vector3> zombieSpawnPoints = new HashSet<Vector3>();

    // Lista de posiciones candidatas para monedas
    private List<Vector3> coinPositions = new List<Vector3>();

    #endregion

    #region Unity game loop methods

    private void Awake()
    {
        // Creamos un padre para mantener todo ordenado
        GameObject parentObject = new GameObject("RoomsParent");
        roomParent = parentObject.transform;
    }

    #endregion

    #region World building methods

    /// <summary>
    /// Genera la estructura del nivel sin monedas.
    /// </summary>
    public void Build()
    {
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
                float x = j * roomWidth;
                float z = i * roomLength;

                CreateRoom(width, length, x, z);

                Vector3 spawnPoint = new Vector3(x + width / 2f, 2f, z + length / 2f);
                if (i % 2 == 0 && j % 2 == 0)
                    humanSpawnPoints.Add(spawnPoint);
                else
                    zombieSpawnPoints.Add(spawnPoint);
            }
        }

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
                // Selección aleatoria de terreno
                var floor = floorPrefabs[Random.Range(0, floorPrefabs.Length)];
                Vector3 pos = new Vector3(x * tileSize + offsetX, 0, z * tileSize + offsetZ);
                Instantiate(floor, pos, Quaternion.identity, roomParent);

                // Obstáculos decorativos
                if (ShouldPlaceItem(x, z, width, length))
                {
                    var obs = obstaclesPrefabs[Random.Range(0, obstaclesPrefabs.Length)];
                    Instantiate(obs, pos, Quaternion.identity, roomParent).tag = "Obstacle";
                }

                // Registramos posición de moneda candidata
                if (ShouldPlaceCoin(x, z, width, length))
                    coinPositions.Add(pos);
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
            // Inferior / Superior
            if (i == dpX) { Place(doorPrefab, i * tileSize + offsetX, offsetZ); Place(doorHolePrefab, i * tileSize + offsetX, offsetZ); }
            else Place(wallPrefab, i * tileSize + offsetX, offsetZ);

            if (i == dpX) { Place(doorPrefab, i * tileSize + offsetX, (length - 1) * tileSize + offsetZ, Quaternion.Euler(0, 180, 0)); Place(doorHolePrefab, i * tileSize + offsetX, (length - 1) * tileSize + offsetZ); }
            else Place(wallPrefab, i * tileSize + offsetX, (length - 1) * tileSize + offsetZ);
        }
        for (int i = 1; i < length - 1; i++)
        {
            // Izquierda / Derecha
            if (i == dpZ) { Place(doorPrefab, offsetX, i * tileSize + offsetZ, Quaternion.Euler(0, 90, 0)); Place(doorHolePrefab, offsetX, i * tileSize + offsetZ, Quaternion.Euler(0, 90, 0)); }
            else Place(wallPrefab, offsetX, i * tileSize + offsetZ, Quaternion.Euler(0, 90, 0));

            if (i == dpZ) { Place(doorPrefab, (width - 1) * tileSize + offsetX, i * tileSize + offsetZ, Quaternion.Euler(0, 270, 0)); Place(doorHolePrefab, (width - 1) * tileSize + offsetX, i * tileSize + offsetZ, Quaternion.Euler(0, 270, 0)); }
            else Place(wallPrefab, (width - 1) * tileSize + offsetX, i * tileSize + offsetZ, Quaternion.Euler(0, 90, 0));
        }
    }

    private void CreateExterior(int rows, int cols, int width, int length)
    {
        float minX = -tileSize, maxX = cols * width, minZ = -tileSize, maxZ = rows * length;
        for (float x = minX; x <= maxX; x += tileSize) { Place(exteriorPrefab, x, minZ); Place(exteriorPrefab, x, maxZ); }
        for (float z = minZ; z < maxZ; z += tileSize) { Place(exteriorPrefab, minX, z); Place(exteriorPrefab, maxX, z); }
    }

    private void Place(GameObject prefab, float x, float z, Quaternion rot = default)
    {
        Instantiate(prefab, new Vector3(x, 0, z), rot, roomParent);
    }

    
    public void SpawnCoins()
    {
        if (!Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Solo el servidor debe ejecutar SpawnCoins()");
            return;
        }

        foreach (var pos in coinPositions)
        {
            // 1) Instanciamos la moneda localmente
            var coinInstance = Instantiate(coinPrefab, pos, Quaternion.identity, roomParent);
            // 2) Obtenemos su NetworkObject
            var no = coinInstance.GetComponent<Unity.Netcode.NetworkObject>();
            if (no == null)
            {
                Debug.LogError("coinPrefab NO tiene NetworkObject!");
                continue;
            }
            // 3) Y la spawnemos en red
            no.Spawn();
            CoinsGenerated++;
        }
        Debug.Log($"Monedas instanciadas en red: {CoinsGenerated}");
    }


    #endregion

    #region Public methods

    public List<Vector3> GetHumanSpawnPoints() => humanSpawnPoints.ToList();
    public List<Vector3> GetZombieSpawnPoints() => zombieSpawnPoints.ToList();
    public int GetCoinsGenerated() => CoinsGenerated;

    #endregion

    #region Helpers

    private bool ShouldPlaceItem(int x, int z, int w, int l)
    {
        bool inside = x > 0 && x < w - 1 && z > 0 && z < l - 1;
        return inside && Random.Range(0f, 100f) < ítemsDensity;
    }

    private bool ShouldPlaceCoin(int x, int z, int w, int l)
    {
        bool inside = x > 0 && x < w - 1 && z > 0 && z < l - 1;
        return inside && Random.Range(0f, 100f) < coinsDensity;
    }

    #endregion
}
