using System;
using System.Collections.Generic;
using System.Reflection;
using Fusion;
using UnityEngine;

/// Her yeteneğin kimliği. Yeni yetenek = buraya bir satır + SkillBase'den türeyen yeni bir class.
public enum SkillId
{
    PowerShot = 0,
    InvisibleShot = 1,
    Speed = 2,
    Teleport = 3,
    Wall = 4,
    Lifesteal = 5,
    Paint = 6,
    Disc = 7,
    MultiShot = 8,
    BallSize = 9,
    Curve = 10,
    GoldenBall = 11,
    Decoy = 12,
    Mirror = 13,
    Magnet = 14,
    SecondChance = 15,
    Reverse = 16,
    Silence = 17,
    Cleanse = 18,
    Telekinesis = 19,
    Bounty = 20,
    Ceasefire = 21,
    SoulSwap = 22,
    Kamikaze = 23,
    UpsideDown = 24,
    Curse = 25,
    PuppetMaster = 26,
    HotBomb = 27,
    Sharpshooter = 28,
    Gift = 29,
    Push = 30,
}

/// <summary>
/// Tüm yeteneklerin ortak temeli: 3 seviye, seviye fiyatları, satın alma.
/// Yetenekler Keeper prefab'ına bileşen olarak eklenir. Seviye ağdan senkronlanır.
/// Satın alma ve kullanma kararını host verir (hile yapılamaz).
/// </summary>
public abstract class SkillBase : NetworkBehaviour
{
    public const int MaxLevel = 3;

    [Tooltip("1., 2. ve 3. seviyenin altın bedeli")]
    [SerializeField] int[] prices = { 150, 300, 600 };

    [Networked] public int Level { get; set; } // 0 = alınmadı

    public abstract SkillId Id { get; }

    KeeperController _keeper;
    protected KeeperController Keeper => _keeper != null ? _keeper : (_keeper = GetComponent<KeeperController>());

    public bool IsMaxed => Level >= MaxLevel;
    public int PriceOf(int level) => prices[Mathf.Clamp(level - 1, 0, prices.Length - 1)];

    // ---- Aktif kullanım (butonla). Pasif yetenekler bunları değiştirmez. ----
    public virtual bool HasActiveAbility => false;
    public virtual float CooldownRemaining => 0f;
    /// Şu an kullanılabilir mi (ör. top elinde mi)? Arayüz butonu buna göre açar.
    public virtual bool CanActivateNow => true;
    /// aimX: oyuncunun o anki sağ/sol girdisi (fırlatan yetenekler için)
    public virtual void ServerActivate(float aimX) { }

    // ---- Birden fazla kullanma butonu olan yetenekler için (action 0 = ilk buton, 1 = ikinci) ----
    public const int MaxActions = 2;
    public virtual bool HasAction(int action) => action == 0 && HasActiveAbility;
    public virtual bool CanUseAction(int action) => CanActivateNow;
    /// true ise kullanım client'ta da tahmin edilir (gecikmesiz). Sadece pozisyon gibi basit şeyler için.
    public virtual bool PredictOnClient => false;
    /// Butona göre ayrı bekleme süresi olan yetenekler bunu değiştirir.
    public virtual float CooldownRemainingFor(int action) => CooldownRemaining;
    public virtual void UseAction(int action, float aimX)
    {
        if (action == 0) ServerActivate(aimX);
    }

    // ---- Oyun içi kontroller (deste sistemi butonları/joystick'leri buna göre dağıtır) ----
    /// Bu yetenek kaç tane "kullan" butonu ister (0 = pasif ya da joystick'li).
    public virtual int ActionButtonCount => 0;
    /// Susturulmuşken de kullanılabilir mi (ör. Arındır).
    public virtual bool UsableWhileSilenced => false;
    /// Kullan butonunda isimden sonra yazacak ek (ör. "Sol" / "Sağ").
    public virtual string ActionLabel(int action) => "";
    /// Joystick ile kullanılan yetenekler (ör. Sabitleyen Disk).
    public virtual bool UsesJoystick => false;
    public virtual bool JoystickActive => false;    // joystick görünsün mü
    public virtual bool CanUseJoystick => false;    // şu an bırakınca çalışır mı
    public virtual bool JoystickAnyDirection => false; // false: sadece ileri yarım sayılır
    /// Client: kullan butonu sadece bu bilgisayarda bir şey yapacaksa (ör. joystick'i açmak) true döner,
    /// o zaman host'a gönderilmez.
    public virtual bool HandleLocalAction(int action) => false;
    /// Client: joystick ileri yönde bırakıldı (localDir: oyuncunun ekranına göre yön).
    public virtual void OnJoystickReleased(Vector2 localDir) { }

    // ---- Pasif yeteneklerin durum yazısı (ör. İkinci Şans bekleme süresi) ----
    /// true ise dükkan arayüzü bu yeteneğe bir durum yazısı ayırır.
    public virtual bool HasPassiveStatus => false;
    /// Durum yazısı şu an görünsün mü (ör. seviye alındıysa).
    public virtual bool ShowsPassiveStatus => false;
    /// Durum yazısında görünecek metin (yeteneğin adından sonra).
    public virtual string PassiveStatus => "";

    /// Bu yetenek oyuncunun destesinde mi? Destede olmayan yetenek satın alınamaz.
    public bool IsInDeck => Keeper != null && Keeper.HasInDeck(Id);

    /// Destenin 'deckIndex'. sırasındaki yeteneğin 'action' butonu için girdi biti.
    /// (Yetenek sayısı ne olursa olsun 6 × 2 = 12 bit yeter.)
    public static int InputButtonFor(int deckIndex, int action) => (int)InputButton.Ability0 + deckIndex * MaxActions + action;

    // ---- Bekleme süresi kısaltma (Ödül Avcısı) ----
    // Adı "Cooldown" ile başlayan tüm [Networked] TickTimer'lar bekleme süresi sayılır (Cooldown, CooldownLeft...).
    static readonly Dictionary<Type, PropertyInfo[]> _cooldownProps = new Dictionary<Type, PropertyInfo[]>();

    PropertyInfo[] CooldownProps
    {
        get
        {
            Type t = GetType();
            if (_cooldownProps.TryGetValue(t, out PropertyInfo[] props)) return props;

            var list = new List<PropertyInfo>();
            foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (p.PropertyType == typeof(TickTimer) && p.Name.StartsWith("Cooldown") && p.CanRead && p.CanWrite)
                    list.Add(p);
            props = list.ToArray();
            _cooldownProps[t] = props;
            return props;
        }
    }

    /// Host: bu yeteneğin bekleme sürelerini 'seconds' kadar kısaltır (bekleme yoksa bir şey olmaz).
    public void ServerReduceCooldowns(float seconds)
    {
        if (!HasStateAuthority || Runner == null || seconds <= 0f) return;

        foreach (PropertyInfo p in CooldownProps)
        {
            TickTimer timer = (TickTimer)p.GetValue(this);
            float left = timer.RemainingTime(Runner) ?? 0f;
            if (left <= 0f) continue;

            float newLeft = left - seconds;
            p.SetValue(this, newLeft <= 0.01f ? TickTimer.None : TickTimer.CreateFromSeconds(Runner, newLeft));
        }
    }

    /// Host: bir sonraki seviyeyi satın almayı dener.
    public bool ServerTryBuy()
    {
        if (!HasStateAuthority || IsMaxed || !IsInDeck) return false;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing) return false;
        if (!mm.ServerTrySpend(Keeper.Slot, PriceOf(Level + 1))) return false;

        Level++;
        return true;
    }
}
