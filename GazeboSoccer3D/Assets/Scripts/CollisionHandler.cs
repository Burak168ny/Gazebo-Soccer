using UnityEngine;
using Fusion;

public class CollisionHandler : NetworkBehaviour
{
    private GoalDetector parentGoalDetector;

    public void Initialize(GoalDetector goalDetector)
    {
        parentGoalDetector = goalDetector;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            if (Object.HasStateAuthority)
            {
                RpcReduceScoreForAll(1);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcReduceScoreForAll(int scoreReduction)
    {
        if (parentGoalDetector == null)
        {
            return;
        }

        parentGoalDetector.ReduceScore(scoreReduction);
    }
}
