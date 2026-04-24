using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeviceLibraryPanel : MonoBehaviour
{
    [Header("引用")]
    public GameObject itemPrefab; // 修正：改为GameObject类型（指向包含DeviceLibraryItem组件的预制体）
    public Transform itemContainer;
    public Dropdown categoryDropdown;

    private List<DeviceLibraryItem> spawnedItems = new List<DeviceLibraryItem>();

   

    private void Start()
    {
        // 1. 这是Start的第一行代码，只要进入Start就一定会输出（即使后续报错）
        Debug.LogError("[DeviceLibraryPanel] Start方法已进入！！！（第一行强制日志）");

        try // 新增：捕获Start内所有异常
        {
            Debug.Log("[DeviceLibraryPanel] 面板Start()方法已触发！开始检查配置...");

            bool hasError = CheckReferences();
            if (hasError)
            {
                Debug.LogError("[DeviceLibraryPanel] 存在关键配置错误，设备列表可能无法生成！");
                return;
            }

            Debug.Log("[DeviceLibraryPanel] 配置检查通过，开始初始化分类下拉框...");
            InitializeCategories();

            Debug.Log("[DeviceLibraryPanel] 分类初始化完成，开始刷新设备列表...");
            RefreshDeviceList();

            if (categoryDropdown != null)
            {
                categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
                Debug.Log("[DeviceLibraryPanel] 已为分类下拉框添加监听...");
            }

            Debug.Log("[DeviceLibraryPanel] Start方法执行完成！");
        }
        catch (System.Exception ex) // 捕获所有类型的异常
        {
            // 强制输出异常的所有信息（关键！）
            Debug.LogError(
                $"[DeviceLibraryPanel] Start执行异常：\n" +
                $"异常类型：{ex.GetType().Name}\n" +
                $"异常消息：{ex.Message}\n" +
                $"调用栈：{ex.StackTrace}"
            );
        }
    }

    private bool CheckReferences()
    {
        bool hasError = false;

        try // 新增：捕获配置检查中的异常
        {
            // 1. 检查itemPrefab
            Debug.Log("[DeviceLibraryPanel] 开始检查itemPrefab...");
            if (itemPrefab == null)
            {
                Debug.LogError("[DeviceLibraryPanel] ❌ 错误：itemPrefab未配置！请指向包含DeviceLibraryItem组件的GameObject预制体");
                hasError = true;
            }
            else
            {
                // 额外检查：确保预制体是激活状态
                if (!itemPrefab.activeSelf)
                {
                    Debug.LogError("[DeviceLibraryPanel] ❌ 错误：itemPrefab是禁用状态！请在Prefab窗口中启用它");
                    hasError = true;
                }

                // 检查预制体是否有DeviceLibraryItem组件
                DeviceLibraryItem prefabScript = itemPrefab.GetComponent<DeviceLibraryItem>();
                if (prefabScript == null)
                {
                    Debug.LogError("[DeviceLibraryPanel] ❌ 错误：itemPrefab缺少DeviceLibraryItem组件！请给预制体添加该脚本");
                    hasError = true;
                }
                else
                {
                    Debug.Log($"[DeviceLibraryPanel] ✅ 已找到有效设备项预制体：{itemPrefab.name}（包含DeviceLibraryItem组件）");
                }
            }

            // 2. 检查itemContainer
            Debug.Log("[DeviceLibraryPanel] 开始检查itemContainer...");
            if (itemContainer == null)
            {
                Debug.LogError("[DeviceLibraryPanel] ❌ 错误：itemContainer未配置！请指向DeviceScrollRect > Viewport > Content");
                hasError = true;
            }
            else
            {
                // 额外检查：确保容器是激活状态
                if (!itemContainer.gameObject.activeSelf)
                {
                    Debug.LogError($"[DeviceLibraryPanel] ❌ 错误：itemContainer（{itemContainer.name}）是禁用状态！请启用它");
                    hasError = true;
                }
                else
                {
                    Debug.Log($"[DeviceLibraryPanel] ✅ 已找到设备项容器：{itemContainer.name}（激活状态）");
                }
            }

            // 3. 检查DeviceLibraryManager
            // 3. 检查DeviceLibraryManager（重点：逐行加日志）
            Debug.Log("[DeviceLibraryPanel] 开始检查DeviceLibraryManager...（步骤1：检查Instance是否存在）");
            if (DeviceLibraryManager.Instance == null)
            {
                Debug.LogError("[DeviceLibraryPanel] ❌ 错误：DeviceLibraryManager.Instance为空！场景中是否有该管理器？");
                hasError = true;
            }
            else
            {
                Debug.Log("[DeviceLibraryPanel] ✅ Instance存在，开始检查libraryData...（步骤2：检查libraryData是否配置）");

                // 检查Instance的libraryData是否为null
                if (DeviceLibraryManager.Instance.libraryData == null)
                {
                    Debug.LogError("[DeviceLibraryPanel] ❌ 错误：DeviceLibraryManager的libraryData未配置！请关联DefaultDeviceLibrary资源");
                    hasError = true;
                }
                else
                {
                    Debug.Log("[DeviceLibraryPanel] ✅ libraryData配置存在，开始检查allDevices...（步骤3：检查allDevices是否为null）");

                    // 关键检查：避免allDevices为null（这是最常见的静默中断点）
                    if (DeviceLibraryManager.Instance.libraryData.allDevices == null)
                    {
                        Debug.LogError("[DeviceLibraryPanel] ❌ 错误：libraryData的allDevices是null！请在DefaultDeviceLibrary中初始化allDevices列表");
                        hasError = true;
                    }
                    else
                    {
                        Debug.Log("[DeviceLibraryPanel] ✅ allDevices列表已初始化，开始统计设备数量...（步骤4：统计设备数）");

                        int deviceCount = DeviceLibraryManager.Instance.libraryData.allDevices.Count;
                        Debug.Log($"[DeviceLibraryPanel] ✅ 设备库包含 {deviceCount} 个设备（步骤5：检查完成）");

                        if (deviceCount == 0)
                        {
                            Debug.LogWarning("[DeviceLibraryPanel] ⚠️ 设备库数据为空，请在DefaultDeviceLibrary中添加设备");
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[DeviceLibraryPanel] CheckReferences执行异常：\n" +
                $"异常类型：{ex.GetType().Name}\n" +
                $"异常消息：{ex.Message}\n" +
                $"调用栈：{ex.StackTrace}"
            );
            hasError = true; // 异常时标记为有错误
        }

        return hasError;
    }

    private void InitializeCategories()
    {
        if (categoryDropdown == null || DeviceLibraryManager.Instance.libraryData == null)
        {
            Debug.LogWarning("[DeviceLibraryPanel] 分类下拉框或设备库数据为空，无法初始化分类");
            return;
        }

        categoryDropdown.ClearOptions();
        List<string> options = new List<string> { "所有分类" };
        options.AddRange(DeviceLibraryManager.Instance.libraryData.GetAllCategories());
        categoryDropdown.AddOptions(options);
    }

    private void RefreshDeviceList(string category = "")
    {
        Debug.Log($"[DeviceLibraryPanel] 开始刷新设备列表，当前分类：{category}");

        ClearAllItems();

        if (DeviceLibraryManager.Instance == null || DeviceLibraryManager.Instance.libraryData == null)
        {
            Debug.LogError("[DeviceLibraryPanel] ❌ 刷新失败：设备库管理器或数据为空");
            return;
        }

        List<DeviceData> devicesToShow = string.IsNullOrEmpty(category) || category == "所有分类"
            ? DeviceLibraryManager.Instance.libraryData.allDevices
            : DeviceLibraryManager.Instance.libraryData.GetDevicesByCategory(category);

        Debug.Log($"[DeviceLibraryPanel] 本次需生成 {devicesToShow.Count} 个设备项");

        foreach (var device in devicesToShow)
        {
            SpawnDeviceItem(device);
        }
    }

    // 修正：完整的类型转换逻辑
    private void SpawnDeviceItem(DeviceData data)
    {
        if (itemPrefab == null)
        {
            Debug.LogError("[DeviceLibraryPanel] ❌ 生成设备项失败：itemPrefab为空");
            return;
        }

        if (itemContainer == null)
        {
            Debug.LogError("[DeviceLibraryPanel] ❌ 生成设备项失败：itemContainer为空");
            return;
        }

        if (data == null)
        {
            Debug.LogError("[DeviceLibraryPanel] ❌ 生成设备项失败：DeviceData为空");
            return;
        }

        // 关键修复：先实例化GameObject，再获取组件
        GameObject itemGameObject = Instantiate(itemPrefab, itemContainer);
        itemGameObject.name = $"DeviceItem_{data.deviceID}"; // 重命名实例，方便调试

        // 获取组件（确保预制体上有该组件）
        DeviceLibraryItem itemComponent = itemGameObject.GetComponent<DeviceLibraryItem>();
        if (itemComponent == null)
        {
            Debug.LogError($"[DeviceLibraryPanel] ❌ 设备项预制体缺少DeviceLibraryItem组件！");
            Destroy(itemGameObject); // 销毁无效实例
            return;
        }

        // 初始化设备项
        itemComponent.Initialize(data);
        spawnedItems.Add(itemComponent);
        Debug.Log($"[DeviceLibraryPanel] ✅ 成功生成设备项：{data.deviceName}（实例名：{itemGameObject.name}）");
    }

    private void ClearAllItems()
    {
        foreach (var item in spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject); // 通过组件获取GameObject并销毁
        }
        spawnedItems.Clear();
        Debug.Log("[DeviceLibraryPanel] 已清除所有设备项");
    }

    private void OnCategoryChanged(int index)
    {
        if (categoryDropdown == null || index >= categoryDropdown.options.Count)
            return;

        string selectedCategory = categoryDropdown.options[index].text;
        RefreshDeviceList(selectedCategory);
    }
}
