using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SceneManager1 : MonoBehaviour
{
    public static SceneManager1 Instance;

    private string savePath;

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 设置保存路径
        savePath = Application.persistentDataPath + "/Scenes/";

        // 创建目录（如果不存在）
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
    }

    // 保存场景
    public void SaveScene(string sceneName)
    {
        SceneData data = new SceneData();
        data.sceneName = sceneName;
        data.saveDate = DateTime.Now.ToString();

        // 收集所有设备状态
        foreach (var device in DeviceLibraryManager.Instance.GetAllSpawnedDevices())
        {
            DeviceController controller = device.GetComponent<DeviceController>();
            if (controller != null)
            {
                DeviceStateData state = new DeviceStateData();
                state.deviceID = controller.GetDeviceData().deviceID;
                state.position = device.transform.position;
                state.rotation = device.transform.eulerAngles;
                state.scale = device.transform.localScale;

                data.deviceStates.Add(state);
            }
        }

        // 序列化为JSON
        string json = JsonUtility.ToJson(data, true);

        // 保存文件
        File.WriteAllText(savePath + sceneName + ".json", json);

        Debug.Log("场景保存成功: " + savePath + sceneName + ".json");
    }

    // 加载场景
    public void LoadScene(string sceneName)
    {
        string filePath = savePath + sceneName + ".json";

        if (!File.Exists(filePath))
        {
            Debug.LogError("场景文件不存在: " + filePath);
            return;
        }

        // 读取文件
        string json = File.ReadAllText(filePath);

        // 解析JSON
        SceneData data = JsonUtility.FromJson<SceneData>(json);

        if (data == null)
        {
            Debug.LogError("场景数据解析失败");
            return;
        }

        // 清除当前场景中的设备
        DeviceLibraryManager.Instance.ClearAllDevices();

        // 加载所有设备
        foreach (var state in data.deviceStates)
        {
            GameObject device = DeviceLibraryManager.Instance.SpawnDevice(state.deviceID, state.position);
            if (device != null)
            {
                device.transform.eulerAngles = state.rotation;
                device.transform.localScale = state.scale;
            }
        }

        Debug.Log("场景加载成功: " + sceneName);
    }

    // 获取所有保存的场景名称
    public List<string> GetSavedScenes()
    {
        List<string> sceneNames = new List<string>();

        if (Directory.Exists(savePath))
        {
            string[] files = Directory.GetFiles(savePath, "*.json");
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                sceneNames.Add(fileName);
            }
        }

        return sceneNames;
    }

    // 删除场景
    public bool DeleteScene(string sceneName)
    {
        string filePath = savePath + sceneName + ".json";

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }

        return false;
    }
}