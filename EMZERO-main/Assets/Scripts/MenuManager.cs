using System.Net.Sockets;                     // TcpClient
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;                          // NetworkManager
using Unity.Netcode.Transports.UTP;           // UnityTransport (si lo usas)


public class MenuManager : MonoBehaviour
{
    [Tooltip("IP del host (normalmente localhost para LAN)")]
    [SerializeField] private string hostIP = "127.0.0.1";
    [Tooltip("Puerto que usa Netcode (configurado en tu UnityTransport)")]
    [SerializeField] private int hostPort = 7777;
    private void Awake()
    {
        Time.timeScale = 1f;
        // Subscribe to Netcode callbacks
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    public void StartGameAsPlayer()
    {
        NetworkManager.Singleton.StartClient();
        // don’t load scene here!
    }

    public void StartGameAsHost()
    {
        NetworkManager.Singleton.StartHost();
        // for host, you can choose to load immediately, or wait just like client
    }

    private void HandleClientConnected(ulong clientId)
    {
        // Only load the scene when this is *your* client
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SceneManager.LoadScene("GameScene");
        }
    }

    private void OnDestroy()
    {
        // clean up the subscription
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }



    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Salir en el editor
#else
            Application.Quit(); // Salir en una build
#endif
    }
}
