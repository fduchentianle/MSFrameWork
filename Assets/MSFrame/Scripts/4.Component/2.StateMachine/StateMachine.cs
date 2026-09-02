using System.Collections.Generic;

namespace MSFrame
{

/*
 * TIPS: 使用状态机的脚本需要继承IStateMachineOwner接口，且实现一个StateMachine字段
 * 在Awake中调用PoolManager.Instance.GetObj<StateMachine>()获取stateMachine;
 * 然后对StateMachine进行初始化 stateMachine.Init(this);
 */

/// <summary>
/// 需要用到状态机的类必须继承此接口
/// </summary>
public interface IStateMachineOwner { }

/// <summary>
/// 状态控制器
/// </summary>
[Pool(maxNum = 100)]
public class StateMachine : IPoolObject
{
    //当前状态
    public int CurrentStateNum { get; private set; } = -1;
    //当前生效的状态
    private StateBase currentStateObj;
    //宿主
    private IStateMachineOwner owner;
    //所有的状态 Key：状态枚举的值 Value：具体的状态
    private Dictionary<int, StateBase> stateDic = new Dictionary<int, StateBase>();

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="owner">宿主</param>
    public void Init(IStateMachineOwner owner)
    {
        this.owner = owner;
    }

    public bool ChangeState<T>(int newStateNum, bool reCurrentState = false) where T : StateBase, new()
    {
        //状态一致并且不需要刷新状态 切换失败
        if (newStateNum == CurrentStateNum && !reCurrentState)
        {
            return false;
        }

        //退出当前状态
        if (currentStateObj != null)
        {
            currentStateObj.Exit();
            currentStateObj.RemoveFixedUpdateListener(currentStateObj.FixedUpdate);
            currentStateObj.RemoveUpdateListener(currentStateObj.Update);
            currentStateObj.RemoveLateUpdateListener(currentStateObj.LateUpdate);
        }

        //进入新状态
        currentStateObj = GetState<T>(newStateNum);
        CurrentStateNum = newStateNum;
        currentStateObj.Enter();
        currentStateObj.AddFixedUpdateListener(currentStateObj.FixedUpdate);
        currentStateObj.AddUpdateListener(currentStateObj.Update);
        currentStateObj.AddLateUpdateListener(currentStateObj.LateUpdate);

        return true;
    }

    /// <summary>
    /// 从对象池中获取一个状态
    /// </summary>
    private StateBase GetState<T>(int stateNum) where T : StateBase, new()
    {
        //此状态如果已经被加载过 直接获取
        if (stateDic.ContainsKey(stateNum))
        {
            return stateDic[stateNum];
        }
        //如果状态还没有加载过 从对象池中加载 并放入字典
        T state = PoolManager.Instance.GetObj<T>();
        state.Init(owner,this);
        stateDic.Add(stateNum,state);
        return state;
    }

    /// <summary>
    /// 停止工作
    /// 把所有状态都释放 但是StateMachine未来还可以工作
    /// </summary>
    public void Stop()
    {
        //处理当前状态的额外逻辑
        if (currentStateObj != null)
        {
            currentStateObj.Exit();
            currentStateObj.RemoveFixedUpdateListener(currentStateObj.FixedUpdate);
            currentStateObj.RemoveUpdateListener(currentStateObj.Update);
            currentStateObj.RemoveLateUpdateListener(currentStateObj.LateUpdate);
        }
        CurrentStateNum = -1;
        currentStateObj = null;

        //处理缓存中那个所有状态的逻辑
        var enumerator = stateDic.GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerator.Current.Value.UnInit();
        }
        stateDic.Clear();
    }

    /// <summary>
    /// 销毁 宿主应该释放掉StateMachine的引用
    /// </summary>
    public void Destroy()
    {
        Stop();
        //放入对象池
        this.PushObj<StateMachine>();
    }
    
    /// <summary>
    /// 放入对象池时需要置空引用
    /// </summary>
    public void ResetInfo()
    {
        owner = null;
    }
}
}
