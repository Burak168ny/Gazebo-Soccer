using UnityEngine;

public class ApplyForceWithReference : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rb; // Rigidbody'yi Inspector'dan referans olarak al

    [SerializeField]
    private Vector3 forceDirection = Vector3.forward; // Kuvvetin yönü (varsayılan ileri doğru)

    [SerializeField]
    private float forceAmount = 10f; // Kuvvetin büyüklüğü (Inspector'dan ayarlanabilir)

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) // A tuşuna basıldığında
        {
            ApplyForceToBall();
        }
    }

private void ApplyForceToBall()
{
    if (rb != null)
    {
        Vector3 appliedForce = forceDirection.normalized * forceAmount;
        rb.AddForce(appliedForce, ForceMode.Impulse); // Kuvvet uygular
        Debug.Log($"Kuvvet uygulandı: {appliedForce}");
    }
    else
    {
        Debug.LogError("Rigidbody referansı atanmadı! Lütfen Inspector'da Rigidbody'yi ayarlayın.");
    }
}

}
