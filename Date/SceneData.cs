using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SceneData
{
    public string sceneName;
    public string saveDate;
    public List<DeviceStateData> deviceStates = new List<DeviceStateData>();
}