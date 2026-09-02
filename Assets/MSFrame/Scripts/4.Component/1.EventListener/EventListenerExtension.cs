using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MSFrame
{

/// <summary>
/// EventListener 的强类型扩展入口。
/// 外部通过具体的指针、碰撞或触发器方法注册事件，由本类保证 EventListenerType 与事件数据类型正确对应。
/// </summary>
public static class EventListenerExtension
{
    /// <summary>
    /// 获取组件所在物体上的 EventListener；仅在添加监听时允许自动挂载。
    /// </summary>
    private static EventListener GetOrAddEventListener(Component com)
    {
        EventListener listener = com.GetComponent<EventListener>();
        if (listener == null)
            listener = com.gameObject.AddComponent<EventListener>();
        return listener;
    }

    /// <summary>
    /// 添加监听的统一内部入口。具体公开扩展方法负责传入正确的事件类型和泛型类型。
    /// </summary>
    private static void AddEventListener<T>(this Component com, EventListenerType eventType, UnityAction<T, object[]> UnityAction, params object[] args)
    {
        EventListener lis = GetOrAddEventListener(com);
        lis.AddListener<T>(eventType, UnityAction, args);
    }

    /// <summary>
    /// 移除监听的统一内部入口。移除操作不会自动添加 EventListener 组件。
    /// </summary>
    private static void RemoveEventListener<T>(this Component com, EventListenerType eventType, UnityAction<T, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        EventListener listener = com.GetComponent<EventListener>();
        if (listener == null)
            Debug.LogWarning("MSFrame: " + com.name + "没有挂载EventListener脚本，无法移除监听");
        else
            listener.RemoveListener(eventType, UnityAction, checkArgs, args);
    }

    /// <summary>
    /// 移除组件上指定事件类型的全部监听。
    /// </summary>
    public static void RemoveAllListener(this Component com, EventListenerType eventType)
    {
        EventListener listener = com.GetComponent<EventListener>();
        if (listener == null)
            Debug.LogWarning("MSFrame: " + com.name + "没有挂载EventListener脚本，无法移除监听");
        else
            listener.RemoveAllListener(eventType);
    }

    /// <summary>
    /// 移除组件上 EventListener 保存的全部监听。
    /// </summary>
    public static void RemoveAllListener(this Component com)
    {
        EventListener listener = com.GetComponent<EventListener>();
        if (listener == null)
            Debug.LogWarning("MSFrame: " + com.name + "没有挂载EventListener脚本，无法移除监听");
        else
            listener.RemoveAllListener();
    }

    #region 鼠标相关事件
    // 指针与拖拽事件统一使用 PointerEventData 作为 Unity 事件数据。
    public static void OnMouseEnter(this Component com, UnityAction<PointerEventData, object[]> UnityAction, params object[] args)
    {
        AddEventListener<PointerEventData>(com, EventListenerType.OnMouseEnter, UnityAction, args);
    }
    public static void OnMouseExit(this Component com, UnityAction<PointerEventData, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnMouseExit, UnityAction, args);
    }
    public static void OnClick(this Component com, UnityAction<PointerEventData, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnClick, UnityAction, args);
    }
    public static void OnClickDown(this Component com, UnityAction<PointerEventData, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnClickDown, UnityAction, args);
    }
    public static void OnClickUp(this Component com, UnityAction<PointerEventData, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnClickUp, UnityAction, args);
    }
    public static void OnDrag(this Component com, UnityAction<PointerEventData, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnDrag, UnityAction, args);
    }
    public static void OnBeginDrag(this Component com, UnityAction<PointerEventData, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnBeginDrag, UnityAction, args);
    }
    public static void OnEndDrag(this Component com, UnityAction<PointerEventData, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnEndDrag, UnityAction, args);
    }
    public static void RemoveMouseEnter(this Component com, UnityAction<PointerEventData, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnMouseEnter, UnityAction, checkArgs, args);
    }
    public static void RemoveMouseExit(this Component com, UnityAction<PointerEventData, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnMouseExit, UnityAction, checkArgs, args);
    }
    public static void RemoveClick(this Component com, UnityAction<PointerEventData, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnClick, UnityAction, checkArgs, args);
    }
    public static void RemoveClickDown(this Component com, UnityAction<PointerEventData, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnClickDown, UnityAction, checkArgs, args);
    }
    public static void RemoveClickUp(this Component com, UnityAction<PointerEventData, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnClickUp, UnityAction, checkArgs, args);
    }
    public static void RemoveDrag(this Component com, UnityAction<PointerEventData, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnDrag, UnityAction, checkArgs, args);
    }
    public static void RemoveBeginDrag(this Component com, UnityAction<PointerEventData, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnBeginDrag, UnityAction, checkArgs, args);
    }
    public static void RemoveEndDrag(this Component com, UnityAction<PointerEventData, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnEndDrag, UnityAction, checkArgs, args);
    }


    #endregion

    #region 碰撞相关事件
    // 3D 碰撞使用 Collision，2D 碰撞使用 Collision2D。

    public static void OnCollisionEnter(this Component com, UnityAction<Collision, object[]> UnityAction, params object[] args)
    {
        com.AddEventListener(EventListenerType.OnCollisionEnter, UnityAction, args);
    }


    public static void OnCollisionStay(this Component com, UnityAction<Collision, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnCollisionStay, UnityAction, args);
    }
    public static void OnCollisionExit(this Component com, UnityAction<Collision, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnCollisionExit, UnityAction, args);
    }
    public static void OnCollisionEnter2D(this Component com, UnityAction<Collision2D, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnCollisionEnter2D, UnityAction, args);
    }
    public static void OnCollisionStay2D(this Component com, UnityAction<Collision2D, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnCollisionStay2D, UnityAction, args);
    }
    public static void OnCollisionExit2D(this Component com, UnityAction<Collision2D, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnCollisionExit2D, UnityAction, args);
    }
    public static void RemoveCollisionEnter(this Component com, UnityAction<Collision, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnCollisionEnter, UnityAction, checkArgs, args);
    }
    public static void RemoveCollisionStay(this Component com, UnityAction<Collision, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnCollisionStay, UnityAction, checkArgs, args);
    }
    public static void RemoveCollisionExit(this Component com, UnityAction<Collision, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnCollisionExit, UnityAction, checkArgs, args);
    }
    public static void RemoveCollisionEnter2D(this Component com, UnityAction<Collision2D, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnCollisionEnter2D, UnityAction, checkArgs, args);
    }
    public static void RemoveCollisionStay2D(this Component com, UnityAction<Collision2D, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnCollisionStay2D, UnityAction, checkArgs, args);
    }
    public static void RemoveCollisionExit2D(this Component com, UnityAction<Collision2D, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnCollisionExit2D, UnityAction, checkArgs, args);
    }
    #endregion

    #region 触发相关事件
    // 3D 触发器使用 Collider，2D 触发器使用 Collider2D。
    public static void OnTriggerEnter(this Component com, UnityAction<Collider, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnTriggerEnter, UnityAction, args);
    }
    public static void OnTriggerStay(this Component com, UnityAction<Collider, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnTriggerStay, UnityAction, args);
    }
    public static void OnTriggerExit(this Component com, UnityAction<Collider, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnTriggerExit, UnityAction, args);
    }
    public static void OnTriggerEnter2D(this Component com, UnityAction<Collider2D, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnTriggerEnter2D, UnityAction, args);
    }
    public static void OnTriggerStay2D(this Component com, UnityAction<Collider2D, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnTriggerStay2D, UnityAction, args);
    }
    public static void OnTriggerExit2D(this Component com, UnityAction<Collider2D, object[]> UnityAction, params object[] args)
    {
        AddEventListener(com, EventListenerType.OnTriggerExit2D, UnityAction, args);
    }
    public static void RemoveTriggerEnter(this Component com, UnityAction<Collider, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnTriggerEnter, UnityAction, checkArgs, args);
    }
    public static void RemoveTriggerStay(this Component com, UnityAction<Collider, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnTriggerStay, UnityAction, checkArgs, args);
    }
    public static void RemoveTriggerExit(this Component com, UnityAction<Collider, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnTriggerExit, UnityAction, checkArgs, args);
    }
    public static void RemoveTriggerEnter2D(this Component com, UnityAction<Collider2D, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnTriggerEnter2D, UnityAction, checkArgs, args);
    }
    public static void RemoveTriggerStay2D(this Component com, UnityAction<Collider2D, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnTriggerStay2D, UnityAction, checkArgs, args);
    }
    public static void RemoveTriggerExit2D(this Component com, UnityAction<Collider2D, object[]> UnityAction, bool checkArgs = false, params object[] args)
    {
        RemoveEventListener(com, EventListenerType.OnTriggerExit2D, UnityAction, checkArgs, args);
    }
    #endregion
}
}
