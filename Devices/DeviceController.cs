using UnityEngine;
using UnityEngine.EventSystems;

public class DeviceController : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerClickHandler
{
    [Header("操作设置")]
    public float rotationSpeed = 120f;
    public LayerMask groundLayer;

    private DeviceData deviceData;
    private Camera mainCamera;
    private bool isDragging = false;
    private bool isRotating = false;
    private Vector3 dragOffset;
    private bool isSelected = false;

    // 高亮材质
    private Material highlightMaterial;
    private Material originalMaterial;
    private Renderer objectRenderer;

    // 动画管理核心字段
    private ICustomAnimation currentAnim;
    // 缓存所有动画组件
    private PathMoveAnim pathMoveAnim;
    private RotationAnim rotationAnim;
    private LinearMoveAnim linearMoveAnim; // 新增：缓存直线移动组件

    private void Awake()
    {
        mainCamera = Camera.main;
        groundLayer = ~0;
        Debug.Log("✅ Ground Layer 已强制设置为「检测所有层（Everything）」");
        highlightMaterial = CreateHighlightMaterial();

        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
            Debug.Log($"📌 设备 {gameObject.name} 已获取Renderer组件");
        }
        else
        {
            Debug.LogWarning($"⚠️ 设备 {gameObject.name} 没有Renderer组件，无法显示高亮效果");
        }

        // 预加载所有动画组件
        PreloadAnimationComponents();

