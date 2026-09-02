using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Events;

namespace MSFrame
{

/// <summary>
/// 事件信息基类 主要用于父类装子类
/// </summary>
public abstract class EventInfoBase { }

/// <summary>
/// 无参事件
/// </summary>
public class EventInfo : EventInfoBase
{
    public UnityAction actions;
}

/// <summary>
/// 一个参数事件
/// </summary>
public class EventInfo<T> : EventInfoBase
{
    public UnityAction<T> actions;
}

/// <summary>
/// 两个参数事件
/// </summary>
public class EventInfo<T,K> : EventInfoBase
{
    public UnityAction<T,K> actions;
}

/// <summary>
/// 三个参数事件
/// </summary>
public class EventInfo<T, K, L> : EventInfoBase
{
    public UnityAction<T,K,L> actions;
}

public class EventCenter : BaseManager<EventCenter>
{
    private Dictionary<EventType, EventInfoBase> eventDic = new Dictionary<EventType, EventInfoBase>();
    private EventCenter() { }

#if UNITY_EDITOR
    // 仅在编辑器中通知运行时监视器刷新事件注册快照。
    internal static event Action DebugSnapshotChanged;
#endif

    // Conditional会让正式包在编译阶段直接移除所有调用点，不留下空方法调用开销。
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private static void NotifyDebugSnapshotChanged()
    {
#if UNITY_EDITOR
        DebugSnapshotChanged?.Invoke();
#endif
    }

    #region 添加事件
    /// <summary>
    /// 添加事件
    /// </summary>
    /// <param name="eventName">事件枚举</param>
    /// <param name="action">要添加的事件</param>
    public void AddEventListener(EventType eventName, UnityAction action)
    {
        if (!eventDic.ContainsKey(eventName))
            eventDic.Add(eventName, new EventInfo());
        (eventDic[eventName] as EventInfo).actions += action;
        NotifyDebugSnapshotChanged();
    }

    public void AddEventListener<T>(EventType eventName, UnityAction<T> action)
    {
        if (!eventDic.ContainsKey(eventName))
            eventDic.Add(eventName, new EventInfo<T>());
        (eventDic[eventName] as EventInfo<T>).actions += action;
        NotifyDebugSnapshotChanged();
    }

    public void AddEventListener<T,K>(EventType eventName, UnityAction<T,K> action)
    {
        if (!eventDic.ContainsKey(eventName))
            eventDic.Add(eventName, new EventInfo<T,K>());
        (eventDic[eventName] as EventInfo<T,K>).actions += action;
        NotifyDebugSnapshotChanged();
    }

    public void AddEventListener<T,K,L>(EventType eventName, UnityAction<T,K,L> action)
    {
        if (!eventDic.ContainsKey(eventName))
            eventDic.Add(eventName, new EventInfo<T,K,L>());
        (eventDic[eventName] as EventInfo<T,K,L>).actions += action;
        NotifyDebugSnapshotChanged();
    }
    #endregion

    #region 移除事件
    /// <summary>
    /// 移除事件
    /// </summary>
    /// <param name="eventName">时间枚举</param>
    /// <param name="action">要移除的事件</param>
    public void RemoveEventListener(EventType eventName, UnityAction action)
    {
        if (eventDic.ContainsKey(eventName))
        {
            (eventDic[eventName] as EventInfo).actions -= action;
            NotifyDebugSnapshotChanged();
        }
    }

    public void RemoveEventListener<T>(EventType eventName, UnityAction<T> action)
    {
        if (eventDic.ContainsKey(eventName))
        {
            (eventDic[eventName] as EventInfo<T>).actions -= action;
            NotifyDebugSnapshotChanged();
        }
    }

    public void RemoveEventListener<T,K>(EventType eventName, UnityAction<T,K> action)
    {
        if (eventDic.ContainsKey(eventName))
        {
            (eventDic[eventName] as EventInfo<T,K>).actions -= action;
            NotifyDebugSnapshotChanged();
        }
    }

    public void RemoveEventListener<T,K,L>(EventType eventName, UnityAction<T,K,L> action)
    {
        if (eventDic.ContainsKey(eventName))
        {
            (eventDic[eventName] as EventInfo<T,K,L>).actions -= action;
            NotifyDebugSnapshotChanged();
        }
    }
    #endregion

    #region 触发事件
    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="eventName">事件枚举</param>
    public void EventTrigger(EventType eventName)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo).actions?.Invoke();
    }

    public void EventTrigger<T>(EventType eventName, T info)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo<T>).actions?.Invoke(info);
    }

    public void EventTrigger<T,K>(EventType eventName, T info1, K info2)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo<T,K>).actions?.Invoke(info1, info2);
    }

    public void EventTrigger<T,K,L>(EventType eventName, T info1, K info2, L info3)
    {
        if (eventDic.ContainsKey(eventName))
            (eventDic[eventName] as EventInfo<T,K,L>).actions?.Invoke(info1, info2, info3);
    }
    #endregion

    #region 清空事件
    public void Clear()
    {
        if (eventDic.Count == 0)
            return;

        eventDic.Clear();
        NotifyDebugSnapshotChanged();
    }

    public void Clear(EventType eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            eventDic.Remove(eventName);
            NotifyDebugSnapshotChanged();
        }
    }
    #endregion

#if UNITY_EDITOR
    #region 调试快照
    /// <summary>
    /// 获取 EventCenter 当前注册委托的只读调试快照。
    /// 反射仅在监视器刷新时执行，不影响事件触发路径。
    /// </summary>
    public List<EventCenterDebugInfo> GetDebugSnapshot()
    {
        List<EventCenterDebugInfo> snapshot = new List<EventCenterDebugInfo>();
        for (Dictionary<EventType, EventInfoBase>.Enumerator enumerator = eventDic.GetEnumerator(); enumerator.MoveNext();)
        {
            KeyValuePair<EventType, EventInfoBase> item = enumerator.Current;
            FieldInfo actionsField = item.Value.GetType().GetField("actions", BindingFlags.Instance | BindingFlags.Public);
            Delegate actions = actionsField?.GetValue(item.Value) as Delegate;
            if (actions == null)
                continue;

            Delegate[] invocationList = actions.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
                snapshot.Add(new EventCenterDebugInfo(item.Key, invocationList[i]));
        }

        snapshot.Sort((left, right) =>
        {
            int eventCompare = string.Compare(left.eventName, right.eventName, StringComparison.Ordinal);
            if (eventCompare != 0)
                return eventCompare;

            int scriptCompare = string.Compare(left.scriptName, right.scriptName, StringComparison.Ordinal);
            return scriptCompare != 0
                ? scriptCompare
                : string.Compare(left.functionName, right.functionName, StringComparison.Ordinal);
        });
        return snapshot;
    }
    #endregion
#endif
}
}
