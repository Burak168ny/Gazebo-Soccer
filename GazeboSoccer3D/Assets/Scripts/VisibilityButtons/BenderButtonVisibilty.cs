using UnityEngine;
using Fusion;

public class BenderButtonVisibility : MonoBehaviour
{
    private GameObject movementJoystick;
    private GameObject horizontalButton;
    private GameObject avarageButton;
    private GameObject verticalButton;
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
            movementJoystick = canvas.transform.Find("Movement Joystick")?.gameObject;
            horizontalButton = canvas.transform.Find("HorizontalButton")?.gameObject;
            avarageButton = canvas.transform.Find("AvarageButton")?.gameObject;
            verticalButton = canvas.transform.Find("VerticalButton")?.gameObject;

            if (movementJoystick != null) movementJoystick.SetActive(true);
            if (horizontalButton != null) horizontalButton.SetActive(true);
            if (avarageButton != null) avarageButton.SetActive(true);
            if (verticalButton != null) verticalButton.SetActive(true);
        }
    }
}
