using UnityEngine;

public class BallControl : MonoBehaviour
{
    public GameObject ball; // Topun referansı
    public float throwForce = 10f; // Fırlatma kuvveti
    public Rigidbody ballRb; // Topun Rigidbody bileşeni
    private bool hasBall = false; // Top kontrolünde mi?

    public float defx = 1f; // Topun karaktere göre x yönündeki göreli konumu
    public float defy = -1f; // Topun karaktere göre y yönündeki göreli konumu
    public float defz = 0f; // Topun karaktere göre z yönündeki göreli konumu

    public FixedJoystick joystick; // Joystick bileşeni
    private Vector3 lastInputDirection = Vector3.zero; // Joystick'ten alınan son giriş yönü

    public Collider ballCollider; // Topun collider bileşeni

    private void Update()
    {
        if (hasBall)
        {
            // Topu karakterin belirli bir pozisyonunda tut
            ball.transform.position = transform.position + new Vector3(defx, defy, defz);

            // Joystick'ten gelen girdiği güncelle
            Vector3 inputDirection = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
            if (inputDirection.magnitude > 0.1) // Ölü bölge uygulayarak küçük girdileri yoksay
            {
                lastInputDirection = inputDirection.normalized;
            }
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
        }

        // Joystick bırakıldığında ve bir yön belirlenmişse topu fırlat
        if (hasBall && lastInputDirection != Vector3.zero && Mathf.Approximately(joystick.Horizontal, 0f) && Mathf.Approximately(joystick.Vertical, 0f))
        {
            if (ballRb != null)
            {
                // Mevcut kuvvetleri ve dönüşleri sıfırla
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;

                // Topu son belirlenen yön doğrultusunda fırlat
                ballRb.AddForce(lastInputDirection * throwForce, ForceMode.Impulse);
                hasBall = false; // Top kontrolünü kaybet
                lastInputDirection = Vector3.zero; // Son yönü sıfırla

            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Ball"))
        {
            hasBall = true; // Top kontrolünü al
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            hasBall = true; // Top kontrolünü al
        }
    }

//     // Topun hızını döndüren metot
// public Vector3 CalculateBallVelocity()
// {
//     if (ballRb != null)
//     {
//         return ballRb.velocity;
//     }
//     else
//     {
//         return Vector3.zero;
//     }
// }

}
