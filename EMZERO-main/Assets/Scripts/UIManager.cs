using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private NetworkManager _NetworkManager;

    [Header("Paneles UI")]
    [SerializeField] private GameObject panelNick;
    [SerializeField] private GameObject panelModoJuego;

    [Header("Referencias de texto")]
    [SerializeField] private TMP_Text nickText;

    [Header("Generador de nombres")]
    [SerializeField] private UniqueIdGenerator idGenerator;

    private string currentNick;
    private bool hasStartedConnection = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        panelNick.SetActive(false);
        panelModoJuego.SetActive(false);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Cliente conectado con ID: " + clientId);
            MostrarPanelNick();
        }
    }

    public void MostrarPanelNick()
    {
        currentNick = idGenerator.GenerateUniqueID();
        nickText.text = currentNick;

        panelNick.SetActive(true);
        panelModoJuego.SetActive(false);
    }

    public void ConfirmarNick(string nick)
    {
        currentNick = nick;
        // Guardar el nick en PlayerPrefs para que PlayerData lo use
        PlayerPrefs.SetString("PlayerNickname", currentNick);
        PlayerPrefs.Save();

        Debug.Log($"Nick confirmado: {currentNick} | IsHost={NetworkManager.Singleton.IsHost} | IsClient={NetworkManager.Singleton.IsClient}");

        panelNick.SetActive(false);

        if (NetworkManager.Singleton.IsHost)
        {
            panelModoJuego.SetActive(true);
            Debug.Log("Host: mostrando panelModoJuego");
        }
        else
        {
            panelModoJuego.SetActive(false);
        }
    }

    // Llamado por botón del host para iniciar el juego
    public void ElegirModoDeJuego()
    {
        panelModoJuego.SetActive(false);

        Debug.Log("Host ha elegido el modo de juego. Iniciando escena...");
        IniciarJuegoServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void IniciarJuegoServerRpc()
    {
        // Aquí puedes reemplazarlo por tu lógica de cambio de escena
        // NetworkManager.SceneManager.LoadScene("NombreEscena", LoadSceneMode.Single);
        Debug.Log("ServerRpc: iniciando juego para todos los clientes...");

        // Simulación de carga (puedes cambiarlo luego)
        NetworkManager.Singleton.SceneManager.LoadScene("NombreDeLaEscena", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!_NetworkManager.IsClient && !_NetworkManager.IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
        }
        GUILayout.EndArea();
    }

    private void StartButtons()
    {
        if (hasStartedConnection) return;

        if (GUILayout.Button("Host"))
        {
            hasStartedConnection = true;
            _NetworkManager.StartHost();
            MostrarPanelNick();
        }
        if (GUILayout.Button("Client"))
        {
            hasStartedConnection = true;
            _NetworkManager.StartClient();
        }
        if (GUILayout.Button("Server"))
        {
            hasStartedConnection = true;
            _NetworkManager.StartServer();
        }
    }

    private void StatusLabels()
    {
        var mode = _NetworkManager.IsHost ? "Host" :
                   _NetworkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " + _NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }
}
