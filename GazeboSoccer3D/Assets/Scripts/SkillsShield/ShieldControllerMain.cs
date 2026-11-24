// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public enum ShieldState
// {
//     InHands,
//     Flying,
//     Returning,
// }

// public class ShieldController : MonoBehaviour
// {
//     public ShieldState state = ShieldState.InHands;
//     public Transform player, handHolder;

//     public float throwForce = 20f, pickupRange = 3f;
//     public int maxBounces = 17;

//     private Rigidbody rb;
//     private Collider col;
//     private int bounceCount = 0;
//     private bool mustReturn = false;

//     void Start()
//     {
//         rb = GetComponent<Rigidbody>();
//         col = GetComponent<Collider>();

//         // Yalnızca y ekseninde konum sabitliyoruz ve x, z ekseninde rotasyon sabitliyoruz
//         rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

//         // Başlangıçta kalkan statik olmalı, o yüzden isKinematic
//         rb.isKinematic = true;
//     }

//     void Update()
//     {
//         if (state == ShieldState.InHands)
//         {
//             if (Input.GetKeyDown(KeyCode.Mouse0))
//             {
//                 Throw();
//             }
//         }
//         else if (state == ShieldState.Flying || state == ShieldState.Returning)
//         {
//             if (mustReturn)
//             {
//                 ReturnToPlayer();
//             }

//             float distanceToPlayer = Vector3.Distance(transform.position, player.position);
//             if (distanceToPlayer < pickupRange && state == ShieldState.Returning)
//             {
//                 Returned();
//             }
//         }
//     }

//     void Throw()
//     {
//         state = ShieldState.Flying;
//         rb.isKinematic = false;
//         col.enabled = true;

//         // Kalkanın fırlatılacağı konum: Player'ın sağ tarafı (local x ekseninde 5 birim sağda)
//         Vector3 throwPosition = player.position + player.up * 1f + player.right * 0.3f - player.forward * 0.3f;

//         // Kalkanı belirlenen konuma taşıyoruz
//         transform.position = throwPosition;

//         // Kalkanın ileri doğru kuvvetle fırlatılması (normalize edilmiş)
//         Vector3 throwDirection = transform.forward.normalized;
//         rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);

//         bounceCount = 0;
//         mustReturn = false;
//     }

// void ReturnToPlayer()
// {
//     // Oyuncunun 3 birim önünde hedef konumu hesaplıyoruz
//     Vector3 targetPosition = player.position + player.forward * 3f;

//     // Kalkanın bu hedef pozisyona dönmesi için yön vektörünü hesaplıyoruz
//     Vector3 directionToTarget = (targetPosition - transform.position).normalized;

//     // Geri dönüş kuvvetini uyguluyoruz
//     rb.velocity = Vector3.zero; // Önceki hareketi durdurmak için hızı sıfırla
//     rb.AddForce(directionToTarget * 5f, ForceMode.Impulse);  // Hedefe geri dönmek için kuvvet uygula

//     state = ShieldState.Returning;
// }


//     void OnCollisionEnter(Collision collision)
//     {
//         if (state == ShieldState.Flying)
//         {
//             bounceCount++;  // Her çarpma olduğunda sektirme sayısını artır

//             if (bounceCount >= maxBounces)
//             {
//                 mustReturn = true;  // Max sektirme gerçekleştiyse geri dönüş başlasın
//             }
//         }
//     }

// void Returned()
// {
//     state = ShieldState.InHands;
//     rb.isKinematic = true;  // Tekrar statik hale getiriyoruz
//     col.enabled = false;

//     transform.SetParent(handHolder);

//     // Kalkanı handHolder'ın 3 birim önüne yerleştiriyoruz
//     transform.localPosition = handHolder.forward * 2f; // 3 birim önde
//     transform.localRotation = Quaternion.identity; // Rotasyonu sıfırlıyoruz

//     bounceCount = 0;
//     mustReturn = false;
// }

// }
