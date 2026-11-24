using Fusion;
using UnityEngine;

public class BallController : NetworkBehaviour
{
    private Rigidbody rb;
    private bool isControlled = false;
    private NetworkObject controllingPlayer;

    public float throwForce = 500f; // Fırlatma kuvveti

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void FixedUpdateNetwork()
    {
        if (isControlled && controllingPlayer != null)
        {
            // Topu kontrol eden oyuncu varsa, top oyuncunun konumunu takip et
            transform.position = controllingPlayer.transform.position + Vector3.up; // Top oyuncunun üstünde olsun

            // Eğer fırlatma inputu varsa, topu fırlat
            if (Object.HasInputAuthority && GetInput(out NetworkInputData data) && data.buttons.IsSet(NetworkInputData.SHOOT))
            {
                Vector3 force = controllingPlayer.transform.forward * throwForce;
                RpcThrowBall(force); // Fırlatma işlemini tüm istemcilerde senkronize et
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Eğer topa bir oyuncu çarptıysa kontrolü al
        if (!isControlled && other.CompareTag("Player") && other.TryGetComponent<NetworkObject>(out var player))
        {
            isControlled = true;
            controllingPlayer = player;
            rb.isKinematic = true; // Topu hareketsiz yap
            rb.linearVelocity = Vector3.zero;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RpcThrowBall(Vector3 force)
    {
        // Topu fırlat
        isControlled = false;
        rb.isKinematic = false; // Topu tekrar fizik kurallarına dahil et
        rb.AddForce(force, ForceMode.Impulse);

        // Kontrolü bırak
        controllingPlayer = null;
    }
}
