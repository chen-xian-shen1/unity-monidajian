using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class PathEditor : MonoBehaviour
{
    public static PathEditor Instance;

    [Header("路径可视化配置")]
    public LineRenderer lineRenderer;
    public GameObject pathNodePrefab;
    public Color pathColor = Color.blue;
    public float nodeSize = 0.2f;

    [Header("坐标输入UI")]
    public GameObject coordinateInputPanel;
    public TMP_InputField xInput;
    public TMP_InputField yInput;
    public TMP_InputField zInput;
    public Button addCoordinateNodeBtn;

    // 编辑状态变量
    private Transform editingDevice;
    private Transform editingPathRoot;
    private Camera mainCamera;
    private bool isInEditMode = false;

    // 显示状态变量
    private Transform currentShowingDevice;
    private Transform showingPathRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        mainCamera = Camera.main;

        // 初始化LineRenderer
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.startColor = pathColor;
            lineRenderer.endColor = pathColor;
            lineRenderer.widthMultiplier = 0.1f;
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }

        // 绑定坐标输入事件
        if (addCoordinateNodeBtn != null)
        {
            addCoordinateNodeBtn.onClick.AddListener(OnAddCoordinateNode);
        }
        else
        {
            Debug.LogWarning("⚠️ AddCoordinateNodeBtn 未赋值！");
        }

        // 初始隐藏坐标面板
        if (coordinateInputPanel != null)
        {
            coordinateInputPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("❌ CoordinateInputPanel 未赋值！请在Unity编辑器中赋值！");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("🧹 PathEditor 单例已销毁");
        }

        ClearAllState();
    }

    private void Update()
    {
        if (!isInEditMode || editingDevice == null) return;

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            AddPathNodeAtMousePosition();
        }

        if (Input.GetMouseButtonDown(1))
        {
            RemoveLastNode();
        }
    }

    #region 核心编辑生命周期方法
    /// <summary>
    /// 进入路径编辑模式（点击路径动画类型时调用）
    /// </summary>
    public void StartEditPath(Transform targetDevice)
    {
        Debug.Log("🎨 ===== StartEditPath 被触发 =====");

        // 1. 检查 targetDevice 是否为 null
        if (targetDevice == null)
        {
            Debug.LogError("❌ StartEditPath：targetDevice 为 null！");
            return;
        }
        Debug.Log($"🎨 1. targetDevice 检查通过：{targetDevice.name}");

        // 2. 检查 pathNodePrefab 是否为 null
        if (pathNodePrefab == null)
        {
            Debug.LogError("❌ StartEditPath：pathNodePrefab 为 null！请在Unity编辑器的PathEditor组件中赋值！");
            return;
        }
        Debug.Log($"🎨 2. pathNodePrefab 检查通过");

        // 3. 检查 lineRenderer 是否为 null
        if (lineRenderer == null)
        {
            Debug.LogError("❌ StartEditPath：lineRenderer 为 null！请在Unity编辑器的PathEditor组件中赋值！");
            return;
        }
        Debug.Log($"🎨 3. lineRenderer 检查通过");

        // 先清空所有状态
        ClearAllState();
        Debug.Log("🎨 4. 已清空所有状态");

        // 绑定编辑设备
        editingDevice = targetDevice;
        isInEditMode = true;
        lineRenderer.enabled = true;
        Debug.Log($"🎨 5. 已绑定编辑设备：{targetDevice.name}");

        // ========================================
        // 核心修改：支持无父物体的设备
        // ========================================
        Transform deviceParent = targetDevice.parent;
        string pathRootName = $"{targetDevice.name}_PathRoot";
        Transform pathParent = null;

        if (deviceParent != null)
        {
            // 设备有父物体，路径根节点和设备同级
            pathParent = deviceParent;
            Debug.Log($"🎨 6. 设备有父物体：{deviceParent.name}，路径根节点将和设备同级");
        }
        else
        {
            // 设备没有父物体，路径根节点直接放在场景根目录
            pathParent = null;
            Debug.Log($"🎨 6. 设备没有父物体，路径根节点将放在场景根目录");
        }

        // 查找设备已有的路径根节点，没有则新建
        if (pathParent != null)
        {
            editingPathRoot = pathParent.Find(pathRootName);
        }
        else
        {
            // 场景根目录下查找
            GameObject existingRoot = GameObject.Find(pathRootName);
            editingPathRoot = existingRoot != null ? existingRoot.transform : null;
        }

        if (editingPathRoot == null)
        {
            editingPathRoot = new GameObject(pathRootName).transform;
            if (pathParent != null)
            {
                editingPathRoot.SetParent(pathParent);
            }
            editingPathRoot.position = Vector3.zero;
            editingPathRoot.rotation = Quaternion.identity;
            Debug.Log($"🎨 7. 已新建路径根节点：{pathRootName}");
        }
        else
        {
            // 已有路径节点，激活并刷新线条
            editingPathRoot.gameObject.SetActive(true);
            UpdateEditPathLine();
            Debug.Log($"🎨 7. 已加载已有路径根节点，节点数：{editingPathRoot.childCount}");
        }

        // ========================================
        // 核心：强制显示坐标输入面板
        // ========================================
        Debug.Log($"🎨 8. 准备显示坐标输入面板，coordinateInputPanel 是否为 null：{coordinateInputPanel == null}");

        if (coordinateInputPanel != null)
        {
            coordinateInputPanel.SetActive(true);
            Debug.Log($"✅ 坐标输入面板已强制显示！面板名称：{coordinateInputPanel.name}，激活状态：{coordinateInputPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("❌❌❌ 坐标输入面板未赋值！！！请在Unity编辑器的PathEditor组件中，将CoordinateInputPanel物体拖拽到Coordinate Input Panel字段！！！");
        }

        Debug.Log($"🎨 ===== StartEditPath 执行完成 =====");
    }

    /// <summary>
    /// 结束路径编辑，返回路径节点（点击完成编辑时调用）
    /// </summary>
    public Vector3[] EndEditPath(bool keepVisualNodes = true)
    {
        isInEditMode = false;
        lineRenderer.enabled = false;

        if (coordinateInputPanel != null)
        {
            coordinateInputPanel.SetActive(false);
            Debug.Log("🎨 编辑完成，已隐藏坐标输入面板");
        }

        if (editingPathRoot == null || editingPathRoot.childCount < 2)
        {
            Debug.LogWarning($"⚠️ 路径编辑完成，节点数不足：{editingPathRoot?.childCount ?? 0} 个");
            ClearEditState();
            return new Vector3[0];
        }

        // 核心：收集节点的世界坐标（兼容有/无父物体）
        Vector3[] nodeWorldPositions = new Vector3[editingPathRoot.childCount];
        for (int i = 0; i < editingPathRoot.childCount; i++)
        {
            nodeWorldPositions[i] = editingPathRoot.GetChild(i).position;
        }

        // 处理可视化节点：默认保留
        if (keepVisualNodes)
        {
            editingPathRoot.gameObject.SetActive(false);
        }
        else
        {
            DestroyImmediate(editingPathRoot.gameObject);
            editingPathRoot = null;
        }

        Debug.Log($"✅ 路径编辑完成 | 设备：{editingDevice.name} | 节点数：{nodeWorldPositions.Length}");
        ClearEditState();
        return nodeWorldPositions;
    }

    public void AddPathNode()
    {
        AddPathNodeAtMousePosition();
    }

    private void AddPathNodeAtMousePosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            CreateEditNode(hit.point);
        }
    }

    private void OnAddCoordinateNode()
    {
        if (editingDevice == null)
        {
            Debug.LogWarning("⚠️ 无正在编辑的设备，无法添加节点");
            return;
        }

        float x = 0f, y = 0f, z = 0f;
        float.TryParse(xInput.text, out x);
        float.TryParse(yInput.text, out y);
        float.TryParse(zInput.text, out z);

        CreateEditNode(new Vector3(x, y, z));

        xInput.text = "";
        yInput.text = "";
        zInput.text = "";
    }

    private void CreateEditNode(Vector3 worldPos)
    {
        if (editingPathRoot == null) return;

        GameObject node = Instantiate(pathNodePrefab, editingPathRoot);
        node.name = $"PathNode_{editingPathRoot.childCount}";
        node.transform.position = worldPos;

        AddNodeVisual(node);
        UpdateEditPathLine();

        Debug.Log($"📍 编辑模式添加节点 | 序号：{editingPathRoot.childCount} | 世界坐标：{worldPos}");
    }

    public void RemoveLastNode()
    {
        if (editingPathRoot == null || editingPathRoot.childCount == 0) return;

        DestroyImmediate(editingPathRoot.GetChild(editingPathRoot.childCount - 1).gameObject);
        UpdateEditPathLine();
        Debug.Log("🗑️ 删除最后一个路径节点");
    }

    private void UpdateEditPathLine()
    {
        if (editingPathRoot == null) return;

        lineRenderer.positionCount = editingPathRoot.childCount;
        for (int i = 0; i < editingPathRoot.childCount; i++)
        {
            lineRenderer.SetPosition(i, editingPathRoot.GetChild(i).position);
        }
    }
    #endregion

    #region 路径显示/隐藏核心方法
    /// <summary>
    /// 显示指定设备的已保存路径（点击显示路径按钮调用）
    /// </summary>
    /// <param name="targetDevice">目标设备</param>
    /// <param name="savedNodeWorldPos">已保存的路径节点（世界坐标）</param>
    public void ShowDevicePath(Transform targetDevice, Vector3[] savedNodeWorldPos)
    {
        ClearAllState();

        if (targetDevice == null || savedNodeWorldPos == null || savedNodeWorldPos.Length < 2)
        {
            Debug.LogWarning("⚠️ 无法显示路径：设备为空或节点数不足");
            return;
        }

        currentShowingDevice = targetDevice;
        string pathRootName = $"{targetDevice.name}_PathRoot";
        Transform pathParent = targetDevice.parent;

        // 查找设备已有的路径根节点
        if (pathParent != null)
        {
            showingPathRoot = pathParent.Find(pathRootName);
        }
        else
        {
            GameObject existingRoot = GameObject.Find(pathRootName);
            showingPathRoot = existingRoot != null ? existingRoot.transform : null;
        }

        // 没有可视化节点，或节点数量不匹配，重新创建
        if (showingPathRoot == null || showingPathRoot.childCount != savedNodeWorldPos.Length)
        {
            if (showingPathRoot != null)
            {
                DestroyImmediate(showingPathRoot.gameObject);
            }

            showingPathRoot = new GameObject(pathRootName).transform;
            if (pathParent != null)
            {
                showingPathRoot.SetParent(pathParent);
            }
            showingPathRoot.position = Vector3.zero;
            showingPathRoot.rotation = Quaternion.identity;

            // 创建可视化节点（用世界坐标）
            foreach (Vector3 worldPos in savedNodeWorldPos)
            {
                GameObject node = Instantiate(pathNodePrefab, showingPathRoot);
                node.transform.position = worldPos;
                AddNodeVisual(node);
            }
        }

        // 激活节点，渲染线条
        showingPathRoot.gameObject.SetActive(true);
        lineRenderer.enabled = true;

        // 更新线条点位
        lineRenderer.positionCount = showingPathRoot.childCount;
        for (int i = 0; i < showingPathRoot.childCount; i++)
        {
            lineRenderer.SetPosition(i, showingPathRoot.GetChild(i).position);
        }

        Debug.Log($"📍 已显示设备 [{targetDevice.name}] 的路径 | 节点数：{savedNodeWorldPos.Length}");
    }

    public void HideDevicePath()
    {
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;

        if (showingPathRoot != null)
        {
            showingPathRoot.gameObject.SetActive(false);
        }

        currentShowingDevice = null;
        showingPathRoot = null;

        Debug.Log("📍 已隐藏路径");
    }
    #endregion

    #region 加载/销毁路径方法
    public void LoadPathNodes(Transform targetDevice, Vector3[] savedNodeWorldPos)
    {
        LoadSavedPath(targetDevice, savedNodeWorldPos);
    }

    /// <summary>
    /// 加载保存的路径，创建可视化节点（设备初始化时调用）
    /// </summary>
    public void LoadSavedPath(Transform targetDevice, Vector3[] savedNodeWorldPos)
    {
        if (targetDevice == null || savedNodeWorldPos == null || savedNodeWorldPos.Length < 2) return;

        DestroyDevicePath(targetDevice);

        string pathRootName = $"{targetDevice.name}_PathRoot";
        Transform pathParent = targetDevice.parent;

        // 创建路径根节点
        Transform pathRoot = new GameObject(pathRootName).transform;
        if (pathParent != null)
        {
            pathRoot.SetParent(pathParent);
        }
        pathRoot.position = Vector3.zero;
        pathRoot.rotation = Quaternion.identity;

        // 创建节点（用世界坐标）
        foreach (Vector3 worldPos in savedNodeWorldPos)
        {
            GameObject node = Instantiate(pathNodePrefab, pathRoot);
            node.transform.position = worldPos;
            AddNodeVisual(node);
        }

        // 默认隐藏，不显示线条
        pathRoot.gameObject.SetActive(false);
        Debug.Log($"✅ 已为设备 [{targetDevice.name}] 预加载路径可视化节点 | 节点数：{savedNodeWorldPos.Length}");
    }

    /// <summary>
    /// 彻底销毁指定设备的路径（删除动画/删除设备时调用）
    /// </summary>
    public void DestroyDevicePath(Transform targetDevice)
    {
        if (targetDevice == null)
        {
            Debug.LogWarning("⚠️ DestroyDevicePath：目标设备为空，跳过销毁");
            return;
        }

        string pathRootName = $"{targetDevice.name}_PathRoot";
        Transform pathRoot = null;

        // 查找路径根节点（兼容有/无父物体）
        Transform deviceParent = targetDevice.parent;
        if (deviceParent != null)
        {
            pathRoot = deviceParent.Find(pathRootName);
        }
        else
        {
            GameObject existingRoot = GameObject.Find(pathRootName);
            pathRoot = existingRoot != null ? existingRoot.transform : null;
        }

        // 路径根节点存在，安全销毁
        if (pathRoot != null)
        {
            try
            {
                if (pathRoot.gameObject != null)
                {
                    DestroyImmediate(pathRoot.gameObject);
                    Debug.Log($"🗑️ 已安全销毁设备 [{targetDevice.name}] 的路径节点");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ 销毁路径根节点失败：{e.Message}");
            }
        }

        // 安全清空编辑/显示状态
        try
        {
            if (editingDevice == targetDevice)
            {
                ClearEditState();
            }
            if (currentShowingDevice == targetDevice)
            {
                HideDevicePath();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ 清空路径状态失败：{e.Message}");
        }
    }
    #endregion

    #region 状态清理方法
    public void ClearEditState()
    {
        isInEditMode = false;
        editingDevice = null;
        editingPathRoot = null;
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;

        if (xInput != null) xInput.text = "";
        if (yInput != null) yInput.text = "";
        if (zInput != null) zInput.text = "";

        Debug.Log("🧹 已清空路径编辑状态");
    }

    public void ClearAllState()
    {
        ClearEditState();
        HideDevicePath();
    }

    public void ClearCurrentEditState()
    {
        ClearEditState();
    }
    #endregion

    #region 工具方法
    private void AddNodeVisual(GameObject node)
    {
        foreach (var collider in node.GetComponents<Collider>())
        {
            DestroyImmediate(collider);
        }
        foreach (var oldRenderer in node.GetComponents<Renderer>())
        {
            DestroyImmediate(oldRenderer);
        }
        foreach (var oldFilter in node.GetComponents<MeshFilter>())
        {
            DestroyImmediate(oldFilter);
        }

        SphereCollider sphere = node.AddComponent<SphereCollider>();
        sphere.radius = nodeSize;
        sphere.isTrigger = true;

        MeshRenderer renderer = node.AddComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Unlit/Color"));
        renderer.material.color = Color.red;

        MeshFilter filter = node.AddComponent<MeshFilter>();
        filter.mesh = CreateSphereMesh();
    }

    private Mesh CreateSphereMesh()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = sphere.GetComponent<MeshFilter>().mesh;
        Destroy(sphere);
        return mesh;
    }
    #endregion
}