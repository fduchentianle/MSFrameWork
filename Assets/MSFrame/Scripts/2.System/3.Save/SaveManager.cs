using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.Serialization;
using UnityEngine;

namespace MSFrame
{

/// <summary>
/// 存档管理器
/// </summary>
public class SaveManager : BaseManager<SaveManager>
{
    /// <summary>
    /// 存档管理器数据，用于记录存档编号和所有存档信息
    /// </summary>
    [Serializable]
    private class SaveManagerData
    {
        public int currentID;
        public List<SaveItem> saveItemList = new List<SaveItem>();
    }

    #region 基础设置
    private const string saveDirName = "saveData";
    private const string settingDirName = "setting";
    private const string jsonExtension = ".json";

    private readonly string saveDirPath;
    private readonly string settingDirPath;
    private readonly Dictionary<int, Dictionary<string, object>> cacheDic = new Dictionary<int, Dictionary<string, object>>();

    private SaveManagerData saveManagerData;

    private SaveManager()
    {
        saveDirPath = Path.Combine(Application.persistentDataPath, saveDirName);
        settingDirPath = Path.Combine(Application.persistentDataPath, settingDirName);

        if (!Directory.Exists(saveDirPath))
            Directory.CreateDirectory(saveDirPath);
        if (!Directory.Exists(settingDirPath))
            Directory.CreateDirectory(settingDirPath);

        InitSaveManagerData();
    }
    #endregion

    #region 存档对象
    /// <summary>
    /// 保存对象到指定存档
    /// </summary>
    public void SaveObject(object saveObject, string saveFileName, int saveID = 0)
    {
        string dirPath = GetSavePath(saveID);
        if (dirPath == null)
            return;

        SaveFile(saveObject, Path.Combine(dirPath, saveFileName));
        GetSaveItem(saveID).UpdateTime(DateTime.Now);
        UpdateSaveManagerData();
        SetCache(saveID, saveFileName, saveObject);
    }

    /// <summary>
    /// 保存对象到指定存档
    /// </summary>
    public void SaveObject(object saveObject, string saveFileName, SaveItem saveItem)
    {
        SaveObject(saveObject, saveFileName, saveItem.saveID);
    }

    /// <summary>
    /// 使用对象类型名作为文件名保存
    /// </summary>
    public void SaveObject(object saveObject, int saveID = 0)
    {
        SaveObject(saveObject, saveObject.GetType().Name, saveID);
    }

    /// <summary>
    /// 使用对象类型名作为文件名保存
    /// </summary>
    public void SaveObject(object saveObject, SaveItem saveItem)
    {
        SaveObject(saveObject, saveObject.GetType().Name, saveItem.saveID);
    }

    /// <summary>
    /// 从指定存档加载对象
    /// </summary>
    public T LoadObject<T>(string saveFileName, int saveID = 0) where T : class
    {
        T obj = GetCache<T>(saveID, saveFileName);
        if (obj != null)
            return obj;

        string dirPath = GetSavePath(saveID, false);
        if (dirPath == null)
            return null;

        obj = LoadFile<T>(Path.Combine(dirPath, saveFileName));
        if (obj != null)
            SetCache(saveID, saveFileName, obj);
        return obj;
    }

    /// <summary>
    /// 从指定存档加载对象
    /// </summary>
    public T LoadObject<T>(string saveFileName, SaveItem saveItem) where T : class
    {
        return LoadObject<T>(saveFileName, saveItem.saveID);
    }

    /// <summary>
    /// 使用对象类型名作为文件名加载
    /// </summary>
    public T LoadObject<T>(int saveID = 0) where T : class
    {
        return LoadObject<T>(typeof(T).Name, saveID);
    }

    /// <summary>
    /// 使用对象类型名作为文件名加载
    /// </summary>
    public T LoadObject<T>(SaveItem saveItem) where T : class
    {
        return LoadObject<T>(typeof(T).Name, saveItem.saveID);
    }
    #endregion

    #region 全局设置
    /// <summary>
    /// 保存全局设置
    /// </summary>
    public void SaveSetting(object settingObject, string fileName)
    {
        SaveFile(settingObject, Path.Combine(settingDirPath, fileName));
    }

    /// <summary>
    /// 使用设置类型名作为文件名保存
    /// </summary>
    public void SaveSetting(object settingObject)
    {
        SaveSetting(settingObject, settingObject.GetType().Name);
    }

    /// <summary>
    /// 加载全局设置
    /// </summary>
    public T LoadSetting<T>(string fileName) where T : class
    {
        return LoadFile<T>(Path.Combine(settingDirPath, fileName));
    }

    /// <summary>
    /// 使用设置类型名作为文件名加载
    /// </summary>
    public T LoadSetting<T>() where T : class
    {
        return LoadSetting<T>(typeof(T).Name);
    }
    #endregion

    #region 存档管理
    /// <summary>
    /// 创建一个自动编号的新存档
    /// </summary>
    public SaveItem CreateSaveItem()
    {
        SaveItem saveItem = new SaveItem(saveManagerData.currentID, DateTime.Now);
        saveManagerData.saveItemList.Add(saveItem);
        saveManagerData.currentID++;
        UpdateSaveManagerData();
        return saveItem;
    }

