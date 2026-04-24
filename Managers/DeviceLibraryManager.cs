using System.Collections.Generic;
using UnityEngine;

public class DeviceLibraryManager : MonoBehaviour
{
    public static DeviceLibraryManager Instance;

    [Header("引用设置")]
    public DeviceLibraryData libraryData;

    [Header("运行时数据")]
    private List<GameObject> spawnedDevices = new List<GameObject>();

    // 新增：实时保存开关（可在Inspector中控制）
    [Header("自动保存设置")]
    public bool enableRealTimeSave = true;

    private void Awake()
    {
        // 输出初始化日志（确认管理器是否被创建）
        Debug.LogError("[DeviceLibraryManager] Awake方法触发！开始初始化单例...");

        // 实现单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.LogError($"[DeviceLibraryManager] 单例创建成功！Instance已赋值，物体名：{gameObject.name}");

            // 检查libraryData是否配置
            if (libraryData == null)
            {
                Debug.LogError("[DeviceLibraryManager] ❌ 错误：libraryData未配置！请在Inspector中关联DefaultDeviceLibrary资源");
            }
            else
            {
                // 关键检查：确保allDevices列表不是null（是空列表也可以，但不能是null）
                if (libraryData.allDevices == null)
                {
                    Debug.LogError("[DeviceLibraryManager] ❌ 错误：libraryData的allDevices是null！请在DefaultDeviceLibrary中点击allDevices的\"+\"添加至少一个空项");
                    // 强制初始化allDevices，避免后续空引用
                    libraryData.allDevices = new List<DeviceData>();
                }
                else
                {
                    Debug.Log($"[DeviceLibraryManager] ✅ libraryData配置正常，allDevices数量：{libraryData.allDevices.Count}");
                }
            }
        }
        else
        {
            Debug.LogError($"[DeviceLibraryManager] ❌ 发现重复实例！当前物体：{gameObject.name}，已销毁重复实例");
            Destroy(gameObject);
        }
    }

    // 生成设备到场景
    public GameObject SpawnDevice(string deviceID, Vector3 position)
    {
        DeviceData deviceData = libraryData.GetDeviceByID(deviceID);
        return SpawnDevice(deviceData, position);
    }

    // 生成设备到场景
    // 修改后的代码
    public GameObject SpawnDevice(DeviceData deviceData)
    {
        // 直接使用(0,0,0)作为固定位置
        return SpawnDevice(deviceData, Vector3.zero);
    }

    // 保留原方法但作为内部使用
    public GameObject SpawnDevice(DeviceData deviceData, Vector3 position)
    {
        if (deviceData == null || deviceData.prefab == null)
        {
            Debug.LogError("无法生成设备：数据或预制体为空");
            return null;
        }
        // ↓↓↓↓ 关键修复 ↓↓↓↓
        deviceData.animData = null; // 新设备必须清空动画！

        // 实例化设备 - 现在使用传入的position，我们会传入Vector3.zero
        GameObject deviceInstance = Instantiate(
            deviceData.prefab,
            position,  // 现在这个位置将是(0,0,0)
            Quaternion.Euler(deviceData.defaultRotation)
        );

        // 设置名称和缩放
        deviceInstance.name = deviceData.deviceName;
        // 即使deviceData中的缩放有问题，也强制使用1,1,1
        deviceInstance.transform.localScale = Vector3.one;
        AddColliderToDevice(deviceInstance);
        // 可选：输出日志确认缩放值
        Debug.Log($"设备 {deviceData.deviceName} 缩放已设置为：{deviceInstance.transform.localScale}");

        // 添加设备控制器
        DeviceController controller = deviceInstance.AddComponent<DeviceController>();
        controller.Initialize(deviceData);

        // 添加到已生成设备列表
        spawnedDevices.Add(deviceInstance);

        // 新增：生成设备后实时保存
        if (enableRealTimeSave)
        {
            TriggerRealTimeSave("生成设备");
        }

        return deviceInstance;
    }

    // 移除设备（强化版：增加实时保存）
    public void RemoveDevice(GameObject device)
    {
        if (device == null) return;

        if (spawnedDevices.Contains(device))
        {
            spawnedDevices.Remove(device);
            Debug.Log($"📥 设备 {device.name} 已从管理列表移除");
        }

        // 销毁物体
        Destroy(device);

        // 新增：移除设备后实时保存
        //if (enableRealTimeSave)
        // {
        //TriggerRealTimeSave("移除设备");
        // }
    }

    // 获取所有已生成的设备
    public List<GameObject> GetAllSpawnedDevices()
    {
        return new List<GameObject>(spawnedDevices);
    }

    // 清除场景中所有设备（强化版：增加实时保存）
    public void ClearAllDevices()
    {
        foreach (var device in spawnedDevices)
        {
            Destroy(device);
        }
        spawnedDevices.Clear();

        // 新增：清空设备后实时保存
        // if (enableRealTimeSave)
        // {
        //TriggerRealTimeSave("清空所有设备");
        //}
    }

    // ================ 新增：AddDevice方法（ScenePersistenceManager需要） ================
    /// <summary>
    /// 将设备添加到管理列表（加载设备时调用）
    /// </summary>
    public void AddDevice(GameObject deviceObj)
    {
        if (deviceObj != null && !spawnedDevices.Contains(deviceObj))
        {
            spawnedDevices.Add(deviceObj);
            Debug.Log($"📥 设备 {deviceObj.name} 已加入管理列表");

            // 新增：加载设备（AddDevice）后实时保存
            if (enableRealTimeSave)
            {
                TriggerRealTimeSave("加载设备");
            }
        }
    }

    // 公开AddColliderToDevice，供ScenePersistenceManager调用
    public void AddColliderToDevice(GameObject device)
    {
        // 保持你原有的逻辑不变
        Debug.Log($"📌 开始为设备 {device.name} 添加碰撞器...");
        Collider existingCollider = device.GetComponent<Collider>();
        if (existingCollider != null)
        {
            Debug.Log($"📌 设备 {device.name} 已有碰撞器：{existingCollider.GetType().Name}");
            return;
        }

        MeshFilter meshFilter = device.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            device.AddComponent<MeshCollider>();
            Debug.Log($"✅ 为设备 {device.name} 添加 MeshCollider");
        }
        else
        {
            device.AddComponent<BoxCollider>();
            Debug.Log($"✅ 为设备 {device.name} 添加 BoxCollider");
        }
    }

    // ================ 核心新增：实时保存触发方法 ================
    /// <summary>
    /// 触发实时保存（统一封装，便于维护）
    /// </summary>
    /// <param name="actionDesc">操作描述（用于日志）</param>
    private void TriggerRealTimeSave(string actionDesc)
    {
        if (ScenePersistenceManager.Instance == null)
        {
            Debug.LogWarning($"⚠️ 实时保存失败：未找到ScenePersistenceManager实例！（操作：{actionDesc}）");
            return;
        }

        // 调用ScenePersistenceManager的保存方法
        ScenePersistenceManager.Instance.SaveAllDevices();
        Debug.Log($"✅ 实时保存完成！触发原因：{actionDesc}");
    }
}