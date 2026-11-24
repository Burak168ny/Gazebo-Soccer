using UnityEngine;
using System.Collections;

public class RandomThrower : MonoBehaviour
{
    public Vector3 minPosition = new Vector3(-11, -22, 59);
    public Vector3 maxPosition = new Vector3(3, -18, 60);
    public float forceMultiplier = 10f; // X ve Z yönlerindeki kuvvet
    public float yForceMultiplier = 5f; // Y yönündeki kuvvet

    [Header("Topun Rigidbody Referansı")]
    public Rigidbody ballRigidbody; // Inspector'dan atanan Rigidbody referansı

    [Header("Şut Açısı Ayarı")]
    public float minAngleRange = -80f; // Minimum rastgele açı (negatif)
    public float maxAngleRange = 80f;  // Maksimum rastgele açı (pozitif)

    private bool hasBallControl = false; // Topun kontrolü alındı mı?
    private float controlDuration = 0.2f; // Kontrol süresi (saniye)

    private void OnTriggerEnter(Collider other)
    {
        // Eğer top objesi tagi "Ball" ise
        if (other.CompareTag("Ball"))
        {
            // Topun mevcut hız ve açısal hızını sıfırla
            ballRigidbody.linearVelocity = Vector3.zero;
            ballRigidbody.angularVelocity = Vector3.zero;

            // Top kontrolünü al ve zamanlayıcı başlat
            hasBallControl = true;
            StartCoroutine(ReleaseBallAfterDelay());
        }
    }

    private IEnumerator ReleaseBallAfterDelay()
    {
        // Kontrol süresi boyunca topun hızını sıfırla
        float elapsedTime = 0f;
        while (elapsedTime < controlDuration)
        {
            ballRigidbody.linearVelocity = Vector3.zero;
            ballRigidbody.angularVelocity = Vector3.zero;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Rastgele bir açı hesapla (belirtilen aralıklar arasında)
        float randomAngle = Random.Range(minAngleRange, maxAngleRange);

        // Lokal yönü ayarla (-Z yönüne göre)
        Quaternion rotation = Quaternion.Euler(0, randomAngle, 0); // Y ekseninde dönüş
        Vector3 direction = (rotation * -transform.forward).normalized; // Lokal -Z yönünü döndür ve normalize et

        // Rigidbody referansı ile X-Z düzleminde kuvvet uygula
        ballRigidbody.AddForce(direction * forceMultiplier, ForceMode.Impulse);

        // Y eksenine kuvvet uygula
        Vector3 upwardForce = new Vector3(0, yForceMultiplier, 0);
        ballRigidbody.AddForce(upwardForce, ForceMode.Impulse);

        // Kontrolü bırak
        hasBallControl = false;
    }
}