        // 初始化动画组件为null
        currentAnim = null;
    }

    // 预加载所有动画组件
    private void PreloadAnimationComponents()
    {
        // 旋转动画组件
        if (!GetComponent<RotationAnim>())
        {
            rotationAnim = gameObject.AddComponent<RotationAnim>();
            Debug.Log($"✅ 设备 {gameObject.name} 预加载 RotationAnim 组件，实例ID：{rotationAnim.GetInstanceID()}");
        }
        else
        {
            rotationAnim = GetComponent<RotationAnim>();
        }

        // 直线移动组件（新增：预加载并缓存）
        if (!GetComponent<LinearMoveAnim>())
        {
            linearMoveAnim = gameObject.AddComponent<LinearMoveAnim>();
            Debug.Log($"✅ 设备 {gameObject.name} 预加载 LinearMoveAnim 组件，实例ID：{linearMoveAnim.GetInstanceID()}");
        }
        else
        {
            linearMoveAnim = GetComponent<LinearMoveAnim>();
        }

        // 路径动画组件
        if (!GetComponent<PathMoveAnim>())
        {
            pathMoveAnim = gameObject.AddComponent<PathMoveAnim>();
            Debug.Log($"✅ 设备 {gameObject.name} 预加载 PathMoveAnim 组件");
        }
        else
        {
            pathMoveAnim = GetComponent<PathMoveAnim>();
        }
    }

    // 初始化设备（保留原有逻辑 + 新增动画数据加载）
    public void Initialize(DeviceData data)
    {
        deviceData = data;

        if (transform.localScale == Vector3.one || transform.localScale == Vector3.zero)
        {
            Vector3 targetScale = (data.defaultScale == Vector3.zero) ? Vector3.one : data.defaultScale;
            transform.localScale = targetScale;
            Debug.Log($"✅ 设备 {deviceData.deviceName} 首次生成，使用默认缩放: {targetScale}");
        }
        else
        {
            Debug.Log($"✅ 设备 {deviceData.deviceName} 从存储加载，保留现有缩放: {transform.localScale}");
        }

        // 加载动画数据
        if (data != null && data.animData != null && data.animData.animType != AnimationType.None)
        {
            AddAnimation(data.animData.animType);
            if (currentAnim != null)
            {
                currentAnim.SetAnimData(data.animData);
                // 如果是路径动画，同步节点到缓存组件 + 加载路径可视化
                if (currentAnim is PathMoveAnim)
                {
                    pathMoveAnim.SetPathNodes(data.animData.pathNodes);
                    if (PathEditor.Instance != null)
                    {
                        PathEditor.Instance.LoadSavedPath(transform, data.animData.pathNodes);
                    }
                }
                Debug.Log($"✅ 设备 {deviceData.deviceName} 加载动画数据：{data.animData.animType}");
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"✅ DeviceController.OnPointerDown 被调用！点击按钮：{eventData.button}");
        bool isOverUI = EventSystem.current.IsPointerOverGameObject();
        Debug.Log($"🖱️  点击是否被UI遮挡：{isOverUI}");

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            isRotating = true;
            isDragging = false;
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (groundLayer.value == 0)
            {
                Debug.LogError("❌ 无法拖拽：请在Inspector的DeviceController组件中设置'groundLayer'");
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(eventData.position);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                dragOffset = transform.position - hit.point;
                isDragging = true;
                isRotating = false;
            }

            SelectDevice();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        isRotating = false;

        if (isSelected && DevicePositionPanel.Instance != null && DevicePositionPanel.Instance.gameObject.activeSelf)
        {
            DevicePositionPanel.Instance.UpdateInputFields();
            Debug.Log($"📌 设备 {gameObject.name} 操作结束，同步面板输入框");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isRotating)
        {
            float rotationAmount = eventData.delta.x * rotationSpeed * Time.deltaTime;
            Vector3 currentEuler = transform.eulerAngles;
            float newY = currentEuler.y + rotationAmount;
            newY = (newY + 360) % 360;
            transform.eulerAngles = new Vector3(currentEuler.x, newY, currentEuler.z);
        }
        else if (isDragging)
        {
            Ray ray = mainCamera.ScreenPointToRay(eventData.position);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                Vector3 targetPosition = hit.point + dragOffset;
                targetPosition.y = transform.position.y;
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
            }
            else
            {
                Debug.LogWarning("🚨 拖拽时射线未命中地面！");
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2 && eventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log($"🗑️ 检测到双击，开始彻底删除设备：{gameObject.name}");

            RemoveCurrentAnimation();

            if (DeviceLibraryManager.Instance != null)
            {
                DeviceLibraryManager.Instance.RemoveDevice(gameObject);
            }

            if (ScenePersistenceManager.Instance != null)
            {
                ScenePersistenceManager.Instance.SaveAllDevices();
            }

            DeselectDevice();
            if (DevicePositionPanel.Instance != null)
            {
                DevicePositionPanel.Instance.HidePanel();
            }
        }
    }

    public void SelectDevice()
    {
        foreach (var device in DeviceLibraryManager.Instance.GetAllSpawnedDevices())
        {
            DeviceController controller = device.GetComponent<DeviceController>();
            if (controller != null && controller != this)
            {
                controller.DeselectDevice();
            }
        }

        isSelected = true;
        ApplyHighlight(true);

        if (DevicePositionPanel.Instance != null)
        {
            DevicePositionPanel.Instance.ShowPanel(this);
        }

        if (AnimationConfigPanel.Instance != null)
        {
            AnimationConfigPanel.Instance.OpenPanel(this);
            Debug.Log($"📌 设备 {gameObject.name} 选中，打开动画配置面板");
        }
    }

    public void DeselectDevice()
    {
        isSelected = false;
        ApplyHighlight(false);

        if (AnimationConfigPanel.Instance != null)
        {
            AnimationConfigPanel.Instance.ClosePanel();
        }
    }

    public void DeselectOnEmptyClick()
    {
        if (isSelected)
        {
            DeselectDevice();
            if (DevicePositionPanel.Instance != null && DevicePositionPanel.Instance.gameObject.activeSelf)
            {
                DevicePositionPanel.Instance.HidePanel();
            }
        }
    }

    private void ApplyHighlight(bool highlight)
    {
        if (objectRenderer == null) return;

        if (highlight)
        {
            objectRenderer.material = highlightMaterial;
        }
        else
        {
            objectRenderer.material = originalMaterial;
        }
    }

    private Material CreateHighlightMaterial()
    {
        Shader targetShader = Shader.Find("Standard");
        if (targetShader == null)
        {
            targetShader = Shader.Find("Universal Render Pipeline/Lit");
            if (targetShader == null)
            {
                targetShader = Shader.Find("Mobile/Diffuse");
                Debug.LogWarning("⚠️ 未找到Standard Shader，使用Mobile/Diffuse备用");
            }
            else
            {
                Debug.LogWarning("⚠️ 未找到Standard Shader，使用URP Lit备用");
            }
        }

        Material mat = new Material(targetShader);
        mat.color = new Color(1f, 0.92f, 0.016f, 0.5f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void SetRotation(Vector3 eulerAngles)
    {
        eulerAngles.x = (eulerAngles.x + 360) % 360;
        eulerAngles.y = (eulerAngles.y + 360) % 360;
        eulerAngles.z = (eulerAngles.z + 360) % 360;
        transform.eulerAngles = eulerAngles;
    }

    public void SetScale(Vector3 newScale)
    {
        transform.localScale = newScale;
    }

    public DeviceData GetDeviceData()
    {
        return deviceData;
    }

    // ================ 动画管理核心方法（所有动画都复用预加载组件） ================
    /// <summary>
    /// 添加指定类型的动画组件（所有动画都复用预加载组件）
    /// </summary>
    public void AddAnimation(AnimationType animType)
    {
        // 移除原有动画引用（但不销毁组件）
        RemoveCurrentAnimation();

        // 根据类型复用预加载的组件
        switch (animType)
        {
            case AnimationType.Rotation:
                if (rotationAnim == null)
                {
                    rotationAnim = GetComponent<RotationAnim>();
                    if (rotationAnim == null)
                    {
                        rotationAnim = gameObject.AddComponent<RotationAnim>();
                    }
                }
                currentAnim = rotationAnim;
                Debug.Log($"✅ 设备 {gameObject.name} 复用 RotationAnim 组件，实例ID：{rotationAnim.GetInstanceID()}");
                break;
            case AnimationType.LinearMove:
                // 核心修复：直线移动也复用预加载的组件
                if (linearMoveAnim == null)
                {
                    linearMoveAnim = GetComponent<LinearMoveAnim>();
                    if (linearMoveAnim == null)
                    {
                        linearMoveAnim = gameObject.AddComponent<LinearMoveAnim>();
                    }
                }
                currentAnim = linearMoveAnim;
                Debug.Log($"✅ 设备 {gameObject.name} 复用 LinearMoveAnim 组件，实例ID：{linearMoveAnim.GetInstanceID()}");
                break;
            case AnimationType.PathMove:
                if (pathMoveAnim == null)
                {
                    pathMoveAnim = GetComponent<PathMoveAnim>();
                    if (pathMoveAnim == null)
                    {
                        pathMoveAnim = gameObject.AddComponent<PathMoveAnim>();
                    }
                }
                currentAnim = pathMoveAnim;
                Debug.Log($"✅ 设备 {gameObject.name} 复用 PathMoveAnim 组件，实例ID：{pathMoveAnim.GetInstanceID()}");
                break;
            default:
                currentAnim = null;
                break;
        }

        if (currentAnim != null)
        {
            Debug.Log($"🎬 设备 {gameObject.name} 添加动画：{animType}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 设备 {gameObject.name} 无法添加未知类型动画：{animType}");
        }
    }

    /// <summary>
    /// 移除当前动画引用（所有动画都只清空引用，不销毁组件）
    /// </summary>
    public void RemoveCurrentAnimation()
    {
        if (currentAnim == null) return;

        // 停止动画
        try
        {
            currentAnim.Stop();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ 停止动画失败：{e.Message}");
        }

        // 路径动画：彻底销毁路径
        if (currentAnim is PathMoveAnim)
        {
            try
            {
                if (PathEditor.Instance != null && PathEditor.Instance.gameObject != null)
                {
                    PathEditor.Instance.DestroyDevicePath(transform);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ 销毁路径失败：{e.Message}");
            }
            Debug.Log($"🎬 设备 {gameObject.name} 已移除路径动画");
        }
        else
        {
            Debug.Log($"🎬 设备 {gameObject.name} 移除当前动画引用");
        }

        // 只清空引用，不销毁组件
        currentAnim = null;
    }

    /// <summary>
    /// 播放当前动画（保持原样）
    /// </summary>
    public void PlayCurrentAnimation()
    {
        if (currentAnim != null)
        {
            // 路径动画额外验证节点
            if (currentAnim is PathMoveAnim pathAnim)
            {
                Debug.Log($"📌 播放前验证：路径节点数={pathAnim.pathNodes?.Length ?? 0}，速度={pathAnim.moveSpeed}");
                if (pathAnim.pathNodes == null || pathAnim.pathNodes.Length < 2)
                {
                    Debug.LogError("❌ 路径节点不足，无法播放！");
                    return;
                }
            }

            currentAnim.Play();
            Debug.Log($"🎬 设备 {gameObject.name} 播放动画：{currentAnim.AnimType}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 设备 {gameObject.name} 无可用动画可播放");
        }
    }

    /// <summary>
    /// 暂停当前动画（保持原样）
    /// </summary>
    public void PauseCurrentAnimation()
    {
        currentAnim?.Pause();
        if (currentAnim != null)
        {
            Debug.Log($"🎬 设备 {gameObject.name} 暂停动画：{currentAnim.AnimType}");
        }
    }

    /// <summary>
    /// 停止当前动画（重置状态）（保持原样）
    /// </summary>
    public void StopCurrentAnimation()
    {
        currentAnim?.Stop();
        if (currentAnim != null)
        {
            Debug.Log($"🎬 设备 {gameObject.name} 停止动画：{currentAnim.AnimType}");
        }
    }

    /// <summary>
    /// 获取当前动画数据（供保存时调用）（保持原样）
    /// </summary>
    public AnimationData GetCurrentAnimData()
    {
        return currentAnim?.GetAnimData() ?? new AnimationData { animType = AnimationType.None };
    }

    /// <summary>
    /// 设置路径节点（供PathEditor调用）（保持原样）
    /// </summary>
    public void SetPathNodes(Vector3[] worldNodes)
    {
        if (pathMoveAnim != null)
        {
            // 将世界坐标转换为设备的局部坐标（如果设备有父物体）
            Vector3[] localNodes = new Vector3[worldNodes.Length];
            for (int i = 0; i < worldNodes.Length; i++)
            {
                if (transform.parent != null)
                {
                    localNodes[i] = transform.parent.InverseTransformPoint(worldNodes[i]);
                }
                else
                {
                    localNodes[i] = worldNodes[i];
                }
            }

            pathMoveAnim.SetPathNodes(localNodes);
            Debug.Log($"🎬 设备 {gameObject.name} 设置路径节点数量：{worldNodes.Length}");

            // 同步currentAnim的节点
            if (currentAnim is PathMoveAnim)
            {
                (currentAnim as PathMoveAnim).SetPathNodes(localNodes);
            }
        }
        else
        {
            Debug.LogError($"⚠️ 设备 {gameObject.name} 路径动画组件为空，无法设置节点");
        }
    }

    /// <summary>
    /// 切换当前设备路径的显示/隐藏（UI按钮调用）（保持原样）
    /// </summary>
    public void ToggleCurrentPathVisibility()
    {
        // 强校验：路径动画组件和节点数据
        if (pathMoveAnim == null || pathMoveAnim.pathNodes == null || pathMoveAnim.pathNodes.Length < 2)
        {
            Debug.LogWarning("⚠️ 当前设备无有效路径数据，无法显示");
            return;
        }

        if (PathEditor.Instance == null)
        {
            Debug.LogError("❌ PathEditor 实例不存在");
            return;
        }

        // 将局部坐标转换为世界坐标
        Vector3[] worldNodes = new Vector3[pathMoveAnim.pathNodes.Length];
        for (int i = 0; i < pathMoveAnim.pathNodes.Length; i++)
        {
            if (transform.parent != null)
            {
                worldNodes[i] = transform.parent.TransformPoint(pathMoveAnim.pathNodes[i]);
            }
            else
            {
                worldNodes[i] = pathMoveAnim.pathNodes[i];
            }
        }

        // 判断当前是否正在显示路径
        bool isPathShowing = PathEditor.Instance.lineRenderer.enabled;

        if (isPathShowing)
        {
            // 正在显示，执行隐藏
            PathEditor.Instance.HideDevicePath();
        }
        else
        {
            // 未显示，执行显示（传入世界坐标）
            PathEditor.Instance.ShowDevicePath(transform, worldNodes);
        }
    }

    /// <summary>
    /// 删除当前设备的路径（供UI按钮调用）（保持原样）
    /// </summary>
    public void DeleteCurrentPath()
    {
        // 1. 销毁路径可视化节点
        if (PathEditor.Instance != null)
        {
            PathEditor.Instance.DestroyDevicePath(transform);
        }

        // 2. 清空动画中的路径节点
        if (pathMoveAnim != null)
        {
            pathMoveAnim.pathNodes = null;
            pathMoveAnim.Stop(); // 停止路径动画
        }

        // 3. 如果当前动画是路径动画，清空引用
        if (currentAnim is PathMoveAnim)
        {
            currentAnim.Stop();
            currentAnim = null;
        }

        Debug.Log($"🗑️ 设备 {gameObject.name} 的路径已彻底删除");
    }

    private void OnDestroy()
    {
        // 1. 安全销毁高亮材质
        try
        {
            if (highlightMaterial != null)
            {
                Destroy(highlightMaterial);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ 销毁高亮材质失败：{e.Message}");
        }

        // 2. 核心：安全销毁路径（严格检查 PathEditor 单例）
        try
        {
            // 关键：先检查 PathEditor.Instance 是否存在，再检查是否已被销毁
            if (PathEditor.Instance != null && PathEditor.Instance.gameObject != null)
            {
                PathEditor.Instance.DestroyDevicePath(transform);
            }
            else
            {
                Debug.Log("🧹 PathEditor 实例已不存在，跳过路径销毁");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ 销毁路径失败：{e.Message}");
        }

        // 3. 安全停止并清空动画
        try
        {
            if (currentAnim != null)
            {
                currentAnim.Stop();
                currentAnim = null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ 停止动画失败：{e.Message}");
        }

        // 4. 清空所有动画组件引用
        pathMoveAnim = null;
        rotationAnim = null;
        linearMoveAnim = null;

        Debug.Log($"🧹 设备 {gameObject.name} 已安全销毁，所有资源已清理");
    }
}