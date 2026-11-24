using System.Collections; // Coroutine için gerekli kütüphane
using UnityEngine;

public class DeactivateObjects : MonoBehaviour
{
    [SerializeField] private Animator smokeAnimation;
     // Referans alınacak objeler için dizi
    public GameObject[] objectsToDeactivate;

    // Ekstra referanslar
    public GameObject firstObject;  // İlk referans obje
    public GameObject secondObject; // İkinci referans obje
    public GameObject thirdObject; // İkinci referans obje
    public GameObject fourthObject; // İkinci referans obje
    public GameObject pentaObject; // İkinci referans obje

    // Materyal referansı
    public Material newMaterial;

    // Dizinin sırasını takip etmek için bir sayaç
    private int currentIndex = 0;

    // Çarpışma kontrolü
    private void OnCollisionEnter(Collision collision)
    {
        // Çarpan objenin tagi "Ball" ise
        if (collision.gameObject.CompareTag("Ball"))
        {
            // Eğer tüm objeler kapatıldıysa bir işlem yapma
            if (currentIndex >= objectsToDeactivate.Length)
                return;

                const string SMOKE_CLIP_NAME = "Smoke";
                smokeAnimation.Play(SMOKE_CLIP_NAME);

            // İlk objeyi devre dışı bırak
            objectsToDeactivate[currentIndex].SetActive(false);

            // Eğer sıradaki obje varsa ikinci objeyi de devre dışı bırak
            if (currentIndex + 1 < objectsToDeactivate.Length)
            {
                objectsToDeactivate[currentIndex + 1].SetActive(false);
            }

            // Sırayı iki birim artır (çünkü iki obje birden devre dışı bırakıldı)
            currentIndex += 2;

            // Eğer tüm objeler devre dışı bırakıldıysa işlemleri yap
            if (currentIndex >= objectsToDeactivate.Length)
            {
                // İlk referans obje işlemleri (materyal değişimi ve setactive)
                if (firstObject != null)
                {
                    Renderer renderer = firstObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = newMaterial;
                    }

                    // 2 saniye sonra firstObject'i devre dışı bırak
                    StartCoroutine(DisableFirstObjectAfterDelay());
                }

                // İkinci referans obje işlemlerini gecikmeli yap
                if (secondObject != null)
                {
                    StartCoroutine(HandleSecondObjectAfterDelay());
                }
            }
        }
    }

// İkinci obje işlemleri için coroutine
private IEnumerator HandleSecondObjectAfterDelay()
{
    // 1 saniye bekle
    yield return new WaitForSeconds(1f);

    // Ana objenin layer'ını değiştir
    secondObject.layer = LayerMask.NameToLayer("Default");

    // Ana objenin MeshRenderer'ını etkinleştir
    MeshRenderer mainMeshRenderer = secondObject.GetComponent<MeshRenderer>();
    if (mainMeshRenderer != null)
    {
        mainMeshRenderer.enabled = true;
    }

    // Child objelerin layer'larını değiştir ve MeshRenderer'larını etkinleştir
    foreach (Transform child in secondObject.transform)
    {
        child.gameObject.layer = LayerMask.NameToLayer("Default");

        MeshRenderer childMeshRenderer = child.GetComponent<MeshRenderer>();
        if (childMeshRenderer != null)
        {
            childMeshRenderer.enabled = true;
        }
    }
    if (pentaObject != null)
    {
        pentaObject.SetActive(false);
    }

    // Mesh renderer'lar aktif olduktan sonra 1 saniye bekle
    yield return new WaitForSeconds(1f);

    // İkinci objeyi devre dışı bırak
    secondObject.SetActive(false);
    thirdObject.SetActive(false);

    yield return new WaitForSeconds(0.3f);

    fourthObject.SetActive(true);
}


    // İlk obje işlemleri için coroutine
    private IEnumerator DisableFirstObjectAfterDelay()
    {
        // 2 saniye bekle
        yield return new WaitForSeconds(2f);

        // İlk objeyi devre dışı bırak
        if (firstObject != null)
        {
            firstObject.SetActive(false);
        }
    }
}
