using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject), typeof(Collider))]
public class DetectPlayerCollision : NetworkBehaviour
{
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        // Intentamos obtener el PlayerController del objeto que ha chocado
        if (!other.TryGetComponent<PlayerController>(out var plc))
            return;

        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        // 2) Sólo los humanos recogen
        var pc = other.GetComponent<PlayerController>();
        if (pc == null || pc.isZombie) return;

        // 3) Sonido inmediato al cliente local
        AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // 4) Pedimos al servidor que actualice su contador
       // pc.AddCoinServerRpc();

        // 5) Pedimos al servidor que despawnee esta moneda en toda la red
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
