using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GameModeSelector : NetworkBehaviour
{
    public GameObject panel; // Panel de selección de modo
    
    private StartGameVariables sgVariables;

     private void Awake()
    {
        sgVariables = FindObjectOfType<StartGameVariables>();

    }

    public override void OnNetworkSpawn()
    {
        // Solo el Host verá el panel de selección
        Debug.Log($"[Selector] OnNetworkSpawn ▶ IsHost={IsHost}, panel activo={IsHost}");
        // Desactivar todos los hijos del panel
        foreach (Transform hijo in panel.transform)
        {
            hijo.gameObject.SetActive(IsHost);
        }
    }

    public void OnTimeButton()
    {
        // El Host selecciona el modo de juego de tiempo
        Debug.Log("[Selector] Pulsado TimeButton ▶ IsHost=" + IsHost);
        if (IsHost && sgVariables != null)
            sgVariables.SelectGameModeServerRpc(GameMode.Tiempo);
        // Desactivar todos los hijos del panel
        foreach (Transform hijo in panel.transform)
        {
            hijo.gameObject.SetActive(false);
        }

    }

    public void OnCoinButton()
    {
        // El Host selecciona el modo de juego de monedas
        Debug.Log("[Selector] Pulsado CoinButton ▶ IsHost=" + IsHost);
        if (IsHost && sgVariables != null)
            sgVariables.SelectGameModeServerRpc(GameMode.Monedas);
        foreach (Transform hijo in panel.transform)
        {
            hijo.gameObject.SetActive(false);
        }

    }
}
