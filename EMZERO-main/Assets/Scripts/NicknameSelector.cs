using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NicknameSelector : MonoBehaviour
{
    public UniqueIdGenerator idGenerator;      // Generador de nombres �nicos
    public TMP_Text nicknameText;              // Texto donde se muestra el nick
    public Button generateButton;              // Bot�n para generar nuevo nick
    public Button confirmButton;               // Bot�n para confirmar el nick

    private string currentNickname;

    void Start()
    {
        // Asignar listeners a los botones
        generateButton.onClick.AddListener(GenerateNickname);
        confirmButton.onClick.AddListener(ConfirmNickname);

        GenerateNickname(); // Generar un nick al iniciar
    }

    void GenerateNickname()
    {
        currentNickname = idGenerator.GenerateUniqueID(); // Obtener nick �nico
        nicknameText.text = currentNickname;
    }

    void ConfirmNickname()
    {
        // Guardar el nick en las preferencias del jugador
        PlayerPrefs.SetString("PlayerNickname", currentNickname);
        Debug.Log("Nickname confirmado: " + currentNickname);

        // Llamar al UIManager para avanzar en la UI
        UIManager.Instance.ConfirmarNick(currentNickname);


    }
}
