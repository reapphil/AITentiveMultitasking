using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class DS4
{
    // Gyroscope
    public static ButtonControl gyroX = null;
    public static ButtonControl gyroY = null;
    public static ButtonControl gyroZ = null;
    public static ButtonControl buttonEast = null;
    public static ButtonControl leftShoulder = null;
    public static ButtonControl rightShoulder = null;

    public static int counter = 0;

    public static Gamepad controller = null;

    public static Gamepad getController(string layoutFile = null)
    {
        // Read layout from JSON file
        string layout = File.ReadAllText(layoutFile == null ? "./Assets/SharedAssets/Scripts/customLayout.json" : layoutFile);

        // Overwrite the default layout
        InputSystem.RegisterLayoutOverride(layout, "DualShock4GamepadHID");

        

        var ds4 = Gamepad.current;
        DS4.controller = ds4;
        bindControls(DS4.controller);
        return ds4;
    }

    private static void bindControls(Gamepad ds4)
    {
        gyroX = ds4.GetChildControl<ButtonControl>("gyro X 14");
        gyroY = ds4.GetChildControl<ButtonControl>("gyro Y 16");
        gyroZ = ds4.GetChildControl<ButtonControl>("gyro Z 18");
        buttonEast = ds4.GetChildControl<ButtonControl>("buttonEast");
        leftShoulder = ds4.GetChildControl<ButtonControl>("leftShoulder");
        rightShoulder = ds4.GetChildControl<ButtonControl>("rightShoulder");

    }

    public static Quaternion getRotation(float scale = 100)
    {
        //Debug.Log("BEFORE: x: " + processRawData(gyroX.ReadValue()) + "\n y: " + processRawData(gyroY.ReadValue()) + "\n z: " + processRawData(gyroZ.ReadValue()));

        float x = processRawData(gyroX.ReadValue()) * scale;
        float y = processRawData(gyroY.ReadValue()) * scale;
        float z = -processRawData(gyroZ.ReadValue()) * scale;
        //Debug.Log("AFTER: x: " + x + "\n y: " + y + "\n z: " + z);
        return Quaternion.Euler(x, y, z);
    }
    

    public static float getX(float scale = 100) { return processRawData(gyroX.ReadValue()) * scale; }
    public static float getY(float scale = 100) { return processRawData(gyroY.ReadValue()) * scale; }
    public static float getZ(float scale = 100) { return processRawData(gyroZ.ReadValue()) * scale; }


    private static float processRawData(float data)
    {
        counter++;
        if (counter == 100)
        {
            //Debug.Log("data: " + data);
            counter = 0;
        }

        return data > 0.5 ? 1 - data : -data;
    }
}

