using UnityEngine;

public class LiderTakibi : MonoBehaviour
{
    [System.Serializable]
    public class Takipci
    {
        public Transform obje; // Takip eden objenin Transform komponenti
        public float xFark;    // Liderden x ekseninde fark
        public float yFark;    // Liderden y ekseninde fark
        public float zFark;    // Liderden z ekseninde fark
    }

    public Takipci[] takipEdenler; // Takip eden objelerin listesi
    private Rigidbody liderRigidbody;

    void Start()
    {
        liderRigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        foreach (Takipci takipci in takipEdenler)
        {
            if (takipci.obje != null)
            {
                Rigidbody takipciRigidbody = takipci.obje.GetComponent<Rigidbody>();
                if (takipciRigidbody != null)
                {
                    // Liderin pozisyonuna göre takipçilerin hedef pozisyonlarını hesapla
                    Vector3 hedefPozisyon = new Vector3(
                        transform.position.x + takipci.xFark,
                        transform.position.y + takipci.yFark,
                        transform.position.z + takipci.zFark
                    );

                    // Takipçiyi yeni pozisyona taşı
                    takipciRigidbody.MovePosition(hedefPozisyon);
                    // Takipçiye liderin hızını ve dönme hızını aktar
                    takipciRigidbody.linearVelocity = liderRigidbody.linearVelocity/2;
                    takipciRigidbody.angularVelocity = liderRigidbody.angularVelocity/2;

                }
            }
        }
    }
}
