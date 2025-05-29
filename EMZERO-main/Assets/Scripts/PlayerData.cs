using Unity.Netcode;
using UnityEngine;

public class PlayerData : NetworkBehaviour
{
    // Variable de red que almacena el nickname del jugador.
    // Todos pueden leerla, pero solo el propietario puede escribirla.
    public NetworkVariable<string> Nickname = new NetworkVariable<string>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        // Solo el dueño del objeto establece su nickname
        if (IsOwner)
        {
            // Recupera el nickname guardado en PlayerPrefs o pone uno por defecto
            string nickname = PlayerPrefs.GetString("PlayerNickname", "JugadorDesconocido");

            // Llama al ServerRpc para actualizar la variable de red en el servidor
            SetNicknameServerRpc(nickname);
        }
    }

    [ServerRpc]
    private void SetNicknameServerRpc(string nickname)
    {
        // Establece el valor de la variable de red (esto se replicará a todos los clientes)
        Nickname.Value = nickname;
    }
}