using UnityEngine;
using Fusion;

public class SunButtonVisibility : MonoBehaviour
{
    private GameObject jumpButton;
    private GameObject movementJoystick;
    private GameObject shootJoystick;
    private GameObject shieldJoystick;
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

        GameObject canvas = GameObject.Find("Canvas");

        if (canvas != null)
        {
            jumpButton = canvas.transform.Find("JumpButton")?.gameObject;
            movementJoystick = canvas.transform.Find("Movement Joystick")?.gameObject;
            shootJoystick = canvas.transform.Find("Shoot Joystick")?.gameObject;
            shieldJoystick = canvas.transform.Find("ShieldJoystick")?.gameObject;

            if (jumpButton != null) jumpButton.SetActive(true);
            if (movementJoystick != null) movementJoystick.SetActive(true);
            if (shootJoystick != null) shootJoystick.SetActive(true);
            if (shieldJoystick != null) shieldJoystick.SetActive(true);
        }
    }
}
