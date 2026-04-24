using UnityEngine;

public class PathMoveAnim : MonoBehaviour, ICustomAnimation
{
    public AnimationType AnimType => AnimationType.PathMove;
    public bool IsPlaying { get; private set; }

    [Header("路径移动配置")]
    public float moveSpeed = 1f;
    public bool isLoop = true;
    public PathMoveMode moveMode = PathMoveMode.Loop;
    public bool isSmoothPath = true;
    public Vector3[] pathNodes; // 存储：设备父物体空间下的局部坐标

    // 新增：简单的自动转头配置
    [Header("自动转头（最小化）")]
    public bool autoFacePath = true;
    public float turnSpeed = 5f; // 转头速度

    // 核心运行时变量（完全保持你原来的）
    private int currentSegmentIndex = 0;
    private float segmentProgress = 0f;
    private int moveDirection = 1;
    private Vector3 deviceStartLocalPos;
    private Quaternion deviceStartLocalRot; // 新增：记录初始旋转
    private int maxNodeIndex;

    public enum PathMoveMode
    {
        Once,
        PingPong,
        Loop
    }

    private void Start()
    {
        // 完全保持你原来的逻辑
        deviceStartLocalPos = transform.localPosition;
        deviceStartLocalRot = transform.localRotation; // 新增：记录初始旋转
        if (pathNodes != null)
        {
            maxNodeIndex = pathNodes.Length - 1;
        }
    }

    private void Update()
    {
        // 完全保持你原来的核心校验
        if (!IsPlaying || pathNodes == null || pathNodes.Length < 2) return;

        // 完全保持你原来的边界保护
        maxNodeIndex = pathNodes.Length - 1;
        int maxSegmentIndex = maxNodeIndex - 1;
        currentSegmentIndex = Mathf.Clamp(currentSegmentIndex, 0, maxSegmentIndex);

        // 完全保持你原来的路段起点终点获取
        Vector3 segmentStart;
        Vector3 segmentEnd;

        if (moveDirection == 1)
        {
            segmentStart = pathNodes[currentSegmentIndex];
            segmentEnd = pathNodes[currentSegmentIndex + 1];
        }
        else
        {
            segmentStart = pathNodes[currentSegmentIndex + 1];
            segmentEnd = pathNodes[currentSegmentIndex];
        }

        // 完全保持你原来的距离计算
        float segmentDistance = Vector3.Distance(segmentStart, segmentEnd);
        if (segmentDistance <= 0.01f)
        {
            currentSegmentIndex += moveDirection;
            segmentProgress = 0f;
            return;
        }

        // 完全保持你原来的进度更新
        segmentProgress += moveSpeed * Time.deltaTime / segmentDistance;
        segmentProgress = Mathf.Clamp01(segmentProgress);

        // 完全保持你原来的位置计算
        Vector3 targetLocalPos;
        if (isSmoothPath && pathNodes.Length > 2)
        {
            targetLocalPos = moveDirection == 1
                ? GetCatmullRomSmoothPos(currentSegmentIndex, segmentProgress)
                : GetCatmullRomSmoothPos(currentSegmentIndex, 1 - segmentProgress);
        }
        else
        {
            targetLocalPos = Vector3.Lerp(segmentStart, segmentEnd, segmentProgress);
        }

        // 完全保持你原来的位置设置
        transform.localPosition = targetLocalPos;

        // ========================================
        // 唯一新增：最小化的自动转头逻辑
        // ========================================
        if (autoFacePath)
        {
            // 计算下一个位置（用于获取方向）
            float nextProgress = Mathf.Clamp01(segmentProgress + 0.01f);
            Vector3 nextPos;

            if (isSmoothPath && pathNodes.Length > 2)
            {
                nextPos = moveDirection == 1
                    ? GetCatmullRomSmoothPos(currentSegmentIndex, nextProgress)
                    : GetCatmullRomSmoothPos(currentSegmentIndex, 1 - nextProgress);
            }
            else
            {
                nextPos = Vector3.Lerp(segmentStart, segmentEnd, nextProgress);
            }

            // 计算方向并转头（适配X轴正方向）
            Vector3 direction = (nextPos - targetLocalPos).normalized;
            if (direction != Vector3.zero)
            {
                // 核心修改：适配X轴正方向
                Quaternion pathRot = Quaternion.LookRotation(direction, Vector3.up);
                Quaternion offsetRot = Quaternion.Euler(0, 90, 0); // Y轴转90度
                Quaternion targetRot = pathRot * offsetRot;
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * turnSpeed);
            }
        }

