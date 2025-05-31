using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject), typeof(Collider))]
public class DetectPlayerCollision : NetworkBehaviour
{
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        
        if (!other.TryGetComponent<PlayerController>(out var pc))
            return;
        // Si el jugador es zombie, no recogemos la moneda
        if (pc.isZombie)
        {
            return;
        }
        AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        var levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.CheckEndGameCondition(); // Nueva comprobación
        }

        // pedimos al servidor que despawnee la moneda en red
        SubmitPickupServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPickupServerRpc(ServerRpcParams rpcParams = default)
    {
        var no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned)
            no.Despawn();

        // ¡Aquí incrementamos el contador!
        var lm = FindObjectOfType<LevelManager>();
        if (lm != null)
        {
            lm.coinsCollected.Value += 1;
        }
    }
}
