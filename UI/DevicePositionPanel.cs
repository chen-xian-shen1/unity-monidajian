using UnityEngine;
using UnityEngine.UI;

public class DevicePositionPanel : MonoBehaviour
{
    public static DevicePositionPanel Instance;

    [Header("位置控制")]
    public InputField xInput;
    public InputField yInput;
    public InputField zInput;

    [Header("旋转控制")]
    public InputField rotXInput;
    public InputField rotYInput;
    public InputField rotZInput;

    [Header("缩放控制")]
    public InputField scaleXInput;
    public InputField scaleYInput;
    public InputField scaleZInput;

    [Header("按钮")]
    public Button applyButton;
    public Button closeButton;

    private DeviceController selectedDevice;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        gameObject.SetActive(false);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplyTransform);

        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);
    }

    public void ShowPanel(DeviceController device)
    {
        selectedDevice = device;
        if (selectedDevice == null) return;

        UpdateInputFields(); // 打开面板时同步一次
        gameObject.SetActive(true);
    }

    public void HidePanel()
    {
        gameObject.SetActive(false);
        selectedDevice = null;
    }

    // 关键修改1：改为public，允许外部（如DeviceController）调用同步输入框
    public void UpdateInputFields()
    {
        if (selectedDevice == null) return;

        Vector3 position = selectedDevice.transform.position;
        Vector3 rotation = selectedDevice.transform.eulerAngles;
        Vector3 scale = selectedDevice.transform.localScale;

        // 实时更新位置输入框（解决核心同步问题）
        xInput.text = position.x.ToString("F2");
        yInput.text = position.y.ToString("F2");
        zInput.text = position.z.ToString("F2");

        rotXInput.text = rotation.x.ToString("F2");
        rotYInput.text = rotation.y.ToString("F2");
        rotZInput.text = rotation.z.ToString("F2");

        scaleXInput.text = scale.x.ToString("F2");
        scaleYInput.text = scale.y.ToString("F2");
        scaleZInput.text = scale.z.ToString("F2");
    }

    private void ApplyTransform()
    {
        if (selectedDevice == null) return;

        try
        {
            // 解析输入值
            float x = float.Parse(xInput.text);
            float y = float.Parse(yInput.text);
            float z = float.Parse(zInput.text);
            float rotX = float.Parse(rotXInput.text);
            float rotY = float.Parse(rotYInput.text);
            float rotZ = float.Parse(rotZInput.text);
            float scaleX = float.Parse(scaleXInput.text);
            float scaleY = float.Parse(scaleYInput.text);
            float scaleZ = float.Parse(scaleZInput.text);

            // 限制缩放范围
            scaleX = Mathf.Clamp(scaleX, 0.1f, 10f);
            scaleY = Mathf.Clamp(scaleY, 0.1f, 10f);
            scaleZ = Mathf.Clamp(scaleZ, 0.1f, 10f);

            // 应用变换
            selectedDevice.SetPosition(new Vector3(x, y, z));
            selectedDevice.SetRotation(new Vector3(rotX, rotY, rotZ));
            selectedDevice.SetScale(new Vector3(scaleX, scaleY, scaleZ));

            // 应用后再次同步（防止计算误差导致显示不一致）
            UpdateInputFields();

            // 保存修改
            if (ScenePersistenceManager.Instance != null)
            {
                ScenePersistenceManager.Instance.SaveAllDevices();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("输入格式错误: " + e.Message);
            // 关键修改2：输入错误时也同步输入框，避免显示旧值
            UpdateInputFields();
        }
    }
}
