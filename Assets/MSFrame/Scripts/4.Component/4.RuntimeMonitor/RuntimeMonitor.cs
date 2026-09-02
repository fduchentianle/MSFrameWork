#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace MSFrame
{

/// <summary>
/// 资源缓存调试信息，仅表示ResManager内部记录的逻辑引用
/// </summary>
[Serializable]
public sealed class ResCacheDebugInfo
{
    [LabelText("资源路径"), ReadOnly]
    public string path;

    [LabelText("资源类型"), ReadOnly]
    public string typeName;

    [LabelText("加载状态"), ReadOnly]
    public E_ResLoadState state;

    [LabelText("逻辑引用数"), ReadOnly]
    public int refCount;

    [LabelText("资源对象"), ReadOnly]
    public UnityEngine.Object asset;

    public ResCacheDebugInfo(
        string path,
        string typeName,
        E_ResLoadState state,
        int refCount,
        UnityEngine.Object asset)
    {
        this.path = path;
        this.typeName = typeName;
        this.state = state;
        this.refCount = refCount;
        this.asset = asset;
    }
}

/// <summary>
/// 带有 Pool 特性的类型配置。
/// </summary>
[Serializable]
public sealed class PoolConfigDebugInfo
{
    [LabelText("对象类型"), ReadOnly]
    public string typeName;

    [LabelText("最大缓存数量"), ReadOnly]
    public int maxNum;

    public PoolConfigDebugInfo(Type type, PoolAttribute attribute)
    {
        typeName = type.FullName;
        maxNum = attribute.maxNum;
    }
}

/// <summary>
/// EventCenter 中的一条事件注册信息。
/// </summary>
[Serializable]
public sealed class EventCenterDebugInfo
{
    [LabelText("事件类型"), ReadOnly]
    public string eventName;

    [LabelText("注册脚本"), ReadOnly]
    public string scriptName;

    [LabelText("注册函数"), ReadOnly]
    public string functionName;

    public EventCenterDebugInfo(EventType eventType, Delegate action)
    {
        eventName = eventType.ToString();
        scriptName = EventDebugInfoUtility.GetScriptName(action);
        functionName = EventDebugInfoUtility.GetFunctionName(action);
    }
}

/// <summary>
/// EventListener 中的一条事件注册信息。
/// </summary>
[Serializable]
public sealed class EventListenerDebugInfo
{
    [LabelText("监听对象"), ReadOnly]
    public GameObject owner;

    [LabelText("事件类型"), ReadOnly]
    public EventListenerType eventType;

    [LabelText("注册脚本"), ReadOnly]
    public string scriptName;

    [LabelText("注册函数"), ReadOnly]
    public string functionName;

    public EventListenerDebugInfo(
        EventListener listener,
        EventListenerType eventType,
        Delegate action)
    {
        owner = listener.gameObject;
        this.eventType = eventType;
        scriptName = EventDebugInfoUtility.GetScriptName(action);
        functionName = EventDebugInfoUtility.GetFunctionName(action);
    }
}

/// <summary>
/// 统一提取委托在调试面板中需要显示的信息。
/// </summary>
internal static class EventDebugInfoUtility
{
    public static string GetScriptName(Delegate action)
    {
        Type declaringType = action.Method.DeclaringType;
        if (declaringType != null && declaringType.DeclaringType != null && declaringType.Name.Contains("<"))
            declaringType = declaringType.DeclaringType;

        if (declaringType != null)
            return declaringType.Name;

        return action.Target != null ? action.Target.GetType().Name : "未知";
    }

    public static string GetFunctionName(Delegate action)
    {
        string methodName = action.Method.Name;
        int nameStart = methodName.IndexOf('<');
        int nameEnd = methodName.IndexOf('>');
        if (nameStart >= 0 && nameEnd > nameStart)
            return methodName.Substring(nameStart + 1, nameEnd - nameStart - 1) + "（Lambda）";

        return methodName;
    }
}

/// <summary>
/// 统一显示资源缓存、EventCenter 和 EventListener 注册信息的运行时监视器。
/// </summary>
[CreateAssetMenu(fileName = "RuntimeMonitor", menuName = "MSFrame/RuntimeMonitor")]
public class RuntimeMonitor : ScriptableObject
{
    [SerializeField, LabelText("自动刷新")]
    private bool autoRefresh = true;

    [SerializeField, LabelText("刷新间隔（秒）"), MinValue(0.1f)]
    private float refreshInterval = 0.1f;

    [NonSerialized]
    private double nextRefreshTime;

#if UNITY_EDITOR
    [NonSerialized]
    private bool refreshQueued;
#endif

    // 各个独立分组会同时绘制在Inspector中，便于直接对照配置和运行时状态。
    [BoxGroup("对象池配置")]
    [NonSerialized, ShowInInspector, ReadOnly]
    [LabelText("带有Pool特性的类型")]
    [TableList(AlwaysExpanded = true, IsReadOnly = true)]
    private List<PoolConfigDebugInfo> poolConfigInfos = new List<PoolConfigDebugInfo>();

    [BoxGroup("资源缓存")]
    [NonSerialized, ShowInInspector, ReadOnly]
    [LabelText("当前资源缓存")]
    [TableList(AlwaysExpanded = true, IsReadOnly = true)]
    private List<ResCacheDebugInfo> cacheInfos = new List<ResCacheDebugInfo>();

    [BoxGroup("EventCenter")]
    [NonSerialized, ShowInInspector, ReadOnly]
    [LabelText("当前全局事件")]
    [TableList(AlwaysExpanded = true, IsReadOnly = true)]
    private List<EventCenterDebugInfo> eventCenterInfos = new List<EventCenterDebugInfo>();

    [BoxGroup("EventListener")]
    [NonSerialized, ShowInInspector, ReadOnly]
    [LabelText("当前对象事件")]
    [TableList(AlwaysExpanded = true, IsReadOnly = true)]
    private List<EventListenerDebugInfo> eventListenerInfos = new List<EventListenerDebugInfo>();

    [Button("立即刷新", ButtonSizes.Medium)]
    public void Refresh()
    {
#if UNITY_EDITOR
        RefreshImmediately();
#else
        RefreshNow();
#endif
    }

    private void RefreshNow()
    {
        RefreshPoolConfigInfos();

        if (!Application.isPlaying)
        {
            ClearDebugInfos();
            return;
        }

        ReplaceContents(ref cacheInfos, ResManager.Instance.GetDebugSnapshot());
        ReplaceContents(ref eventCenterInfos, EventCenter.Instance.GetDebugSnapshot());
        ReplaceContents(ref eventListenerInfos, EventListener.GetDebugSnapshot());
    }

    private void RefreshPoolConfigInfos()
    {
        if (poolConfigInfos == null)
            poolConfigInfos = new List<PoolConfigDebugInfo>();
        else
            poolConfigInfos.Clear();

        foreach (Type type in TypeCache.GetTypesWithAttribute<PoolAttribute>())
        {
            PoolAttribute attribute = type.GetCustomAttribute<PoolAttribute>();
            poolConfigInfos.Add(new PoolConfigDebugInfo(type, attribute));
        }

        poolConfigInfos.Sort((left, right) => string.CompareOrdinal(left.typeName, right.typeName));
    }

    private void ClearDebugInfos()
    {
        if (cacheInfos == null)
            cacheInfos = new List<ResCacheDebugInfo>();
        else
            cacheInfos.Clear();

        if (eventCenterInfos == null)
            eventCenterInfos = new List<EventCenterDebugInfo>();
        else
            eventCenterInfos.Clear();

        if (eventListenerInfos == null)
            eventListenerInfos = new List<EventListenerDebugInfo>();
        else
            eventListenerInfos.Clear();
    }

    private static void ReplaceContents<T>(ref List<T> target, List<T> snapshot)
    {
        if (target == null)
            target = new List<T>(snapshot.Count);
        else
            target.Clear();

        target.AddRange(snapshot);
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        nextRefreshTime = 0d;
        RefreshPoolConfigInfos();
        ResManager.DebugSnapshotChanged -= OnDebugSnapshotChanged;
        ResManager.DebugSnapshotChanged += OnDebugSnapshotChanged;
        EventCenter.DebugSnapshotChanged -= OnDebugSnapshotChanged;
        EventCenter.DebugSnapshotChanged += OnDebugSnapshotChanged;
        EventListener.DebugSnapshotChanged -= OnDebugSnapshotChanged;
        EventListener.DebugSnapshotChanged += OnDebugSnapshotChanged;
        EditorApplication.update -= UpdateAutoRefresh;
        EditorApplication.update += UpdateAutoRefresh;
    }

    private void OnDebugSnapshotChanged()
    {
        if (!autoRefresh || !IsMonitorInspected())
            return;

        RefreshImmediately();
    }

    private void RefreshImmediately()
    {
        RefreshNow();
        RepaintInspector();
    }

    private void QueueRefresh()
    {
        if (refreshQueued)
            return;

        refreshQueued = true;
        EditorApplication.delayCall += ApplyQueuedRefresh;
    }

    private void ApplyQueuedRefresh()
    {
        EditorApplication.delayCall -= ApplyQueuedRefresh;
        refreshQueued = false;

        if (this == null)
            return;

        RefreshImmediately();
    }

    private void OnDisable()
    {
        ResManager.DebugSnapshotChanged -= OnDebugSnapshotChanged;
        EventCenter.DebugSnapshotChanged -= OnDebugSnapshotChanged;
        EventListener.DebugSnapshotChanged -= OnDebugSnapshotChanged;
        EditorApplication.update -= UpdateAutoRefresh;
        EditorApplication.delayCall -= ApplyQueuedRefresh;
        refreshQueued = false;
    }

    private void UpdateAutoRefresh()
    {
        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime < nextRefreshTime)
            return;

        nextRefreshTime = currentTime + refreshInterval;

        if (!autoRefresh || !IsMonitorInspected())
            return;

        if (!Application.isPlaying)
        {
            bool hasDebugInfo =
                cacheInfos != null && cacheInfos.Count > 0 ||
                eventCenterInfos != null && eventCenterInfos.Count > 0 ||
                eventListenerInfos != null && eventListenerInfos.Count > 0;
            if (hasDebugInfo)
                QueueRefresh();
            return;
        }

        QueueRefresh();
    }

    private void RepaintInspector()
    {
        Editor[] activeEditors = ActiveEditorTracker.sharedTracker.activeEditors;
        for (int i = 0; i < activeEditors.Length; i++)
        {
            Editor editor = activeEditors[i];
            if (editor != null && editor.target == this)
                editor.Repaint();
        }

        GUIHelper.RequestRepaint();
    }

    private bool IsMonitorInspected()
    {
        if (Selection.activeObject == this)
            return true;

        Editor[] activeEditors = ActiveEditorTracker.sharedTracker.activeEditors;
        for (int i = 0; i < activeEditors.Length; i++)
        {
            if (activeEditors[i] != null && activeEditors[i].target == this)
                return true;
        }

        return false;
    }
#endif
}
}
#endif