        // 完全保持你原来的路段完成处理
        if (segmentProgress >= 1f)
        {
            currentSegmentIndex += moveDirection;
            segmentProgress = 0f;
            HandleModeBoundary();
        }
    }

    // 完全保持你原来的 HandleModeBoundary
    private void HandleModeBoundary()
    {
        maxNodeIndex = pathNodes.Length - 1;
        int maxSegmentIndex = maxNodeIndex - 1;

        switch (moveMode)
        {
            case PathMoveMode.Once:
                if (currentSegmentIndex > maxSegmentIndex)
                {
                    Stop();
                }
                break;

            case PathMoveMode.PingPong:
                if (currentSegmentIndex > maxSegmentIndex)
                {
                    moveDirection = -1;
                    currentSegmentIndex = maxSegmentIndex;
                    segmentProgress = 0f;
                }
                else if (currentSegmentIndex < 0)
                {
                    moveDirection = 1;
                    currentSegmentIndex = 0;
                    segmentProgress = 0f;
                }
                break;

            case PathMoveMode.Loop:
                if (currentSegmentIndex > maxSegmentIndex)
                {
                    currentSegmentIndex = 0;
                    transform.localPosition = pathNodes[0];
                    segmentProgress = 0f;
                }
                break;
        }
    }

    // 完全保持你原来的 Catmull-Rom
    private Vector3 GetCatmullRomSmoothPos(int segmentIndex, float progress)
    {
        maxNodeIndex = pathNodes.Length - 1;
        Vector3 p0 = pathNodes[Mathf.Max(0, segmentIndex - 1)];
        Vector3 p1 = pathNodes[segmentIndex];
        Vector3 p2 = pathNodes[Mathf.Min(maxNodeIndex, segmentIndex + 1)];
        Vector3 p3 = pathNodes[Mathf.Min(maxNodeIndex, segmentIndex + 2)];

        float t = progress;
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    #region 接口实现（完全保持你原来的，只加了旋转复位）
    public void Play()
    {
        if (pathNodes == null || pathNodes.Length < 2)
        {
            return;
        }

        IsPlaying = true;
        maxNodeIndex = pathNodes.Length - 1;
        currentSegmentIndex = 0;
        segmentProgress = 0f;
        moveDirection = 1;
        transform.localPosition = pathNodes[0];
        // 注意：这里不改变旋转，保持初始方向
    }

    public void Pause()
    {
        IsPlaying = false;
    }

    public void Stop()
    {
        IsPlaying = false;
        currentSegmentIndex = 0;
        segmentProgress = 0f;
        moveDirection = 1;
        transform.localPosition = deviceStartLocalPos;
        transform.localRotation = deviceStartLocalRot; // 核心：复位到初始旋转
    }

    public AnimationData GetAnimData()
    {
        return new AnimationData
        {
            animType = AnimType,
            isPlaying = IsPlaying,
            speed = moveSpeed,
            isLoop = isLoop,
            pathNodes = pathNodes,
            isSmoothPath = isSmoothPath,
            pathMoveMode = moveMode
        };
    }

    public void SetAnimData(AnimationData data)
    {
        moveSpeed = data.speed;
        isLoop = data.isLoop;
        pathNodes = data.pathNodes;
        isSmoothPath = data.isSmoothPath;
        moveMode = data.pathMoveMode;
        IsPlaying = data.isPlaying;

        if (pathNodes != null)
        {
            maxNodeIndex = pathNodes.Length - 1;
        }

        if (pathNodes != null && pathNodes.Length > 0 && PathEditor.Instance != null)
        {
            PathEditor.Instance.LoadPathNodes(transform, pathNodes);
        }
    }

    public void SetPathNodes(Vector3[] nodes)
    {
        pathNodes = nodes;
        deviceStartLocalPos = transform.localPosition;
        deviceStartLocalRot = transform.localRotation; // 记录初始旋转
        if (nodes != null)
        {
            maxNodeIndex = nodes.Length - 1;
        }
    }
    #endregion
}