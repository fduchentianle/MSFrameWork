using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MSFrame
{

public class TimerManager : BaseManager<TimerManager>
{
    private int TIMER_KEY = 0;
    //受Time.timescale影响的计时器
    private Dictionary<int, TimerItem> timerDic = new Dictionary<int, TimerItem>();
    //不受Time.timescale影响的计时器
    private Dictionary<int, TimerItem> realTimerDic = new Dictionary<int, TimerItem>();
    //要移除的列表
    private List<TimerItem> delList = new List<TimerItem>();
    private Coroutine timer;
    private Coroutine realTimer;

    //计时器管理器中唯一计时用的间隔时间
    private const float intervalTime = 0.1f;
    private TimerManager()
    {
        StartTimer();
        StartRealTimer();
    }

    private IEnumerator StartTiming(bool isRealTime, Dictionary<int, TimerItem> dic)
    {
        while (true)
        {
            if (isRealTime)
                yield return new WaitForSecondsRealtime(intervalTime);
            else
                yield return new WaitForSeconds(intervalTime);

            foreach (var item in dic.Values)
            {
                if (!item.isRunning)
                    continue;
                item.allTime -= (int)(intervalTime * 1000);
                item.intervalTime -= (int)(intervalTime * 1000);
                if (item.intervalTime <= 0)
                {
                    item.callBack?.Invoke();
                    item.intervalTime = item.maxIntervalTime;
                }
                if (item.allTime <= 0)
                {
                    item.overCallBack?.Invoke();
                    delList.Add(item);
                }
            }

            for (int i = 0; i < delList.Count; i++)
            {
                dic.Remove(delList[i].keyID);
                PoolManager.Instance.PushObj<TimerItem>(delList[i]);
            }
            delList.Clear();
        }
    }

    #region 关闭开启计时器
    /// <summary>
    /// 关闭收timescale影响的计时器
    /// </summary>
    public void StopTimer()
    {
        if (timer == null)
            return;
        MonoManager.Instance.StopCoroutine(timer);
        timer = null;
    }

    /// <summary>
    /// 关闭不受timescale影响的计时器
    /// </summary>
    public void StopRealTimer()
    {
        if (realTimer == null)
            return;
        MonoManager.Instance.StopCoroutine(realTimer);
        realTimer = null;
    }

    /// <summary>
    /// 关闭收timescale影响的计时器
    /// </summary>
    public void StartTimer()
    {
        if (timer == null)
           timer = MonoManager.Instance.StartCoroutine(StartTiming(false, timerDic));
    }

    /// <summary>
    /// 关闭不受timescale影响的计时器
    /// </summary>
    public void StartRealTimer()
    {
        if (realTimer == null)
            realTimer = MonoManager.Instance.StartCoroutine(StartTiming(true, realTimerDic));
    }

    /// <summary>
    /// 关闭所有计时器
    /// </summary>
    public void StopAllTimer()
    {
        StopTimer();
        StopRealTimer();
    }
    #endregion

    #region 创建计时器
    /// <summary>
    /// 创建单个受timescale影响的计时器
    /// </summary>
    /// <param name="allTime">总的时间 毫秒</param>
    /// <param name="overCallBack">总时间结束回调</param>
    /// <param name="intervalTime">间隔计时时间</param>
    /// <param name="callBack">间隔计时时间结束回调</param>
    /// <returns>返回唯一ID 用于外部控制对应计时器</returns>
    public int CreateTimer(int allTime, UnityAction overCallBack, int intervalTime, UnityAction callBack = null)
    {
        TimerItem timer = PoolManager.Instance.GetObj<TimerItem>();
        int keyID = ++TIMER_KEY;
        timer.InitInfo(keyID, allTime, overCallBack, intervalTime, callBack);
        timerDic.Add(keyID, timer);
        return keyID;
    }

    /// <summary>
    /// 创建单个受timescale影响的计时器
    /// </summary>
    /// <param name="allTime">总的时间 毫秒</param>
    /// <param name="overCallBack">总时间结束回调</param>
    /// <param name="intervalTime">间隔计时时间</param>
    /// <param name="callBack">间隔计时时间结束回调</param>
    /// <returns>返回唯一ID 用于外部控制对应计时器</returns>
    public int CreateRealTimer(int allTime, UnityAction overCallBack, int intervalTime = 0, UnityAction callBack = null)
    {
        TimerItem timer = PoolManager.Instance.GetObj<TimerItem>();
        int keyID = ++TIMER_KEY;
        timer.InitInfo(keyID, allTime, overCallBack, intervalTime, callBack);
        realTimerDic.Add(keyID, timer);
        return keyID;
    }
    #endregion

    #region 移除计时器
    /// <summary>
    /// 移除单个受timescale影响的计时器
    /// </summary>
    /// <param name="keyID">唯一ID</param>
    public void RemoveTimer(int keyID)
    {
        if (timerDic.ContainsKey(keyID))
        {
            //移除对应id计时器 放入缓存池
            PoolManager.Instance.PushObj<TimerItem>(timerDic[keyID]);
            //从字典中移除
            timerDic.Remove(keyID);
        }
        else
            Debug.LogWarning("MSFrame: " + keyID + " 对应的计时器不存在");
    }

    /// <summary>
    /// 移除单个不受timescale影响的计时器
    /// </summary>
    /// <param name="keyID">唯一ID</param>
    public void RemoveRealTimer(int keyID)
    {
        if (realTimerDic.ContainsKey(keyID))
        {
            //移除对应id计时器 放入缓存池
            PoolManager.Instance.PushObj<TimerItem>(realTimerDic[keyID]);
            //从字典中移除
            realTimerDic.Remove(keyID);
        }
        else
            Debug.LogWarning("MSFrame: " + keyID + " 对应的计时器不存在");
    }
    #endregion

    #region 重置计时器
    /// <summary>
    /// 重置单个受timescale计时器
    /// </summary>
    /// <param name="keyID">唯一ID</param>
    public void ResetTimer(int keyID)
    {
        if (timerDic.ContainsKey(keyID))
            timerDic[keyID].ResetTimer();
        else
            Debug.LogWarning("MSFrame: " + keyID + " 对应的计时器不存在");
    }

    /// <summary>
    /// 重置单个不受timescale计时器
    /// </summary>
    /// <param name="keyID"></param>
    public void ResetRealTimer(int keyID)
    {
        if (realTimerDic.ContainsKey(keyID))
            realTimerDic[keyID].ResetTimer();
        else
            Debug.LogWarning("MSFrame: " + keyID + " 对应的计时器不存在");
    }


    #endregion

    #region 开启计时器
    /// <summary>
    /// 开启单个受timescale计时器
    /// </summary>
    /// <param name="keyID">计时器ID</param>
    public void StartTimer(int keyID)
    {
        if (timerDic.ContainsKey(keyID))
            timerDic[keyID].isRunning = true;
        else
            Debug.LogWarning("MSFrame: " + keyID + " 对应的计时器不存在");
    }

    /// <summary>
    /// 开启单个不受timescale计时器
    /// </summary>
    /// <param name="keyID">计时器ID</param>
    public void StartRealTimer(int keyID)
    {
        if (realTimerDic.ContainsKey(keyID))
            realTimerDic[keyID].isRunning = true;
        else
            Debug.LogWarning("MSFrame: " + keyID + " 对应的计时器不存在");
    }
    #endregion

    #region 停止计时器
    /// <summary>
    /// 停止单个受timescale计时器
    /// </summary>
    /// <param name="keyID">计时器ID</param>
    public void StopTimer(int keyID)
    {
        if (timerDic.ContainsKey(keyID))
            timerDic[keyID].isRunning = false;
        else
            Debug.LogWarning("MSFrame: " + keyID + " 对应的计时器不存在");
    }

    /// <summary>
    /// 停止单个不受timescale计时器
    /// </summary>
    /// <param name="keyID">计时器ID</param>
    public void StopRealTimer(int keyID)
    {
        if (realTimerDic.ContainsKey(keyID))
            realTimerDic[keyID].isRunning = false;
        else
            Debug.LogWarning("MSFrame: " + keyID + " 对应的计时器不存在");
    }
    #endregion
}

[Pool(maxNum = 100)]
/// <summary>
/// 计时器对象 里面存储了计时器的相关数据
/// </summary>
public class TimerItem : IPoolObject
{
    public int keyID;
    //计时结束后的回调
    public UnityAction overCallBack;
    //间隔执行回调的回调
    public UnityAction callBack;
    public int allTime;
    public int maxAllTime;
    public int intervalTime;
    public int maxIntervalTime;
    public bool isRunning = true;

    /// <summary>
    /// 初始化计时器数据
    /// </summary>
    /// <param name="keyID">唯一ID</param>
    /// <param name="allTime">总的时间</param>
    /// <param name="overCallBack">总时间计时后的回调函数</param>
    /// <param name="intervalTime">间隔执行的时间</param>
    /// <param name="callBack">间隔执行时间结束后的回调</param>
    public void InitInfo(int keyID, int allTime, UnityAction overCallBack, int intervalTime, UnityAction callBack = null)
    {
        this.keyID = keyID;
        this.allTime = this.maxAllTime = allTime;
        this.overCallBack = overCallBack;
        this.callBack = callBack;
        this.intervalTime = this.maxIntervalTime = intervalTime;
    }

    /// <summary>
    /// 缓存池回收时 清除相关引用数据
    /// </summary>
    public void ResetInfo()
    {
        overCallBack = null;
        callBack = null;
        isRunning = true;
    }

    /// <summary>
    /// 重置计时器
    /// </summary>
    public void ResetTimer()
    {
        this.allTime = this.maxAllTime;
        this.intervalTime = this.maxIntervalTime;
        this.isRunning = true;
    }
}
}
