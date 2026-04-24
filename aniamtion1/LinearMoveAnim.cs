using UnityEngine;

public class LinearMoveAnim : MonoBehaviour, ICustomAnimation
{
    public AnimationType AnimType => AnimationType.LinearMove;
    public bool IsPlaying { get; private set; }

    [Header("直线移动配置")]
    public Vector3 moveDirection = Vector3.forward; // 移动方向
    public float moveSpeed = 1f;
    public float moveRange = 5f;
    public bool isLoop = true;
    public bool isPingPong = true;

    // 新增：运动模式枚举
    public enum LinearMoveMode
    {
        Once,       // 单次：到终点停止
        PingPong,   // 来回：到终点后退
        Loop        // 重复：到终点回到起点
    }

    // 新增：运动模式字段
    public LinearMoveMode moveMode = LinearMoveMode.Loop;

    private Vector3 startLocalPos; // 初始位置
    private float moveDistance = 0f;
    private int moveDirectionSign = 1; // 1=前进，-1=后退

    private void Start()
    {
        startLocalPos = transform.localPosition;
    }

    private void Update()
    {
        if (!IsPlaying) return;

        // 移动逻辑
        Vector3 delta = moveDirectionSign * moveDirection.normalized * moveSpeed * Time.deltaTime;
        transform.Translate(delta, Space.Self);
        moveDistance += delta.magnitude;

        // 模式判断
        if (moveDistance >= moveRange)
        {
            switch (moveMode)
            {
                case LinearMoveMode.Once:
                    Stop();
                    break;
                case LinearMoveMode.PingPong:
                    moveDirectionSign *= -1;
                    moveDistance = 0;
                    break;
                case LinearMoveMode.Loop:
                    transform.localPosition = startLocalPos;
                    moveDistance = 0;
                    break;
            }
        }
    }

    public void Play()
    {
        IsPlaying = true;
        startLocalPos = transform.localPosition;
        moveDistance = 0;
        moveDirectionSign = 1;
    }

    public void Pause()
    {
        IsPlaying = false;
    }

    public void Stop()
    {
        IsPlaying = false;
        transform.localPosition = startLocalPos; // 复位到初始位置
        moveDistance = 0;
        moveDirectionSign = 1;
    }

    public AnimationData GetAnimData()
    {
        return new AnimationData
        {
            animType = AnimType,
            isPlaying = IsPlaying,
            speed = moveSpeed,
            isLoop = isLoop,
            moveDirection = moveDirection,
            moveRange = moveRange,
            isPingPong = isPingPong,
            linearMoveDirection = GetLinearDirectionEnum(moveDirection),
            linearMoveMode = moveMode
        };
    }

    public void SetAnimData(AnimationData data)
    {
        moveSpeed = data.speed;
        isLoop = data.isLoop;
        moveDirection = data.moveDirection;
        moveRange = data.moveRange;
        isPingPong = data.isPingPong;
        moveMode = data.linearMoveMode;
        IsPlaying = data.isPlaying;
    }

    // 辅助：将Vector3方向转换为枚举（用于序列化）
    private LinearMoveAnim.LinearMoveDirection GetLinearDirectionEnum(Vector3 dir)
    {
        if (dir == Vector3.forward) return LinearMoveDirection.Forward;
        if (dir == Vector3.back) return LinearMoveDirection.Back;
        if (dir == Vector3.left) return LinearMoveDirection.Left;
        if (dir == Vector3.right) return LinearMoveDirection.Right;
        if (dir == Vector3.up) return LinearMoveDirection.Up;
        if (dir == Vector3.down) return LinearMoveDirection.Down;
        return LinearMoveDirection.Forward;
    }

    // 新增：方向枚举（适配UI选择）
    public enum LinearMoveDirection
    {
        Forward, Back, Left, Right, Up, Down
    }
}