using UnityEngine;

public class ShieldJoystickController : MonoBehaviour
{
    public Joystick shieldJoystick;

    public float GetShieldJoystickHorizontalInput()
    {
        return shieldJoystick.Horizontal;
    }

    public Vector3 GetShieldJoystickInput()
    {
        float horizontal = shieldJoystick.Horizontal;
        float vertical = shieldJoystick.Vertical;
        return new Vector3(horizontal, 0, vertical);
    }

    public bool IsJoystickReleased()
    {
        // Joystick'in ortada olup olmadığını kontrol eder
        return Mathf.Approximately(shieldJoystick.Horizontal, 0f) && Mathf.Approximately(shieldJoystick.Vertical, 0f);
    }
}
