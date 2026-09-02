using UnityEngine;

namespace MSFrame
{

/// <summary>
/// 自动挂载式 继承Mono的单例模式基类
/// 推荐使用
/// 无需手动挂载 不用担心重复挂载 不用担心过场景问题
/// </summary>
public class SingletonAutoMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                //动态创建一个空物体
                GameObject obj = new GameObject();
                //空物体的名字是单例模式的脚本类名
                obj.name = typeof(T).Name;
                //动态挂载对应的单例模式脚本
                instance = obj.AddComponent<T>();
                //过场景中不移除对象 保证在整个生命周期中都存在
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }
}
}
