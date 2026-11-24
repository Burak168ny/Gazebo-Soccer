using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VsScore : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Button setTimerButton;
    [SerializeField] private TMP_Text setTimerButtonText;
    [SerializeField] private Button startTimerButton;
    [SerializeField] private TMP_Text startTimerButtonText; // Start/Stop butonu metni

    [Header("Timer Settings")]
    [SerializeField] private int[] timerDurations = { 10, 30, 60 }; // Seçilebilir süreler

    private int currentTimerIndex = 0;
    private float remainingTime;
    private bool isTimerRunning = false;

    [Header("References")]
    [SerializeField] private BallMlTVS ballMlTVS; // BallMlTVS referansı

    private void Start()
    {
        // Buton metinlerini güncelle
        UpdateSetTimerButtonText();
        UpdateStartTimerButtonState();

        // Butonlara tıklama olaylarını bağla
        setTimerButton.onClick.AddListener(CycleTimerDuration);
        startTimerButton.onClick.AddListener(ToggleTimer);

        // Başlangıç skorunu güncelle
        UpdateScoreText();
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0)
            {
                remainingTime = 0;
                isTimerRunning = false;

                // Kronometre sıfırlandığında "Start" durumuna geç
                startTimerButtonText.text = "Start";
                UpdateStartTimerButtonState();

                // Skoru sıfırlama, sadece zamanlayıcıyı durdur
                UpdateTimerText();
            }
            else
            {
                UpdateTimerText();
            }
        }

        // BallMlTVS skorunu takip ederek güncelle
        UpdateScoreText();
    }

    private void ToggleTimer()
    {
        if (isTimerRunning)
        {
            // Eğer zamanlayıcı çalışıyorsa durdur
            isTimerRunning = false;
            startTimerButtonText.text = "Start";
        }
        else
        {
            // Eğer zamanlayıcı duruyorsa başlat
            StartTimer();
        }

        UpdateStartTimerButtonState();
    }

    private void StartTimer()
    {
        // Süreyi başlat
        remainingTime = timerDurations[currentTimerIndex];
        isTimerRunning = true;
        startTimerButtonText.text = "Stop";

        // BallMlTVS skoru sıfırla
        if (ballMlTVS != null)
        {
            ballMlTVS.ResetScore();
        }

        UpdateScoreText();
        UpdateTimerText();
    }

    private void CycleTimerDuration()
    {
        // Sonraki süreyi seç
        currentTimerIndex = (currentTimerIndex + 1) % timerDurations.Length;
        UpdateSetTimerButtonText();
    }

    private void UpdateScoreText()
    {
        if (ballMlTVS != null)
        {
            scoreText.text = $"{ballMlTVS.GetScore()}";
        }
    }

    private void UpdateTimerText()
    {
        timerText.text = $"{Mathf.CeilToInt(remainingTime)}s";
    }

    private void UpdateSetTimerButtonText()
    {
        setTimerButtonText.text = $"{timerDurations[currentTimerIndex]}s";
    }

    private void UpdateStartTimerButtonState()
    {
        startTimerButton.interactable = true;
    }
}
