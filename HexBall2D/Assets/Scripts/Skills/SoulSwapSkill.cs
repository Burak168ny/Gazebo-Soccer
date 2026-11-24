using Fusion;
using UnityEngine;

/// <summary>
/// RUH TAKASI (butonla, maç boyunca BİR KEZ)
/// Butona basınca ekranda canı seninkinden yeterince fazla olan rakipler listelenir, birine dokunursun:
/// ikinizin canı yer değiştirir.
/// Şart (rakibin canı en az senin canın + şu kadar olmalı):
/// Seviye 1: +20. Seviye 2: +10. Seviye 3: +5.
/// Uygun rakip yoksa buton kapalı kalır. Kullanınca buton kaybolur.
/// </summary>
public class SoulSwapSkill : SkillBase
{
    [Tooltip("1., 2. ve 3. seviyede rakibin canının seninkinden en az ne kadar fazla olması gerektiği")]
    [SerializeField] int[] lifeGaps = { 20, 10, 5 };

    [Networked] public NetworkBool Used { get; set; } // maç boyunca bir kez

    bool _picking; // sadece bu bilgisayarda: rakip seçme listesi açık

    public override SkillId Id => SkillId.SoulSwap;
    public override bool HasActiveAbility => Level >= 1 && !Used;
    public override int ActionButtonCount => 1;
    public override bool CanActivateNow => HasEligibleTarget();

    int LevelIndex => Mathf.Clamp(Level - 1, 0, 2);
    int Gap => lifeGaps[Mathf.Min(LevelIndex, lifeGaps.Length - 1)];

    public override bool HandleLocalAction(int action)
    {
        if (!HasInputAuthority) return false;
        _picking = !_picking;
        return true;
    }

    bool IsEligible(int slot)
    {
        MatchManager mm = MatchManager.Instance;
        if (mm == null || Keeper == null) return false;
        int me = Keeper.Slot;
        return slot != me && mm.IsAlive(slot) && mm.IsAlive(me) && mm.Lives[slot] >= mm.Lives[me] + Gap;
    }

    bool HasEligibleTarget()
    {
        for (int s = 0; s < HexArena.SlotCount; s++)
            if (IsEligible(s)) return true;
        return false;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_Swap(int targetSlot)
    {
        if (Level < 1 || Used || Keeper.IsSilenced) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing || !IsEligible(targetSlot)) return; // şart host'ta tekrar kontrol

        Used = true;
        mm.ServerSwapLives(Keeper.Slot, targetSlot);
    }

    void OnGUI()
    {
        if (!_picking || Object == null || !Object.IsValid || !HasInputAuthority) return;

        MatchManager mm = MatchManager.Instance;
        if (mm == null || mm.Phase != MatchPhase.Playing || Used || !HasEligibleTarget())
        {
            _picking = false;
            return;
        }

        int picked = SlotPicker.Draw("Ruh Takası: kiminle?", IsEligible);
        if (picked == SlotPicker.Cancel) _picking = false;
        else if (picked >= 0) { _picking = false; RPC_Swap(picked); }
    }
}
