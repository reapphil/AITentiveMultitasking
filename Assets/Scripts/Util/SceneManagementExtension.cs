using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneManagementExtension
{
    public static GameObject GetRootGameObjectByName(this Scene scene, string name)
    {
        foreach (GameObject child in scene.GetRootGameObjects())
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }
}
