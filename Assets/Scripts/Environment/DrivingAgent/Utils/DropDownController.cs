using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class DropDownController : MonoBehaviour {
    public TextMeshProUGUI buttonText;
    public GameObject itemPrefab;
    public GameObject dropDownField;

    private GameManager gameManager;

    private void Start() {
        gameManager = GameManager.Instance;
        if (gameManager.ActiveScenarioSettings) {
            buttonText.text = gameManager.ActiveScenarioSettings.name;
        } else {
            GetComponent<Button>().interactable = false;
        }
    }
    
    public void CreateItems() {
        if (dropDownField.activeSelf) {
            dropDownField.SetActive(false);
            return;
        }

        foreach (Transform child in dropDownField.transform) {
            Destroy(child.gameObject);
        }

        foreach (ScenarioSettings currentSettings in gameManager.Scenarios) {
            GameObject newObject = Instantiate(itemPrefab, Vector3.zero, Quaternion.identity, dropDownField.transform);
            DropDownItem item = newObject.GetComponent<DropDownItem>();
            item.textMeshProText.text = currentSettings.name;
            item.Init(this, currentSettings);

            if (currentSettings == gameManager.ActiveScenarioSettings) {
                item.textMeshProText.color = Color.green;
                item.GetComponent<Toggle>().interactable = false;
            }
        }

        dropDownField.SetActive(true);
    }


    public void CloseDropDown(ScenarioSettings settings) {
        dropDownField.SetActive(false);
        gameManager.ActiveScenarioSettings = settings;
        buttonText.text = gameManager.ActiveScenarioSettings.name;
        gameManager.LoadScenarioSettings();
        gameManager.RestartSimulation();
    }
}