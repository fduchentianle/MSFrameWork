
namespace MSFrame
{

/*
 * TIPS：使用状态机的对象要继承IStateMachineOwner的接口
 * 并且在StateBase的子类中添加对应的字段T owner
 * Example
 * public class Player : IStateMachineOwner
 * public class PlayerIdleState : StateBase
 * {
 *     public Player owner
 * }
 */

[Pool(maxNum = 100)]
public abstract class StateBase : IPoolObject
{    
    //接管的状态机
    protected StateMachine stateMachine;

    /// <summary>
    /// 初始化状态(第一次创建的时候执行) 子类需重写给owner赋值
    /// </summary>
    /// <param name="owner">宿主</param>
    /// <param name="machine">所属状态机</param>
    public virtual void Init(IStateMachineOwner owner, StateMachine machine)
    {
        this.stateMachine = machine;
    }

    /// <summary>
    /// 不再使用时 放回对象池调用 子类必须重写
    /// 子类必须加上this.PushObj();
    /// </summary>
    public abstract void UnInit();

    /// <summary>
    /// 放回对象池要进行清空引用
    /// 子类需要重写 将owner置空
    /// </summary>
    public virtual void ResetInfo()
    {
        stateMachine = null;
    }

    public virtual void Enter() { }

    public virtual void FixedUpdate() { }

    public virtual void Update() { }

    public virtual void LateUpdate() { }

    public virtual void Exit() { }


}
}
