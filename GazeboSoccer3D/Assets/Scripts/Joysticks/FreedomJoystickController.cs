using UnityEngine;

public class FreedomJoystickController : MonoBehaviour
{
    // Hareket için joystick referansı
    public Joystick freedomJoystick;

    // Şut joystick'inden sadece yatay eksen girdisini almak için
    public float GetFreedomJoystickHorizontalInput()
    {
        return freedomJoystick.Horizontal;  // Şut joystick'inin yatay ekseni (-1 ile 1 arasında)
    }

    // Şut joystick'inden hem X hem de Y ekseni girdilerini almak için (isterseniz)
    public Vector3 GetFreedomJoystickInput()
    {
        float horizontal = freedomJoystick.Horizontal;
        float vertical = freedomJoystick.Vertical;
        return new Vector3(horizontal, 0, vertical);
    }
}
