using UnityEngine;
using Fusion;

public class ShieldCollisionHandler : NetworkBehaviour
{
    [Networked] private bool IsKinematic { get; set; } // Kinematik durumu ağ üzerinde senkronize ediliyor.

    private Rigidbody objectRigidbody;

    private void Start()
    {
        objectRigidbody = GetComponent<Rigidbody>();

        if (objectRigidbody == null)
        {
            Debug.LogError("ShieldCollisionHandler: Rigidbody bileşeni eksik!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Çarpışmayı yalnızca sunucu hesaplar
        if (!Object.HasStateAuthority) return;

        // Çarpan objenin etiketi "Shield" ise
        if (collision.gameObject.CompareTag("Shield"))
        {
            // 3 saniye boyunca kinematik yap
            RPC_SetKinematic(true);
            Invoke(nameof(ResetKinematic), 3f); // 3 saniye sonra kinematik durumu sıfırla
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetKinematic(bool isKinematic)
    {
        if (objectRigidbody != null)
        {
            objectRigidbody.isKinematic = isKinematic;
        }

        IsKinematic = isKinematic; // Ağ üzerinde senkronize et
    }

    private void ResetKinematic()
    {
        if (!Object.HasStateAuthority) return;

        RPC_SetKinematic(false); // Kinematik durumu sıfırla ve istemcilere bildir
    }

    public override void FixedUpdateNetwork()
    {
        // İstemciler, sunucudan gelen kinematik durumu uygular
        if (objectRigidbody != null)
        {
            objectRigidbody.isKinematic = IsKinematic;
        }
    }
}
