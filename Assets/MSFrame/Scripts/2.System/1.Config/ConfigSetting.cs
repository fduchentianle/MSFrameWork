using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace MSFrame
{

/// <summary>
/// 配置基类 角色配置、武器配置
/// </summary>
public class ConfigBase : SerializedScriptableObject { }

/// <summary>
/// 游戏中配置：游戏运行时只有一个
/// 包含所有配置文件
/// </summary>
[CreateAssetMenu(fileName ="ConfigSetting", menuName = "MSFrame/ConfigSetting")]
public class ConfigSetting : SerializedScriptableObject
{
    [DictionaryDrawerSettings(KeyLabel = "类型", ValueLabel = "列表")]
    public Dictionary<string, Dictionary<int, ConfigBase>> configDict;

    /// <summary>
    /// 获取配置
    /// </summary>
    /// <typeparam name="T">具体配置类型</typeparam>
    /// <param name="configTypeName">配置类型名称</param>
    /// <param name="id">id</param>
    /// <returns></returns>
    public T GetConfig<T>(string configTypeName, int id) where T : ConfigBase
    {
        if (!configDict.ContainsKey(configTypeName))
        {
            Debug.LogWarning("MSFrame: 配置文件中不包括这个Key" + configTypeName);
            return null;
        }
        else if (!configDict[configTypeName].ContainsKey(id))
        {
            Debug.LogWarning("MSFrame: 配置文件中包括Key" + configTypeName + ",但不包括这个ID" + id);
            return null;
        }
        return configDict[configTypeName][id] as T;
    }
}
}
