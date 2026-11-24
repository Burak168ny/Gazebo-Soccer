using UnityEngine;
using Fusion;

public class WolfButtonVisibility : MonoBehaviour
{
    private GameObject jumpButton;
    private GameObject movementJoystick;
    private GameObject shootJoystick;

    private NetworkObject networkObject;

    void Start()
    {
        // NetworkObject referansı al
        networkObject = GetComponent<NetworkObject>();

        // Eğer bu karakterin sahibi bu cihaz değilse, bu script çalışmasın
        if (networkObject == null || !networkObject.HasInputAuthority)
        {
            this.enabled = false; // Script'i devre dışı bırak
            return;
        }

        // Sadece karakterin sahibi olan oyuncu için buton ve joystickleri ayarla
        GameObject canvas = GameObject.Find("Canvas");

        if (canvas != null)
        {
            jumpButton = canvas.transform.Find("JumpButton")?.gameObject;
            movementJoystick = canvas.transform.Find("Movement Joystick")?.gameObject;
            shootJoystick = canvas.transform.Find("Shoot Joystick")?.gameObject;

            if (jumpButton != null) jumpButton.SetActive(true);
            if (movementJoystick != null) movementJoystick.SetActive(true);
            if (shootJoystick != null) shootJoystick.SetActive(true);
        }
    }
}
