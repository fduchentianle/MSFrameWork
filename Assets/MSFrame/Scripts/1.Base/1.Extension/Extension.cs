using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MSFrame
{

/// <summary>
/// 框架通用扩展方法。
/// </summary>
public static class Extension
{
    #region 通用
    /// <summary>
    /// 按顺序比较两个 object 数组的长度和每个元素是否相等。
    /// 元素通过 object.Equals 比较，因此数组元素本身可以为 null。
    /// </summary>
    /// <param name="objs">作为比较基准的数组。</param>
    /// <param name="other">需要比较的数组。</param>
    /// <returns>两个数组长度相同且对应元素全部相等时返回 true，否则返回 false。</returns>
    public static bool ArrayEquals(this object[] objs, object[] other)
    {
        if (other == null || objs.GetType() != other.GetType())
            return false;

        if (objs.Length == other.Length)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                if (!object.Equals(objs[i], other[i]))
                    return false;
            }
            return true;
        }
        return false;
    }
    #endregion

    #region 对象池
    /// <summary>
    /// GameObject放入对象池
    /// </summary>
    public static void PushGameObj(this GameObject go)
    {
        PoolManager.Instance.PushGameObj(go);
    }

    /// <summary>
    /// GameObject放入对象池
    /// </summary>
    public static void PushGameObj(this Component com)
    {
        PoolManager.Instance.PushGameObj(com.gameObject);
    }

    /// <summary>
    /// 普通类放入池子
    /// </summary>
    public static void PushObj<T>(this T obj) where T : class, IPoolObject
    {
        PoolManager.Instance.PushObj<T>(obj);
    }
    #endregion

    #region Mono
    /// <summary>
    /// 添加Update监听
    /// </summary>
    public static void AddUpdateListener(this object obj, UnityAction action)
    {
        MonoManager.Instance.AddUpdateListener(action);
    }

    /// <summary>
    /// 添加FixedUpdate监听
    /// </summary>
    public static void AddFixedUpdateListener(this object obj, UnityAction action)
    {
        MonoManager.Instance.AddFixedUpdateListener(action);
    }

    /// <summary>
    /// 添加LateUpdate监听
    /// </summary>
    public static void AddLateUpdateListener(this object obj, UnityAction action)
    {
        MonoManager.Instance.AddLateUpdateListener(action);
    }

    /// <summary>
    /// 移除Update监听
    /// </summary>
    public static void RemoveUpdateListener(this object obj, UnityAction action)
    {
        MonoManager.Instance.RemoveUpdateListener(action);
    }

    /// <summary>
    /// 移除FixedUpdate监听
    /// </summary>
    public static void RemoveFixedUpdateListener(this object obj, UnityAction action)
    {
        MonoManager.Instance.RemoveFixedUpdateListener(action);
    }

    /// <summary>
    /// 移除LateUpdate监听
    /// </summary>
    public static void RemoveLateUpdateListener(this object obj, UnityAction action)
    {
        MonoManager.Instance.RemoveLateUpdateListener(action);
    }

    /// <summary>
    /// 开启协程
    /// </summary>
    public static Coroutine StartCoroutine(this object obj, IEnumerator routine)
    {
        return MonoManager.Instance.StartCoroutine(routine);
    }

    /// <summary>
    /// 关闭协程
    /// </summary>
    public static void StopCoroutine(this object obj, Coroutine routine)
    {
        MonoManager.Instance.StopCoroutine(routine);
    }

    /// <summary>
    /// 关闭所有协程
    /// </summary>
    public static void StopAllCoroutine(this object obj)
    {
        MonoManager.Instance.StopAllCoroutines();
    }
    #endregion
}
}
