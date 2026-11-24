using Fusion;
using UnityEngine;

public class AbilityController : NetworkBehaviour
{
    [SerializeField] private GameObject abilityObject1; // İlk obje
    [SerializeField] private GameObject abilityObject2; // İkinci obje

    private bool isAbilityActive = false;

public override void FixedUpdateNetwork()
{
    if (GetInput(out NetworkInputData data))
    {
        if (data.buttons.IsSet(NetworkInputData.TOGGLE_ABILITY))
        {
            if (Object.HasStateAuthority)
            {
                isAbilityActive = !isAbilityActive;
                RpcToggleAbility(isAbilityActive);
            }
        }
    }
}


[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RpcToggleAbility(bool isActive)
{
    ApplyToggle(isActive);
}

private void ApplyToggle(bool isActive)
{
    isAbilityActive = isActive;
    abilityObject1.SetActive(isAbilityActive);
    abilityObject2.SetActive(!isAbilityActive);
}

}

