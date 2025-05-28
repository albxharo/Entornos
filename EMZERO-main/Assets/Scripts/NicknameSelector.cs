using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NicknameSelector : MonoBehaviour
{
    public UniqueIdGenerator idGenerator;
    public TMP_Text nicknameText;
    public Button generateButton;
    public Button confirmButton;

    private string currentNickname;

    void Start()
    {
        generateButton.onClick.AddListener(GenerateNickname);
        confirmButton.onClick.AddListener(ConfirmNickname);

        GenerateNickname();
    }

    void GenerateNickname()
    {
        currentNickname = idGenerator.GenerateUniqueID();
        nicknameText.text = currentNickname;
    }

    void ConfirmNickname()
    {
        PlayerPrefs.SetString("PlayerNickname", currentNickname);
        Debug.Log("Nickname confirmado: " + currentNickname);

        // Delegar al UIManager para gestionar paneles
        UIManager.Instance.ConfirmarNick(currentNickname);


    }
}
