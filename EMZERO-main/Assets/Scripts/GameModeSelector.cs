using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GameModeSelector : NetworkBehaviour
{
    // Panel de seleccion de modo
    public GameObject modeSelectionPanel;
    // Panel para ajustar tiempo
    public GameObject sliderTimePanel;
    // Panel para ajustar densidad de monedas
    public GameObject sliderCoinPanel;

    // Referencia al script que maneja las variables de inicio de juego
    private StartGameVariables sgVariables;

    // Slider para densidad de monedas
    [SerializeField] private Slider coinDensitySlider;
    // Label que muestra el valor de densidad de monedas
    [SerializeField] private TextMeshProUGUI coinDensityLabel;

    // Slider para tiempo de partida
    [SerializeField] private Slider timeDensitySlider;
    // Label que muestra el valor de tiempo de partida
    [SerializeField] private TextMeshProUGUI timeDensityLabel;

    private void Awake()
    {
        // Buscar el componente StartGameVariables en la escena
        sgVariables = FindObjectOfType<StartGameVariables>();
    }

    public override void OnNetworkSpawn()
    {
        // Solo el host vera el panel de seleccion de modo
        Debug.Log($"[Selector] OnNetworkSpawn ▶ IsHost={IsHost}, panel activo={IsHost}");

        // Desactivar todos los hijos del panel de seleccion, segun si es host
        foreach (Transform hijo in modeSelectionPanel.transform)
        {
            hijo.gameObject.SetActive(IsHost);
        }
    }

    public void OnTimeButton()
    {
        // El host selecciona el modo de juego basado en tiempo
        Debug.Log("[Selector] Pulsado TimeButton ▶ IsHost=" + IsHost);
        if (IsHost && sgVariables != null)
            // Llamada RPC para informar al servidor del modo Tiempo
            sgVariables.SelectGameModeServerRpc(GameMode.Tiempo);

        // Desactivar todas las opciones del panel principal
        foreach (Transform hijo in modeSelectionPanel.transform)
        {
            hijo.gameObject.SetActive(false);
        }

        // Activar todos los elementos del panel de ajuste de tiempo
        foreach (Transform hijo in sliderTimePanel.transform)
        {
            hijo.gameObject.SetActive(true);
        }

        // Obtener referencias del slider y su label dentro del panel de tiempo
        timeDensitySlider = sliderTimePanel.GetComponentInChildren<Slider>();
        timeDensityLabel = sliderTimePanel.GetComponentInChildren<TextMeshProUGUI>();

        if (timeDensitySlider != null)
        {
            // Configurar rango y valor inicial del slider segun la variable estatica
            timeDensitySlider.minValue = 1f;
            timeDensitySlider.maxValue = 10f;
            timeDensitySlider.value = StartGameVariables.Instance.minutes;

            // Agregar listener para actualizar el tiempo cada vez que cambie el slider
            timeDensitySlider.onValueChanged.AddListener(SetTime);
        }
    }

    public void OnConfirmTimeButton()
    {
        // Confirmar el tiempo seleccionado y cerrar el panel de ajuste de tiempo
        foreach (Transform hijo in sliderTimePanel.transform)
        {
            hijo.gameObject.SetActive(false);
        }
    }

    public void SetTime(float value)
    {
        // Actualizar la variable de minutos en la clase de inicio de juego
        StartGameVariables.Instance.minutes = (int)value;
        UpdateTimeLabel(value);
    }

    private void UpdateTimeLabel(float val)
    {
        // Actualizar el texto que muestra los minutos de partida
        if (timeDensityLabel != null)
            timeDensityLabel.text = $"Tiempo de partida: {val:F1} minutos";
    }

    #region Coins

    public void OnCoinButton()
    {
        // El host selecciona el modo de juego basado en recoleccion de monedas
        Debug.Log("[Selector] Pulsado CoinButton ▶ IsHost=" + IsHost);
        if (IsHost && sgVariables != null)
            // Llamada RPC para informar al servidor del modo Monedas
            sgVariables.SelectGameModeServerRpc(GameMode.Monedas);

        // Desactivar todas las opciones del panel principal
        foreach (Transform hijo in modeSelectionPanel.transform)
        {
            hijo.gameObject.SetActive(false);
        }

        // Activar todos los elementos del panel de ajuste de monedas
        foreach (Transform hijo in sliderCoinPanel.transform)
        {
            hijo.gameObject.SetActive(true);
        }

        // Obtener referencias del slider y su label dentro del panel de monedas
        coinDensitySlider = sliderCoinPanel.GetComponentInChildren<Slider>();
        coinDensityLabel = sliderCoinPanel.GetComponentInChildren<TextMeshProUGUI>();

        if (coinDensitySlider != null)
        {
            // Configurar rango y valor inicial del slider segun la variable estatica
            coinDensitySlider.minValue = 1f;
            coinDensitySlider.maxValue = 100f;
            coinDensitySlider.value = StartGameVariables.Instance.coinsDensity;

            // Agregar listener para actualizar la densidad de monedas cada vez que cambie el slider
            coinDensitySlider.onValueChanged.AddListener(SetCoinsDensity);
        }
    }

    public void SetCoinsDensity(float value)
    {
        // Actualizar la variable de densidad de monedas en la clase de inicio de juego
        StartGameVariables.Instance.coinsDensity = value;
        UpdateCoinLabel(value);
    }

    private void UpdateCoinLabel(float val)
    {
        // Actualizar el texto que muestra la densidad de monedas
        if (coinDensityLabel != null)
            coinDensityLabel.text = $"Densidad Monedas: {val:F1}%";
    }

    public void OnConfirmCoinButton()
    {
        // Confirmar la densidad de monedas seleccionada y cerrar el panel de ajuste de monedas
        foreach (Transform hijo in sliderCoinPanel.transform)
        {
            hijo.gameObject.SetActive(false);
        }
    }

    #endregion
}
