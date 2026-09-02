using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MSFrame
{

/// <summary>
/// 场景切换管理器 主要用于切换场景
/// </summary>
public class ChangeSceneManager : BaseManager<ChangeSceneManager>
{
    private ChangeSceneManager() { }

    /// <summary>
    /// 同步切换场景的方法
    /// </summary>
    /// <param name="name">场景名称</param>
    /// <param name="callBack">回调函数</param>
    public void LoadScene(string name, UnityAction callBack = null)
    {
        //切换场景
        SceneManager.LoadScene(name);
        //调用回调
        callBack?.Invoke();
    }

    /// <summary>
    /// 异步切换场景的方法
    /// </summary>
    /// <param name="name">场景名称</param>
    /// <param name="callBack">回调函数</param>
    public void LoadSceneAsync(string name, UnityAction callBack = null)
    {
        MonoManager.Instance.StartCoroutine(ReallyLoadSceneAsync(name,callBack));
    }

    private IEnumerator ReallyLoadSceneAsync(string name, UnityAction callBack)
    {
        AsyncOperation ao = SceneManager.LoadSceneAsync(name);
        //不停的在协同程序中每帧检测是否加载结束 如果加载结束 就不会进这个循环
        while (!ao.isDone)
        {
            //在这里利用事件中心 每一帧把进度发送给想要的地方
            EventCenter.Instance.EventTrigger<float>(EventType.E_SceneLoadChange, ao.progress);
            yield return null;
        }
        //避免最后一帧直接结束了 没有同步1出去
        EventCenter.Instance.EventTrigger<float>(EventType.E_SceneLoadChange, 1);
        callBack?.Invoke();
    }
}
}
