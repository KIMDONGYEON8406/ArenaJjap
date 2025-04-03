using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Linq;

public class MatchManager : MonoBehaviourPunCallbacks
{
    public GameObject championPanel;
    public Button readyBtn;

    [SerializeField, Header("프로필 이미지")]
    public Image[] ProfileImgs;

    [SerializeField, Header("뒷 사진")]
    public GameObject[] BackgroundImgs;

    [SerializeField, Header("챔피언 버튼")]
    public Button[] championBtns;

    [Header("소환사들")]
    public Image[] summoners;

    [Header("닉네임 UI")]
    public TextMeshProUGUI[] nickNameTexts;


    private List<Player> playerList = new List<Player>();
    private List<Player> blueteam = new List<Player>();
    private List<Player> redteam = new List<Player>();

    private Dictionary<Player, int> playerChampion = new Dictionary<Player, int>();
    private int readyCount = 0;

    public static int myChampionIndex = -1; // <- 여기에 저장
     

    private void Awake()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignTeams();
        }
    }

    void AssignTeams()
    {
        playerList = new List<Player>(PhotonNetwork.PlayerList);

        blueteam.Add(playerList[0]);
        blueteam.Add(playerList[1]);
        redteam.Add(playerList[2]);
        redteam.Add(playerList[3]);

        photonView.RPC("SyncTeams", RpcTarget.All, playerList[0].ActorNumber, playerList[1].ActorNumber,
                                                 playerList[2].ActorNumber, playerList[3].ActorNumber);
    }

    [PunRPC]
    private void SyncTeams(int p1, int p2, int p3, int p4)
    {
        Player[] players = PhotonNetwork.PlayerList;

        blueteam.Clear();
        redteam.Clear();

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == p1 || p.ActorNumber == p2)
                blueteam.Add(p);
            else if (p.ActorNumber == p3 || p.ActorNumber == p4)
                redteam.Add(p);
        }

        // 모든 플레이어 닉네임을 정확한 위치에 설정
        foreach (Player p in players)
        {
            int playerIndex;
            if (blueteam.Contains(p))
            {
                playerIndex = blueteam.IndexOf(p); // 블루팀 내에서 위치 찾기
                if (PhotonNetwork.IsMasterClient && GameManager.Instance != null)
                {
                    GameManager.Instance.AddBlueTeam(p.ActorNumber);
                }
            } 
            else
            {
                playerIndex = redteam.IndexOf(p) + blueteam.Count; // 레드팀이면 블루팀 개수만큼 추가한 위치
                if (PhotonNetwork.IsMasterClient && GameManager.Instance != null)
                {
                    GameManager.Instance.AddRedTeam(p.ActorNumber);
                }
            }

            if (playerIndex >= 0 && playerIndex < nickNameTexts.Length)
            {
                nickNameTexts[playerIndex].text = p.NickName;
            }

        }
    }


    private void Start()
    {
        foreach (var bg in BackgroundImgs)
        {
            bg.SetActive(false);
        }

        for (int i = 0; i < championBtns.Length; i++)
        {
            if (i == 4)
            {
                championBtns[i].onClick.AddListener(OnRandomChampionClick);
            }
            else
            {
                int index = i;
                championBtns[i].onClick.AddListener(() => OnChampionClick(index));
            }
        }

        readyBtn.interactable = false; // 준비 버튼 초기 비활성화
    }

    private void OnChampionClick(int index)
    {
        Player localPlayer = PhotonNetwork.LocalPlayer;

        List<Player> myTeam = blueteam.Contains(localPlayer) ? blueteam : redteam;
        int playerIndex = myTeam.IndexOf(localPlayer);
        playerChampion[localPlayer] = index;

        myChampionIndex = index;

        foreach (var bg in BackgroundImgs)
        {
            bg.SetActive(false);
        }

        BackgroundImgs[index].SetActive(true);

        if (playerIndex >= 0 && playerIndex < summoners.Length)
        {
            summoners[playerIndex].sprite = ProfileImgs[index].sprite;
            nickNameTexts[playerIndex].text = localPlayer.NickName; // 닉네임 UI 적용
        }

        // 챔피언 선택 동기화
        photonView.RPC("SyncChampionSelection", RpcTarget.All, localPlayer.ActorNumber, index, localPlayer.NickName);

        // 챔피언 선택 후 준비 버튼 활성화
        readyBtn.interactable = true;
    }

    private void OnRandomChampionClick()
    {
        Player localPlayer = PhotonNetwork.LocalPlayer;
        List<Player> myTeam = blueteam.Contains(localPlayer) ? blueteam : redteam;

        // 사용 가능한 챔피언 찾기
        List<int> availableChampions = new List<int> { 0, 1, 2, 3 };
        foreach (var member in myTeam)
        {
            if (playerChampion.ContainsKey(member))
            {
                availableChampions.Remove(playerChampion[member]);
            }
        }

        int randomIndex = availableChampions[Random.Range(0, availableChampions.Count)];
        OnChampionClick(randomIndex);
    }

    [PunRPC]
    private void SyncChampionSelection(int actorNumber, int championIndex, string nickName)
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == actorNumber)
            {
                List<Player> team = blueteam.Contains(p) ? blueteam : redteam;
                int playerIndex = team.IndexOf(p) + (redteam.Contains(p) ? blueteam.Count : 0);

                if (playerIndex >= 0 && playerIndex < summoners.Length)
                {
                    summoners[playerIndex].sprite = ProfileImgs[championIndex].sprite;
                    nickNameTexts[playerIndex].text = nickName; // 닉네임 동기화
                }
                return;
            }
        }
    }
    public void OnReadyClick()
    {
        Player localPlayer = PhotonNetwork.LocalPlayer;

        if (playerChampion.TryGetValue(localPlayer, out int champIndex))
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "selectedChampion", champIndex }
            };
            localPlayer.SetCustomProperties(props);
        }

        championPanel.SetActive(false);
        readyBtn.gameObject.SetActive(false);
        photonView.RPC("PlayerReady", RpcTarget.All, localPlayer.ActorNumber, playerChampion[localPlayer]);
    }

    [PunRPC]
    private void PlayerReady(int actorNumber, int championIndex)
    {
        if (championIndex >= 0 && championIndex < championBtns.Length)
        {
            championBtns[championIndex].interactable = false;
        }

        readyCount++;

        if (readyCount == PhotonNetwork.PlayerList.Length && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("StartGame", RpcTarget.All);
        }
    }

    [PunRPC]
    public void StartGame()
    {
        PhotonNetwork.LoadLevel("Scene4");
    }
}