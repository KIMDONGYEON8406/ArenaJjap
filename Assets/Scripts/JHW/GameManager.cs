using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance
    {
        get; private set;
    }

    public WinLoseUI wlUi;
    public InGameManager inGameManager;

    Dictionary<int, bool> blueAlive = new Dictionary<int, bool>(); //actorNum, 생존여부
    Dictionary<int, bool> redAlive = new Dictionary<int, bool>();

    Dictionary<int, PlayerController> blueChamps = new Dictionary<int, PlayerController>(); //viewid, 플레이어컨트롤러
    Dictionary<int, PlayerController> redChamps = new Dictionary<int, PlayerController>();

    public int round = 0;

    int blueWin = 0;
    int redWin = 0;

    private void Awake()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != null)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddBlueTeam(int pn)
    {
        blueAlive.Add(pn, false);
        Debug.Log(blueAlive.Count);

    }

    public void AddRedTeam(int pn)
    {
        redAlive.Add(pn, false);
        Debug.Log(redAlive.Count);

    }

    public void AddBlueChamp(int viewID, PlayerController playerController)
    {
        blueChamps.Add(viewID, playerController);
    }

    public void AddRedChamp(int viewID, PlayerController playerController)
    {
        redChamps.Add(viewID, playerController);
    }

    public void PlayerDeath(int pn)
    {
        
        if(blueAlive.ContainsKey(pn))
        {            
            blueAlive[pn] = false;
        }
        else if(redAlive.ContainsKey(pn))
        {
            redAlive[pn] = false;
        }

        CheckWin();
    }

    public void ResetRound()
    {
        Debug.Log("ResetRound");
        foreach(var blue in blueChamps)
        {
            Debug.Log(blue.Value + "레드 초기화");
            blue.Value.character.ResetState();
            blue.Value.transform.position = inGameManager.spawnPoints[blue.Value.pv.ControllerActorNr].position;
        }

        foreach (var red in redChamps)
        {
            Debug.Log(red.Value + "레드 초기화");
            red.Value.character.ResetState();
            red.Value.transform.position = inGameManager.spawnPoints[red.Value.pv.ControllerActorNr].position;
        }
    }

    public void CheckWin()
    {
        int teamAliveCount = blueAlive.Count;
        foreach(var blue in blueAlive)
        {
            if(blue.Value == false)
            {
                teamAliveCount--;
            }
        }

        if(teamAliveCount <= 0)
        {
            RedWin();
            return;
        }

        teamAliveCount = redAlive.Count;

        foreach (var red in redAlive)
        {
            if (red.Value == false)
            {
                teamAliveCount--;
            }
        }

        if (teamAliveCount <= 0)
        {
            BlueWin();
            return;
        }
    }

    public void BlueWin()
    {
        blueWin++;
        round++;
        ResetRound();
    }

    public void RedWin()
    {
        redWin++;
        round++;
        ResetRound();
    }
    
    public void CheckVictory()
    {
        if(blueWin >= 3)
        {
            BlueVictory();
            return;
        }

        if(redWin >= 3)
        {
            RedVictory();
        }
    }

    public void BlueVictory()
    {
        wlUi.AnnounceResult("Blue");
    }

    public void RedVictory()
    {
        wlUi.AnnounceResult("Red");
    }
}
