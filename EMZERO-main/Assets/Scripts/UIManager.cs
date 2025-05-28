using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; // Singleton para acceso fácil

    [SerializeField] private NetworkManager _NetworkManager;

    [Header("Paneles UI")]
    [SerializeField] private GameObject panelNick;
    [SerializeField] private GameObject panelModoJuego;
    [SerializeField] private GameObject panelEspera;

    [Header("Referencias de texto")]
    [SerializeField] private TMP_Text nickText;
    [SerializeField] private TMP_Text textoEspera;

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
        panelEspera.SetActive(false);

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

    // Mostrar panel para elegir nick y generar uno nuevo
    public void MostrarPanelNick()
    {
        currentNick = idGenerator.GenerateUniqueID();
        nickText.text = currentNick;

        panelNick.SetActive(true);
        panelModoJuego.SetActive(false);
        panelEspera.SetActive(false);
    }

    // Método para que NicknameSelector confirme el nick y gestione paneles
    public void ConfirmarNick(string nick)
    {
        currentNick = nick;
        Debug.Log($"Nick confirmado: {currentNick} | IsHost={NetworkManager.Singleton.IsHost} | IsClient={NetworkManager.Singleton.IsClient}");

        panelNick.SetActive(false);

        if (NetworkManager.Singleton.IsHost)
        {
            panelModoJuego.SetActive(true);
            panelEspera.SetActive(false);
            Debug.Log("Host: mostrando panelModoJuego");
        }
        else
        {
            panelModoJuego.SetActive(false);
            panelEspera.SetActive(true);
            textoEspera.text = $"Esperando al host para elegir el modo de juego...\nTu nick: {currentNick}";
            Debug.Log("Cliente: mostrando panelEspera");
        }
    }

    // Botones para iniciar Host, Client o Server (puedes adaptarlo o quitarlo si usas UI en canvas)
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
