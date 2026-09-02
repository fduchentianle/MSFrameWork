using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace MSFrame
{

/// <summary>
/// 对象池中GameObject的资源加载方式
/// </summary>
public enum E_GameObjectState
{
    ResLoad,
    ABLoad,
}

/// <summary>
/// 对象池中GameObject对象
/// </summary>
public class PoolGameObject
{
    //缓存池中对象的上限个数
    private int maxNum;

    //缓存池中对象的资源加载方式
    private E_GameObjectState state;

    //用来记录对象池中的对象
    private Stack<GameObject> gameObjectStack = new Stack<GameObject>();

    //对象池中对象的数量
    public int Count => gameObjectStack.Count;

    //抽屉根对象 用来进行布局管理的对象
    private GameObject rootObj;

    public PoolGameObject(GameObject root, string name, int maxNum, E_GameObjectState state)
    {
        //创建抽屉父对象
        rootObj = new GameObject(name);
        rootObj.transform.SetParent(root.transform);

        this.maxNum = maxNum;
        this.state = state;
    }

    /// <summary>
    /// 从对象池中取GameObject
    /// </summary>
    public GameObject Pop()
    {
        GameObject obj = null;
        //如果对象池中还有空闲的对象
        if (Count > 0)
        {
            obj = gameObjectStack.Pop();
            obj.SetActive(true);
            obj.transform.SetParent(null);
        }
        return obj;
    }

    /// <summary>
    /// 向对象池中放入GameObject
    /// </summary>
    public void Push(GameObject obj)
    {
        //如果对象池已满
        if (Count >= maxNum )
        {
            //直接销毁对象
            UnityEngine.Object.Destroy(obj);
            //如果是Resources进行加载的，则需要卸载一次资源
            if (state == E_GameObjectState.ResLoad)
                ResManager.Instance.UnloadAsset<GameObject>(obj.name);
            return;
        }
        //如果对象池还没满
        obj.SetActive(false);
        obj.transform.SetParent(rootObj.transform);
        // 清空根节点及所有子节点的 EventListener
        EventListener[] eventListeners = obj.GetComponentsInChildren<EventListener>(true);

        for (int i = 0; i < eventListeners.Length; i++)
        {
            eventListeners[i].RemoveAllListener();
        }
        gameObjectStack.Push(obj);
    }
}

/// <summary>
/// 方便再字典中用里氏替换原则 存储子类对象
/// </summary>
public abstract class PoolObjectBase { }

/// <summary>
/// 对象池中不继承Mono的对象
/// </summary>
public class PoolObject<T> : PoolObjectBase where T : class
{
    public Queue<T> poolObjs = new Queue<T>();
    public int maxNum;
    public int Count => poolObjs.Count;
}

public class PoolManager : BaseManager<PoolManager>
{
    //对象池的根对象
    private GameObject poolObj;
    //管理对象池中GameObject对象
    private Dictionary<string, PoolGameObject> poolGameObjDic = new Dictionary<string, PoolGameObject>();
    //管理对象池中Object对象
    private Dictionary<string, PoolObjectBase> poolObjDic = new Dictionary<string, PoolObjectBase>();

    private PoolManager() { }

    /// <summary>
    /// 场景切换后Pool根节点会被销毁，此时旧池中的GameObject也已经失效
    /// </summary>
    private void CheckPoolRoot()
    {
        if (poolObj != null)
            return;

        poolGameObjDic.Clear();
        poolObj = new GameObject("Pool");
    }

    /// <summary>
    /// 获取普通对象对应的缓存池，不存在时创建
    /// </summary>
    private PoolObject<T> GetOrCreatePool<T>() where T : class, IPoolObject
    {
        string poolName = typeof(T).FullName;
        if (poolObjDic.ContainsKey(poolName))
            return poolObjDic[poolName] as PoolObject<T>;

        PoolAttribute attribute = typeof(T).GetCustomAttribute<PoolAttribute>();
        if (attribute == null)
        {
            Debug.LogWarning("MSFrame: " + typeof(T).Name + "从对象池中加载时候没有获取Pool特性");
            return null;
        }

        PoolObject<T> pool = new PoolObject<T>();
        pool.maxNum = attribute.maxNum;
        poolObjDic.Add(poolName, pool);
        return pool;
    }

    #region 从缓存池中取对象
    /// <summary>
    /// 从对象池中取GameObject对象(Resources同步加载)
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="name">资源地址</param>
    /// <returns></returns>
    public T GetGameObj<T>(string name) where T : MonoBehaviour
    {
        GameObject obj;

        CheckPoolRoot();

        //如果是第一次拿或对象池中没有空闲的对象 直接资源加载并实例化
        if (!poolGameObjDic.ContainsKey(name) || poolGameObjDic[name].Count == 0)
        {
            GameObject go = ResManager.Instance.Load<GameObject>(name);
            if (go == null)
                return null;
            obj = GameObject.Instantiate(go);
            obj.name = name;
            //获取Pool特性 并赋值maxNum
            if (!poolGameObjDic.ContainsKey(name))
            {
                PoolAttribute attribute = typeof(T).GetCustomAttribute<PoolAttribute>();
                if (attribute == null)
                {
                    Debug.LogWarning("MSFrame: " + typeof(T).Name + "从对象池中加载时没有获得Pool特性");
                    return null;
                }
                poolGameObjDic.Add(name, new PoolGameObject(poolObj, name, attribute.maxNum, E_GameObjectState.ResLoad));
            }
        }
        //如果对象池中有空闲对象 直接弹出使用
        else
            obj = poolGameObjDic[name].Pop();
        return obj.GetComponent<T>();
    }

    /// <summary>
    /// 从对象池中取GameObject对象(Resources异步加载)
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="name">资源地址</param>
    /// <param name="callBack">加载完毕后的回调函数</param>
    public void GetGameObjAsync<T>(string name, UnityAction<GameObject> callBack = null) where T: MonoBehaviour
    {
        GameObject obj;

        CheckPoolRoot();

        //如果是第一次拿或对象池中没有空闲的对象 直接资源加载并实例化
        if (!poolGameObjDic.ContainsKey(name) || poolGameObjDic[name].Count == 0)
        {
            ResManager.Instance.LoadAsync<GameObject>(name,(res)=>
            {
                CheckPoolRoot();
                obj = GameObject.Instantiate(res);
                obj.name = name;
                //如果是第一次拿 需要获取Pool特性 并赋值maxNum
                if (!poolGameObjDic.ContainsKey(name))
                {
                    PoolAttribute attribute = typeof(T).GetCustomAttribute<PoolAttribute>();
                    if (attribute == null)
                    {
                        Debug.LogWarning("MSFrame: " + typeof(T).Name + "从对象池中加载时没有获得Pool特性");
                        return;
                    }
                    poolGameObjDic.Add(name, new PoolGameObject(poolObj, name, attribute.maxNum, E_GameObjectState.ResLoad));
                }
                callBack?.Invoke(obj);
            });
            
        }
        //如果对象池中有空闲对象 直接弹出使用
        else
        {
            obj = poolGameObjDic[name].Pop();
            callBack?.Invoke(obj);
        }
    }

    /// <summary>
    /// 获取自定义的数据结构类和逻辑类对象
    /// </summary>
    /// <typeparam name="T">对应类型</typeparam>
    /// <returns></returns>
    public T GetObj<T>() where T: class, IPoolObject, new()
    {
        PoolObject<T> pool = GetOrCreatePool<T>();
        if (pool == null)
            return null;

        return pool.Count > 0 ? pool.poolObjs.Dequeue() : new T();
    }
    #endregion

    #region 从缓存池中放入对象
    /// <summary>
    /// 向对象池中放入GameObject对象
    /// </summary>
    /// <param name="obj">要放入的GameObject对象</param>
    public void PushGameObj(GameObject obj)
    {
        poolGameObjDic[obj.name].Push(obj);
    }

    /// <summary>
    /// 向对象池中放入Object对象
    /// </summary>
    /// <typeparam name="T">泛型类型</typeparam>
    /// <param name="obj">要放入的Obj对象</param>
    public void PushObj<T>(T obj) where T : class, IPoolObject
    {
        if (obj == null)
            return;

        PoolObject<T> pool = GetOrCreatePool<T>();
        if (pool == null)
            return;

        obj.ResetInfo();
        //如果存储的对象还没满 就放入对象池
        if (pool.Count < pool.maxNum)
            pool.poolObjs.Enqueue(obj);
    }
    #endregion

    #region 清空缓存池
    /// <summary>
    /// 清空所有缓存池
    /// </summary>
    public void ClearPool()
    {
        if (poolObj != null)
            GameObject.Destroy(poolObj);

        poolGameObjDic.Clear();
        poolObjDic.Clear();
        poolObj = null;
    }
    #endregion
}
}
