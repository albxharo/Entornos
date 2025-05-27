using Unity.Netcode;
using UnityEngine;

public class GameModeSelector : NetworkBehaviour
{
    public GameObject panel;
    private GameManager gm;

    private void Awake()
    {
        gm = FindObjectOfType<GameManager>();
        Debug.Log($"[Selector] Awake ▶ gm es null? {gm == null}");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[Selector] OnNetworkSpawn ▶ IsHost={IsHost}, panel activo={IsHost}");
        panel.SetActive(IsHost);
    }

    public void OnTimeButton()
    {
        Debug.Log("[Selector] Pulsado TimeButton ▶ IsHost=" + IsHost);
        if (IsHost && gm != null)
            gm.SelectGameModeServerRpc(GameMode.Tiempo);
        panel.SetActive(false);
    }

    public void OnCoinButton()
    {
        Debug.Log("[Selector] Pulsado CoinButton ▶ IsHost=" + IsHost);
        if (IsHost && gm != null)
            gm.SelectGameModeServerRpc(GameMode.Monedas);
        panel.SetActive(false);
    }
}
