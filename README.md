# MSFrame

Unity 游戏开发框架：一套按职责分层的通用模块集合（单例、对象池、事件中心、资源管理、音频、UI、状态机、定时器、存档、配置、场景切换等），可直接在项目中复用。

## 环境要求

- Unity **2022.3 LTS**（开发版本 2022.3.59f1c1）
- 第三方依赖：
  - **Sirenix Odin Inspector**（付费资产，需自行从 Asset Store 导入；`Config`、`Save` 模块使用了 `SerializedScriptableObject` / `SerializationUtility` / `DictionaryDrawerSettings`）
  - **TextMeshPro**（Unity 内置，首次打开工程时按提示 Import TMP Essentials）

## 目录结构

```
Assets/MSFrame/Scripts/
├─ 1.Base       基础层：单例基类 / 扩展方法 / 对象池
├─ 2.System     系统层：配置 / 全局事件 / 存档 / 场景切换
├─ 3.Service    服务层：资源加载 / 音频 / Mono 帧驱动
├─ 4.Component  组件层：组件事件 / 状态机 / 数学工具 / 运行时监视器 / 定时器
└─ 5.UI         UI 层：面板管理 / 面板基类
```

完整 UML 类图见 **[docs/UML/MSFrame-UML.md](docs/UML/MSFrame-UML.md)**。

## 核心模块

| 模块 | 职责 |
|---|---|
| 单例 | `BaseManager<T>`（纯 C#）、`SingletonMono<T>`（手动挂载）、`SingletonAutoMono<T>`（自动挂载） |
| 对象池 | `PoolManager`：GameObject 池 + 普通对象池，配合 `[Pool(maxNum)]` 与 `IPoolObject` |
| 事件 | `EventCenter`（全局广播，0~3 参）；`EventListener` + `EventListenerExtension`（组件级，挂物体上） |
| 资源 | `ResManager`：Resources 同步/异步加载 + 引用计数卸载 |
| 音频 | `AudioManager`：背景音乐 + 音效（音效实例走对象池），音量经 `SaveManager` 持久化 |
| UI | `UIManager` + `BasePanel`：面板加载/显示/隐藏，控件按名字自动绑定与事件接线 |
| 状态机 | `StateMachine` + `StateBase`：`ChangeState<T>` 切换，状态对象池复用 |
| 定时器 | `TimerManager`：受/不受 timeScale 影响的两套倒计时 |
| 配置/存档 | `ConfigManager`（ScriptableObject 配置读取）、`SaveManager`（本地 JSON 存档） |
| 场景 | `ChangeSceneManager`：同步/异步切场景 + 进度事件 |
| 驱动 | `MonoManager`：把 Update/FixedUpdate/LateUpdate 广播为事件，并托管协程 |

## 快速使用

```csharp
// 1. 单例（三选一，按需继承）
public class MyManager : BaseManager<MyManager>          // 纯 C#，需私有无参构造
public class MyManager : SingletonMono<MyManager>        // 手动挂场景
public class MyManager : SingletonAutoMono<MyManager>    // 自动挂载（推荐）

// 2. 对象池
[Pool(maxNum = 100)]
public class Bullet : MonoBehaviour { }
Bullet b = PoolManager.Instance.GetGameObj<Bullet>("Bullet");
PoolManager.Instance.PushGameObj(b.gameObject);

// 3. 全局事件（0~3 参）
EventCenter.Instance.AddEventListener(EventType.E_SceneLoadChange, OnProgress);
EventCenter.Instance.EventTrigger(EventType.E_SceneLoadChange, 1.0f);

// 4. 组件事件（挂 EventListener 组件后）
button.OnClick((data, args) => { /* 处理点击 */ });

// 5. UI 面板
public class MainPanel : BasePanel {
    public override void ShowMe() { /* 显示逻辑 */ }
    public override void HideMe() { /* 隐藏逻辑 */ }
}
UIManager.Instance.ShowPanel<MainPanel>();

// 6. 状态机（宿主实现 IStateMachineOwner）
stateMachine.ChangeState<IdleState>(stateNum);

// 7. 定时器
TimerManager.Instance.CreateTimer(3000, () => { /* 3 秒后回调 */ });
```

## 注意事项

- **对象池**：入池须同时满足 `[Pool(maxNum)]` 特性 + 实现 `IPoolObject`（提供 `ResetInfo`），否则打 Warning 拒绝入池；`PushGameObj` 用 `obj.name` 作键，取/还必须同一资源名。
- **全局事件**：同一 `EventType` 的委托参数个数必须固定（0/1/2/3 参对应不同 `EventInfo` 子类），混用会强转失败。
- **单例**：`BaseManager<T>` 子类必须声明**私有无参构造**（供反射调用）；`SingletonMono<T>` 无懒加载，场景加载完成前 `Instance` 可能为 null。
- **状态机**：宿主须实现 `IStateMachineOwner`；`ChangeState<T>` 要求 `T : StateBase, new()`。
- **定时器**：10Hz（100ms）心跳离散累减，毫秒值需能被 100 整除才精确；`timerDic` 受 timeScale 影响，`realTimerDic` 不受。
- **UI**：面板预制体须放 `Resources/UI/面板类型名` 且挂对应 `BasePanel` 子类；控件以 `GameObject.name` 为键，同名只登记第一个。
- **音频**：`SoundObj` 走对象池，池满归还会被销毁；BGM 的 AudioSource 挂在常驻 GameObject（DontDestroyOnLoad）上。
- **编辑器监视器**：`RuntimeMonitor` 整个文件在 `#if UNITY_EDITOR` 内，不进运行时包体。
- **资源**：`ResManager` 缓存键 = `path + "_" + 类型 FullName`；采用引用计数，异步加载用协程（由 `MonoManager` 托管）。
