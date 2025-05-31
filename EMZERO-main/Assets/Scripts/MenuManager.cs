using System.Net.Sockets;                     // TcpClient
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;                          // NetworkManager
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;           // UnityTransport (si lo usas)


public class MenuManager : MonoBehaviour
{
    public GameObject uiManager;
    public GameObject readyPanel;
    public GameObject timeButton;
    public GameObject coinButton;


    public void Awake()
    {
        Time.timeScale = 1f; // Asegúrate de que el tiempo está restaurado al cargar la escena

    }

    public void StartGame()
    {
        // Verifica si el cliente ya está conectado a una sesión de red
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            uiManager.SetActive(true);
        }
        else if(NetworkManager.Singleton.IsHost)
        {
            timeButton.SetActive(true);
            coinButton.SetActive(true);

        }
        else
        {
            readyPanel.SetActive(true);
            StartGameVariables.Instance.GO_readyButton = GameObject.Find("ReadyButton"); 
            StartGameVariables.Instance.readyButton = StartGameVariables.Instance.GO_readyButton.GetComponent<Button>();

        }
        //SceneManager.LoadScene("GameScene");
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
