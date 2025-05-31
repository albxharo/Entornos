using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


public class GameModeSelector : NetworkBehaviour
{
    public GameObject modeSelectionPanel; // Panel de selección de modo
    public GameObject sliderTimePanel;
    public GameObject sliderCoinPanel;

    private StartGameVariables sgVariables;
    [SerializeField] private Slider coinDensitySlider;
    [SerializeField] private TextMeshPro coinDensityLabel;

    [SerializeField] private Slider timeDensitySlider;
    [SerializeField] private TextMeshPro timeDensityLabel;


    private void Awake()
    {
        sgVariables = FindObjectOfType<StartGameVariables>();


    }

    public override void OnNetworkSpawn()
    {
        // Solo el Host verá el panel de selección
        Debug.Log($"[Selector] OnNetworkSpawn ▶ IsHost={IsHost}, panel activo={IsHost}");
        // Desactivar todos los hijos del panel
        foreach (Transform hijo in modeSelectionPanel.transform)
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
        foreach (Transform hijo in modeSelectionPanel.transform)
        {
            hijo.gameObject.SetActive(false);
        }
        foreach (Transform hijo in sliderTimePanel.transform)
        {
            hijo.gameObject.SetActive(true);
        }
    }

    public void OnConfirmTimeButton()
    {
        // Desactivar todos los hijos del panel
        foreach (Transform hijo in sliderTimePanel.transform)
        {
            hijo.gameObject.SetActive(false);
        }
        timeDensitySlider = sliderTimePanel.GetComponentInChildren<Slider>();
        timeDensityLabel = sliderTimePanel.GetComponentInChildren<TextMeshPro>();
        if (coinDensitySlider != null)
        {
            // Inicializa el slider al valor actual
            timeDensitySlider.minValue = 1f;
            timeDensitySlider.maxValue = 100f;
            timeDensitySlider.value = StartGameVariables.Instance.coinsDensity;

            // Cada vez que cambie el slider, actualiza el tiempo
            timeDensitySlider.onValueChanged.AddListener(SetTime);
        }
    }

    public void SetTime(float value)
    {
        StartGameVariables.Instance.minutes = (int)value;    
        UpdateTimeLabel(value);
    }

    private void UpdateTimeLabel(float val)
    {
        if (timeDensityLabel != null)
            timeDensityLabel.text = $"Tiempo de partida: {val:F1}%";
    }

    #region Coins
    public void OnCoinButton()
    {
        // El Host selecciona el modo de juego de monedas
        Debug.Log("[Selector] Pulsado CoinButton ▶ IsHost=" + IsHost);
        if (IsHost && sgVariables != null)
            sgVariables.SelectGameModeServerRpc(GameMode.Monedas);
        foreach (Transform hijo in modeSelectionPanel.transform)
        {
            hijo.gameObject.SetActive(false);
        }
        foreach (Transform hijo in sliderCoinPanel.transform)
        {
            hijo.gameObject.SetActive(true);
        }
        coinDensitySlider = sliderCoinPanel.GetComponentInChildren<Slider>();
        coinDensityLabel = sliderCoinPanel.GetComponentInChildren<TextMeshPro>();
        if (coinDensitySlider != null)
        {
            // Inicializa el slider al valor actual
            coinDensitySlider.minValue = 1f;
            coinDensitySlider.maxValue = 100f;
            coinDensitySlider.value = StartGameVariables.Instance.coinsDensity;

            // Cada vez que cambie el slider, actualiza coinsDensity
            coinDensitySlider.onValueChanged.AddListener(SetCoinsDensity);
        }
    }

    public void SetCoinsDensity(float value)
    {
        StartGameVariables.Instance.coinsDensity = value;
        UpdateCoinLabel(value);
    }

    private void UpdateCoinLabel(float val)
    {
        if (coinDensityLabel != null)
            coinDensityLabel.text = $"Densidad Monedas: {val:F1}%";
    }

    public void OnConfirmCoinButton()
    {
        // Desactivar todos los hijos del panel
        foreach (Transform hijo in sliderCoinPanel.transform)
        {
            hijo.gameObject.SetActive(false);
        }
    }

    #endregion
}
