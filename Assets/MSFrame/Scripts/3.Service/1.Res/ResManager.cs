using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MSFrame
{

/// <summary>
/// 资源加载状态
/// </summary>
public enum E_ResLoadState
{
    Loading,
    Loaded,
}

/// <summary>
/// 资源信息基类 主要用于里氏替换原则
/// </summary>
public abstract class ResInfoBase
{
    //资源路径
    public string path;
    //资源类型名称
    public string typeName;
    //引用计数
    public int refCount;
    //加载状态
    public E_ResLoadState state;
    //引用为0是否要立即卸载
    public bool isDel;
    //异步加载的协程
    public Coroutine coroutine;

    //用于缓存监视器读取资源对象
    public abstract UnityEngine.Object AssetObject { get; }
}

/// <summary>
/// 资源信息对象
/// </summary>
public class ResInfo<T> : ResInfoBase where T : UnityEngine.Object
{
    //资源
    public T asset;

    //异步加载后回调函数
    public UnityAction<T> callBack;

    public override UnityEngine.Object AssetObject => asset;

    public void AddRefCount()
    {
        refCount += 1;
    }

    public void SubRefCount()
    {
        refCount -= 1;
        if (refCount < 0)
        {
            Debug.LogWarning("MSFrame: 引用计数小于0 请检查加载和卸载是否配合执行");
        }
    }

    public void ClearCoroutineAndCallBack()
    {
        coroutine = null;
        callBack = null;
    }
}

public class ResManager : BaseManager<ResManager>
{
    private Dictionary<string, ResInfoBase> resDic = new Dictionary<string, ResInfoBase>();
    private ResManager() { }

#if UNITY_EDITOR
    // 仅在编辑器中提供缓存变化事件，用于驱动MSFrameRuntimeMonitor即时刷新。
    internal static event Action DebugSnapshotChanged;
#endif

    /// <summary>
    /// 通知编辑器中的缓存监视器重新获取调试快照
    /// 正式发布时方法为空，不产生快照和Inspector刷新开销
    /// </summary>
    // Conditional会让正式包在编译阶段直接移除所有调用点，不留下空方法调用开销。
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private static void NotifyDebugSnapshotChanged()
    {
#if UNITY_EDITOR
        DebugSnapshotChanged?.Invoke();
#endif
    }

    #region 同步加载
    /// <summary>
    /// 使用泛型同步加载资源
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="path">Resources文件夹下的路径</param>
    /// <returns></returns>
    public T Load<T>(string path) where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).FullName;
        ResInfo<T> info;
        //如果字典中不存在资源 直接同步加载
        if (!resDic.ContainsKey(resName))
        {
            T res = Resources.Load<T>(path);
            //同步加载失败
            if (res == null)
            {
                Debug.LogWarning("MSFrame: " + path + "资源同步加载失败");
                return null;
            }

            //同步加载成功 引用数+1 并放入字典
            info = new ResInfo<T>();
            info.path = path;
            //监视器只显示类型本身，不显示UnityEngine等命名空间
            info.typeName = typeof(T).Name;
            info.asset = res;
            info.state = E_ResLoadState.Loaded;
            info.AddRefCount();
            resDic[resName] = info;
            //新增同步资源缓存后立即更新监视器
            NotifyDebugSnapshotChanged();
            return info.asset;
        }
        //正在异步加载或者已经加载完毕
        else
        {
            info = resDic[resName] as ResInfo<T>;
            info.AddRefCount();
            //缓存命中时引用数已经变化，立即更新监视器
            NotifyDebugSnapshotChanged();
            //如果正在异步加载中
            if (info.state == E_ResLoadState.Loading)
            {
                //停止异步加载 转为同步加载 加载完毕后执行回调函数
                MonoManager.Instance.StopCoroutine(info.coroutine);
                T res = Resources.Load<T>(path);
                //同步资源加载失败
                if (res == null)
                {
                    Debug.LogWarning("MSFrame: " + path + "资源同步加载失败");
                    info.ClearCoroutineAndCallBack();
                    resDic.Remove(resName);
                    //同步接管加载失败，缓存记录已移除
                    NotifyDebugSnapshotChanged();
                    return null;
                }
                //同步资源加载成功
                info.asset = res;
                info.state = E_ResLoadState.Loaded;
                info.callBack?.Invoke(info.asset);
                //清空
                info.ClearCoroutineAndCallBack();
                //同步接管异步加载完成，刷新资源状态和对象信息
                NotifyDebugSnapshotChanged();
                return info.asset;
            }
            //已经加载过了
            else
                return info.asset;
        }
    }
    #endregion

    #region 异步加载
    /// <summary>
    /// 使用泛型异步加载资源
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="path">Resources文件夹下的路径</param>
    /// <param name="callBack">加载结束后的回调函数</param>
    public void LoadAsync<T>(string path, UnityAction<T> callBack) where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).FullName;
        ResInfo<T> info;
        //如果字典中不存在资源 直接异步加载
        if (!resDic.ContainsKey(resName))
        {
            info = new ResInfo<T>();
            info.path = path;
            //监视器只显示类型本身，不显示UnityEngine等命名空间
            info.typeName = typeof(T).Name;
            info.AddRefCount();
            info.state = E_ResLoadState.Loading;
            info.callBack = callBack;
            resDic.Add(resName,info);
            //异步请求加入缓存后，立即显示Loading状态和当前引用数
            NotifyDebugSnapshotChanged();
            info.coroutine = MonoManager.Instance.StartCoroutine(ReallyLoadAsync<T>(path));
        }
        //如果字典中存在资源
        else
        {
            info = resDic[resName] as ResInfo<T>;
            info.AddRefCount();
            //复用已有异步请求或缓存资源时，立即显示新的引用数
            NotifyDebugSnapshotChanged();
            if (info.state == E_ResLoadState.Loading)
                info.callBack += callBack;
            else
                callBack?.Invoke(info.asset);
        }
    }

    /// <summary>
    /// 真正异步加载资源的协程
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="path">Resources文件夹下的路径</param>
    /// <returns></returns>
    private IEnumerator ReallyLoadAsync<T>(string path) where T : UnityEngine.Object
    {
        //异步加载资源
        ResourceRequest rq = Resources.LoadAsync<T>(path);
        string resName = path + "_" + typeof(T).FullName;
        yield return rq;

        //如果异步加载的时候没有清空字典
        if (resDic.ContainsKey(resName))
        {
            ResInfo<T> info = resDic[resName] as ResInfo<T>;

            //异步资源加载失败
            if (rq.asset == null)
            {
                Debug.LogWarning("MSFrame:" + path + "资源异步加载失败");
                info.ClearCoroutineAndCallBack();
                resDic.Remove(resName);
                //异步加载失败，移除缓存记录后更新监视器
                NotifyDebugSnapshotChanged();
                yield break;
            }

            //异步资源加载成功
            info.asset = rq.asset as T;
            info.state = E_ResLoadState.Loaded;
            //异步加载完成，立即显示Loaded状态和已加载资源
            NotifyDebugSnapshotChanged();

            //如果异步加载同时卸载了资源
            if (info.refCount == 0)
                UnloadAsset<T>(path, info.isDel, null, false);
            else
                info.callBack?.Invoke(info.asset);
            info.ClearCoroutineAndCallBack();
        }
        //如果异步加载的时候清空了字典
        yield break;
    }
    #endregion

    #region 卸载资源
    /// <summary>
    /// 使用泛型卸载资源
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="path">Resources文件夹下的路径</param>
    /// <param name="callBack">要取消订阅的回调函数</param>
    /// <param name="isSub">引用次数是否减1</param>
    public void UnloadAsset<T>(string path, bool isDel = false, UnityAction<T> callBack = null, bool isSub = true) where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).FullName;
        //如果字典有存在对应资源
        if (resDic.ContainsKey(resName))
        {
            ResInfo<T> info = resDic[resName] as ResInfo<T>;
            //引用计数是否减1
            if (isSub)
                info.SubRefCount();
            //记录 引用计数为0时 是否马上移除标签
            info.isDel = isDel;
            //如果资源存在且引用计数为0 则进行资源卸载
            if (info.state == E_ResLoadState.Loaded && info.refCount <= 0 && info.isDel)
            {
                info.ClearCoroutineAndCallBack();
                resDic.Remove(resName);

                //普通资源可以立即卸载
                if (!(info.asset is GameObject) && !(info.asset is Component))
                {
                    Resources.UnloadAsset(info.asset);
                }
                
                //清空管理器持有的资源引用
                info.asset = null;
            }
            //如果资源正在异步加载中
            else if (info.state == E_ResLoadState.Loading)
            {
                //当异步加载不想使用时 应该移除它的回调函数 而不是直接去卸载资源
                if (callBack != null)
                    info.callBack -= callBack;
            }

            //引用数、加载回调或缓存记录可能发生变化，统一更新监视器
            NotifyDebugSnapshotChanged();
        }
        //如果字典中不存在对应资源
        else
        {
            Debug.LogWarning("MSFrame: " + path + "资源卸载失败");
            return;
        }
    }

    /// <summary>
    /// 卸载没有引用的所有资源
    /// </summary>
    /// <param name="callBack"></param>
    public void UnloadUnusedAssets(UnityAction callBack = null)
    {
        MonoManager.Instance.StartCoroutine(ReallyUnloadUnusedAssets(callBack));
    }

    private IEnumerator ReallyUnloadUnusedAssets(UnityAction callBack = null)
    {
        //在真正移除不适用的资源之前 应该把我们自己记录的那些引用计数为0 并且没有被移除记录的资源
        List<string> list = new List<string>();
        for (Dictionary<string, ResInfoBase>.Enumerator enumerator = resDic.GetEnumerator(); enumerator.MoveNext();)
        {
            KeyValuePair<string, ResInfoBase> item = enumerator.Current;
            if (item.Value.state == E_ResLoadState.Loaded && item.Value.refCount <= 0)
                list.Add(item.Key);
        }
        for (int i = 0; i < list.Count; i++)
        {
            resDic.Remove(list[i]);
        }

        //批量移除零引用缓存后，只发送一次变化通知
        if (list.Count > 0)
            NotifyDebugSnapshotChanged();

        AsyncOperation ao = Resources.UnloadUnusedAssets();
        yield return ao;
        //卸载完毕后通知外部
        callBack?.Invoke();
    }

    /// <summary>
    /// 过场景的时候清空所有资源
    /// </summary>
    /// <param name="callBack"></param>
    public void ClearDic(UnityAction callBack)
    {
        MonoManager.Instance.StartCoroutine(ReallyClearDic(callBack));
    }

    private IEnumerator ReallyClearDic(UnityAction callBack)
    {
        //记录清空前是否存在缓存，避免无变化时触发无意义刷新
        bool hadCachedResources = resDic.Count > 0;
        resDic.Clear();
        //字典清空后立即让监视器清空显示内容
        if (hadCachedResources)
            NotifyDebugSnapshotChanged();

        AsyncOperation ao = Resources.UnloadUnusedAssets();
        yield return ao;
        callBack?.Invoke();
    }
    #endregion

    #region 通用函数
#if UNITY_EDITOR
    /// <summary>
    /// 获取资源缓存的只读调试快照
    /// </summary>
    /// <returns></returns>
    public List<ResCacheDebugInfo> GetDebugSnapshot()
    {
        List<ResCacheDebugInfo> snapshot = new List<ResCacheDebugInfo>(resDic.Count);
        for (Dictionary<string, ResInfoBase>.Enumerator enumerator = resDic.GetEnumerator(); enumerator.MoveNext();)
        {
            ResInfoBase info = enumerator.Current.Value;
            snapshot.Add(new ResCacheDebugInfo(
                info.path,
                info.typeName,
                info.state,
                info.refCount,
                info.AssetObject));
        }
        return snapshot;
    }
#endif

    /// <summary>
    /// 获取资源引用数
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="path">Resources文件夹下的路径</param>
    /// <returns></returns>
    public int GetRefCount<T>(string path) where T : UnityEngine.Object
    {
        string resName = path + "_" + typeof(T).FullName;
        if (resDic.ContainsKey(resName))
        {
            return (resDic[resName] as ResInfo<T>).refCount;
        }
        return 0;
    }
    #endregion
}
}
