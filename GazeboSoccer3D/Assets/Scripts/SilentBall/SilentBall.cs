using UnityEngine;
using Fusion;

public class SilentBall : NetworkBehaviour
{
    private MeshRenderer _meshRenderer;
    private Collider _collider;

    [Networked] public bool IsInvisible { get; private set; }

    public Rigidbody rb;
    [Networked] private NetworkObject CurrentOwner { get; set; }

    private float throwForce = 15f; // Fırlatma kuvveti
    private float minHorizontalVelocity = 10f; // Minimum yatay hız limiti

    public void AssignOwner(NetworkObject newOwner)
    {
        if (HasStateAuthority)
        {
            CurrentOwner = newOwner;
            Debug.Log($"Topun yeni sahibi: {newOwner.name}");
        }
    }

    public bool IsOwnedBy(NetworkObject owner)
    {
        return CurrentOwner == owner;
    }

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        if (_meshRenderer == null)
        {
            Debug.LogError("MeshRenderer bileşeni bulunamadı! Lütfen top nesnesinde MeshRenderer olduğundan emin olun.");
        }
        if (_collider == null)
        {
            Debug.LogError("Collider bileşeni bulunamadı! Lütfen top nesnesinde Collider olduğundan emin olun.");
        }
        if (rb == null)
        {
            Debug.LogError("Rigidbody bileşeni bulunamadı! Lütfen top nesnesinde Rigidbody olduğundan emin olun.");
        }
    }

    public void SetInvisibility(bool invisible)
    {
        if (HasStateAuthority)
        {
            IsInvisible = invisible;
            RPC_UpdateInvisibility(invisible);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateInvisibility(bool invisible)
    {
        IsInvisible = invisible;
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = !IsInvisible;
            Debug.Log("Top görünürlük durumu güncellendi: " + !IsInvisible);
        }
    }

    public void SThrowBall(Vector3 force)
    {
        if (HasStateAuthority && rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(force, ForceMode.Impulse);
            Debug.Log("Top fırlatıldı: Kuvvet - " + force);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (HasStateAuthority && IsInvisible)
        {
            SetInvisibility(false);
            Debug.Log("Top çarpıştı, görünür hale geldi.");
        }
    }

    private void FixedUpdate()
    {
        if (HasStateAuthority && rb != null)
        {
            // Sadece yatay hız bileşenlerini kontrol et
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizontalVelocity.magnitude < minHorizontalVelocity) // Eğer yatay hız limitin altına düşerse
            {
                Vector3 horizontalDirection = horizontalVelocity.normalized; // Yatay hareket yönünü al
                if (horizontalDirection == Vector3.zero) // Eğer duruyorsa rastgele bir yatay yön belirle
                {
                    horizontalDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                }

                Vector3 additionalForce = horizontalDirection * (3); // Eksik yatay hızı tamamlayacak kuvvet
                rb.AddForce(additionalForce, ForceMode.Impulse);
                Debug.Log("Topun yatay hızı düşük, ek yatay kuvvet uygulandı: " + additionalForce);
            }
        }
    }
}
