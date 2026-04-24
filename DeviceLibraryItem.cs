using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeviceLibraryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [Tooltip("指定地面所在的层（必须勾选地面层）")]
    [SerializeField] private LayerMask groundLayerMask;
    [Tooltip("强制检测触发器（如果地面碰撞器是触发器，需要勾选）")]
    [SerializeField] private bool detectTriggers = true;
    [Tooltip("调试模式：显示详细日志")]
    [SerializeField] private bool debugMode = true;

    private DeviceData deviceData;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private GameObject dragPreview;
    private Camera mainCamera;
    private bool isDragging = false;

    private void Awake()
    {
        // 初始化基础组件
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        // 验证主相机
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            LogError("未找到主相机！请确保场景中有Tag为「MainCamera」的相机");
        }

        // 验证父级Canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            LogError("未找到父级Canvas！设备项必须放在Canvas下");
        }

        // 初始化拖拽预览
        dragPreview = new GameObject("DragPreview");
        dragPreview.SetActive(false);
        Image previewImage = dragPreview.AddComponent<Image>();
        previewImage.raycastTarget = false;
        RectTransform previewRect = dragPreview.GetComponent<RectTransform>();
        previewRect.sizeDelta = new Vector2(80, 80);
        dragPreview.transform.SetParent(parentCanvas.transform, false);
        dragPreview.transform.SetAsLastSibling();

        // 启动时验证层配置（关键！）
        ValidateLayerMask();
    }

    // 验证层掩码配置是否正确
    private void ValidateLayerMask()
    {
        if (groundLayerMask == 0)
        {
            LogError("⚠️ 未配置地面层！请在Inspector中勾选地面所在的层（如Ground层）");
            return;
        }

        // 输出当前选中的层名称（方便确认）
        string selectedLayers = "";
        for (int i = 0; i < 32; i++)
        {
            if ((groundLayerMask.value & (1 << i)) != 0)
            {
                selectedLayers += LayerMask.LayerToName(i) + "、";
            }
        }
        if (debugMode)
        {
            Debug.Log($"当前地面层配置：{selectedLayers.TrimEnd('、')}");
        }
    }

    public void Initialize(DeviceData data)
    {
        if (data == null)
        {
            LogError("初始化失败：DeviceData为空");
            return;
        }

        deviceData = data;
        if (iconImage != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = data.icon != null;
        }
        if (nameText != null)
        {
            nameText.text = data.deviceName;
        }

        Image previewImage = dragPreview.GetComponent<Image>();
        previewImage.sprite = data.icon;

        if (debugMode)
        {
            Debug.Log($"设备项初始化完成：{data.deviceName}（预制体：{(data.prefab ? data.prefab.name : "null")}）");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        if (debugMode) Debug.Log("开始拖拽...");

        if (deviceData == null || deviceData.prefab == null)
        {
            LogError("无法拖拽：设备数据或预制体为空");
            return;
        }

        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;
        dragPreview.SetActive(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // 更新UI项位置
        if (rectTransform != null && parentCanvas != null)
        {
            rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
        }

        // 更新预览位置
        if (dragPreview.activeSelf)
        {
            dragPreview.GetComponent<RectTransform>().position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (debugMode) Debug.Log("结束拖拽，尝试放置设备...");

        // 恢复状态
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        rectTransform.anchoredPosition = Vector2.zero;
        dragPreview.SetActive(false);
        isDragging = false;

        // 尝试放置设备（核心逻辑）
        TryPlaceDeviceInScene();
    }

    private bool TryPlaceDeviceInScene()
    {
        // 1. 基础条件检查
        if (mainCamera == null)
        {
            LogError("放置失败：主相机为空");
            return false;
        }
        if (deviceData == null || deviceData.prefab == null)
        {
            LogError("放置失败：设备数据不完整");
            return false;
        }
        if (groundLayerMask == 0)
        {
            LogError("放置失败：未配置地面层（请在Inspector中设置）");
            return false;
        }
        if (DeviceLibraryManager.Instance == null)
        {
            LogError("放置失败：DeviceLibraryManager.Instance为空（场景中是否有该管理器？）");
            return false;
        }

        // 2. 射线检测（关键修改：支持触发器检测）
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        QueryTriggerInteraction triggerMode = detectTriggers ?
            QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        // 可视化射线（持续5秒，红色粗线）
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 5f);

        // 执行射线检测（包含触发器处理）
        bool isHit = Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayerMask, triggerMode);

        // 3. 输出射线检测详情（关键调试信息）
        if (isHit)
        {
            // 检测到碰撞时，输出碰撞物体的详细信息
            Collider hitCollider = hit.collider;
            Debug.Log($"射线击中物体：{hitCollider.gameObject.name}\n" +
                      $"层级：{LayerMask.LayerToName(hitCollider.gameObject.layer)}\n" +
                      $"是否为触发器：{hitCollider.isTrigger}\n" +
                      $"碰撞位置：{hit.point}");

            // 检查击中的层是否在配置的地面层中
            if (!IsLayerInMask(hitCollider.gameObject.layer, groundLayerMask))
            {
                LogWarning($"⚠️ 击中物体不在配置的地面层中！配置层：{groundLayerMask}，实际层：{hitCollider.gameObject.layer}");
                return false;
            }
        }
        else
        {
            LogWarning("射线未击中任何物体（检查：1.地面是否在配置的层 2.地面是否有碰撞器 3.射线是否真的穿过地面）");
            return false;
        }

        // 4. 生成设备
        GameObject spawnedDevice = DeviceLibraryManager.Instance.SpawnDevice(deviceData, Vector3.zero);
        if (spawnedDevice != null)
        {
            Debug.Log($"✅ 设备放置成功：{deviceData.deviceName}（位置：{hit.point}）");
            return true;
        }
        else
        {
            LogError("设备生成失败（检查DeviceLibraryManager的SpawnDevice方法）");
            return false;
        }
    }

    // 辅助方法：检查层是否在层掩码中
    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return ((mask.value & (1 << layer)) != 0);
    }

    private void OnDestroy()
    {
        if (dragPreview != null)
        {
            Destroy(dragPreview);
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[设备库] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[设备库] {message}");
    }
}
