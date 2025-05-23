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
    public void Awake()
    {
        Time.timeScale = 1f; // Asegúrate de que el tiempo está restaurado al cargar la escena
    }

    public void StartGame()
    {
        // Suscribimos los handlers correctos
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnServerStarted += HandleServerStarted;

        // Decide: cliente o host
        if (IsHostAvailable(hostIP, hostPort))
        {
            Debug.Log("Host detectado: arrancando cliente.");
            NetworkManager.Singleton.StartClient();
        }
        else
        {
            Debug.Log("No hay host: arrancando host.");
            NetworkManager.Singleton.StartHost();
        }
    }

    // Se llama cuando un cliente se conecta (incluido host, que también es cliente local)
    private void HandleClientConnected(ulong clientId)
    {
        // Asegurémonos de que es un cliente realmente conectado, no un host auto-conectado
        if (NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsConnectedClient)
        {
            Debug.Log("Cliente conectado al host, cargo escena de juego.");
            LoadGameScene();
        }
        // Y nos desuscribimos para no volver a disparar
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }

    // Se llama cuando el host ha terminado de levantarse
    private void HandleServerStarted()
    {
        Debug.Log("Host listo, cargo escena de juego.");
        LoadGameScene();
        // Desuscribirse
        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
    }

    private bool IsHostAvailable(string ip, int port, int timeoutMs = 500)
    {
        using (var tcp = new TcpClient())
        {
            try
            {
                var result = tcp.BeginConnect(ip, port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(timeoutMs);
                if (!success) return false;
                tcp.EndConnect(result);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
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
