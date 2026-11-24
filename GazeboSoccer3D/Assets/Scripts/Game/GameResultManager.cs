using UnityEngine;
using TMPro;  // TextMeshPro için gerekli

public class GameResultManager : MonoBehaviour
{
    [Header("Liste 1 (Kazananlar)")]
    public GameObject[] winnersList;  // Kazananlar listesi (Inspector'dan doldurulacak)

    [Header("Liste 2 (Kaybedenler)")]
    public GameObject[] losersList;   // Kaybedenler listesi (Inspector'dan doldurulacak)

    [Header("Sonuç Mesajı")]
    public GameObject resultMessagePanel;  // Sonuç mesajının gösterileceği GameObject (Panel veya UI Container)

    [Header("Sonuç Mesajı Metni")]
    public TextMeshProUGUI resultText;  // Sonuç mesajının gösterileceği TextMeshPro

    private void Start()
    {
        // Başlangıçta resultMessagePanel'i gizle
        if (resultMessagePanel != null)
        {
            resultMessagePanel.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        // Her iki listeyi kontrol et
        CheckGameResult();
    }

    private void CheckGameResult()
    {
        // Kazananlar listesindeki tüm objeler "SetActive(false)" mi?
        bool allWinnersInactive = true;
        foreach (var winner in winnersList)
        {
            if (winner.activeSelf)
            {
                allWinnersInactive = false;
                break;
            }
        }

        // Kaybedenler listesindeki tüm objeler "SetActive(false)" mi?
        bool allLosersInactive = true;
        foreach (var loser in losersList)
        {
            if (loser.activeSelf)
            {
                allLosersInactive = false;
                break;
            }
        }

        // Sonuç durumunu belirle
        if (allWinnersInactive)
        {
            resultText.text = "Kazandınız!";  // Kazanma durumu
        }
        else if (allLosersInactive)
        {
            resultText.text = "Kaybettiniz!";  // Kaybetme durumu
        }
        else
        {
            // Durum henüz kesinleşmemişse, hiçbir şey yapma
            return;
        }

        // Sonuç güncellendikten sonra resultMessagePanel'i aktif hale getir
        if (resultMessagePanel != null)
        {
            resultMessagePanel.SetActive(true);
        }
    }
}
