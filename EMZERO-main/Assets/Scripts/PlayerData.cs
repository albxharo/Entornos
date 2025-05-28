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
            SetNicknameServerRpc(nickname);
        }
    }

    [ServerRpc]
    private void SetNicknameServerRpc(string nickname)
    {
        Nickname.Value = nickname;
    }
}
