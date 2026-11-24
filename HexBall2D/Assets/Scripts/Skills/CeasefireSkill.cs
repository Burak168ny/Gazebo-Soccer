using Fusion;
using UnityEngine;

/// <summary>
/// ATEŞKES (butonla, rakibi sen seçersin)
/// Butona basınca ekranda hayattaki rakiplerin listesi çıkar, birine dokunursun. Ateşkes hemen başlar
/// (karşı tarafın kabul etmesi gerekmez) ve iki keeper arasında beyaz bir bağ görünür.
/// Ateşkes sırasında ikiniz birbirinize gol atamazsınız: atan ateşkesi BOZAR.
/// Bozan, gol yemiş sayılır ve şu kadar can kaybeder (golü yiyen can kaybetmez), ateşkes biter:
/// Seviye 1: 5 can, bekleme 120 sn.
/// Seviye 2: 10 can, bekleme 90 sn.
/// Seviye 3: 20 can, bekleme 60 sn.
/// Ateşkes süre dolunca ya da biri elenince de biter.
/// </summary>
public class CeasefireSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede ateşkesi bozanın kaybedeceği can")]
    [SerializeField] int[] breakPenalties = { 5, 10, 20 };
    [Tooltip("1., 2. ve 3. seviyede bekleme süresi (saniye)")]
    [SerializeField] float[] cooldowns = { 120f, 90f, 60f };
    [Tooltip("Ateşkes kaç saniye sürer (bozulmazsa)")]
    [SerializeField] float durationSeconds = 60f;

    [Networked] public TickTimer Cooldown { get; set; }
    [Networked] public TickTimer ActiveTimer { get; set; }
    [Networked] public int PartnerSlot { get; set; } // ateşkes yapılan rakip (-1 = yok)
    [Networked] public int Penalty { get; set; }     // ilan edildiği andaki ceza

    bool _picking; // sadece bu bilgisayarda: rakip seçme listesi açık

    public override SkillId Id => SkillId.Ceasefire;
    public override bool HasActiveAbility => Level >= 1;
    public override int ActionButtonCount => 1;
    public override float CooldownRemaining => Runner != null ? Cooldown.RemainingTime(Runner) ?? 0f : 0f;
    public override bool CanActivateNow => !IsActive && HasOpponent();

    public bool IsActive => PartnerSlot >= 0 && Runner != null && !ActiveTimer.ExpiredOrNotRunning(Runner);
    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);

    public override void Spawned()
    {
        if (HasStateAuthority) PartnerSlot = -1;
        CreateLink();
    }

    // ---------------- Kullanma ----------------

    public override bool HandleLocalAction(int action)
    {
        if (!HasInputAuthority) return false;
        _picking = !_picking; // listeyi aç / kapat
        return true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_Declare(int targetSlot)
    {
        if (Level < 1 || IsActive || !Cooldown.ExpiredOrNotRunning(Runner) || Keeper.IsSilenced) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing) return;
        if (targetSlot == Keeper.Slot || !mm.IsAlive(targetSlot)) return;

        PartnerSlot = targetSlot;
        Penalty = breakPenalties[Mathf.Min(LevelIndex, breakPenalties.Length - 1)];
        ActiveTimer = TickTimer.CreateFromSeconds(Runner, durationSeconds);
        Cooldown = TickTimer.CreateFromSeconds(Runner, cooldowns[Mathf.Min(LevelIndex, cooldowns.Length - 1)]);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || PartnerSlot < 0) return;

        MatchManager mm = MatchManager.Instance;
        bool ended = !IsActive || mm == null || !mm.IsAlive(PartnerSlot) || !mm.IsAlive(Keeper.Slot);
        if (ended) PartnerSlot = -1;
    }

    /// Host (MatchManager.ServerGoal başında çağırır): bu gol bir ateşkesi bozuyor mu?
    /// Bozuyorsa bozan cezayı yer, ateşkes biter ve true döner (gol normal sayılmaz).
    public static bool ServerTryBreak(int goalSlot, int scorerSlot)
    {
        MatchManager mm = MatchManager.Instance;
        if (mm == null || scorerSlot < 0 || scorerSlot == goalSlot) return false;

        foreach (KeeperController k in KeeperController.All)
        {
            CeasefireSkill c = k.GetSkill<CeasefireSkill>();
            if (c == null || !c.IsActive) continue;

            int a = k.Slot, b = c.PartnerSlot;
            bool pair = (scorerSlot == a && goalSlot == b) || (scorerSlot == b && goalSlot == a);
            if (!pair) continue;

            int penalty = c.Penalty;
            c.PartnerSlot = -1;                    // ateşkes biter
            mm.ServerDamage(scorerSlot, penalty);  // bozan gol yemiş sayılır
            return true;
        }
        return false;
    }

    bool HasOpponent()
    {
        MatchManager mm = MatchManager.Instance;
        if (mm == null || Keeper == null) return false;
        for (int s = 0; s < HexArena.SlotCount; s++)
            if (s != Keeper.Slot && mm.IsAlive(s)) return true;
        return false;
    }

    // ---------------- Rakip seçme listesi (sadece bu bilgisayarda) ----------------

    void OnGUI()
    {
        if (!_picking || Object == null || !Object.IsValid || !HasInputAuthority) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing || !CanActivateNow || CooldownRemaining > 0f)
        {
            _picking = false;
            return;
        }

        int me = Keeper.Slot;
        int picked = SlotPicker.Draw("Ateşkes: kiminle?", s => s != me && mm.IsAlive(s));
        if (picked == SlotPicker.Cancel) _picking = false;
        else if (picked >= 0) { _picking = false; RPC_Declare(picked); }
    }

    // ---------------- Görüntü: iki keeper arasında beyaz bağ ----------------

    SpriteRenderer _link;

    void CreateLink()
    {
        var go = new GameObject("CeasefireLink");
        _link = go.AddComponent<SpriteRenderer>();
        _link.sprite = SpriteUtil.Square;
        _link.color = new Color(1f, 1f, 1f, 0.45f);
        _link.sortingOrder = 4;
        go.SetActive(false);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_link != null) Destroy(_link.gameObject);
    }

    public override void Render()
    {
        if (_link == null) return;

        KeeperController partner = IsActive ? KeeperController.FindBySlot(PartnerSlot) : null;
        bool show = partner != null;
        if (_link.gameObject.activeSelf != show) _link.gameObject.SetActive(show);
        if (!show) return;

        Vector2 a = Keeper.transform.position, b = partner.transform.position;
        Vector2 d = b - a;
        _link.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg));
        _link.transform.localScale = new Vector3(d.magnitude, 0.08f, 1f);
    }
}
