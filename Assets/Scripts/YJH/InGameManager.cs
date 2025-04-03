using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using System.Collections;

public class InGameManager : MonoBehaviourPunCallbacks
{
    PlayerController target;

    public Transform[] spawnPoints; // 인원 수 만큼 위치 설정
    SkillCooldownUI skillUI;

    private readonly string[] championNames = { "Ryze", "Sion", "Tryn", "Vayne" };

    [SerializeField] private GameObject[] canvasUIs;


    void Start()
    {
        SpawnMyChampion();
        if(GameManager.Instance != null)
        {
            GameManager.Instance.inGameManager = this;
        }
    }
    void SpawnMyChampion()
    {
        int champIndex = MatchManager.myChampionIndex;

        string prefabName = championNames[champIndex];
        int actorIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;

        Vector3 spawnPos = spawnPoints[actorIndex].position;

        GameObject champObj = PhotonNetwork.Instantiate(prefabName, spawnPos, Quaternion.identity);
        PlayerController player = champObj.GetComponent<PlayerController>();
        PhotonView view = player.GetComponent<PhotonView>();


        // 태그 설정
        if (actorIndex == 0 || actorIndex == 1)
        {
            view.RPC("SetMyTag", RpcTarget.AllBufferedViaServer, 1);
        }
        else if (actorIndex == 2 || actorIndex == 3)
        {
            view.RPC("SetMyTag", RpcTarget.AllBufferedViaServer, 2);
        }
        // 체력바 프리팹 로드
        GameObject barPrefab = Resources.Load<GameObject>("HealthBarCanvas");
        if (barPrefab == null)  return;

        // 체력바 생성 및 연결
        GameObject bar = Instantiate(barPrefab);
        HealthBar healthBar = bar.GetComponent<HealthBar>();
        if (healthBar != null)
        {
            healthBar.target = player;
            healthBar.useLocalData = view.IsMine;
        }

        // 체력바 네트워크 동기화 설정 (ViewID는 건드리지 않음!)
        HealthBarNetworkSync sync = bar.GetComponent<HealthBarNetworkSync>();
    }    
}