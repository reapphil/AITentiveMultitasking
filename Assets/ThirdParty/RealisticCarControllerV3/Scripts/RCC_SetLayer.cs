using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Set Layer")]
public class RCC_SetLayer : MonoBehaviour {

    public string layer = "Default";
    public bool setChildren = false;

    private void OnEnable() {

        gameObject.layer = LayerMask.NameToLayer(layer);

        if (setChildren) {

            foreach (Transform item in transform)
                item.gameObject.layer = LayerMask.NameToLayer(layer);

        }

    }

}
