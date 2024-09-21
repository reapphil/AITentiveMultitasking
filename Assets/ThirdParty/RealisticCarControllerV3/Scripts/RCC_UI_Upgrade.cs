//----------------------------------------------
//			Realistic Car Controller
//
// Copyright © 2023 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UI upgrade button.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/Modification/RCC UI Upgrade Button")]
public class RCC_UI_Upgrade : MonoBehaviour {

    public UpgradeClass upgradeClass = UpgradeClass.Speed;
    public enum UpgradeClass { Speed, Handling, Brake }

    public void OnClick() {

        RCC_CustomizationManager handler = RCC_CustomizationManager.Instance;

        if (!handler) {

            Debug.LogError("You are trying to customize the vehicle, but there is no ''RCC_CustomizationManager'' in your scene yet.");
            return;

        }

        RCC_CustomizationApplier applier = handler.vehicle;

        if (!applier)
            return;

        switch (upgradeClass) {

            case UpgradeClass.Speed:
                if (applier.UpgradeManager.engineLevel < 5)
                    handler.UpgradeSpeed();
                break;
            case UpgradeClass.Handling:
                if (applier.UpgradeManager.handlingLevel < 5)
                    handler.UpgradeHandling();
                break;
            case UpgradeClass.Brake:
                if (applier.UpgradeManager.brakeLevel < 5)
                    handler.UpgradeBrake();
                break;

        }

    }

}
