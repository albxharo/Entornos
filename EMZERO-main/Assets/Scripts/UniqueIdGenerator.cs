using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class UniqueIdGenerator : NetworkBehaviour
{
    // NetworkVariable para sincronizar el ID a todos los clientes
    private NetworkVariable<string> uniqueId = new NetworkVariable<string>(string.Empty,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [SerializeField] private GameObject idFloatingTextPrefab;
    private GameObject idFloatingTextInstance;
    private TMP_Text idTextComponent;

    private List<string> adjectives = new List<string>
    {
        "Terrible", "Misterioso", "Oscuro", "Poderoso", "Inmortal",
        "Temible", "Fantástico", "Despiadado", "Legendario", "Siniestro"
    };
    private List<string> monsters = new List<string>
    {
        "Frankenstein", "Drácula", "Hombre Lobo", "Momia", "Fantasma",
        "Zombi", "Vampiro", "Bruja", "Espectro", "Gárgola"
    };
    private List<string> works = new List<string>
    {
        "El Mago de Oz", "1984", "La Guerra de los Mundos", "Dune", "El Señor de los Anillos",
        "Willow", "Blade Runner", "Star Wars", "Matrix", "Jurassic Park"
    };

    // Almacena los IDs ya generados en esta instancia
    private HashSet<string> generatedIDs = new HashSet<string>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Usamos GenerateUniqueID para aprovechar los 3 intentos + fallback GUID
            string id = GenerateUniqueID();
            uniqueId.Value = id;
            Debug.Log($"[Server] Generated unique ID '{id}' for client {OwnerClientId}");
        }

        // Crear el texto flotante en todos los clientes
        idFloatingTextInstance = Instantiate(idFloatingTextPrefab, transform);
        idTextComponent = idFloatingTextInstance.GetComponentInChildren<TMP_Text>();
        idFloatingTextInstance.transform.localPosition = new Vector3(0, 2.2f, 0);

        // Registrar callback para cambios
        uniqueId.OnValueChanged += OnIdChanged;

        // Mostrar valor inicial si ya llegó antes del callback
        if (!string.IsNullOrEmpty(uniqueId.Value))
            OnIdChanged(string.Empty, uniqueId.Value);
    }

    private void OnDestroy()
    {
        uniqueId.OnValueChanged -= OnIdChanged;
    }

    private void OnIdChanged(string oldId, string newId)
    {
        if (idTextComponent != null)
            idTextComponent.text = newId;
        Debug.Log($"[Client {NetworkManager.LocalClientId}] ID changed from '{oldId}' to '{newId}'");
    }

    /// <summary>
    /// Genera un ID único usando hasta 3 combinaciones nuevas antes de recurrir a GUID.
    /// </summary>
    public string GenerateUniqueID()
    {
        string uniqueID = null;
        bool isUnique = false;
        int attempts = 0;

        while (attempts < 3 && !isUnique)
        {
            uniqueID = GenerateRandomID();
            if (!generatedIDs.Contains(uniqueID))
            {
                isUnique = true;
            }
            attempts++;
        }

        if (!isUnique)
        {
            uniqueID = System.Guid.NewGuid().ToString();
        }

        generatedIDs.Add(uniqueID);
        return uniqueID;
    }

    /// <summary>
    /// Combina aleatoriamente adjetivo, monstruo y obra, e incluye el OwnerClientId.
    /// </summary>
    private string GenerateRandomID()
    {
        string adjective = adjectives[Random.Range(0, adjectives.Count)];
        string monster = monsters[Random.Range(0, monsters.Count)];
        string work = works[Random.Range(0, works.Count)];
        return $"El {adjective} {monster} de {work} (Jugador {OwnerClientId})";
    }

    public List<string> GetGeneratedIDs()
    {
        return new List<string>(generatedIDs);
    }
}
