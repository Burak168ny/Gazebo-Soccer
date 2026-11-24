using Fusion;
using UnityEngine;

/// <summary>
/// TUZAKLI HEDİYE (butonla, rakibi sen seçersin)
/// Butona basınca rakip listesi çıkar, birine hediye kutusu yollarsın. Rakibin ekranında
/// "Hediye geldi! AÇ / AÇMA" kutusu çıkar (10 sn içinde karar vermezse hediye kaybolur).
/// Açarsa yarı yarıya şans: ya altın kazanır ya da donar:
/// Seviye 1: 150 altın ya da 1 sn donma. Bekleme 90 sn.
/// Seviye 2: 300 altın ya da 2 sn donma. Bekleme 60 sn.
/// Seviye 3: 400 altın ya da 3 sn donma. Bekleme 30 sn.
/// Sonuç hem açana hem yollayana kısa bir yazıyla gösterilir.
/// </summary>
public class GiftSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede hediyeden çıkan altın")]
    [SerializeField] int[] goldAmounts = { 150, 300, 400 };
    [Tooltip("1., 2. ve 3. seviyede tuzaktan çıkan donma süresi (saniye)")]
    [SerializeField] float[] stunSeconds = { 1f, 2f, 3f };
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 90f, 60f, 30f };
    [Tooltip("Tuzak çıkma ihtimali (0-1)")]
    [SerializeField, Range(0f, 1f)] float trapChance = 0.5f;
    [Tooltip("Rakip bu kadar saniye içinde açmazsa hediye kaybolur")]
    [SerializeField] float openWindowSeconds = 10f;

    [Networked] public TickTimer Cooldown { get; set; }
    [Networked] public int TargetSlot { get; set; }      // hediyenin gittiği rakip (-1 = yok)
    [Networked] public TickTimer GiftTimer { get; set; }
    [Networked] public int GiftLevel { get; set; }       // yollandığı andaki seviye

    bool _picking;                 // yollayan: rakip listesi açık
    string _toast; float _toastUntil; // sonuç yazısı (sadece bu bilgisayarda)

    public override SkillId Id => SkillId.Gift;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public override bool CanActivateNow => !GiftPending && HasTarget();

    public bool GiftPending => TargetSlot >= 0 && Runner != null && !GiftTimer.ExpiredOrNotRunning(Runner);
    int Idx(int level) => Mathf.Clamp(level - 1, 0, 2);

    public override void Spawned()
    {
        if (HasStateAuthority) TargetSlot = -1;
    }

    public override bool HandleLocalAction(int action)
    {
        if (!HasInputAuthority) return false;
        _picking = !_picking;
        return true;
    }

    bool IsValidTarget(int slot)
    {
        MatchManager mm = MatchManager.Instance;
        return mm != null && Keeper != null && slot != Keeper.Slot && mm.IsAlive(slot);
    }

    bool HasTarget()
    {
        for (int s = 0; s < HexArena.SlotCount; s++)
            if (IsValidTarget(s)) return true;
        return false;
    }

    // ---------------- Ağ ----------------

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_Send(int targetSlot)
    {
        if (Level < 1 || GiftPending || !Cooldown.ExpiredOrNotRunning(Runner) || Keeper.IsSilenced) return;
        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing || !IsValidTarget(targetSlot)) return;

        TargetSlot = targetSlot;
        GiftLevel = Level;
        GiftTimer = TickTimer.CreateFromSeconds(Runner, openWindowSeconds);
        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(Idx(Level), cooldowns.Length - 1)]);
    }

    /// Hediyeyi alan oyuncu çağırır (AÇ ya da AÇMA). Sadece gerçekten alan kişiden kabul edilir.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_Answer(NetworkBool open, RpcInfo info = default)
    {
        if (!GiftPending) return;

        KeeperController target = KeeperController.FindBySlot(TargetSlot);
        PlayerRef source = info.Source == PlayerRef.None ? Runner.LocalPlayer : info.Source;
        if (target == null || target.Object.InputAuthority != source) return;

        int slot = TargetSlot;
        int level = GiftLevel;
        TargetSlot = -1;
        GiftTimer = TickTimer.None;
        if (!open) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing) return;

        int i = Idx(level);
        bool trap = Random.value < trapChance;
        if (trap)
        {
            float seconds = stunSeconds[Mathf.Min(i, stunSeconds.Length - 1)];
            target.ServerStun(seconds);
            RPC_Result(slot, false, Mathf.RoundToInt(seconds * 10f));
        }
        else
        {
            int gold = goldAmounts[Mathf.Min(i, goldAmounts.Length - 1)];
            mm.ServerAddGold(slot, gold);
            RPC_Result(slot, true, gold);
        }
    }

    /// Sonucu herkese bildirir; açan ve yollayan ekranında yazı görür.
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_Result(int targetSlot, NetworkBool gold, int value)
    {
        int me = NetworkBootstrap.LocalSlot;
        string what = gold ? value + " altın" : (value / 10f).ToString("0.#") + " sn donma";
        if (me == targetSlot) Toast(gold ? "Hediyeden " + what + " çıktı!" : "Tuzak! " + what);
        else if (Keeper != null && me == Keeper.Slot) Toast(HexArena.SlotNames[targetSlot] + " hediyeyi açtı: " + what);
    }

    void Toast(string text)
    {
        _toast = text;
        _toastUntil = Time.time + 2.5f;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && TargetSlot >= 0 && !GiftPending) TargetSlot = -1; // süre doldu
    }

    // ---------------- Ekran ----------------

    GUIStyle _big, _btn;

    void OnGUI()
    {
        if (Object == null || !Object.IsValid) return;
        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing) { _picking = false; return; }

        // Yollayan: rakip listesi
        if (_picking && HasInputAuthority)
        {
            if (!CanActivateNow || CooldownRemaining > 0f) _picking = false;
            else
            {
                int picked = SlotPicker.Draw("Hediye: kime?", IsValidTarget);
                if (picked == SlotPicker.Cancel) _picking = false;
                else if (picked >= 0) { _picking = false; RPC_Send(picked); }
            }
        }

        // Alan: hediye kutusu (bu bilgisayardaki oyuncuya geldiyse)
        bool forMe = GiftPending && TargetSlot == NetworkBootstrap.LocalSlot;
        bool toast = _toast != null && Time.time < _toastUntil;
        if (!forMe && !toast) return;

        const float refHeight = 1080f;
        float scale = Screen.height / refHeight;
        Matrix4x4 oldMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float w = Screen.width / scale;

        if (_big == null)
        {
            _big = new GUIStyle(GUI.skin.label) { fontSize = 46, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 42 };
        }

        if (forMe)
        {
            float bw = 560f, x = (w - bw) * 0.5f, y = refHeight * 0.3f;
            int left = Mathf.CeilToInt(GiftTimer.RemainingTime(Runner) ?? 0f);
            GUI.Box(new Rect(x - 20f, y - 20f, bw + 40f, 260f), GUIContent.none);
            GUI.Label(new Rect(x, y, bw, 120f), HexArena.SlotNames[Keeper.Slot] + " sana hediye yolladı! (" + left + ")", _big);

            Color old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.35f);
            if (GUI.Button(new Rect(x, y + 130f, bw * 0.48f, 90f), "AÇ", _btn)) RPC_Answer(true);
            GUI.backgroundColor = new Color(0.85f, 0.3f, 0.3f);
            if (GUI.Button(new Rect(x + bw * 0.52f, y + 130f, bw * 0.48f, 90f), "AÇMA", _btn)) RPC_Answer(false);
            GUI.backgroundColor = old;
        }
        else if (toast)
        {
            GUI.Label(new Rect(0f, refHeight * 0.2f, w, 120f), _toast, _big);
        }

        GUI.matrix = oldMatrix;
    }
}
