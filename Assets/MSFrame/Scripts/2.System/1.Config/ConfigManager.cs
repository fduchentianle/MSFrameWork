using UnityEngine;

namespace MSFrame
{

public class ConfigManager : BaseManager<ConfigManager>
{
    [SerializeField]
    private ConfigSetting configSetting;
    private ConfigManager()
    {
        configSetting = ResManager.Instance.Load<ConfigSetting>("Config/ConfigSetting");
    }

    public T GetConfig<T>(string configTypeName, int id) where T : ConfigBase
    {
        return configSetting.GetConfig<T>(configTypeName, id);
    }
}
}
