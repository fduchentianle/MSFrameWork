using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MSFrame
{

/// <summary>
/// 事件枚举
/// </summary>
public enum EventListenerType
{
    OnMouseEnter,
    OnMouseExit,
    OnClick,
    OnClickDown,
    OnClickUp,
    OnDrag,
    OnBeginDrag,
    OnEndDrag,
    OnCollisionEnter,
    OnCollisionStay,
    OnCollisionExit,
    OnCollisionEnter2D,
    OnCollisionStay2D,
    OnCollisionExit2D,
    OnTriggerEnter,
    OnTriggerStay,
    OnTriggerExit,
    OnTriggerEnter2D,
    OnTriggerStay2D,
    OnTriggerExit2D,
}

public interface IMouseEvent : IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerUpHandler, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{

}

/// <summary>
/// EventListener监听的是 IMouseEvent 碰撞 触发 事件
/// </summary>
public class EventListener : MonoBehaviour, IMouseEvent
{
    #region 内部类 接口
    /// <summary>
    /// 单个事件的包裹
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class EventListenerInfo<T>
    {
        public UnityAction<T, object[]> action;
        public object[] args;

        /// <summary>
        /// 初始化单个事件包裹
        /// </summary>
        /// <param name="action">事件</param>
        /// <param name="args">传入参数</param>
        public void Init(UnityAction<T, object[]> action, object[] args)
        {
            this.action = action;
            this.args = args;
        }

        /// <summary>
        /// 触发单个事件
        /// </summary>
        public void TriggerEvent(T eventData)
        {
            action?.Invoke(eventData, args);
        }

        /// <summary>
        /// 释放单个事件
        /// </summary>
        public void Release()
        {
            action = null;
            args = null;
        }

    }

    interface IEventListenerInfos
    {
        void RemoveAll();
#if UNITY_EDITOR
        void AppendDebugInfos(EventListener owner, EventListenerType type, List<EventListenerDebugInfo> snapshot);
#endif
    }

    /// <summary>
    /// 同一类事件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class EventListenerInfos<T> : IEventListenerInfos
    {
        private List<EventListenerInfo<T>> eventInfoList = new List<EventListenerInfo<T>>();

        #region 添加监听
        /// <summary>
        /// 添加监听
        /// </summary>
        /// <param name="action">事件</param>
        /// <param name="args">传入参数</param>
        public void AddListener(UnityAction<T, object[]> action, params object[] args)
        {
            EventListenerInfo<T> info = new EventListenerInfo<T>();
            info.Init(action, args);
            eventInfoList.Add(info);
        }
        #endregion

        #region 移除监听
        /// <summary>
        /// 移除某一个事件的监听
        /// </summary>
        /// <param name="action">某一个事件</param>
        /// <param name="checkArgs">是否要比较传入参数</param>
        /// <param name="args">传入参数</param>
        public void RemoveListener(UnityAction<T, object[]> action, bool checkArgs = false, params object[] args)
        {
            for (int i = 0; i < eventInfoList.Count; i++)
            {
                if (eventInfoList[i].action.Equals(action))
                {
                    if (checkArgs && args.ArrayEquals(eventInfoList[i].args) || !checkArgs)
                    {
                        eventInfoList[i].Release();
                        eventInfoList.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 移除这一类的所有事件
        /// </summary>
        public void RemoveAll()
        {
            for (int i = 0; i < eventInfoList.Count; i++)
            {
                eventInfoList[i].Release();
            }

            eventInfoList.Clear();
        }
        #endregion

        #region 触发事件

        /// <summary>
        /// 触发所有事件
        /// </summary>
        /// <param name="eventData"></param>
        public void TriggerEvent(T eventData)
        {
            for (int i = 0; i < eventInfoList.Count; i++)
            {
                eventInfoList[i].TriggerEvent(eventData);
            }
        }
        #endregion

        #region 调试快照
#if UNITY_EDITOR
        /// <summary>
        /// 将当前类型下的每条监听转换为监视器需要的调试记录。
        /// </summary>
        public void AppendDebugInfos(EventListener owner, EventListenerType type, List<EventListenerDebugInfo> snapshot)
        {
            for (int i = 0; i < eventInfoList.Count; i++)
            {
                EventListenerInfo<T> info = eventInfoList[i];
                if (info.action != null)
                    snapshot.Add(new EventListenerDebugInfo(owner, type, info.action));
            }
        }
#endif
        #endregion

    }
    #endregion

    #region 外部访问
    // 所有事件类型 与 对应的所有时间
    private Dictionary<EventListenerType, IEventListenerInfos> eventInfosDic = new Dictionary<EventListenerType, IEventListenerInfos>();

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

    /// <summary>
    /// 为指定事件类型添加监听。
    /// </summary>
    public void AddListener<T>(EventListenerType type, UnityAction<T, object[]> action, params object[] args)
    {
        if (eventInfosDic.ContainsKey(type))
            (eventInfosDic[type] as EventListenerInfos<T>).AddListener(action, args);
        else
        {
            EventListenerInfos<T> infos = new EventListenerInfos<T>();
            infos.AddListener(action, args);
            eventInfosDic.Add(type, infos);
        }
        NotifyDebugSnapshotChanged();
    }

    /// <summary>
    /// 移除指定类型的指定事件监听
    /// </summary>
    public void RemoveListener<T>(EventListenerType type, UnityAction<T, object[]> action, bool checkArgs = false, params object[] args)
    {
        if (eventInfosDic.ContainsKey(type))
        {
            (eventInfosDic[type] as EventListenerInfos<T>).RemoveListener(action, checkArgs, args);
            NotifyDebugSnapshotChanged();
        }
        else
            Debug.LogWarning("MSFrame: " + type.ToString() + "还未注册到eventInfosDic中,无法移除事件");
    }

    /// <summary>
    /// 移除指定类型的事件监听
    /// </summary>
    public void RemoveAllListener(EventListenerType type)
    {
        if (eventInfosDic.ContainsKey(type))
        {
            eventInfosDic[type].RemoveAll();
            eventInfosDic.Remove(type);
            NotifyDebugSnapshotChanged();
        }
        else
            Debug.LogWarning("MSFrame: " + type.ToString() + "还未注册到eventInfosDic中，无法全部移除");
    }

    /// <summary>
    /// 移除所有监听
    /// </summary>
    public void RemoveAllListener()
    {
        bool hadListener = eventInfosDic.Count > 0;
        foreach (var item in eventInfosDic.Values)
        {
            item.RemoveAll();
        }
        eventInfosDic.Clear();
        if (hadListener)
            NotifyDebugSnapshotChanged();
    }

    /// <summary>
    /// 触发监听
    /// </summary>
    public void TriggerAction<T>(EventListenerType type, T eventData)
    {
        if (eventInfosDic.ContainsKey(type))
        {
            (eventInfosDic[type] as EventListenerInfos<T>).TriggerEvent(eventData);
        }
    }
    #endregion

#if UNITY_EDITOR
    #region 调试快照
    /// <summary>
    /// 扫描当前已加载场景中的 EventListener，并获取全部注册事件的只读调试快照。
    /// </summary>
    public static List<EventListenerDebugInfo> GetDebugSnapshot()
    {
        List<EventListenerDebugInfo> snapshot = new List<EventListenerDebugInfo>();
        EventListener[] listeners = FindObjectsByType<EventListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < listeners.Length; i++)
        {
            EventListener listener = listeners[i];
            foreach (KeyValuePair<EventListenerType, IEventListenerInfos> item in listener.eventInfosDic)
                item.Value.AppendDebugInfos(listener, item.Key, snapshot);
        }

        snapshot.Sort((left, right) =>
        {
            int ownerCompare = string.Compare(left.owner.name, right.owner.name, StringComparison.Ordinal);
            if (ownerCompare != 0)
                return ownerCompare;

            int eventCompare = left.eventType.CompareTo(right.eventType);
            if (eventCompare != 0)
                return eventCompare;

            int scriptCompare = string.Compare(left.scriptName, right.scriptName, StringComparison.Ordinal);
            return scriptCompare != 0
                ? scriptCompare
                : string.Compare(left.functionName, right.functionName, StringComparison.Ordinal);
        });
        return snapshot;
    }

    private void OnDestroy()
    {
        if (eventInfosDic.Count > 0)
            NotifyDebugSnapshotChanged();
    }
    #endregion
#endif

    #region 鼠标事件
    // EventSystem 调用以下接口方法后，将 PointerEventData 转发给已注册的监听。
    public void OnPointerEnter(PointerEventData eventData)
    {
        TriggerAction(EventListenerType.OnMouseEnter, eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TriggerAction(EventListenerType.OnMouseExit, eventData);
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        TriggerAction(EventListenerType.OnBeginDrag, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        TriggerAction(EventListenerType.OnDrag, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        TriggerAction(EventListenerType.OnEndDrag, eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TriggerAction(EventListenerType.OnClick, eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TriggerAction(EventListenerType.OnClickDown, eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        TriggerAction(EventListenerType.OnClickUp, eventData);
    }
    #endregion

    #region 碰撞事件
    // Unity Physics/Physics2D 回调分别转发 Collision 和 Collision2D。
    private void OnCollisionEnter(Collision collision)
    {
        TriggerAction(EventListenerType.OnCollisionEnter, collision);
    }
    private void OnCollisionStay(Collision collision)
    {
        TriggerAction(EventListenerType.OnCollisionStay, collision);
    }
    private void OnCollisionExit(Collision collision)
    {
        TriggerAction(EventListenerType.OnCollisionExit, collision);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TriggerAction(EventListenerType.OnCollisionEnter2D, collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TriggerAction(EventListenerType.OnCollisionStay2D, collision);
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        TriggerAction(EventListenerType.OnCollisionExit2D, collision);
    }
    #endregion

    #region 触发事件
    // Unity Physics/Physics2D 回调分别转发 Collider 和 Collider2D。
    private void OnTriggerEnter(Collider other)
    {
        TriggerAction(EventListenerType.OnTriggerEnter, other);
    }
    private void OnTriggerStay(Collider other)
    {
        TriggerAction(EventListenerType.OnTriggerStay, other);
    }
    private void OnTriggerExit(Collider other)
    {
        TriggerAction(EventListenerType.OnTriggerExit, other);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        TriggerAction(EventListenerType.OnTriggerEnter2D, collision);
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        TriggerAction(EventListenerType.OnTriggerStay2D, collision);
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        TriggerAction(EventListenerType.OnTriggerExit2D, collision);
    }
    #endregion

}
}
