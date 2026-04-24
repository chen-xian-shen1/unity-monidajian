using UnityEngine;

[System.Serializable]
public class DeviceData
{
    [Header("设备基本信息")]
    public string deviceID;          // 设备唯一ID
    public string deviceName;        // 设备名称
    public string category;          // 设备分类
    public string description;       // 设备描述
    public AnimationData animData; // 新增：动画数据



    [Header("资源引用")]
    public GameObject prefab;        // 设备预制体
    public Sprite icon;              // 设备图标

    [Header("变换设置")]
    public Vector3 defaultScale = Vector3.one;  // 默认缩放
    public Vector3 defaultRotation = Vector3.zero;  // 默认旋转

}

