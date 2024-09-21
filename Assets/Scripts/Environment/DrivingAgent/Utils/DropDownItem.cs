using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DropDownItem : MonoBehaviour {
    public TextMeshProUGUI textMeshProText;

    private ScenarioSettings settings;
    private DropDownController dropDownController;

    private Toggle toggle;
    
    private void Start() {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }
    
    public void Init(DropDownController dropDownControllerRef, ScenarioSettings scenarioSettingsRef) {
        settings = scenarioSettingsRef;
        dropDownController = dropDownControllerRef;
    }
    
    private void OnToggleValueChanged(bool arg0) {
        dropDownController.CloseDropDown(settings);
    }

    void OnDestroy() {
        // Unsubscribe from the OnValueChanged event
        toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }
}