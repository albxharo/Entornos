using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject), typeof(Collider))]
public class DetectPlayerCollision : NetworkBehaviour
{
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        // 1) Sólo nos interesa si choca con un PlayerController
        if (!other.TryGetComponent<PlayerController>(out var pc))
            return;

       

        // 3) Feedback inmediato: sonido local
        AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // 4) Pedimos al servidor que añada la moneda a este jugador
       // pc.AddCoinServerRpc();

        // 5) Pedimos al servidor que despawnee la moneda en red
        SubmitPickupServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPickupServerRpc(ServerRpcParams rpcParams = default)
    {
        var no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned)
            no.Despawn();
    }
}
