using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class OverheadSignSingle : MonoBehaviour {

    public GameObject blockSign;
    

    [HideInInspector]
    public SignState signState = SignState.None;
    

    public void SetSignState(SignState newSignState) {
        
        signState = newSignState;

        switch (signState) {
            case SignState.Free:
                blockSign.SetActive(false);
                break;
            case SignState.Blocked:
                blockSign.SetActive(true);
                break;
        }

    }
    

}
