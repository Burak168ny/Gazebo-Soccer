using UnityEngine;
using System.Collections;

public class ForceResetter : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody bulunamadı! Lütfen bu script'i bir Rigidbody bileşeni içeren objeye ekleyin.");
        }
    }

    private void Start()
    {
        StartCoroutine(ResetForcesPeriodically());
    }

    private IEnumerator ResetForcesPeriodically()
    {
        while (true)
        {
            if (rb != null)
            {
                Vector3 velocity = rb.linearVelocity;
                velocity.y = 0; // Y bileşenini sıfırla
                rb.linearVelocity = velocity;

                Vector3 angularVelocity = rb.angularVelocity;
                angularVelocity.y = 0; // Y bileşenini sıfırla
                rb.angularVelocity = angularVelocity;
            }
            yield return new WaitForSeconds(0.3f);
        }
    }
}
