using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshPro için gerekli kütüphane

public class LanguageToggle : MonoBehaviour
{
    // Türkçe ve İngilizce objeler için iki ayrı liste
    public List<GameObject> turkishObjects;
    public List<GameObject> englishObjects;

    // TMP Text referansı
    public TMP_Text languageText;

    // Hangi dilin aktif olduğunu belirten bayrak
    private bool isTurkishActive = true;

    // Başlangıçta Türkçe aktif, İngilizce pasif
    private void Start()
    {
        SetLanguage(true); // Türkçe aktif
    }

    // Toggle işlemi: Aktif dili değiştir
    public void ToggleLanguage()
    {
        isTurkishActive = !isTurkishActive;
        SetLanguage(isTurkishActive);
    }

    // Dili ayarla: Türkçe mi, İngilizce mi?
    private void SetLanguage(bool turkishActive)
    {
        // Türkçe objelerin aktifliği
        foreach (GameObject obj in turkishObjects)
        {
            if (obj != null)
                obj.SetActive(turkishActive);
        }

        // İngilizce objelerin aktifliği
        foreach (GameObject obj in englishObjects)
        {
            if (obj != null)
                obj.SetActive(!turkishActive);
        }

        // TMP Text'i güncelle
        if (languageText != null)
        {
            languageText.text = turkishActive ? "TR" : "EN";
        }
    }
}
