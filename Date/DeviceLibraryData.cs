using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DeviceLibrary", menuName = "Device System/Device Library")]
public class DeviceLibraryData : ScriptableObject
{
    public List<DeviceData> allDevices = new List<DeviceData>();

    // 通过ID查找设备
    public DeviceData GetDeviceByID(string id)
    {
        return allDevices.Find(device => device.deviceID == id);
    }

    // 通过分类查找设备
    public List<DeviceData> GetDevicesByCategory(string category)
    {
        return allDevices.FindAll(device => device.category == category);
    }

    // 获取所有分类
    public List<string> GetAllCategories()
    {
        HashSet<string> categories = new HashSet<string>();
        foreach (var device in allDevices)
        {
            if (!string.IsNullOrEmpty(device.category))
                categories.Add(device.category);
        }
        return new List<string>(categories);
    }
}