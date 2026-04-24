using UnityEngine;

/// <summary>
/// 通用动画接口（所有动画组件必须实现）
/// </summary>
public interface ICustomAnimation
{
    // 动画类型标识（用于序列化）
    AnimationType AnimType { get; }
    // 动画运行状态
    bool IsPlaying { get; }
    // 启动动画
    void Play();
    // 暂停动画
    void Pause();
    // 停止动画（重置状态）
    void Stop();
    // 序列化动画参数（保存时调用）
    AnimationData GetAnimData();
    // 反序列化动画参数（加载时调用）
    void SetAnimData(AnimationData data);
}

/// <summary>
/// 动画类型枚举（用于序列化区分）
/// </summary>
public enum AnimationType
{
    None,
    Rotation,       // 自由旋转
    LinearMove,     // 匀速直线移动
    PathMove        // 沿路径移动
}

/// <summary>
/// 动画数据容器（序列化用，保存所有动画的参数）
/// </summary>
[System.Serializable]
public class AnimationData
{
    public AnimationType animType;          // 动画类型
    public bool isPlaying;                  // 播放状态
    // 通用参数
    public float speed;                     // 速度（旋转/移动）
    public bool isLoop;                     // 是否循环
    // 旋转专属
    public Vector3 rotateAxis;              // 旋转轴
    // 直线移动专属
    public Vector3 moveDirection;           // 移动方向
    public float moveRange;                 // 移动范围（往返用）
    public bool isPingPong;                 // 是否往返
    // 路径移动专属
    public Vector3[] pathNodes;             // 路径节点坐标
    public bool isSmoothPath;               // 是否平滑路径（贝塞尔）
    // 新增：旋转目标路径（子物体）
    public string rotateTargetPath;
    // 新增：直线移动枚举字段
    public LinearMoveAnim.LinearMoveDirection linearMoveDirection;
    public LinearMoveAnim.LinearMoveMode linearMoveMode;
    // 新增：路径移动模式
    public PathMoveAnim.PathMoveMode pathMoveMode;
    public int rotateAxisIndex; // 新增：轴索引（0=X,1=Y,2=Z）
}