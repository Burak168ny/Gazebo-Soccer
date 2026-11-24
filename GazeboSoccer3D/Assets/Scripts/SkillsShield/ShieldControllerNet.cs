using UnityEngine;
using Fusion;
using System.Collections;

public class ShieldControllerNet : NetworkBehaviour
{
    [SerializeField] private GameObject controlledObject;  // Kalkan objesi
    [SerializeField] private GameObject characterObject;   // Karakter objesi
    [SerializeField] private Vector3 offset = new Vector3(1, 0, 0);
    [SerializeField] private float throwForce = 1f;
    [SerializeField] private float shieldForce = 40f;
    [SerializeField] private float returnForce = 15f;      // Kalkan geri dönerken uygulanacak kuvvet
    [SerializeField] private int maxBounces = 30;          // Maksimum sekme sayısı

    private Collider[] shieldColliders;
    private bool isShieldThrown = false;
    private bool isReturningToCharacter = false;
    private int bounceCount = 0;

    private Rigidbody shieldRigidbody;
    private NetworkObject shieldNetworkObject;
    private NetworkObject characterNetworkObject;
    private Transform characterTransform;

    private SilentBall ball; // Top referansı
    private bool hasPossession = false;

    private void Awake()
    {
        if (controlledObject != null)
        {
            // Kalkanın parent ilişkisini kaldır
            controlledObject.transform.parent = null;

            shieldRigidbody = controlledObject.GetComponent<Rigidbody>();
            shieldNetworkObject = controlledObject.GetComponent<NetworkObject>();
            shieldColliders = controlledObject.GetComponents<Collider>();
        }

        if (characterObject != null)
        {
            characterTransform = characterObject.GetComponent<Transform>();
            characterNetworkObject = characterObject.GetComponent<NetworkObject>();

            if (characterNetworkObject == null)
            {
                Debug.LogError("Karakter objesine NetworkObject bileşeni eklenmemiş!");
            }
        }
        else
        {
            Debug.LogError("Karakter objesi atanmadı!");
        }

        ball = FindObjectOfType<SilentBall>();
    }

    public override void FixedUpdateNetwork()
    {
        // Kalkanın konumunu güncelle
        UpdateShieldPosition();

        if (Runner.TryGetInputForPlayer(Object.InputAuthority, out NetworkInputData inputData))
        {
            // Şut kontrolü
            if (inputData.buttons.IsSet(NetworkInputData.SHOOT) && hasPossession)
            {
                Vector3 force = CalculateShootForce(inputData.shootDirection);
                ShootBall(force);
            }

            // Eğer topun sahibi isek topu güncel pozisyonda tut
            if (hasPossession)
            {
                UpdateBallPosition();
            }

            // Kalkan fırlatma kontrolü
            if (inputData.buttons.IsSet(NetworkInputData.SHIELD_THROW) && !isShieldThrown)
            {
                ThrowShield(inputData.shieldDirection);
            }
        }

        // Kalkan karaktere geri dönme işlemi
        if (isReturningToCharacter && characterTransform != null)
        {
            Vector3 directionToCharacter = (characterTransform.position - controlledObject.transform.position).normalized;
            shieldRigidbody.linearVelocity = directionToCharacter * returnForce;

            if (Vector3.Distance(controlledObject.transform.position, characterTransform.position) < 0.5f)
            {
                ResetShield();
            }
        }
    }

    private void UpdateShieldPosition()
{
    if (!isShieldThrown && characterTransform != null)
    {
        Vector3 targetPosition = characterTransform.position + offset;
        controlledObject.transform.position = targetPosition;
        controlledObject.transform.rotation = characterTransform.rotation;
    }
}


    private void GainPossession()
    {
        if (ball != null && HasStateAuthority)
        {
            ball.AssignOwner(GetComponent<NetworkObject>());
            ball.rb.isKinematic = true;
            hasPossession = true;
            UpdateBallPosition();
            Debug.Log("Top kontrolü alındı.");
        }
    }

    private void ShootBall(Vector3 force)
    {
        if (hasPossession && ball.IsOwnedBy(GetComponent<NetworkObject>()))
        {
            ball.rb.isKinematic = false;
            ball.SThrowBall(force);
            hasPossession = false;
            Debug.Log("Top şutlandı.");
        }
    }

