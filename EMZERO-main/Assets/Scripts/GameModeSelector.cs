using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class GameModeSelector : NetworkBehaviour
{
    public GameObject panel; // Panel de selección de modo
    private GameManager gm;

    private void Awake()
    {
        gm = FindObjectOfType<GameManager>(); // Referencia al GameManager en escena
        Debug.Log($"[Selector] Awake ▶ gm es null? {gm == null}");
    }

    public override void OnNetworkSpawn()
    {
        // Solo el Host verá el panel de selección
        Debug.Log($"[Selector] OnNetworkSpawn ▶ IsHost={IsHost}, panel activo={IsHost}");
        panel.SetActive(IsHost);
    }

    public void OnTimeButton()
    {
        // El Host selecciona el modo de juego de tiempo
        Debug.Log("[Selector] Pulsado TimeButton ▶ IsHost=" + IsHost);
        if (IsHost && gm != null)
            gm.SelectGameModeServerRpc(GameMode.Tiempo);
        panel.SetActive(false); // Oculta el panel tras seleccionar
    }

    public void OnCoinButton()
    {
        // El Host selecciona el modo de juego de monedas
        Debug.Log("[Selector] Pulsado CoinButton ▶ IsHost=" + IsHost);
        if (IsHost && gm != null)
            gm.SelectGameModeServerRpc(GameMode.Monedas);
        panel.SetActive(false); // Oculta el panel tras seleccionar
    }
}
