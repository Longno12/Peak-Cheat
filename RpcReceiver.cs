using Photon.Pun;
using UnityEngine;

namespace MyCoolMod
{
    public class RpcReceiver : MonoBehaviourPun
    {
        [PunRPC]
        public void RPC_InstantiateLocalObject(string prefabName, int count)
        {
            try
            {
                GameObject prefab = Resources.Load<GameObject>(prefabName);
                if (prefab != null)
                {
                    Vector3 spawnPos = new Vector3(0, 10000, 0);
                    for (int i = 0; i < count; i++)
                    {
                        Object.Instantiate(prefab, spawnPos, Quaternion.identity);
                    }
                }
            }
            catch { }
        }
    }
}