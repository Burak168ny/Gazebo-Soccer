using UnityEngine;

public class ExitButton : MonoBehaviour
{
    // Çıkış butonu çağrıldığında bu fonksiyon çalışır
    public void ExitGame()
    {
        // Oyun derlenmiş bir uygulama ise oyunu kapatır
        Application.Quit();

        // Oyun editörde çalışıyorsa, bu sadece editör modunda çalışır
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
