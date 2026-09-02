using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MSFrame
{

/// <summary>
/// 层级枚举
/// </summary>
public enum E_UILayer
{
    Bottom,
    Middle,
    Top,
    System,
}

/// <summary>
/// 管理所有UI面板的管理器
/// 注意：面板预设体要与面板类型一致
/// </summary>
public class UIManager : BaseManager<UIManager>
{
    /// <summary>
    /// 主要用于里氏替换原则 在字典中 父类装子类
    /// </summary>
    private abstract class BasePanelInfo { }

    /// <summary>
    /// 用于存储面板信息 和 加载完成的回调函数
    /// </summary>
    private class PanelInfo<T> : BasePanelInfo where T : BasePanel
    {
        public T panel;
        public UnityAction<T> callBack;
        public bool isHide = false;
        public PanelInfo(UnityAction<T> callBack)
        {
            this.callBack += callBack;
        }
    }

    private Camera uiCamera;
    private Canvas uiCanvas;
    private EventSystem uiEventSystem;

    //层级父对象
    private Transform bottomLayer;
    private Transform middleLayer;
    private Transform topLayer;
    private Transform systemLayer;

    //用来存储所有面板的容器
    private Dictionary<string, BasePanelInfo> panelDic = new Dictionary<string, BasePanelInfo>();

    //每个UI层级对应的透明射线遮罩
    private Dictionary<Transform, Image> layerMaskDic = new Dictionary<Transform, Image>();

    private UIManager()
    {
        //动态创建唯一的Canvas和EventSystem（摄像机）
        uiCamera = GameObject.Instantiate(ResManager.Instance.Load<GameObject>("UI/UICamera")).GetComponent<Camera>();
        //ui摄像机过场景不移除 专门用来渲染UI面板
        GameObject.DontDestroyOnLoad(uiCamera.gameObject);

        //动态创建Canvas
        uiCanvas = GameObject.Instantiate(ResManager.Instance.Load<GameObject>("UI/Canvas")).GetComponent<Canvas>();
        //设置使用的UI摄像机
        uiCanvas.worldCamera = uiCamera;
        //过场景不移除
        GameObject.DontDestroyOnLoad(uiCanvas.gameObject);

        //找到层级父对象
        bottomLayer = uiCanvas.transform.Find("Bottom");
        middleLayer = uiCanvas.transform.Find("Middle");
        topLayer = uiCanvas.transform.Find("Top");
        systemLayer = uiCanvas.transform.Find("System");

        CreateLayerMask(bottomLayer);
        CreateLayerMask(middleLayer);
        CreateLayerMask(topLayer);
        CreateLayerMask(systemLayer);

        //动态创建EventSystem
        uiEventSystem = GameObject.Instantiate(ResManager.Instance.Load<GameObject>("UI/EventSystem")).GetComponent<EventSystem>();
        GameObject.DontDestroyOnLoad(uiEventSystem.gameObject);
    }
    
    /// <summary>
    /// 存储已经场景里已经存在的面板
    /// </summary>
    private bool ShowLoadedPanel<T>(string panelName, UnityAction<T> callBack = null) where T : BasePanel
    {
        //如果字典中存储了此面板
        if (panelDic.ContainsKey(panelName))
        {
            PanelInfo<T> panelInfo = panelDic[panelName] as PanelInfo<T>;
            //如果正在异步加载
            if (panelInfo.panel == null)
            {
                //如果之前显示了隐藏 之后又显示
                //把第一次的callBack清空 之后还是正常加载的
                panelInfo.isHide = false;
                //先将回调函数记录下来 当异步加载结束后会自动调用
                if (callBack != null)
                    panelInfo.callBack += callBack;
            }
            //如果已经加载完成
            //分为两种情况
            //一是已经显示了 此时只需执行ShowMe和调用CallBack
            //二是没有显示 对应之前让面板隐藏但不销毁 需要让对象激活
            else
            {
                if (!panelInfo.panel.gameObject.activeSelf)
                    panelInfo.panel.gameObject.SetActive(true);
                PutPanelOnTop(panelInfo.panel);
                panelInfo.panel.ShowMe();
                callBack?.Invoke(panelInfo.panel);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Resources同步加载面板
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <param name="layer">UI层级</param>
    /// <param name="callBack">回调函数</param>
    public void ShowPanel<T>(E_UILayer layer = E_UILayer.Middle, UnityAction<T> callBack = null) where T : BasePanel
    {
        //获取面板名 预设体名称必须和面板类型一致
        string panelName = typeof(T).Name;

        //如果字典里又存储此面板
        if (ShowLoadedPanel<T>(panelName, callBack))
            return;

        //如果字典里没有存储此面板 进行同步加载
        Transform father = GetLayerFather(layer);
        if (father == null)
            father = middleLayer;
        GameObject panelRes = ResManager.Instance.Load<GameObject>("UI/" + panelName);
        if (panelRes == null)
            return;
        GameObject panelObj = GameObject.Instantiate(panelRes, father, false);
        panelObj.name = panelName;
        //得到面板脚本 加入字典
        T panel = panelObj.GetComponent<T>();
        if (panel == null)
        {
            Debug.LogWarning("MSFrame: " + panelName + "面板没有挂对应UI脚本");
            return;
        }
        PanelInfo<T> info = new PanelInfo<T>(callBack);
        info.panel = panel;
        panelDic.Add(panelName,info);
        PutPanelOnTop(panel);
        panel.ShowMe();
        info.callBack?.Invoke(panel);
        info.callBack = null;
    }

    /// <summary>
    /// Resources异步加载面板
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <param name="layer">UI层级</param>
    /// <param name="callBack">回调函数</param>

    public void ShowPanelAsync<T>(E_UILayer layer = E_UILayer.Middle, UnityAction<T> callBack = null) where T : BasePanel
    {
        //获取面板名 预设体名称必须和面板类型一致
        string panelName = typeof(T).Name;

        //如果字典里又存储此面板
        if (ShowLoadedPanel<T>(panelName, callBack))
            return;

        //如果字典里不存在此面板 先占个位置
        panelDic.Add(panelName, new PanelInfo<T>(callBack));

        //异步加载
        ResManager.Instance.LoadAsync<GameObject>("UI/" + panelName, (panelRes) =>
        {
            //取出字典中已经占好位置的数据
            PanelInfo<T> panelInfo = panelDic[panelName] as PanelInfo<T>;

            //表示异步加载结束前 我们想要隐藏该面板
            if (panelInfo.isHide)
            {
                panelDic.Remove(panelName);
                ResManager.Instance.UnloadAsset<GameObject>("UI/" + panelName);
                return;
            }

            //层级的处理
            Transform father = GetLayerFather(layer);
            if (father == null)
                father = middleLayer;

            //将面板预设体创建到对应父对象下 并且保持原本的缩放大小不变
            GameObject panelObj = GameObject.Instantiate(panelRes, father, false);
            panelObj.name = panelName;

            //获取对应的UI组件返回出去
            T panel = panelObj.GetComponent<T>();
            if (panel == null)
            {
                Debug.LogWarning("MSFrame: " + panelName + "面板没有挂对应UI脚本");
                return;
            }

            panelInfo.panel = panel;
            PutPanelOnTop(panel);
            panel.ShowMe();
            panelInfo.callBack?.Invoke(panel);
            panelInfo.callBack = null;
        });
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <param name="isDestroy">是否销毁</param>
    public void HidePanel<T>(bool isDestroy = false) where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if (panelDic.ContainsKey(panelName))
        {
            //取出字典中已经占好位置的数据
            PanelInfo<T> panelInfo = panelDic[(panelName)] as PanelInfo<T>;
            //如果在加载中
            if (panelInfo.panel == null)
            {
                panelInfo.isHide = true;
                panelInfo.callBack = null;
            }
            //如果已经加载完毕
            else
            {
                Transform layer = panelInfo.panel.transform.parent;
                panelInfo.panel.HideMe();
                panelInfo.panel.gameObject.SetActive(false);
                RefreshLayerMask(layer);
                if (isDestroy)
                {
                    GameObject.Destroy(panelInfo.panel.gameObject);
                    ResManager.Instance.UnloadAsset<GameObject>("UI/" + panelName);
                    panelDic.Remove(panelName);
                }
            }
        }
    }

    /// <summary>
    /// 为UI层级创建一个不可见的全屏射线遮罩
    /// </summary>
    private void CreateLayerMask(Transform layer)
    {
        RectTransform layerRect = layer as RectTransform;
        layerRect.anchorMin = Vector2.zero;
        layerRect.anchorMax = Vector2.one;
        layerRect.offsetMin = Vector2.zero;
        layerRect.offsetMax = Vector2.zero;

        GameObject maskObj = new GameObject("RaycastMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        maskObj.layer = layer.gameObject.layer;

        RectTransform maskRect = maskObj.GetComponent<RectTransform>();
        maskRect.SetParent(layer, false);
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;
        maskRect.SetAsFirstSibling();

        Image maskImage = maskObj.GetComponent<Image>();
        maskImage.color = Color.clear;
        maskImage.raycastTarget = false;
        layerMaskDic.Add(layer, maskImage);
    }

    /// <summary>
    /// 将面板放到当前层级最上方，并用遮罩挡住其下方的面板
    /// </summary>
    private void PutPanelOnTop(BasePanel panel)
    {
        panel.transform.SetAsLastSibling();
        RefreshLayerMask(panel.transform.parent);
    }

    /// <summary>
    /// 将透明遮罩放在当前层级最上方的激活面板下面
    /// </summary>
    private void RefreshLayerMask(Transform layer)
    {
        Image maskImage = layerMaskDic[layer];
        maskImage.transform.SetAsLastSibling();

        Transform topPanel = null;
        for (int i = layer.childCount - 2; i >= 0; i--)
        {
            Transform child = layer.GetChild(i);
            if (child.gameObject.activeSelf && child.GetComponent<BasePanel>() != null)
            {
                topPanel = child;
                break;
            }
        }

        maskImage.raycastTarget = topPanel != null;
        if (topPanel == null)
            maskImage.transform.SetAsFirstSibling();
        else
            maskImage.transform.SetSiblingIndex(topPanel.GetSiblingIndex());
    }

    /// <summary>
    /// 获取面板
    /// </summary>
    /// <typeparam name="T">面板类型</typeparam>
    /// <param name="callBack">回调函数</param>
    public void GetPanel<T>(UnityAction<T> callBack) where T : BasePanel
    {
        string panelName = typeof(T).Name;
        if (panelDic.ContainsKey(panelName))
        {
            //取出字典里已经占好位置的数据
            PanelInfo<T> panelInfo = panelDic[panelName] as PanelInfo<T>;
            //正在加载
            if (panelInfo.panel == null)
                panelInfo.callBack += callBack;
            else if (!panelInfo.isHide)
                callBack?.Invoke(panelInfo.panel);
        }
    }

    public Transform GetLayerFather(E_UILayer layer)
    {
        switch (layer)
        {
            case E_UILayer.Bottom:
                return bottomLayer;
            case E_UILayer.Middle:
                return middleLayer;
            case E_UILayer.Top:
                return topLayer;
            case E_UILayer.System:
                return systemLayer;
            default:
                return null;
        }
    }
}
}