    private void UpdateBallPosition()
    {
        Vector3 forwardOffset = transform.forward * 1.5f;
        Vector3 upOffset = transform.up * 0.5f;

        ball.transform.position = transform.position + forwardOffset + upOffset;
        ball.transform.rotation = transform.rotation;
    }

    private Vector3 CalculateShootForce(float shootAngle)
    {
        float radians = shootAngle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));
        return transform.TransformDirection(direction) * throwForce;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.GetComponent<SilentBall>() && HasStateAuthority)
        {
            GainPossession();
        }
    }

public void ThrowShield(Vector3 shieldDirection)
{
    if (!Object.HasInputAuthority) return; // Sadece girdiyi sağlayan istemci bu işlemi yapabilir

    // RPC çağırarak sunucudan kalkanı fırlatmasını iste
    RpcThrowShield(shieldDirection);
}

private void ThrowShieldInternal(Vector3 shieldDirection)
{
    if (isShieldThrown) return; // Zaten fırlatılmışsa işlem yapma

    isShieldThrown = true;
    isReturningToCharacter = false;
    bounceCount = 0;

    if (shieldRigidbody != null && characterTransform != null)
    {
        shieldRigidbody.isKinematic = false;

        // Kalkanı karakterin local konumuna taşı
        controlledObject.transform.position = characterTransform.position + characterTransform.TransformDirection(offset);

        // Yerel girdiden gelen yönü uygula
        Vector3 modifiedDirection = characterTransform.TransformDirection(shieldDirection.normalized);

        // Kuvvet uygula
        shieldRigidbody.AddForce(modifiedDirection.normalized * shieldForce, ForceMode.Impulse);

        foreach (Collider col in shieldColliders)
            col.enabled = true;
    }
}

[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RpcThrowShield(Vector3 shieldDirection)
{
    // Sunucu yetkisi ile kalkanı fırlat
    if (Object.HasStateAuthority)
    {
        ThrowShieldInternal(shieldDirection);
    }
}





public void OnShieldCollisionEnter(Collision collision)
{
    if (Runner.IsServer && isShieldThrown)
    {
        bounceCount++;
        Debug.Log($"Kalkan çarptı: {collision.gameObject.name}. Sekme sayısı: {bounceCount}");

        // Rakibi hareketsiz bırak
        NetworkObject hitObject = collision.gameObject.GetComponent<NetworkObject>();
        if (hitObject != null && hitObject != characterNetworkObject && !collision.gameObject.CompareTag("Ball") && !collision.gameObject.CompareTag("Shield"))
        {
            Rigidbody hitRigidbody = hitObject.GetComponent<Rigidbody>();
            if (hitRigidbody != null)
            {
                StartCoroutine(RakibiHareketsizBirak(hitRigidbody, 5f)); // 2 saniye hareketsiz bırak
            }
        }

        if (bounceCount >= maxBounces)
        {
            shieldRigidbody.linearVelocity = Vector3.zero;
            shieldRigidbody.angularVelocity = Vector3.zero;
            isReturningToCharacter = true;
            //shieldRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
}

// Rakibi belli bir süre hareketsiz bırakan coroutine
private IEnumerator RakibiHareketsizBirak(Rigidbody hedefRigidbody, float sure)
{
    hedefRigidbody.isKinematic = true; // Fizik hareketini durdur
    yield return new WaitForSeconds(sure); // Belirtilen süre kadar bekle
    hedefRigidbody.isKinematic = false; // Hareketi yeniden etkinleştir
}


    private void ResetShield()
{
    isShieldThrown = false;
    isReturningToCharacter = false;
    
    // Kalkanın pozisyonunu sıfırla
    if (controlledObject != null)
    {
        controlledObject.transform.position = characterTransform.position + offset;
        controlledObject.transform.rotation = characterTransform.rotation;
    }

    // Rigidbody durumunu sıfırla
    if (shieldRigidbody != null)
    {
        shieldRigidbody.isKinematic = true;
        shieldRigidbody.linearVelocity = Vector3.zero;
        foreach (Collider col in shieldColliders) col.enabled = false;
    }
}

}
