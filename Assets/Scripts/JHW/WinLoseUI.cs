using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinLoseUI : MonoBehaviour
{
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;

    PhotonView pv;
    string team;

    public void Init(PhotonView photon, string teamTag)
    {
        pv = photon;
        team = teamTag;

        if(PhotonNetwork.IsMasterClient)
        {
            GameManager.Instance.wlUi = this;
        }
    }

    public void AnnounceResult(string winTeam)
    {
        pv.RPC("ShowResultScreen", RpcTarget.AllBuffered, team == winTeam);
    }

    [PunRPC]
    public void ShowResultScreen(bool win)
    {
        if(win)
        {
            winPanel.SetActive(true);
        }
        else
        {
            losePanel.SetActive(false);
        }
    }
}
