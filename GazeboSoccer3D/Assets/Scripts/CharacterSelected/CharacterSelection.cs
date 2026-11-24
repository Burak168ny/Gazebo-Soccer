using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterSelection : MonoBehaviour
{
    private GameObject[] characterList;
    public int index;

    // Elle atanacak listeler
    public GameObject[] firstList;  // Türkçe yazılar
    public GameObject[] secondList; // İngilizce yazılar

    // Türkçe ve İngilizce dil seçeneklerini temsil eden objeler
    public GameObject turkishTextObject;  // Türkçe yazı objesi
    public GameObject englishTextObject; // İngilizce yazı objesi

    private bool isFirstListActive = true; // Hangi liste aktif takip etmek için

    // TMP Text referansı
    public TMP_Text languageText;  // Dil göstergesi için TMP Text referansı

    private void Start()
    {
        characterList = new GameObject[transform.childCount];
        for (int j = 0; j < transform.childCount; j++)
            characterList[j] = transform.GetChild(j).gameObject;

        // Karakterleri inaktif yap
        foreach (GameObject go in characterList)
            go.SetActive(false);

        // Sadece aktif karakteri göster
        if (characterList.Length > 0 && index >= 0 && index < characterList.Length)
            characterList[index].SetActive(true);

        // İlk listedeki aktif öğeyi göster
        SetAllInactive(firstList);
        SetAllInactive(secondList);

        if (firstList.Length > 0 && index >= 0 && index < firstList.Length)
            firstList[index].SetActive(true);

        // Dil göstergesini başlat
        UpdateLanguageText();
    }

    public void ToggleLeft()
    {
        if (isFirstListActive)
        {
            // Türkçe liste üzerinden ilerleme
            characterList[index].SetActive(false);
            if (firstList.Length > index) firstList[index].SetActive(false);

            index--;
            if (index < 0) index = characterList.Length - 1;

            characterList[index].SetActive(true);
            if (firstList.Length > index) firstList[index].SetActive(true);
        }
        else
        {
            // İngilizce liste üzerinden ilerleme
            characterList[index].SetActive(false);
            if (secondList.Length > index) secondList[index].SetActive(false);

            index--;
            if (index < 0) index = characterList.Length - 1;

            characterList[index].SetActive(true);
            if (secondList.Length > index) secondList[index].SetActive(true);
        }
    }

    public void ToggleRight()
    {
        if (isFirstListActive)
        {
            // Türkçe liste üzerinden ilerleme
            characterList[index].SetActive(false);
            if (firstList.Length > index) firstList[index].SetActive(false);

            index++;
            if (index >= characterList.Length) index = 0;

            characterList[index].SetActive(true);
            if (firstList.Length > index) firstList[index].SetActive(true);
        }
        else
        {
            // İngilizce liste üzerinden ilerleme
            characterList[index].SetActive(false);
            if (secondList.Length > index) secondList[index].SetActive(false);

            index++;
            if (index >= characterList.Length) index = 0;

            characterList[index].SetActive(true);
            if (secondList.Length > index) secondList[index].SetActive(true);
        }
    }

    public void ToggleLists()
    {
        if (isFirstListActive)
        {
            // Türkçe listeden İngilizce listeye geçiş
            SetAllInactive(firstList); // Türkçe listeyi tamamen kapat
            if (secondList.Length > index) secondList[index].SetActive(true);

            // Türkçe metin objesini kapat, İngilizceyi aç
            if (turkishTextObject != null) turkishTextObject.SetActive(false);
            if (englishTextObject != null) englishTextObject.SetActive(true);
        }
        else
        {
            // İngilizce listeden Türkçe listeye geçiş
            SetAllInactive(secondList); // İngilizce listeyi tamamen kapat
            if (firstList.Length > index) firstList[index].SetActive(true);

            // İngilizce metin objesini kapat, Türkçeyi aç
            if (turkishTextObject != null) turkishTextObject.SetActive(true);
            if (englishTextObject != null) englishTextObject.SetActive(false);
        }

        // Aktif listeyi değiştir
        isFirstListActive = !isFirstListActive;

        // Dil göstergesini güncelle
        UpdateLanguageText();
    }

    private void SetAllInactive(GameObject[] list)
    {
        foreach (GameObject go in list)
            go.SetActive(false);
    }

    private void UpdateLanguageText()
    {
        if (languageText != null)
        {
            // Dil göstergesi TR ya da EN olarak ayarlanır
            languageText.text = isFirstListActive ? "TR" : "EN";
        }
    }

    public void MultiplayerButton()
    {
        SceneManager.LoadScene("GazeboScene");
    }

    public void AgentsButton()
    {
        SceneManager.LoadScene("Agents");
    }

    public void VSButton()
    {
        SceneManager.LoadScene("1V1");
    }

    public void MainMenuButton()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
