using UnityEngine;

namespace MSFrame
{

/// <summary>
/// 继承Mono的单例模式基类
/// </summary>
public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance => instance;

    protected virtual void Awake()
    {
        //已经存在一个对应的单例模式对象了 不需要再有一个了
        if (instance != null)
        {
            Destroy(this);
            return;
        }
        instance = this as T;
        //我们挂在继承该单例模式基类的脚本后 依附对象过场景时就不会被移除了
        //就可以保证在游戏的整个生命周期中都存在
        DontDestroyOnLoad(this.gameObject);
    }
}
}
