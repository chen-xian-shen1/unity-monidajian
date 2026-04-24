using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnimationConfigPanel : MonoBehaviour
{
    public static AnimationConfigPanel Instance;

    [Header("面板控件")]
    public GameObject panel;
    public TMP_Dropdown animTypeDropdown;
    public Button addAnimBtn;
    public Button removeAnimBtn;
    public Button playBtn;
    public Button pauseBtn;
    public Button stopBtn;

    [Header("旋转动画配置")]
    public GameObject rotationConfig;
    public TMP_InputField rotateSpeedInput;
    public Dropdown rotateAxisDropdown;
    // 新增：旋转目标选择（支持子物体旋转）
    public TMP_Dropdown rotateTargetDropdown;

    [Header("直线移动配置")]
    public GameObject linearMoveConfig;
    public TMP_InputField moveSpeedInput;
    public TMP_InputField moveRangeInput;
    public Toggle pingPongToggle;
    // 新增：直线移动方向选择
    public Dropdown linearDirectionDropdown;
    // 新增：直线移动模式选择
    public Dropdown linearMoveModeDropdown;

    [Header("路径移动配置")]
    public GameObject pathMoveConfig;
    public TMP_InputField pathSpeedInput;
    public Toggle smoothPathToggle;
    public Button addNodeBtn;
    public Button removeNodeBtn;
    public Button finishPathBtn;
    public Button togglePathBtn; // 新增：显示/隐藏路径按钮
    public Button deletePathBtn; // 新增：删除路径按钮
    // 新增：路径移动模式选择
    public Dropdown pathMoveModeDropdown;

    // 当前选中的设备
    private DeviceController currentDevice;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);

        // 绑定按钮事件
        addAnimBtn.onClick.AddListener(OnAddAnimation);
        removeAnimBtn.onClick.AddListener(OnRemoveAnimation);
        playBtn.onClick.AddListener(OnPlayAnimation);
        pauseBtn.onClick.AddListener(OnPauseAnimation);
        stopBtn.onClick.AddListener(OnStopAnimation);
        addNodeBtn.onClick.AddListener(OnAddPathNode);
        removeNodeBtn.onClick.AddListener(OnRemovePathNode);
        finishPathBtn.onClick.AddListener(OnFinishPathEdit);

        // 绑定动画类型切换
        animTypeDropdown.onValueChanged.AddListener(OnAnimTypeChanged);
        // 初始隐藏所有配置面板
        HideAllConfigPanels();

        // 初始化下拉框
        InitRotateAxisDropdown();
        InitLinearDirectionDropdown();
        InitLinearMoveModeDropdown();
        InitPathMoveModeDropdown();
        // 初始化旋转目标下拉框（可选）
        InitRotateTargetDropdown();

        // 绑定路径相关按钮（修复：移到Awake最外层，避免空引用）
        if (togglePathBtn != null)
        {
            togglePathBtn.onClick.AddListener(OnTogglePathVisibility);
        }
        if (deletePathBtn != null)
        {
            deletePathBtn.onClick.AddListener(OnDeletePath);
        }
    }

    /// <summary>
    /// 初始化旋转轴下拉框选项
    /// </summary>
    private void InitRotateAxisDropdown()
    {
        rotateAxisDropdown.ClearOptions();
        rotateAxisDropdown.AddOptions(new List<string> { "X轴", "Y轴", "Z轴" });
        rotateAxisDropdown.value = 1; // 默认选中Y轴
    }

    /// <summary>
    /// 初始化旋转目标下拉框（设备自身+子物体）
    /// </summary>
    private void InitRotateTargetDropdown()
    {
        if (rotateTargetDropdown == null) return;
        rotateTargetDropdown.ClearOptions();
        // 默认选项：自身
        rotateTargetDropdown.AddOptions(new List<string> { "自身" });
    }

    /// <summary>
    /// 初始化直线移动方向下拉框
    /// </summary>
    private void InitLinearDirectionDropdown()
    {
        if (linearDirectionDropdown == null) return;
        linearDirectionDropdown.ClearOptions();
        linearDirectionDropdown.AddOptions(new List<string>
        {
            "前", "后", "左", "右", "上", "下"
        });
        linearDirectionDropdown.value = 0; // 默认向前
    }

    /// <summary>
    /// 初始化直线移动模式下拉框
    /// </summary>
    private void InitLinearMoveModeDropdown()
    {
        if (linearMoveModeDropdown == null) return;
        linearMoveModeDropdown.ClearOptions();
        linearMoveModeDropdown.AddOptions(new List<string>
        {
            "单次", "来回", "重复"
        });
        linearMoveModeDropdown.value = 2; // 默认重复
    }

    /// <summary>
    /// 初始化路径移动模式下拉框
    /// </summary>
    private void InitPathMoveModeDropdown()
    {
        if (pathMoveModeDropdown == null) return;
        pathMoveModeDropdown.ClearOptions();
        pathMoveModeDropdown.AddOptions(new List<string>
        {
            "单次", "来回", "重复"
        });
        pathMoveModeDropdown.value = 2; // 默认重复
    }

    /// <summary>
    /// 打开动画配置面板（选中设备时调用）
    /// </summary>
    public void OpenPanel(DeviceController device)
    {
        currentDevice = device;
        panel.SetActive(true);
        animTypeDropdown.value = 0;
        HideAllConfigPanels();

        // 更新旋转目标下拉框（加载设备子物体）
        UpdateRotateTargetDropdown();
    }

    /// <summary>
    /// 更新旋转目标下拉框（包含设备所有子物体）
    /// </summary>
    /// <summary>
    /// 更新旋转目标下拉框（包含设备所有层级的子物体）
    /// </summary>
    /// <summary>
    /// 更新旋转目标下拉框（包含所有层级子物体）
    /// </summary>
    private void UpdateRotateTargetDropdown()
    {
        if (rotateTargetDropdown == null || currentDevice == null) return;

        rotateTargetDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> optionDatas = new List<TMP_Dropdown.OptionData>();
        _rotateTargetPathCache = new List<string>();

        // 第一个选项：自身
        optionDatas.Add(new TMP_Dropdown.OptionData("自身"));
        _rotateTargetPathCache.Add("");

        // 递归获取所有子物体
        GetAllChildPaths(currentDevice.transform, "", ref optionDatas, ref _rotateTargetPathCache);

        rotateTargetDropdown.options = optionDatas;
        rotateTargetDropdown.value = 0;

        Debug.Log($"✅ [Config] 旋转目标下拉框已更新 | 总选项数：{optionDatas.Count}");
    }

    // 新增：缓存旋转目标的路径列表，避免查找错误
    private List<string> _rotateTargetPathCache = new List<string>();

    /// <summary>
    /// 关闭面板
    /// </summary>
    // 2. 优化 ClosePanel 方法（已在之前的代码中，确保关闭面板时清空状态）
    public void ClosePanel()
    {
        panel.SetActive(false);
        currentDevice = null;
        // 关闭面板时清空所有路径状态
        if (PathEditor.Instance != null)
        {
            PathEditor.Instance.ClearAllState();
        }
    }

    // 动画类型切换
    // 1. 优化 OnAnimTypeChanged 方法（已在之前的代码中，确保切换类型时清空状态）
    private void OnAnimTypeChanged(int index)
    {
        HideAllConfigPanels();
        // 切换动画类型时，清空所有路径状态（编辑+显示）
        if (PathEditor.Instance != null)
        {
            PathEditor.Instance.ClearAllState();
        }

        switch ((AnimationType)index)
        {
            case AnimationType.Rotation:
                rotationConfig.SetActive(true);
                break;
            case AnimationType.LinearMove:
                linearMoveConfig.SetActive(true);
                break;
            case AnimationType.PathMove:
                pathMoveConfig.SetActive(true);
                // 进入路径编辑模式
                if (PathEditor.Instance != null && currentDevice != null)
                {
                    PathEditor.Instance.StartEditPath(currentDevice.transform);
                }
                break;
        }
    }


    // 添加动画
    private void OnAddAnimation()
    {
        if (currentDevice == null) return;

        AnimationType animType = (AnimationType)animTypeDropdown.value;
        currentDevice.AddAnimation(animType);

        // 配置动画参数
        switch (animType)
        {
            // 替换 AnimationConfigPanel.cs 中 OnAddAnimation 方法的旋转动画部分为以下代码
            case AnimationType.Rotation:
                RotationAnim rotAnim = currentDevice.GetComponent<RotationAnim>();
                if (rotAnim != null)
                {
                    // ========================================
                    // 核心修复1：速度直接读取，带详细日志
                    // ========================================
                    float speed = 30f;
                    if (!string.IsNullOrEmpty(rotateSpeedInput.text))
                    {
                        if (float.TryParse(rotateSpeedInput.text, out float parsedSpeed))
                        {
                            speed = parsedSpeed;
                        }
                    }
                    rotAnim.rotateSpeed = speed;
                    Debug.Log($"✅ [Config] 速度设置：输入框值='{rotateSpeedInput.text}' -> 解析值={speed}");

                    // ========================================
                    // 核心修复2：轴索引直接赋值
                    // ========================================
                    rotAnim.rotateAxisIndex = rotateAxisDropdown.value;
                    Debug.Log($"✅ [Config] 旋转轴设置：下拉框索引={rotateAxisDropdown.value}");

                    // ========================================
                    // 核心修复3：旋转目标直接查找，带详细日志
                    // ========================================
                    if (rotateTargetDropdown != null && _rotateTargetPathCache != null && _rotateTargetPathCache.Count > rotateTargetDropdown.value)
                    {
                        string targetPath = _rotateTargetPathCache[rotateTargetDropdown.value];
                        if (string.IsNullOrEmpty(targetPath))
                        {
                            rotAnim.rotateTarget = currentDevice.transform;
                            Debug.Log($"✅ [Config] 旋转目标设置：设备自身");
                        }
                        else
                        {
                            Transform target = currentDevice.transform.Find(targetPath);
                            if (target != null)
                            {
                                rotAnim.rotateTarget = target;
                                Debug.Log($"✅ [Config] 旋转目标设置：{targetPath} (找到物体：{target.name})");
                            }
                            else
                            {
                                rotAnim.rotateTarget = currentDevice.transform;
                                Debug.LogWarning($"⚠️ [Config] 未找到子物体：{targetPath}，使用自身");
                            }
                        }
                    }
                    else
                    {
                        rotAnim.rotateTarget = currentDevice.transform;
                        Debug.LogWarning($"⚠️ [Config] 下拉框数据异常，使用自身");
                    }

                    Debug.Log($"🎬 [Config] 旋转动画配置完成！最终参数 -> 目标：{rotAnim.rotateTarget.name} | 轴索引：{rotAnim.rotateAxisIndex} | 速度：{rotAnim.rotateSpeed}");
                }
                break;

            case AnimationType.LinearMove:
                LinearMoveAnim linearAnim = currentDevice.GetComponent<LinearMoveAnim>();
                if (linearAnim != null)
                {
                    Debug.Log($"✅ [Config] 获取到 LinearMoveAnim 组件，实例ID：{linearAnim.GetInstanceID()}");

                    // 解析输入参数（默认值兜底）
                    float moveSpeed = 1f;
                    float moveRange = 5f;
                    if (!string.IsNullOrEmpty(moveSpeedInput.text))
                    {
                        float.TryParse(moveSpeedInput.text, out moveSpeed);
                    }
                    if (!string.IsNullOrEmpty(moveRangeInput.text))
                    {
                        float.TryParse(moveRangeInput.text, out moveRange);
                    }

                    linearAnim.moveSpeed = moveSpeed;
                    linearAnim.moveRange = moveRange;
                    linearAnim.isPingPong = pingPongToggle.isOn;
                    linearAnim.isLoop = true;

                    // 设置移动方向
                    linearAnim.moveDirection = GetLinearDirection(linearDirectionDropdown.value);

                    // 设置移动模式
                    linearAnim.moveMode = (LinearMoveAnim.LinearMoveMode)linearMoveModeDropdown.value;

                    Debug.Log($"🎬 [Config] 直线移动配置完成！最终参数 -> 方向：{linearAnim.moveDirection} | 速度：{linearAnim.moveSpeed} | 范围：{linearAnim.moveRange}");
                }
                break;
            case AnimationType.PathMove:
                PathMoveAnim pathAnim = currentDevice.GetComponent<PathMoveAnim>();
                if (pathAnim != null)
                {
                    float pathSpeed = 1f;
                    float.TryParse(pathSpeedInput.text, out pathSpeed);

                    pathAnim.moveSpeed = pathSpeed;
                    pathAnim.isSmoothPath = smoothPathToggle.isOn;
                    pathAnim.isLoop = true;

                    // 修复：路径模式赋值（匹配下拉框索引）
                    switch (pathMoveModeDropdown.value)
                    {
                        case 0:
                            pathAnim.moveMode = PathMoveAnim.PathMoveMode.Once;
                            break;
                        case 1:
                            pathAnim.moveMode = PathMoveAnim.PathMoveMode.PingPong;
                            break;
                        case 2:
                            pathAnim.moveMode = PathMoveAnim.PathMoveMode.Loop;
                            break;
                    }
                    Debug.Log($"✅ 路径移动模式设置为：{pathAnim.moveMode}");
                }
                break;
        }
        // 新增：添加动画后自动保存
        AutoSaveScene();
    }

    // 移除动画
    private void OnRemoveAnimation()
    {
        currentDevice?.RemoveCurrentAnimation();
        HideAllConfigPanels();
        // 新增：添加动画后自动保存
        AutoSaveScene();
    }

    // 播放动画
    private void OnPlayAnimation()
    {
        currentDevice?.PlayCurrentAnimation();
        // 新增：播放动画后自动保存（保存播放状态）
        AutoSaveScene();
    }

    // 暂停动画
    private void OnPauseAnimation()
    {
        currentDevice?.PauseCurrentAnimation();
        // 新增：暂停动画后自动保存
        AutoSaveScene();
    }

    // 停止动画
    private void OnStopAnimation()
    {
        currentDevice?.StopCurrentAnimation();
        // 新增：停止动画后自动保存
        AutoSaveScene();
    }

    // 添加路径节点
    private void OnAddPathNode()
    {
        if (PathEditor.Instance != null)
        {
            PathEditor.Instance.AddPathNode();
        }
    }

    // 移除最后一个路径节点
    private void OnRemovePathNode()
    {
        if (PathEditor.Instance != null)
        {
            PathEditor.Instance.RemoveLastNode();
        }
    }

    // 完成路径编辑
    private void OnFinishPathEdit()
    {
        if (PathEditor.Instance != null && currentDevice != null)
        {
            Vector3[] nodes = PathEditor.Instance.EndEditPath();
            if (nodes.Length > 0)
            {
                currentDevice.SetPathNodes(nodes);
            }
        }
        // 新增：完成路径编辑后自动保存
        AutoSaveScene();
    }

    // 隐藏所有配置面板
    private void HideAllConfigPanels()
    {
        rotationConfig.SetActive(false);
        linearMoveConfig.SetActive(false);
        pathMoveConfig.SetActive(false);
    }

    // 转换旋转轴
    private Vector3 GetRotateAxis(int dropdownValue)
    {
        switch (dropdownValue)
        {
            case 0: return Vector3.right;   // X轴
            case 1: return Vector3.up;      // Y轴
            case 2: return Vector3.forward; // Z轴
            default: return Vector3.up;
        }
    }

    // 转换直线移动方向
    private Vector3 GetLinearDirection(int dropdownValue)
    {
        switch (dropdownValue)
        {
            case 0: return Vector3.forward; // 前
            case 1: return Vector3.back;    // 后
            case 2: return Vector3.left;    // 左
            case 3: return Vector3.right;   // 右
            case 4: return Vector3.up;      // 上
            case 5: return Vector3.down;    // 下
            default: return Vector3.forward;
        }
    }

    // 新增：递归查找子物体的辅助方法
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform result = FindChildRecursive(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    // 新增：自动保存方法
    private void AutoSaveScene()
    {
        if (ScenePersistenceManager.Instance != null)
        {
            // 延迟0.5秒保存，避免频繁保存
            Invoke(nameof(DelayedSave), 0.5f);
        }
    }

    private void DelayedSave()
    {
        if (ScenePersistenceManager.Instance != null)
        {
            ScenePersistenceManager.Instance.SaveAllDevices();
            Debug.Log("💾 场景已自动保存");
        }
    }

    // 切换路径显示/隐藏
    private void OnTogglePathVisibility()
    {
        if (currentDevice != null)
        {
            currentDevice.ToggleCurrentPathVisibility();
        }
    }

    // 删除当前路径
    private void OnDeletePath()
    {
        if (currentDevice != null)
        {
            currentDevice.DeleteCurrentPath();
            // 清空路径编辑状态
            if (PathEditor.Instance != null)
            {
                PathEditor.Instance.ClearCurrentEditState();
            }
            AutoSaveScene(); // 删除路径后自动保存
        }
    }
    /// <summary>
    /// 递归获取所有子物体，生成带层级路径的选项
    /// </summary>
    /// <param name="parent">父级Transform</param>
    /// <param name="parentPath">父级相对路径</param>
    /// <param name="optionDatas">下拉框选项列表</param>
    /// <param name="targetPaths">路径缓存列表</param>
    private void GetAllChildTransforms(Transform parent, string parentPath, ref List<TMP_Dropdown.OptionData> optionDatas, ref List<string> targetPaths)
    {
        // 遍历当前父级下的所有直接子物体
        foreach (Transform child in parent)
        {
            // 生成当前子物体的相对路径
            string currentPath = string.IsNullOrEmpty(parentPath)
                ? child.name
                : $"{parentPath}/{child.name}";

            // 添加到选项和缓存
            optionDatas.Add(new TMP_Dropdown.OptionData(currentPath));
            targetPaths.Add(currentPath);

            // 递归遍历子物体的子物体（深层级）
            GetAllChildTransforms(child, currentPath, ref optionDatas, ref targetPaths);
        }
    }
    // <summary>
    /// 递归获取所有子物体路径
    /// </summary>
    private void GetAllChildPaths(Transform parent, string parentPath, ref List<TMP_Dropdown.OptionData> options, ref List<string> paths)
    {
        foreach (Transform child in parent)
        {
            string currentPath = string.IsNullOrEmpty(parentPath) ? child.name : $"{parentPath}/{child.name}";
            options.Add(new TMP_Dropdown.OptionData(currentPath));
            paths.Add(currentPath);

            // 递归
            GetAllChildPaths(child, currentPath, ref options, ref paths);
        }
    }
}