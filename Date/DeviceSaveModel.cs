// 在 Scripts/Data 文件夹下创建 DeviceSaveModel.cs
using UnityEngine;
using System.Collections.Generic;

// 单个设备的保存数据
[System.Serializable] // 必须加此标签，否则 JsonUtility 无法序列化
public class SingleDeviceSaveData
{
    // 设备唯一标识（关联 DeviceData 的 ID，确保加载时能找到对应预制体）
    public string deviceID;
    // 设备位置（X/Z 平面，Y 轴固定）
    public float posX;
    public float posY;
    public float posZ;
    // 设备旋转（仅 Y 轴旋转，符合你的拖拽逻辑）
    public float rotY;
    // 设备缩放（固定为 1,1,1，可省略，也可保留用于扩展）
    public float scaleX = 1;
    public float scaleY = 1;
    public float scaleZ = 1;
}

// 所有设备的保存容器（用于批量序列化）
[System.Serializable]
public class AllDevicesSaveData
{
    // 存储所有设备的数据列表
    public List<SingleDeviceSaveData> allDevices = new List<SingleDeviceSaveData>();
}