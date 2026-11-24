using System.Collections.Generic;
using Fusion;
using UnityEngine;

/// <summary>
/// Donduran disk. Topla aynı şekilde hareket eder ve duvarlardan/kalelerden seker (gol saymaz).
/// Değdiği rakip keeper'ı dondurur, içinden geçer. Sekme hakkı bitince yok olur. Kararları host verir.
/// Prefab: NetworkObject + NetworkTransform + bu script. Collider GEREKMEZ.
/// </summary>
public class Disc : NetworkBehaviour
{
    public static readonly List<Disc> All = new List<Disc>();

    [SerializeField] float radius = 0.2f;

    const float Skin = 0.01f;
    const int MaxBouncesPerTick = 3;

    [Networked] public Vector2 Velocity { get; set; }
    [Networked] public int OwnerSlot { get; set; }
    [Networked] public int BouncesLeft { get; set; }
    [Networked] public float StunSeconds { get; set; }
    [Networked] public int HitMask { get; set; } // dondurulan keeper'lar (her biri bir kez)

    public void Init(int ownerSlot, Vector2 velocity, int bounces, float stunSeconds)
    {
        OwnerSlot = ownerSlot;
        Velocity = velocity;
        BouncesLeft = bounces;
        StunSeconds = stunSeconds;
        HitMask = 0;
    }

    /// Host (Shinra Tensei): disk geldiği yöne, daha hızlı geri döner ve artık iten oyuncunun diski olur.
    public void ServerPushBack(int newOwnerSlot, float speedMultiplier)
    {
        if (!HasStateAuthority) return;
        Velocity = -Velocity * speedMultiplier;
        OwnerSlot = newOwnerSlot;
        HitMask = 0; // herkes yeniden donabilir (yeni sahibi hariç)
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
    }

    public override void Spawned()
    {
        All.Add(this);
        transform.rotation = Quaternion.identity;
        Color c = OwnerSlot >= 0 && OwnerSlot < HexArena.SlotCount ? HexArena.SlotColors[OwnerSlot] : Color.cyan;
        AddCircle("Ring", radius * 2f + 0.1f, Color.white, 14);
        AddCircle("Body", radius * 2f, c, 15);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        MatchManager mm = MatchManager.Instance;
        ArenaLayout arena = ArenaLayout.Instance;
        if (mm == null || arena == null || mm.Phase != MatchPhase.Playing)
        {
            Runner.Despawn(Object);
            return;
        }

        Vector2 pos = transform.position;
        Vector2 vel = Velocity;
        float remaining = vel.magnitude * Runner.DeltaTime;

        for (int i = 0; i < MaxBouncesPerTick && remaining > 0.0001f; i++)
        {
            Vector2 dir = vel.normalized;
            RaycastHit2D hit = Physics2D.CircleCast(pos, radius, dir, remaining + Skin, arena.BallCastMask);
            bool blocked = hit && hit.fraction > 0f;

            float travel = blocked ? Mathf.Max(0f, hit.distance - Skin) : remaining;
            Vector2 next = pos + dir * travel;

            StunKeepersOnPath(pos, next);

            pos = next;
            remaining -= travel;
            if (!blocked) break;

            if (BouncesLeft <= 0) // sekme hakkı bitti
            {
                Runner.Despawn(Object);
                return;
            }
            BouncesLeft--;
            vel = Vector2.Reflect(vel, hit.normal);
        }

        Velocity = vel;
        transform.position = pos;
    }

    void StunKeepersOnPath(Vector2 from, Vector2 to)
    {
        foreach (KeeperController k in KeeperController.All)
        {
            if (k.Slot == OwnerSlot) continue;
            int bit = 1 << k.Slot;
            if ((HitMask & bit) != 0) continue;

            if (Ball.DistancePointSegment(k.WorldPosition, from, to) <= radius + k.Radius)
            {
                k.ServerStun(StunSeconds);
                HitMask |= bit;
            }
        }
    }

    void AddCircle(string name, float diameter, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(diameter, diameter, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteUtil.Circle;
        sr.color = color;
        sr.sortingOrder = order;
    }
}
