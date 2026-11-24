using Fusion;
using UnityEngine;

public enum InputButton
{
    Throw = 0,
    Ability0 = 1, // yetenek butonları: SkillBase.InputButtonFor(destedeki sıra, action)
}

/// Her tick'te client'tan host'a giden girdi. Sadece tuş bilgisi gider, pozisyon gitmez.
public struct NetworkInputData : INetworkInput
{
    public Vector2 Move;          // x: kale boyunca, y: merkeze doğru (oyuncunun kendi bakış açısına göre)
    public NetworkButtons Buttons; // Space vb. tek seferlik tuşlar
    public Vector2 ThrowAim;       // şut joystick'inin yönü (sıfır = klavye: karşıya + sağ/sol sapma)
}
