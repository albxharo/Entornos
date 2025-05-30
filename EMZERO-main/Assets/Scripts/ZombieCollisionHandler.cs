using UnityEngine;
using Unity.Netcode;

public class ZombieCollisionHandler : NetworkBehaviour
{
    private LevelManager levelManager; // Referencia al LevelManager

    private void Awake()
    {
        // Obtener la referencia al LevelManager al inicio
        levelManager = FindObjectOfType<LevelManager>();
        if (levelManager == null)
        {
            Debug.LogError("LevelManager no encontrado en la escena. Aseg�rate de que haya uno.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Solo el due�o del zombi ejecuta esto y solo si el LevelManager existe
        if (!IsOwner || levelManager == null) return;

        // Si choca con un humano
        var target = collision.gameObject.GetComponent<PlayerController>();
        if (target != null && !target.isZombie)
        {
            // Llama al ServerRpc del LevelManager para hacer la conversi�n en red
            // Pasamos el NetworkObjectId del humano para que el servidor lo identifique
            levelManager.RequestChangeToZombieServerRpc(target.NetworkObjectId);
        }
    }
}
