using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro ile çalışabilmek için gerekli

public class RandomThrowerManager : MonoBehaviour
{
    [Header("RandomThrower Referansları")]
    public RandomThrower[] randomThrowers = new RandomThrower[5];

    [Header("Başlangıç Değerleri")]
    public float defaultMinAngleRange = -80f;
    public float defaultMaxAngleRange = 80f;

    [Header("Inspector'dan Ayarlanacak Değerler")]
    public float[] customMinAngleRanges = new float[5];
    public float[] customMaxAngleRanges = new float[5];

    [Header("Buton ve Durum")]
    public Button toggleButton; // UI butonu
    public TextMeshProUGUI buttonText; // Butonun TextMeshProUGUI bileşeni
    public Color defaultButtonColor = Color.green; // "Herkes Tek" için yeşil renk
    public Color customButtonColor = Color.red;   // "1'e Karşı 5" için kırmızı renk

    [Header("Directional Light Rengi")]
    public Light directionalLight; // Directional light referansı

    [Header("AudioSource")]
    public AudioSource audioSource; // Müzik kaynağı

    private bool isDefaultMode = true; // Varsayılan modda mı?

    private Color defaultLightColor; // Directional light'ın varsayılan rengi
    private float defaultPitch = 1f;  // Varsayılan pitch değeri

    private void Start()
    {
        // Başlangıçta tüm referansların değerlerini varsayılan değerlere ayarla
        for (int i = 0; i < randomThrowers.Length; i++)
        {
            if (randomThrowers[i] != null)
            {
                randomThrowers[i].minAngleRange = defaultMinAngleRange;
                randomThrowers[i].maxAngleRange = defaultMaxAngleRange;
            }
        }

        // Directional light'ın varsayılan rengini kaydet
        if (directionalLight != null)
        {
            defaultLightColor = directionalLight.color;
        }

        // AudioSource'un varsayılan pitch değerini kaydet
        if (audioSource != null)
        {
            defaultPitch = audioSource.pitch;
        }

        // Buton textini güncelle
        UpdateButtonTextAndColor();

        // Butonun onClick olayına metod ekle
        toggleButton.onClick.AddListener(ToggleThrowerRanges); // Butona tıklanınca ToggleThrowerRanges metodu çalışacak
    }

    public void ToggleThrowerRanges()
    {
        if (isDefaultMode)
        {
            // Özel değerlere geçiş
            for (int i = 0; i < randomThrowers.Length; i++)
            {
                if (randomThrowers[i] != null)
                {
                    randomThrowers[i].minAngleRange = customMinAngleRanges[i];
                    randomThrowers[i].maxAngleRange = customMaxAngleRanges[i];
                }
            }

            // Directional Light rengini değiştir (G ve B'yi 196 yap)
            if (directionalLight != null)
            {
                directionalLight.color = new Color(directionalLight.color.r, 196f / 255f, 196f / 255f);
            }

            // Sesin hızını arttır (Pitch değeri 1.5 yap)
            if (audioSource != null)
            {
                audioSource.pitch = 1.03f; // Ses hızını %50 artır
            }
        }
        else
        {
            // Varsayılan değerlere geçiş
            for (int i = 0; i < randomThrowers.Length; i++)
            {
                if (randomThrowers[i] != null)
                {
                    randomThrowers[i].minAngleRange = defaultMinAngleRange;
                    randomThrowers[i].maxAngleRange = defaultMaxAngleRange;
                }
            }

            // Directional Light rengini eski haline getir (Varsayılan renk)
            if (directionalLight != null)
            {
                directionalLight.color = defaultLightColor;
            }

            // Sesin hızını eski haline getir (Pitch değeri 1)
            if (audioSource != null)
            {
                audioSource.pitch = defaultPitch; // Ses hızını eski haline getir
            }
        }

        // Mod durumunu değiştir
        isDefaultMode = !isDefaultMode;

        // Buton textini ve rengini güncelle
        UpdateButtonTextAndColor();
    }

    private void UpdateButtonTextAndColor()
    {
        if (isDefaultMode)
        {
            buttonText.text = "Herkes Tek";
            toggleButton.GetComponent<Image>().color = defaultButtonColor; // Yeşil renk
        }
        else
        {
            buttonText.text = "1'e Karşı 5";
            toggleButton.GetComponent<Image>().color = customButtonColor; // Kırmızı renk
        }
    }
}
