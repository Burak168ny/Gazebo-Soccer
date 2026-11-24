using UnityEngine;
using Fusion;

public class CharacterReferenceFetcher : NetworkBehaviour
{
    private GameObject targetObject;

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcFetchTargetObject(int characterIndex)
    {
        // Sahnedeki ilgili obje referansını al
        string objectName = characterIndex.ToString();
        targetObject = GameObject.Find(objectName);

        if (targetObject != null)
        {
            Debug.Log($"Target object found: {targetObject.name}");
        }
        else
        {
            Debug.LogWarning($"Target object not found: {objectName}");
        }
    }

    public void FetchAndSync(int characterIndex)
    {
        if (Object.HasStateAuthority) // Eğer bu cihaz sunucuysa
        {
            RpcFetchTargetObject(characterIndex); // İstemcilerle senkronize etmek için RPC çağrısı
        }
    }
}
