using System;
using UnityEngine;
using UnityEngine.Events;

namespace MSFrame
{

public class MathUtil
{
	#region 角度和弧度
	/// <summary>
	/// 角度转弧度的方法
	/// </summary>
	/// <param name="deg">角度</param>
	/// <returns></returns>
	public static float Deg2Rad(float deg)
	{
		return deg * Mathf.Deg2Rad;
	}

	/// <summary>
	/// 弧度转角度的方法
	/// </summary>
	/// <param name="rad">弧度</param>
	/// <returns></returns>
	public static float Rad2Deg(float rad)
	{
		return rad * Mathf.Rad2Deg;
	}
	#endregion

	#region 距离计算相关
	/// <summary>
	/// 获取XZ平面上的两点的距离
	/// </summary>
	/// <param name="srcPos">点1</param>
	/// <param name="targetPos">点2</param>
	/// <returns></returns>
	public static float GetObjDistanceXZ(Vector3 srcPos, Vector3 targetPos)
	{
        srcPos.y = 0;
		targetPos.y = 0;
		return Vector3.Distance(srcPos,targetPos);
	}

    /// <summary>
    /// 获取XY平面上的两点的距离
    /// </summary>
    /// <param name="srcPos">点1</param>
    /// <param name="targetPos">点2</param>
    /// <returns></returns>

    public static float GetObjDistanceXY(Vector3 srcPos, Vector3 targetPos)
    {
        srcPos.z = 0;
        targetPos.z = 0;
        return Vector3.Distance(srcPos, targetPos);
    }

    /// <summary>
    /// 判断两点之间XZ距离 是否小于等于目标距离
    /// </summary>
    /// <param name="srcPos">点1</param>
    /// <param name="targetPos">点2</param>
    /// <param name="distance">目标距离</param>
    /// <returns></returns>
    public static bool CheckObjDistanceXZ(Vector3 srcPos, Vector3 targetPos, float distance)
    {
        return GetObjDistanceXZ(srcPos, targetPos) <= distance ? true : false;
    }

    /// <summary>
    /// 判断两点之间XY距离 是否小于等于目标距离
    /// </summary>
    /// <param name="srcPos">点1</param>
    /// <param name="targetPos">点2</param>
    /// <param name="distance">目标距离</param>
    /// <returns></returns>
    public static bool CheckObjDistanceXY(Vector3 srcPos, Vector3 targetPos, float distance)
    {
        return GetObjDistanceXY(srcPos, targetPos) <= distance ? true : false;
    }
    #endregion

    #region 位置判断相关
    /// <summary>
    /// 判断世界坐标系下的某一点是否在屏幕外 如果在屏幕外返回true
    /// </summary>
    /// <param name="pos">世界坐标系下一点</param>
    /// <returns></returns>
    public static bool IsWorldPosOutScreen(Vector3 pos)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(pos);
        if (screenPos.z <= 0)
            return true;
        if (screenPos.x >= 0 && screenPos.x <= Screen.width && screenPos.y >= 0 && screenPos.y <= Screen.height)
            return false;
        return true;
    }

    /// <summary>
    /// 判断某一个位置 是否在指定扇形范围内
    /// </summary>
    /// <param name="pos">扇形中心点位置</param>
    /// <param name="forward">自己的面朝向</param>
    /// <param name="targetPos">目标对象</param>
    /// <param name="radius">半径</param>
    /// <param name="angle">扇形的角度的一半</param>
    /// <returns></returns>
    public static bool IsInSectorRangeXZ(Vector3 pos, Vector3 forward, Vector3 targetPos, float radius, float angle)
    {
        pos.y = 0;
        forward.y = 0;
        targetPos.y = 0;
        //判断距离是否在半径内 角度是否在扇形角度内
        return Vector3.Distance(pos, targetPos) <= radius && Vector3.Angle(forward, targetPos - pos) <= angle ? true : false;
    }
    #endregion

