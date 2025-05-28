using UnityEngine;
using Unity.Netcode;

public class ZombieCollisionHandler : NetworkBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return; // Solo el dueño del zombi ejecuta esto

        // Si choca con un humano
        var target = collision.gameObject.GetComponent<PlayerController>();
        if (target != null && !target.isZombie)
        {
            // Llama al servidor para hacer la conversión en red
            RequestConversionServerRpc(target.NetworkObjectId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestConversionServerRpc(ulong targetNetId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var targetObject))
        {
            var player = targetObject.GetComponent<PlayerController>();
            if (player != null && !player.isZombie)
            {
                var levelManager = FindObjectOfType<LevelManager>();
                if (levelManager != null)
                {
                    levelManager.ChangeToZombie(targetObject.gameObject, true);
                }
            }
        }
    }
}
