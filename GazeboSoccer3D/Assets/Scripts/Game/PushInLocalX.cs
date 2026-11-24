using UnityEngine;

public class PushInLocalX : MonoBehaviour
{
    // Kuvvetin büyüklüğü
    public float pushForce = 10f;

    private void OnCollisionEnter(Collision collision)
    {
        // Çarpışan objenin bir Rigidbody'si var mı?
        Rigidbody collidedRigidbody = collision.rigidbody;

        if (collidedRigidbody != null)
        {
            // Objenin local +X yönünü dünya uzayına çevir
            Vector3 localXDirection = transform.right;

            // Kuvvet uygulama
            collidedRigidbody.AddForce(localXDirection * pushForce, ForceMode.Impulse);
        }
    }
}
