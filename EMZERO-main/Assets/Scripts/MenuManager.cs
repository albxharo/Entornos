using System.Net.Sockets;                     // TcpClient
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;                          // NetworkManager
using Unity.Netcode.Transports.UTP;           // UnityTransport (si lo usas)


public class MenuManager : MonoBehaviour
{

    public void Awake()
    {
        Time.timeScale = 1f; // Asegúrate de que el tiempo está restaurado al cargar la escena
    }

    public void StartGame()
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
