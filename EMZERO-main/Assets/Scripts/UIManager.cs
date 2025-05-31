using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;

public class UIManager : MonoBehaviour
{
    // Singleton: solo habrá una instancia accesible desde cualquier parte
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
        // Implementación del patrón Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persiste entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Oculta los paneles al inicio
        //panelNick.SetActive(false);
        //panelModoJuego.SetActive(false);

        // Suscripción al evento cuando un cliente se conecta
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        // Elimina la suscripción para evitar errores al destruir el objeto
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    // Se llama automáticamente cuando un cliente se conecta
    private void OnClientConnected(ulong clientId)
    {
        // Solo queremos mostrar el panel al cliente local
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Cliente conectado con ID: " + clientId);
            MostrarPanelNick();
        }
    }

    // Muestra el panel de selección de nickname
    public void MostrarPanelNick()
    {
        currentNick = idGenerator.GenerateUniqueID();
        nickText.text = currentNick;

        panelNick.SetActive(true);

        foreach (Transform hijo in panelModoJuego.transform)
        {
            hijo.gameObject.SetActive(false);
        }

        //panelModoJuego.SetActive(false);
    }

    // Se llama cuando el jugador confirma su nickname
    public void ConfirmarNick(string nick)
    {
        currentNick = nick;
        Debug.Log($"Nick confirmado: {currentNick} | IsHost={NetworkManager.Singleton.IsHost} | IsClient={NetworkManager.Singleton.IsClient}");

        panelNick.SetActive(false);

        // Solo el host ve el panel de elegir modo de juego
        if (NetworkManager.Singleton.IsHost)
        {
            foreach (Transform hijo in panelModoJuego.transform)
            {
                hijo.gameObject.SetActive(true);
            }
            Debug.Log("Host: mostrando panelModoJuego");
        }
        else
        {
            panelModoJuego.SetActive(false);
        }
    }

    // Botón que el host pulsa para iniciar la partida
    public void ElegirModoDeJuego()
    {
        panelModoJuego.SetActive(false);

        Debug.Log("Host ha elegido el modo de juego. Iniciando escena...");
        IniciarJuegoServerRpc();
    }

    // Solo el servidor puede cargar la escena para todos
    [ServerRpc(RequireOwnership = false)]
    private void IniciarJuegoServerRpc()
    {
        Debug.Log("ServerRpc: iniciando juego para todos los clientes...");
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    // Debug GUI para iniciar host, cliente o servidor
    // Debug GUI para iniciar host, cliente o servidor
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!_NetworkManager.IsClient && !_NetworkManager.IsServer)
        {
            StartButtons(); // Si aún no hay conexión, muestra botones para iniciar
        }
        else
        {
            StatusLabels(); // Si ya hay conexión, muestra el estado
        }

        GUILayout.EndArea();
    }

    private void StartButtons()
    {
        if (hasStartedConnection) return;

        if (GUILayout.Button("Host"))
        {
            hasStartedConnection = true;
            _NetworkManager.StartHost(); // Inicia como Host
            MostrarPanelNick();
        }
        if (GUILayout.Button("Client"))
        {
            hasStartedConnection = true;
            _NetworkManager.StartClient(); // Inicia como Cliente
        }
        if (GUILayout.Button("Server"))
        {
            hasStartedConnection = true;
            _NetworkManager.StartServer(); // Inicia solo como Servidor
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
