
namespace MSFrame
{

/// <summary>
/// 想要被复用的 数据结构类 逻辑类 都必须继承的接口
/// </summary>
public interface IPoolObject
{
    /// <summary>
    /// 重置数据的方法
    /// </summary>
    void ResetInfo();
}
}
