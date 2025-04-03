using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameCamera : MonoBehaviour
{
    public GameObject player;
    [SerializeField]
    Vector3 initPos;
    public float moveLimit;
    public float speed;

    bool followPlayer = false;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Confined; //마우스 못나가게 하기
        
    }

    public void Init(GameObject player)
    {
        transform.position = initPos + player.transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(Input.GetKeyDown(KeyCode.Y))
        {
            followPlayer = !followPlayer;
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            followPlayer = true;
        }

        if(Input.GetKeyUp(KeyCode.Space))
        {
            followPlayer = false;
        }

        if(player != null)
        {
            if (followPlayer == true)
            {
                transform.position = initPos + player.transform.position;
            }
            else
            {
                Vector3 tempPos = transform.position;

                //상
                if (Input.mousePosition.y >= Screen.height - moveLimit)
                {
                    tempPos.z += speed * Time.deltaTime;
                }
                //하
                if (Input.mousePosition.y <= moveLimit)
                {
                    tempPos.z -= speed * Time.deltaTime;
                }
                //좌
                if (Input.mousePosition.x <= moveLimit)
                {
                    tempPos.x -= speed * Time.deltaTime;
                }
                //우
                if (Input.mousePosition.x >= Screen.width - moveLimit)
                {
                    tempPos.x += speed * Time.deltaTime;
                }

                transform.position = tempPos;
            }
        }
    }
}
