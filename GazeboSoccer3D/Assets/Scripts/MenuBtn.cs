using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuBtn : MonoBehaviour
{
    public Camera camera1; // Birinci kamera referansı
    public Camera camera2; // İkinci kamera referansı
    public TMP_Text buttonText; // Button'un TMP text referansı

    public GameObject object1; // İlk GameObject referansı
    public GameObject object2; // İkinci GameObject referansı
    public GameObject object3; // Üçüncü GameObject referansı
    public GameObject object4; // Dördüncü GameObject referansı
    public GameObject object5; // İlk GameObject referansı
    public GameObject object6; // İkinci GameObject referansı
    public GameObject object7; // Üçüncü GameObject referansı
    public GameObject object8; // Dördüncü GameObject referansı

    public BallControl ballControl; // BallControl referansı
    public TMP_Text throwForceButtonText; // ThrowForce butonunun TMP text referansı

    private int throwForceIndex = 0; // Mevcut throwForce seviyesini takip eder
    private readonly float[] throwForceValues = { 40f, 60f, 80f }; // ThrowForce değerleri
    private readonly string[] throwForceTexts = { "Low", "Medium", "High" }; // Buton metinleri

    private bool isTraining = true; // Kamera durumunu izler

    private void Start()
    {
        if (buttonText != null)
        {
            buttonText.text = "Training"; // Başlangıçta "Training" yaz
        }
    }

    public void MenuButton()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartAgentsButton()
    {
        SceneManager.LoadScene("Agents");
    }

    public void ToggleCamera()
    {
        if (camera1 != null && camera2 != null && buttonText != null)
        {
            // Kameraları toggle et
            bool isCamera1Active = camera1.gameObject.activeSelf;
            camera1.gameObject.SetActive(!isCamera1Active);
            camera2.gameObject.SetActive(isCamera1Active);

            // TMP text'i güncelle
            isTraining = !isTraining; // Kamera durumunu güncelle
            buttonText.text = isTraining ? "Training" : "Watch";

            // Objeleri aktif/pasif yap
            ToggleObjects(isCamera1Active);
        }
    }

    private void ToggleObjects(bool setActive)
    {
        // Objeleri aktif/pasif yap
        if (object1 != null) object1.SetActive(setActive);
        if (object2 != null) object2.SetActive(setActive);
        if (object3 != null) object3.SetActive(setActive);
        if (object4 != null) object4.SetActive(setActive);
        if (object1 != null) object5.SetActive(setActive);
        if (object2 != null) object6.SetActive(setActive);
        if (object3 != null) object7.SetActive(setActive);
        if (object4 != null) object8.SetActive(setActive);
    }

    public void ChangeThrowForce()
    {
        if (ballControl != null && throwForceButtonText != null)
        {
            // ThrowForce seviyesini döngüsel olarak değiştir
            throwForceIndex = (throwForceIndex + 1) % throwForceValues.Length;
            ballControl.throwForce = throwForceValues[throwForceIndex];

            // Buton metnini güncelle
            throwForceButtonText.text = throwForceTexts[throwForceIndex];
        }
    }
}
