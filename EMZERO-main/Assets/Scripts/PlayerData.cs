using Unity.Netcode;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    public NetworkVariable<string> Nickname = new NetworkVariable<string>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string nickname = PlayerPrefs.GetString("PlayerNickname", "JugadorDesconocido");
            Debug.Log($"[PlayerData] Nick en PlayerPrefs para ClientID {OwnerClientId}: {nickname}");
            SetNicknameServerRpc(nickname);
        }

        Nickname.OnValueChanged += (oldValue, newValue) =>
        {
            Debug.Log($"[PlayerData] Nick actualizado para {OwnerClientId}: {newValue}");
        };
    }

    [ServerRpc]
    private void SetNicknameServerRpc(string nickname)
    {
        Nickname.Value = nickname;
    }
}