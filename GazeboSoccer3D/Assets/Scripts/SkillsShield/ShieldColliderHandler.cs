using UnityEngine;
using Fusion;

public class ShieldColliderHandler : NetworkBehaviour
{
    private Rigidbody shieldRigidbody;
    [SerializeField] private ShieldControllerNet shieldController; // ShieldController referansını açıkça bağla

    private void Awake()
    {
        shieldRigidbody = GetComponent<Rigidbody>();

        if (shieldController == null)
        {
            Debug.LogError("ShieldControllerNet referansı atanmadı!");
        }
    }

private void OnCollisionEnter(Collision collision)
{
    if (Runner.IsServer && shieldController != null && shieldController.HasStateAuthority)
    {
        shieldController.OnShieldCollisionEnter(collision);
        Debug.Log($"Çarpışma tespit edildi: {collision.gameObject.name}");
    }
}

}
