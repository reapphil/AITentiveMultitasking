using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ManagerUtil
{
    public static GameObject GetSpawnContainer(this GameObject gameObject)
    {
        if (gameObject.transform.GetChildByName("SpawnContainer"))
        {
            return gameObject.transform.GetChildByName("SpawnContainer").gameObject;
        }
        else
        {
            return GetSpawnContainer(gameObject.transform.parent.gameObject);
        }
    }
}