    /// <summary>
    /// 根据编号获取存档
    /// </summary>
    public SaveItem GetSaveItem(int saveID)
    {
        for (int i = 0; i < saveManagerData.saveItemList.Count; i++)
        {
            if (saveManagerData.saveItemList[i].saveID == saveID)
                return saveManagerData.saveItemList[i];
        }
        return null;
    }

    /// <summary>
    /// 删除指定存档
    /// </summary>
    public void DeleteSaveItem(int saveID)
    {
        string itemDir = GetSavePath(saveID, false);
        if (itemDir != null)
            Directory.Delete(itemDir, true);

        saveManagerData.saveItemList.Remove(GetSaveItem(saveID));
        RemoveCache(saveID);
        UpdateSaveManagerData();
    }

    /// <summary>
    /// 删除指定存档
    /// </summary>
    public void DeleteSaveItem(SaveItem saveItem)
    {
        DeleteSaveItem(saveItem.saveID);
    }
    #endregion

    #region 存档查询
    /// <summary>
    /// 获取所有存档，创建时间从旧到新
    /// </summary>
    public List<SaveItem> GetAllSaveItem()
    {
        return saveManagerData.saveItemList;
    }

    /// <summary>
    /// 获取所有存档，创建时间从新到旧
    /// </summary>
    public List<SaveItem> GetAllSaveItemByCreateTime()
    {
        List<SaveItem> saveItems = new List<SaveItem>(saveManagerData.saveItemList);
        saveItems.Reverse();
        return saveItems;
    }

    /// <summary>
    /// 根据指定条件排序存档
    /// </summary>
    public List<SaveItem> GetAllSaveItem<T>(Func<SaveItem, T> orderFunc, bool isDescending = false)
    {
        return isDescending
            ? saveManagerData.saveItemList.OrderByDescending(orderFunc).ToList()
            : saveManagerData.saveItemList.OrderBy(orderFunc).ToList();
    }

    /// <summary>
    /// 获取所有存档，最后保存时间从新到旧
    /// </summary>
    public List<SaveItem> GetAllSaveItemByUpdateTime()
    {
        List<SaveItem> saveItems = new List<SaveItem>(saveManagerData.saveItemList);
        saveItems.Sort((a, b) => b.lastSaveTime.CompareTo(a.lastSaveTime));
        return saveItems;
    }

    /// <summary>
    /// 保存存档管理器索引数据
    /// </summary>
    public void UpdateSaveManagerData()
    {
        SaveFile(saveManagerData, Path.Combine(saveDirPath, "SaveManagerData"));
    }
    #endregion

    #region 文件读写工具
    private void SaveFile(object saveObject, string path)
    {
        path = GetJsonFilePath(path);
        SerializationContext context = new SerializationContext();
        context.Config.SerializationPolicy = SerializationPolicies.Everything;

        using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write))
        {
            SerializationUtility.SerializeValueWeak(saveObject, file, DataFormat.JSON, context);
        }
    }

    private T LoadFile<T>(string path) where T : class
    {
        path = GetJsonFilePath(path);
        if (!File.Exists(path))
            return null;

        DeserializationContext context = new DeserializationContext();
        context.Config.SerializationPolicy = SerializationPolicies.Everything;

        using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            return SerializationUtility.DeserializeValue<T>(file, DataFormat.JSON, context);
        }
    }

    private string GetJsonFilePath(string path)
    {
        return path.EndsWith(jsonExtension, StringComparison.OrdinalIgnoreCase)
            ? path
            : path + jsonExtension;
    }

    private string GetSavePath(int saveID, bool createDir = true)
    {
        if (GetSaveItem(saveID) == null)
        {
            Debug.LogWarning("MSFrame: saveID存档不存在");
            return null;
        }

        string saveDir = Path.Combine(saveDirPath, saveID.ToString());
        if (!Directory.Exists(saveDir))
        {
            if (!createDir)
                return null;
            Directory.CreateDirectory(saveDir);
        }
        return saveDir;
    }

    private void InitSaveManagerData()
    {
        saveManagerData = LoadFile<SaveManagerData>(Path.Combine(saveDirPath, "SaveManagerData"));
        if (saveManagerData == null)
        {
            saveManagerData = new SaveManagerData();
            UpdateSaveManagerData();
        }
    }
    #endregion

    #region 缓存工具
    private void SetCache(int saveID, string fileName, object saveObject)
    {
        if (!cacheDic.ContainsKey(saveID))
            cacheDic.Add(saveID, new Dictionary<string, object>());

        cacheDic[saveID][fileName] = saveObject;
    }

    private T GetCache<T>(int saveID, string fileName) where T : class
    {
        if (cacheDic.ContainsKey(saveID) && cacheDic[saveID].ContainsKey(fileName))
            return cacheDic[saveID][fileName] as T;
        return null;
    }

    private void RemoveCache(int saveID)
    {
        cacheDic.Remove(saveID);
    }
    #endregion
}

/// <summary>
/// 一个完整存档槽位的信息
/// </summary>
[Serializable]
public class SaveItem
{
    public int saveID { get; private set; }
    public DateTime lastSaveTime { get; private set; }

    public SaveItem(int saveID, DateTime lastSaveTime)
    {
        this.saveID = saveID;
        this.lastSaveTime = lastSaveTime;
    }

    public void UpdateTime(DateTime lastSaveTime)
    {
        this.lastSaveTime = lastSaveTime;
    }
}
}
