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
            return;

        Debug.Log($"[DetectPlayerCollision] Cliente {NetworkManager.Singleton.LocalClientId} tocó moneda. isZombie={pc.isZombie}");

        // Feedback sonoro en cliente:
        AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // En lugar de separar despawneo + check, llamamos a un solo RPC:
        SubmitPickupAndCheckServerRpc();
        Debug.Log($"[DetectPlayerCollision] Cliente {NetworkManager.Singleton.LocalClientId} llamó a SubmitPickupAndCheckServerRpc()");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPickupAndCheckServerRpc(ServerRpcParams rpcParams = default)
    {
        // 1) Esto corre *siempre* en el servidor:
        Debug.Log($"[SubmitPickupAndCheckServerRpc] Servidor recibió petición del cliente {rpcParams.Receive.SenderClientId}");

        // 2) Despawnear la moneda:
        var no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned)
        {
            no.Despawn();
            Debug.Log("[SubmitPickupAndCheckServerRpc] Moneda despawneada en red.");
        }
        else
        {
            Debug.LogWarning("[SubmitPickupAndCheckServerRpc] El NetworkObject de moneda es nulo o ya estaba despawneado.");
        }

        // 3) Incrementar coinsCollected en el servidor:
        var lm = FindObjectOfType<LevelManager>();
        if (lm != null)
        {
            int antes = lm.coinsCollected.Value;
            lm.coinsCollected.Value += 1;
            Debug.Log($"[SubmitPickupAndCheckServerRpc] coinsCollected: {antes} → {lm.coinsCollected.Value} (totalCoins = {lm.totalCoins.Value})");

            // 4) ¡Aquí mismo comprobamos si ya llegó al total! (evitamos un segundo RPC de “Check”)
            if (!lm.isGameOver && lm.coinsCollected.Value >= lm.totalCoins.Value && lm.coinsCollected.Value > 0)
            {
                Debug.Log("[SubmitPickupAndCheckServerRpc] Condición Monedas cumplida: Game Over → llamando a TriggerGameOver()");
                lm.isGameOver = true;
                lm.TriggerGameOverOnAllClients();
            }
            else
            {
                Debug.Log("[SubmitPickupAndCheckServerRpc] Aún no toca Game Over (o ya isGameOver=true).");
            }
        }
        else
        {
            Debug.LogError("[SubmitPickupAndCheckServerRpc] NO se encontró LevelManager para actualizar monedas.");
        }
    }
}
