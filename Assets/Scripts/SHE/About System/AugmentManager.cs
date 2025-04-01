using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AugmentManager : MonoBehaviour
{
    [SerializeField] Character localPlayer;
    [SerializeField] ScriptableStatPlus statAugment;

    private void Start()
    {
        FindLocalPlayer();
    }

    public void FindLocalPlayer()
    {
        // 현재 로컬 플레이어의 PhotonView 찾기
        localPlayer = PhotonNetwork.LocalPlayer.TagObject as Character;
        if(localPlayer != null )
        {
           
            Debug.Log($"{localPlayer.name} 등록!");

        }
        else
        {
            Debug.LogWarning("로컬 플레이어를 찾을 수 없습니다!");
        }
    }

    public void ApplyDamage()
    {
        localPlayer.AdjustATK(statAugment.statPlusInt);
    }
    public void ApplyArmor()
    {
        localPlayer.AdjustDef(statAugment.statPlusInt);
    }
    public void ApplyATKSpeed()
    {
        localPlayer.AdjustAtkSpeed(statAugment.statPlusInt);
    }
    public void ApplyHP()
    {
        localPlayer.AdjustHP(statAugment.statPlusInt);
    }
    public void ApplyCritical()
    {
        localPlayer.AdjustCritChance(statAugment.statPlusInt);
    }
    public void ApplyLifeSteal()
    {
        localPlayer.AdjustLifeSteal(statAugment.statPlusInt);
    }
    public void ApplyMovementSpeed()
    {
        localPlayer.AdjustMoveSpeed((int)statAugment.statPlusInt * localPlayer.MoveSpeed);
    }
    public void ApplyCooldown()
    {
        localPlayer.AdjustAbilityHaste((int)statAugment.statPlusInt);
    }
    public void ApplyPrisomDamage()
    {
        localPlayer.AdjustATK(statAugment.statPlusInt);
    }
    public void ApplyPrisomArmor()
    {
        localPlayer.AdjustDef(statAugment.statPlusInt);
    }
   
    public void ApplyPrisomHP()
    {
        localPlayer.AdjustHP(statAugment.statPlusInt + localPlayer.HP * 0.4f);
    }
    public void ApplyPrisomCritical()
    {
        localPlayer.AdjustCritChance(statAugment.statPlusInt);
    }
    public void ApplyPrisomCritDamage()
    {
        localPlayer.AdjustCritDmg(statAugment.statPlusInt);
    }
    public void ApplyPrisomLifeSteal()
    {
        localPlayer.AdjustLifeSteal(statAugment.statPlusInt);
    }
    public void ApplyPrisomMovementSpeed()
    {
        localPlayer.AdjustMoveSpeed((int)statAugment.statPlusInt * localPlayer.MoveSpeed);
    }
    public void ApplyPrisomCooldown()
    {
        localPlayer.AdjustAbilityHaste((int)statAugment.statPlusInt);
    }

}

