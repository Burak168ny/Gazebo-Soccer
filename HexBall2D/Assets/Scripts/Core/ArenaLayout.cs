using TMPro;
using UnityEngine;

/// <summary>
/// Sahnedeki arenanın tanımı. Sahnede tek bir tane olur (ağ objesi değil, herkeste aynı).
/// Spawn noktalarından her oyuncu için bir "yerel koordinat" çıkarır:
///   x = kale boyunca (oyuncunun ekranında sağ +), y = merkeze doğru (ekranında yukarı +).
/// Spawn noktası yerel (0,0)'dır. Kafes sınırları bu koordinatlarda tanımlanır.
/// </summary>
public class ArenaLayout : MonoBehaviour
{
    public static ArenaLayout Instance { get; private set; }

    [Tooltip("Sırayla: 0 alt, 1 sol-alt, 2 sol-üst, 3 üst, 4 sağ-üst, 5 sağ-alt")]
    [SerializeField] Transform[] spawnPoints = new Transform[HexArena.SlotCount];

    [Header("Kafes (keeper merkezinin gidebileceği alan, spawn noktasına göre)")]
    [SerializeField] float cageHalfWidth = 2.5f;
    [SerializeField] float cageBack = 0.3f;
    [SerializeField] float cageForward = 1.8f;

    [Header("Duvarlar")]
    [Tooltip("Keeper'ın ve topun çarpacağı collider'ların layer'ı")]
    [SerializeField] LayerMask wallMask = 1; // varsayılan: Default layer

    [Header("Kaleler ve skorlar (sıra spawn noktalarıyla AYNI)")]
    [Tooltip("Top bu collider'a çarpınca o oyuncu 1 can kaybeder. Top ondan da seker.")]
    [SerializeField] Collider2D[] goals = new Collider2D[HexArena.SlotCount];
    [SerializeField] TMP_Text[] scoreTexts = new TMP_Text[HexArena.SlotCount];

    public LayerMask WallMask => wallMask;
    /// Arenanın kabaca yarıçapı (merkezden en uzak kaleden biraz fazlası). Dışına çıkan top geri alınır.
    public float BoundsRadius { get; private set; }
    public float CageHalfWidth => cageHalfWidth;
    public float CageBack => cageBack;
    public float CageForward => cageForward;
    /// Topun çarpabileceği her şey: duvarlar + kaleler.
    public int BallCastMask { get; private set; }
    public Vector2 Center { get; private set; }

    void Awake()
    {
        Instance = this;
        Validate();
        Center = ComputeCenter();
        BoundsRadius = 0f;
        foreach (Transform t in spawnPoints)
            if (t != null) BoundsRadius = Mathf.Max(BoundsRadius, Vector2.Distance(Center, t.position));
        BoundsRadius += 3f; // kale ve duvarlar için pay
        foreach (Collider2D g in goals) // kalelerin en uzak ucu da içeride kalsın
            if (g != null) BoundsRadius = Mathf.Max(BoundsRadius, Vector2.Distance(Center, g.bounds.center) + g.bounds.extents.magnitude + 1f);

        BallCastMask = wallMask;
        foreach (Collider2D g in goals)
            if (g != null) BallCastMask |= 1 << g.gameObject.layer;
    }

    /// Bu collider kimin kalesi? Değilse -1.
    public int GoalSlotOf(Collider2D c)
    {
        for (int i = 0; i < goals.Length; i++)
            if (goals[i] == c) return i;
        return -1;
    }

    public void SetScoreText(int slot, string text)
    {
        if (slot < scoreTexts.Length && scoreTexts[slot] != null) scoreTexts[slot].text = text;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ---------------- Geometri ----------------

    public Vector2 SpawnPos(int slot) => spawnPoints[slot].position;

    /// Spawn'dan arena merkezine doğru birim yön (oyuncunun ekranında "yukarı").
    public Vector2 Inward(int slot) => (Center - SpawnPos(slot)).normalized;

    /// Oyuncunun ekranında "sağ" yönü.
    public Vector2 Right(int slot)
    {
        Vector2 inward = Inward(slot);
        return new Vector2(inward.y, -inward.x);
    }

    /// Bu açıyla dönen kamerada oyuncunun kalesi ekranın altında görünür.
    public float ViewAngle(int slot)
    {
        Vector2 inward = Inward(slot);
        return Mathf.Atan2(inward.y, inward.x) * Mathf.Rad2Deg - 90f;
    }

    public Vector2 LocalToWorld(int slot, Vector2 local)
    {
        return SpawnPos(slot) + Right(slot) * local.x + Inward(slot) * local.y;
    }

    public Vector2 WorldToLocal(int slot, Vector2 world)
    {
        Vector2 d = world - SpawnPos(slot);
        return new Vector2(Vector2.Dot(d, Right(slot)), Vector2.Dot(d, Inward(slot)));
    }

    public Vector2 ClampToCage(Vector2 local)
    {
        local.x = Mathf.Clamp(local.x, -cageHalfWidth, cageHalfWidth);
        local.y = Mathf.Clamp(local.y, -cageBack, cageForward);
        return local;
    }

    // ---------------- Yardımcılar ----------------

    Vector2 ComputeCenter()
    {
        Vector2 sum = Vector2.zero;
        int count = 0;
        foreach (Transform t in spawnPoints)
        {
            if (t == null) continue;
            sum += (Vector2)t.position;
            count++;
        }
        return count > 0 ? sum / count : Vector2.zero;
    }

    bool Validate()
    {
        if (spawnPoints == null || spawnPoints.Length != HexArena.SlotCount)
        {
            Debug.LogError("ArenaLayout: 6 spawn noktası gerekli.", this);
            return false;
        }
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                Debug.LogError($"ArenaLayout: Spawn Points [{i}] boş.", this);
                return false;
            }
        }
        return true;
    }

    // Sahne görünümünde kafesleri ve numaraları çizer (oyunda görünmez).
    void OnDrawGizmos()
    {
        if (spawnPoints == null || spawnPoints.Length != HexArena.SlotCount) return;
        foreach (Transform t in spawnPoints) if (t == null) return;

        Center = ComputeCenter();

        for (int s = 0; s < HexArena.SlotCount; s++)
        {
            Gizmos.color = HexArena.SlotColors[s];
            Vector2 a = LocalToWorld(s, new Vector2(-cageHalfWidth, -cageBack));
            Vector2 b = LocalToWorld(s, new Vector2(cageHalfWidth, -cageBack));
            Vector2 c = LocalToWorld(s, new Vector2(cageHalfWidth, cageForward));
            Vector2 d = LocalToWorld(s, new Vector2(-cageHalfWidth, cageForward));
            Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
            Gizmos.DrawWireSphere(SpawnPos(s), 0.2f);
            Gizmos.DrawLine(SpawnPos(s), SpawnPos(s) + Inward(s) * 1f); // "yukarı" yönü
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(SpawnPos(s) - Inward(s) * 0.6f, s.ToString());
#endif
        }
    }
}
