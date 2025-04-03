using UnityEngine;
using Photon.Pun;

public class HealthBarNetworkSync : MonoBehaviourPun
{
    private HealthBar healthBar;
    bool isReady = false;

    void Awake()
    {
        healthBar = GetComponent<HealthBar>();
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine || isReady) return;

        if (healthBar != null && healthBar.target != null && healthBar.target.character != null)
        {
            isReady = true; // 연결된 이후부터만 실행
        }
        else
        {
            return;
        }
        if (photonView.IsMine )
        {
            float cur = healthBar.target.character.CurHP;
            float max = healthBar.target.character.HP;
            photonView.RPC("UpdateHPBar", RpcTarget.Others, cur, max);
        }
    }

    [PunRPC]
    public void UpdateHPBar(float cur, float max)
    {
        if (healthBar != null)
        {
            healthBar.UpdateFromNetwork(cur, max);
        }
    }
}