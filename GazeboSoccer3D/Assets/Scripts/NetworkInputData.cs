using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{    
    public const byte JUMP = 4;
    public const byte SCALE_X_UP = 5;
    public const byte SCALE_Y_UP = 7;
    public const byte SCALE_Z_UP = 9;
    public const byte SHOOT = 11;
    public const byte SHIELD = 12;
    public const byte SHIELD_THROW = 13;
    public const byte TOGGLE_VISIBILITY = 14;
    public const byte FREEDOM_PUSH = 15; // Yeni buton ekleme
    public const byte TOGGLE_ABILITY = 16; // Yeni yetenek için buton


    


    public NetworkButtons buttons;
    public Vector3 direction;
    public float shootDirection;
    public Vector3 shieldDirection;
    public Vector3 freedomDirection;
    public Vector3 resetPosition; // Reset sırasında kullanılacak pozisyon
    public int characterIndex; // Karakter indeksini ekledik





    public void ReadInput(Vector3 joystickDirection, Vector3 shieldDirection, Vector3 freedomDirection)
    {
        // Joystick inputlarını direction ile ilişkilendir
        direction = joystickDirection;

        // Kalkan yönü inputu
        this.shieldDirection = shieldDirection;  // Burada doğru ismi kullandık
        this.freedomDirection = freedomDirection;  // Burada doğru ismi kullandık

        if (freedomDirection != Vector3.zero)
        {
            buttons.Set(FREEDOM_PUSH, true);
        }

            if (shieldDirection != Vector3.zero)
        {
            buttons.Set(SHIELD_THROW, true);
        }

        // // Zıplama kontrolü
        // if (Input.GetKey(KeyCode.Space))
        // {
        //     buttons.Set(JUMP, true);
        // }
        
        // Görünmezlik aç/kapat kontrolü (Örnek: I tuşu)
        // if (Input.GetKeyDown(KeyCode.I))
        // {
        //     buttons.Set(TOGGLE_VISIBILITY, true);
        // }

        // Boyut büyütme kontrolleri
        if (Input.GetKey(KeyCode.Keypad6)) buttons.Set(SCALE_X_UP, true);
        if (Input.GetKey(KeyCode.Keypad4)) buttons.Set(SCALE_Y_UP, true);
        if (Input.GetKey(KeyCode.Keypad5)) buttons.Set(SCALE_Z_UP, true);

    }
}