    #region 射线检测相关
    /// <summary>
    /// 射线检测单个对象
    /// </summary>
    /// <param name="ray">射线</param>
    /// <param name="maxDistance">最大距离</param>
    /// <param name="layerMask">层级</param>
    /// <param name="callBack">回调函数</param>
    public static void RayCast(Ray ray, float maxDistance, int layerMask, UnityAction<RaycastHit> callBack)
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, maxDistance, layerMask))
            callBack?.Invoke(hitInfo);
    }

    public static void RayCast(Ray ray, float maxDistance, int layerMask, UnityAction<GameObject> callBack)
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, maxDistance, layerMask))
            callBack?.Invoke(hitInfo.collider.gameObject);
    }

    public static void RayCast<T>(Ray ray, float maxDistance, int layerMask, UnityAction<T> callBack) where T : Component
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, maxDistance, layerMask))
        {
            T component = hitInfo.collider.gameObject.GetComponent<T>();
            if (component != null)
                callBack?.Invoke(component);
            else
                Debug.LogWarning("MSFrame: " + hitInfo.collider.gameObject.name +"身上没有挂载" + typeof(T).Name + "组件");
        }
    }

    /// <summary>
    /// 射线检测所有对象
    /// </summary>
    /// <param name="ray">射线</param>
    /// <param name="maxDistance">最大距离</param>
    /// <param name="layerMask">层级</param>
    /// <param name="callBack">回调函数</param>
    public static void RayCastAll(Ray ray, float maxDistance, int layerMask, UnityAction<RaycastHit> callBack)
    {
        RaycastHit[] hitInfos = Physics.RaycastAll(ray, maxDistance, layerMask);
        for (int i = 0; i < hitInfos.Length; i++)
        {
            callBack?.Invoke(hitInfos[i]);
        }
    }

    public static void RayCastAll(Ray ray, float maxDistance, int layerMask, UnityAction<GameObject> callBack)
    {
        RaycastHit[] hitInfos = Physics.RaycastAll(ray, maxDistance, layerMask);
        for (int i = 0; i < hitInfos.Length; i++)
        {
            callBack?.Invoke(hitInfos[i].collider.gameObject);
        }
    }

    public static void RayCastAll<T>(Ray ray, float maxDistance, int layerMask, UnityAction<T> callBack) where T : Component
    {
        RaycastHit[] hitInfos = Physics.RaycastAll(ray, maxDistance, layerMask);
        for (int i = 0; i < hitInfos.Length; i++)
        {
            T component = hitInfos[i].collider.gameObject.GetComponent<T>();
            if (component != null)
                callBack?.Invoke(component);
            else
                Debug.LogWarning("MSFrame: " + hitInfos[i].collider.gameObject.name + "身上没有挂载" + typeof(T).Name + "组件");
        }
    }
    #endregion

    #region 范围检测
    /// <summary>
    /// 进行盒装范围检测
    /// </summary>
    /// <typeparam name="T">获取信息类型</typeparam>
    /// <param name="center">盒装中心点</param>
    /// <param name="rotation">盒子的角度</param>
    /// <param name="halfExtents">长宽高的一半</param>
    /// <param name="layerMask">层级</param>
    /// <param name="callBack">回调函数</param>
    public static void OverlapBox<T>(Vector3 center, Quaternion rotation, Vector3 halfExtents, int layerMask, UnityAction<T> callBack) where T : class
    {
        Type type = typeof(T);
        Collider[] colliders = Physics.OverlapBox(center, halfExtents, rotation, layerMask);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (type == typeof(Collider))
                callBack?.Invoke(colliders[i] as T);
            else if (type == typeof(GameObject))
                callBack?.Invoke(colliders[i].gameObject as T);
            else
            {
                T component = colliders[i].gameObject.GetComponent<T>();
                if (component != null)
                    callBack?.Invoke(component);
            }
        }
    }

    /// <summary>
    /// 进行球状范围检测
    /// </summary>
    /// <typeparam name="T">获取信息类型</typeparam>
    /// <param name="center">球体的中心点</param>
    /// <param name="radius">球体的半径</param>
    /// <param name="layerMask">层级</param>
    /// <param name="callBack">回调函数</param>
    public static void OverlapSphere<T>(Vector3 center, float radius, int layerMask, UnityAction<T> callBack) where T : class
    {
        Type type = typeof(T);
        Collider[] colliders = Physics.OverlapSphere(center, radius, layerMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (type == typeof(Collider))
                callBack?.Invoke(colliders[i] as T);
            else if (type == typeof(GameObject))
                callBack?.Invoke(colliders[i].gameObject as T);
            else
            {
                T component = colliders[i].gameObject.GetComponent<T>();
                if (component != null)
                    callBack?.Invoke(component);
            }
        }
    }
    #endregion
}
}
