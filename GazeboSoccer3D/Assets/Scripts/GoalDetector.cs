using UnityEngine;
using Fusion;
using System.Linq; // Sıralama için
using System.Collections.Generic; // Liste ve diğer koleksiyonlar için


public class GoalDetector : NetworkBehaviour
{
    [Networked] public NetworkObject DetectedGoal { get; private set; }
    [Networked] public int Score { get; private set; } = 3;
    [Networked] private bool IsKinematic { get; set; } // Kinematik durumu
    [Networked] private bool IsTrigger { get; set; } // Collider'ın isTrigger durumu
    public bool start = true;

    private List<NetworkObject> detectedObjects = new List<NetworkObject>(); // Bulunan objeleri tutan dizi


    private Rigidbody characterRigidbody;
    private Collider characterCollider;

    public override void Spawned()
    {
        characterRigidbody = GetComponent<Rigidbody>();
        characterCollider = GetComponent<Collider>();

        if (characterCollider == null)
        {
            Debug.LogError("GoalDetector: Collider component is missing!");
        }
    }

    private void Start()
    {
        if (characterRigidbody == null)
        {
            Debug.LogError("GoalDetector: Rigidbody component is missing!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal") && start)
        {
            if (HasStateAuthority)
            {
                DetectedGoal = other.GetComponent<NetworkObject>();
                RPC_SetDetectedGoal(DetectedGoal);
            }
            start = false;
        }
        if (other.CompareTag("kda") && HasStateAuthority )
        {
            NetworkObject networkObj = other.GetComponent<NetworkObject>();
            if (networkObj != null && !detectedObjects.Contains(networkObj))
            {
                detectedObjects.Add(networkObj);
                Debug.Log($"Listeye eklenen obje: {networkObj.name}");
            }
            else
            {
                Debug.LogWarning($"Obje NetworkObject içermiyor veya zaten listede: {other.name}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Goal") && HasStateAuthority)
        {
            if (DetectedGoal != null && DetectedGoal.Id == other.GetComponent<NetworkObject>().Id)
            {
                DetectedGoal = null;
                RPC_SetDetectedGoal(null);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetDetectedGoal(NetworkObject goal)
    {
        DetectedGoal = goal;

        if (goal != null)
        {
            CollisionHandler handler = goal.GetComponent<CollisionHandler>();
            if (handler == null)
            {
                handler = goal.gameObject.AddComponent<CollisionHandler>();
            }
            handler.Initialize(this);
        }
    }

    public void ReduceScore(int amount)
    {
        if (!HasStateAuthority) return;

        Score -= amount;

        if (Score <= 0)
        {
            Score = 0;
            SetCharacterKinematic(true);
            SetColliderTrigger(true); // Skor sıfırlandığında collider'ı trigger yap
        }
        DeactivateNextObject();
        RPC_UpdateScore(Score);
    }

private void DeactivateNextObject()
{
    if (!HasStateAuthority) return; // Sadece sunucu işlemi yapar

    if (detectedObjects.Count > 0)
    {
        var objToDeactivate = detectedObjects.First(); // İlk objeyi al
        if (objToDeactivate != null)
        {
            Debug.Log($"Sunucuda devre dışı bırakılıyor: {objToDeactivate.name}");
            RPC_DeactivateObject(objToDeactivate);
        }

        detectedObjects.RemoveAt(0); // Listeden kaldır
    }
}

[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_DeactivateObject(NetworkObject obj)
{
    if (obj != null)
    {
        obj.gameObject.SetActive(false);
        Debug.Log($"SetActive(false) uygulandı: {obj.name} - Aktif mi? {obj.gameObject.activeSelf}");
    }
}




    private void SetCharacterKinematic(bool isKinematic)
    {
        if (!HasStateAuthority) return;

        if (characterRigidbody != null)
        {
            characterRigidbody.isKinematic = isKinematic;
        }

        IsKinematic = isKinematic;
    }

    private void SetColliderTrigger(bool isTrigger)
    {
        if (!HasStateAuthority) return;

        if (characterCollider != null)
        {
            characterCollider.isTrigger = isTrigger;
        }

        IsTrigger = isTrigger; // Ağ üzerinde durumu senkronize et
    }

    public override void FixedUpdateNetwork()
    {
        // İstemciler için kinematik durumu güncelle
        if (characterRigidbody != null)
        {
            characterRigidbody.isKinematic = IsKinematic;
        }

        // İstemciler için collider trigger durumunu güncelle
        if (characterCollider != null)
        {
            characterCollider.isTrigger = IsTrigger;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateScore(int newScore)
    {
        Score = newScore;
    }
}
