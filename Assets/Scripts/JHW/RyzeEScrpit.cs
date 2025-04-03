using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class RyzeEScrpit : MonoBehaviour
{

    public void SpreadE(string enemyTag, GameObject EObj)
    {
        int layerNum = LayerMask.NameToLayer(enemyTag);
        Collider[] hits = Physics.OverlapSphere(transform.position, 3.25f, 1 << layerNum);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.parent != transform.parent)
            {
                RyzeEScrpit preE = hits[i]?.GetComponentInChildren<RyzeEScrpit>();
                if (preE != null)
                {
                    Destroy(preE.gameObject);
                }
                GameObject tempE = Instantiate(EObj, hits[i].transform);
                Destroy(tempE, 4f);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 3.25f);
    }
}
