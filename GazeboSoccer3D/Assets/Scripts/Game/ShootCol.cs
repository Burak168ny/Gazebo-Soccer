using UnityEngine;

public class ShootCol : MonoBehaviour
{
    public Transform[] takipEdenler; // Takip eden objelerin Transform komponentleri
    public float birimx;
    public float birimy;
    public float birimz;

    void Update()
    {
        // Liderin mevcut pozisyonundan 1 birim x, y ve z eksenlerinde geriye olan pozisyonu hesapla
        Vector3 yeniPozisyon = new Vector3(transform.position.x - birimx, transform.position.y - birimy, transform.position.z - birimz);

        foreach (Transform takipci in takipEdenler)
        {
            if (takipci != null)
            {
                // Her bir takipçiyi, hesaplanan yeni pozisyona yerleştir
                takipci.position = yeniPozisyon;
            }
        }
    }
}
