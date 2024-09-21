using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class KeyboardScript : MonoBehaviour
{

    public TextMeshProUGUI TextField;
    public GameObject EngLayoutSml, SymbLayout;
    
    public char LastPressedButton {  get; private set; }

    public bool ButtonWasPressed { get; set; }


    public void AlphabetFunction(string alphabet)
    {
        TextField.text=TextField.text + alphabet;
        LastPressedButton = alphabet[0];
        ButtonWasPressed = true;
    }

    public void BackSpace()
    {
        if(TextField.text.Length>0) TextField.text= TextField.text.Remove(TextField.text.Length-1);
        LastPressedButton = '\x7F';
        ButtonWasPressed = true;
    }

    public void CloseAllLayouts()
    {
        EngLayoutSml.SetActive(false);
        SymbLayout.SetActive(false);
    }

    public void ShowLayout(GameObject SetLayout)
    {
        CloseAllLayouts();
        SetLayout.SetActive(true);
    }
}
