using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RyzeQScript : MonoBehaviour
{
    public Vector3 startPos;
    public GameObject addQObj;

    string enemyTag;
    float damage;
    int lethal;
    float aPen;
    Character character;

    private void Start()
    {
        startPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(startPos, transform.position) <= 10)
        {
            transform.Translate(Vector3.forward * 0.5f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void GetDmg(float dmg, int let, float ap, string tag, Character chara)
    {
        damage = dmg;
        lethal = let;
        aPen = ap;
        enemyTag = tag;
        character = chara;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<PlayerController>(out PlayerController target) && target.CompareTag(enemyTag))
        {
            RyzeEScrpit mark = target?.GetComponentInChildren<RyzeEScrpit>();
            if (mark != null) //TODO: 표식 확인
            {
                damage *= 2;
                Destroy(mark.gameObject);
                int layerNum = LayerMask.NameToLayer(enemyTag);

                Collider[] hits = Physics.OverlapSphere(transform.position, 3.25f, 1 << layerNum);

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].transform != other.transform)
                    {
                        RyzeEScrpit addMark = hits[i]?.GetComponentInChildren<RyzeEScrpit>();
                        if (addMark != null)
                        {
                            Destroy(addMark.gameObject);
                            GameObject addQ = Instantiate(addQObj, transform.position, Quaternion.identity);
                            addQ.GetComponent<RyzeAddQScript>().GetDmg(hits[i].transform, damage, lethal, aPen, enemyTag, character);
                        }
                    }
                }
            }
            target.character.TakeDamage(character, damage, false, lethal, aPen);
            Debug.Log(target.name + " 맞음 | " + damage + " 의 데미지");
            Destroy(gameObject);
        }
    }
}
