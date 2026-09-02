using System;
using System.Reflection;
using UnityEngine;

namespace MSFrame
{

/// <summary>
/// 不继承Mono的单例模式基类
/// 两遍判空和加锁操作 节约性能 防止多线程并发问题
/// 子类需要显示定义私有无参构造函数
/// </summary>
public abstract class BaseManager<T> where T: class
{
    //用于加锁的对象
    protected static readonly object lockObj = new object();
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock(lockObj)
                {
                    if (instance == null)
                    {
                        //利用反射得到无参私有构造函数 实现对象的实例化
                        Type type = typeof(T);
                        ConstructorInfo info = type.GetConstructor(
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                            null,
                            Type.EmptyTypes,
                            null);
                        if (info != null)
                            instance = info.Invoke(null) as T;
                        else
                            Debug.LogWarning("MSFrame: 没有得到私有的无参构造函数");
                    }
                    return instance;
                }
            }
            return instance;
        }
    }
}
}
