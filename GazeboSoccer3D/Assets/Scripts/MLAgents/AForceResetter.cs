using UnityEngine;
using System.Collections;

public class AForceResetter : MonoBehaviour
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
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
}
