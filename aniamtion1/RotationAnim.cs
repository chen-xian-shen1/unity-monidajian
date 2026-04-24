using UnityEngine;

public class RotationAnim : MonoBehaviour, ICustomAnimation
{
    public AnimationType AnimType => AnimationType.Rotation;
    public bool IsPlaying { get; private set; }

    [Header("旋转配置")]
    public Transform rotateTarget;
    public float rotateSpeed = 30f;
    public int rotateAxisIndex = 1; // 0=X, 1=Y, 2=Z

    // 兼容字段
    public Vector3 rotateAxis = Vector3.up;
    public bool isLoop = true;

    private Quaternion _startRotation;
    private Transform _defaultTarget;
    private float _lastLogTime;

    private void Awake()
    {
        _defaultTarget = transform;
        if (rotateTarget == null)
        {
            rotateTarget = _defaultTarget;
        }
        _startRotation = rotateTarget.localRotation;
        Debug.Log($"🎬 [RotationAnim] Awake 执行 | 实例ID：{GetInstanceID()} | 目标：{rotateTarget.name}");
    }

    private void Update()
    {
        if (!IsPlaying || rotateTarget == null) return;

        // 每秒输出一次运行日志，确认参数
        if (Time.time - _lastLogTime > 1f)
        {
            Debug.Log($"🎬 [RotationAnim] 运行中 | 目标：{rotateTarget.name} | 轴索引：{rotateAxisIndex} | 速度：{rotateSpeed}");
            _lastLogTime = Time.time;
        }

        // 极简旋转逻辑
        switch (rotateAxisIndex)
        {
            case 0:
                rotateTarget.Rotate(Vector3.right, rotateSpeed * Time.deltaTime, Space.Self);
                break;
            case 1:
                rotateTarget.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.Self);
                break;
            case 2:
                rotateTarget.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime, Space.Self);
                break;
        }
    }

    public void Play()
    {
        IsPlaying = true;
        if (rotateTarget == null) rotateTarget = _defaultTarget;
        _startRotation = rotateTarget.localRotation;
        _lastLogTime = Time.time;
        Debug.Log($"🎬 [RotationAnim] Play 执行 | 目标：{rotateTarget.name} | 轴索引：{rotateAxisIndex} | 速度：{rotateSpeed}");
    }

    public void Pause() => IsPlaying = false;

    public void Stop()
    {
        IsPlaying = false;
        if (rotateTarget != null)
        {
            rotateTarget.localRotation = _startRotation;
        }
        Debug.Log($"🎬 [RotationAnim] Stop 执行");
    }

    public AnimationData GetAnimData()
    {
        return new AnimationData
        {
            animType = AnimType,
            isPlaying = IsPlaying,
            speed = rotateSpeed,
            isLoop = isLoop,
            rotateAxis = rotateAxis,
            rotateAxisIndex = rotateAxisIndex,
            rotateTargetPath = GetTransformPath(rotateTarget)
        };
    }

    public void SetAnimData(AnimationData data)
    {
        Debug.Log($"🎬 [RotationAnim] SetAnimData 开始 | 传入速度：{data.speed} | 传入轴索引：{data.rotateAxisIndex}");

        rotateSpeed = data.speed > 0 ? data.speed : 30f;

        if (data.rotateAxisIndex >= 0 && data.rotateAxisIndex <= 2)
        {
            rotateAxisIndex = data.rotateAxisIndex;
        }
        else
        {
            if (data.rotateAxis == Vector3.right) rotateAxisIndex = 0;
            else if (data.rotateAxis == Vector3.forward) rotateAxisIndex = 2;
            else rotateAxisIndex = 1;
        }

        isLoop = data.isLoop;
        rotateAxis = data.rotateAxis;
        IsPlaying = data.isPlaying;

        if (!string.IsNullOrEmpty(data.rotateTargetPath))
        {
            rotateTarget = transform.Find(data.rotateTargetPath);
        }
        if (rotateTarget == null)
        {
            rotateTarget = _defaultTarget;
        }

        _startRotation = rotateTarget.localRotation;
        Debug.Log($"🎬 [RotationAnim] SetAnimData 完成 | 最终目标：{rotateTarget.name} | 最终轴索引：{rotateAxisIndex} | 最终速度：{rotateSpeed}");
    }

    private string GetTransformPath(Transform target)
    {
        if (target == transform || target == null) return "";
        string path = target.name;
        Transform current = target.parent;
        while (current != transform && current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}