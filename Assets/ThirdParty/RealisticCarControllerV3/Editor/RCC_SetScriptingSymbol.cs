//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RCC_SetScriptingSymbol {

    public static void SetEnabled(string defineName, bool enable) {

        foreach (BuildTarget buildTarget in Enum.GetValues(typeof(BuildTarget))) {

            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(buildTarget);

            if (group == BuildTargetGroup.Unknown)
                continue;

            var curDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';').Select(d => d.Trim()).ToList();

            if (enable) {

                if (!curDefineSymbols.Contains(defineName)) {
                    curDefineSymbols.Add(defineName);

                }

            } else {

                if (curDefineSymbols.Contains(defineName))
                    curDefineSymbols.Remove(defineName);

            }

            try {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", curDefineSymbols.ToArray()));
            } catch (Exception e) {
                Debug.Log("Could not set " + defineName + " scripting define symbol for build target: " + buildTarget + " group: " + group + " " + e);
            }

        }

    }

}
