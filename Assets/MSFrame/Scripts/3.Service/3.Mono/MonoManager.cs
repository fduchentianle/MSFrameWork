using UnityEngine.Events;

namespace MSFrame
{

/// <summary>
/// Mono管理器 用于让不继承Mono的脚本实现Update和协程相关功能
/// </summary>
public class MonoManager : SingletonAutoMono<MonoManager>
{
    private event UnityAction fixedUpdateEvent;
    private event UnityAction updateEvent;
    private event UnityAction lateUpdateEvent;

    public void FixedUpdate()
    {
        fixedUpdateEvent?.Invoke();
    }

    public void Update()
    {
        updateEvent?.Invoke();
    }

    public void LateUpdate()
    {
        lateUpdateEvent?.Invoke();
    }


    #region 添加Update相关事件
    public void AddFixedUpdateListener(UnityAction action)
    {
        fixedUpdateEvent += action;
    }

    public void AddUpdateListener(UnityAction action)
    {
        updateEvent += action;
    }

    public void AddLateUpdateListener(UnityAction action)
    {
        lateUpdateEvent += action;
    }
    #endregion

    #region 移除Update相关事件
    public void RemoveFixedUpdateListener(UnityAction action)
    {
        fixedUpdateEvent -= action;
    }

    public void RemoveUpdateListener(UnityAction action)
    {
        updateEvent -= action;
    }

    public void RemoveLateUpdateListener(UnityAction action)
    {
        lateUpdateEvent -= action;
    }
    #endregion

}
}
