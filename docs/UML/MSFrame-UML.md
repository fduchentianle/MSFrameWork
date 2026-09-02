# MSFrame 框架 UML 类图

> 由 `Assets/MSFrame/Scripts` 28 个源文件生成。VS Code 装 “Markdown Preview Mermaid Support” 后 `Ctrl+Shift+V` 预览；Typora / Obsidian / GitHub 直接渲染。

## 目录

- [0. 整体架构](#0-整体架构)
- [1. 单例与生命周期](#1-单例与生命周期)
- [2. 对象池系统](#2-对象池系统)
- [3. 全局事件 EventCenter](#3-全局事件-eventcenter)
- [4. 组件事件 EventListener](#4-组件事件-eventlistener)
- [5. 资源与音频](#5-资源与音频)
- [6. UI 面板系统](#6-ui-面板系统)
- [7. 状态机与定时器](#7-状态机与定时器)
- [8. 配置 / 存档 / 场景切换](#8-配置--存档--场景切换)
- [9. 工具与运行时监视](#9-工具与运行时监视)
- [注意事项](#注意事项)
- [附录 A：类型 → 职责速查](#附录-a类型--职责速查)
- [附录 B：读图符号](#附录-b读图符号)

## 0. 整体架构

```mermaid
flowchart LR
    subgraph USAGE["使用层（业务/组件）"]
        UI["UI 系统：UIManager + BasePanel"]
        COMP["组件：EventListener / StateMachine / TimerManager"]
    end

    subgraph SERVICE["服务层（核心枢纽）"]
        ResM["ResManager\n资源加载缓存"]
        AudioM["AudioManager\n音乐音效"]
        MonoM["MonoManager\n帧事件 + 协程宿主"]
        PoolM["PoolManager\n对象池"]
    end

    subgraph SYS["系统层"]
        EventC["EventCenter\n全局事件"]
        ConfigM["ConfigManager\n配置读取"]
        SaveM["SaveManager\n存档"]
        SceneM["ChangeSceneManager\n切场景"]
    end

    subgraph BASE["基础层"]
        Singleton["单例基类 ×3"]
        Ext["Extension\n扩展方法门面"]
    end

    MonoM -.继承.-> Singleton
    ResM -.继承.-> Singleton
    AudioM -.继承.-> Singleton
    PoolM -.继承.-> Singleton
    EventC -.继承.-> Singleton
    ConfigM -.继承.-> Singleton
    SaveM -.继承.-> Singleton
    SceneM -.继承.-> Singleton

    UI --> ResM : 加载面板预制体
    UI --> EventC : 业务事件
    COMP --> MonoM : 帧驱动/协程
    COMP --> PoolM : 取还对象
    AudioM --> ResM : 加载 AudioClip
    AudioM --> PoolM : 音效实例池
    AudioM --> MonoM : 每帧清扫
    AudioM --> SaveM : 音量设置
    SceneM --> MonoM : 协程
    SceneM --> EventC : 进度事件
    ConfigM --> ResM : 加载配置资产
    PoolM --> ResM : 加载预制体
    MonoM --> Ext : 转发
    PoolM --> Ext : 转发

    style BASE fill:#e8f0fe,stroke:#8ab4f8
    style SYS fill:#e6f4ea,stroke:#81c995
    style SERVICE fill:#fef7e0,stroke:#fdd663
    style USAGE fill:#f3e8fd,stroke:#d7aefb
```

## 1. 单例与生命周期

```mermaid
classDiagram
    direction LR

    class MonoBehaviour {
        「外部」UnityEngine.MonoBehaviour
    }
    class BaseManagerT {
        纯 C# 单例基类（T : class）
        +Instance$ T（双检锁 + 反射私有构造）
    }
    class SingletonMonoT {
        MonoBehaviour 单例（手动挂载）
        +Instance$ T
        #Awake() 去重 + DontDestroyOnLoad
    }
    class SingletonAutoMonoT {
        MonoBehaviour 单例（自动挂载，推荐）
        +Instance$ T（首次访问 new GameObject + AddComponent）
    }

    MonoBehaviour <|-- SingletonMonoT
    MonoBehaviour <|-- SingletonAutoMonoT

    BaseManagerT <|-- PoolManager
    BaseManagerT <|-- EventCenter
    BaseManagerT <|-- ConfigManager
    BaseManagerT <|-- SaveManager
    BaseManagerT <|-- ChangeSceneManager
    BaseManagerT <|-- ResManager
    BaseManagerT <|-- AudioManager
    BaseManagerT <|-- TimerManager
    BaseManagerT <|-- UIManager
    SingletonAutoMonoT <|-- MonoManager

    note for SingletonMonoT "手动挂场景版当前无使用方，保留作扩展用"
```

## 2. 对象池系统

```mermaid
classDiagram
    direction LR

    class PoolManager {
        对象池总入口
        +GetGameObj~T~(name) T
        +GetGameObjAsync~T~(name, callBack) void
        +GetObj~T~() T
        +PushGameObj(GameObject) void
        +PushObj~T~(T) void
        +ClearPool() void
    }
    class PoolGameObject {
        某个资源名的 GameObject 抽屉
        +Count int
        +Pop() GameObject
        +Push(GameObject) void
    }
    class PoolObjectBase {
        <<abstract>> 普通对象抽屉基类
    }
    class PoolObjectT {
        普通对象抽屉（T : class）
        +poolObjs Queue~T~
        +maxNum int
        +Count int
    }
    class PoolAttribute {
        <<attribute>> 标容量上限
        +maxNum int
    }
    class IPoolObject {
        <<interface>> 归还前重置
        +ResetInfo() void
    }
    class SoundObj {
        「入池对象示例」MonoBehaviour
    }
    class StateMachine {
        「入池对象示例」IPoolObject
    }
    class TimerItem {
        「入池对象示例」IPoolObject
    }

    PoolManager *-- PoolGameObject : 持有(按名)
    PoolManager *-- PoolObjectBase : 持有(按类型)
    PoolObjectBase <|-- PoolObjectT
    PoolManager ..> PoolAttribute : 反射读容量
    IPoolObject <.. PoolObjectT : 约束
    IPoolObject <|.. StateMachine : 实现
    IPoolObject <|.. TimerItem : 实现
    PoolManager ..> SoundObj : 池化实例

    note for PoolManager "GetGameObj 池空时经 ResManager 加载预制体；PushGameObj 归还时清理其 EventListener；普通对象池空时 new T() 兜底"
```

## 3. 全局事件 EventCenter

```mermaid
classDiagram
    direction LR

    class EventType {
        <<enumeration>> 全局事件类型
        +E_SceneLoadChange 0
    }
    class EventCenter {
        全局事件中心（单例）
        +AddEventListener(type, action) 及 1/2/3 参重载
        +RemoveEventListener(...) 及 1/2/3 参重载
        +EventTrigger(type) 及 1/2/3 参重载
        +Clear() / Clear(EventType)
    }
    class EventInfoBase {
        <<abstract>> 父类装子类
    }
    class EventInfo {
        无参事件
        +actions UnityAction
    }
    class EventInfoT {
        一参事件
        +actions UnityAction~T~
    }
    class EventInfoTK {
        两参事件
        +actions UnityAction~T,K~
    }
    class EventInfoTKL {
        三参事件
        +actions UnityAction~T,K,L~
    }

    EventInfoBase <|-- EventInfo
    EventInfoBase <|-- EventInfoT
    EventInfoBase <|-- EventInfoTK
    EventInfoBase <|-- EventInfoTKL
    EventCenter *-- EventInfoBase : eventDic
    EventCenter ..> EventType : 事件键

    note for EventCenter "同一 EventType 的委托参数个数必须固定，混用会强转失败（源码注释已提醒）"
```

## 4. 组件事件 EventListener

```mermaid
classDiagram
    direction LR

    class EventListenerType {
        <<enumeration>> 组件事件类型
        鼠标/拖拽 8 种 + 碰撞 6 种 + 触发 6 种
    }
    class EventListener {
        MonoBehaviour 组件级监听
        +AddListener~T~(type, action, args)
        +RemoveListener~T~(type, action, ...)
        +RemoveAllListener(type) / ()
        +TriggerAction~T~(type, eventData)
        +OnPointerEnter/Exit/Click/Down/Up(...)
        +OnBeginDrag/OnDrag/OnEndDrag(...)
        内部：碰撞/触发回调 → 转发
    }
    class IMouseEvent {
        <<interface>> 聚合指针/拖拽接口
    }
    class EventListenerExtension {
        <<static>> 强类型注册入口
        +OnClick / OnMouseEnter / OnDrag ...（this Component）
        +OnCollisionEnter... / OnTriggerEnter...
        +RemoveClick / Remove... / RemoveAllListener
    }

    IMouseEvent <|.. EventListener : 实现
    EventListener ..> EventListenerType
    EventListenerExtension ..> EventListener : 获取并自动挂载
    EventListenerExtension ..> EventListenerType

    note for EventListener "一条监听 = 委托 + 附加参数 object[]；指针/碰撞/触发回调统一转 TriggerAction 分发"
```

## 5. 资源与音频

```mermaid
classDiagram
    direction LR

    class ResManager {
        资源加载缓存（单例）
        +Load~T~(path) T
        +LoadAsync~T~(path, callBack) void
        +UnloadAsset~T~(path, isDel, callBack) void
        +UnloadUnusedAssets(callBack) void
        +ClearDic(callBack) void
        +GetRefCount~T~(path) int
    }
    class ResInfoBase {
        <<abstract>> 资源记录基类
        +path / typeName / refCount / state
    }
    class ResInfoT {
        资源记录（T : Object）
        +asset T
        +callBack UnityAction~T~
        +AddRefCount() / SubRefCount()
    }
    class AudioManager {
        音乐 + 音效管理（单例）
        +PlayBKMusic(name, isSync)
        +StopBKMusic() / PauseBKMusic()
        +ChangeBKMusicValue(v)
        +PlaySound(name, isLoop)
        +PlaySoundAsync(name, isLoop, callBack)
        +StopSound(source)
        +ChangeSoundValue(v)
        +PlayOrPauseSound(isPlay)
        +ClearSound()
    }
    class SoundObj {
        MonoBehaviour + [Pool(100)]
        +Awake() 补 AudioSource
    }
    class AudioSetting {
        <<Serializable>> 音量设置
        +bkMusicValue float
        +soundValue float
    }
    class MonoManager {
        帧事件广播 + 协程宿主
        +Add/Remove(Update|FixedUpdate|LateUpdate)Listener(action)
    }
    class SaveManager {
        存档（音量持久化用）
    }
    class PoolManager {
        对象池（音效实例）
    }

    ResInfoBase <|-- ResInfoT
    ResManager *-- ResInfoBase : 缓存字典
    AudioManager ..> ResManager : 加载/卸载 AudioClip
    AudioManager ..> PoolManager : 取/还 SoundObj
    AudioManager ..> MonoManager : 每帧清扫
    AudioManager ..> SaveManager : 读写 AudioSetting
    AudioManager ..> SoundObj : 池化实例
    AudioManager *-- AudioSetting : 音量来源
    ResManager ..> MonoManager : 协程

    note for AudioManager "音效播放流程：取池化 SoundObj → 设 clip 播放 → 播放完卸载资源并回池；BGM 的 AudioSource 挂在常驻 GameObject 上"
```

## 6. UI 面板系统

```mermaid
classDiagram
    direction LR

    class UIManager {
        面板管理器（单例，非 Mono）
        +ShowPanel~T~(layer, callBack)
        +ShowPanelAsync~T~(layer, callBack)
        +HidePanel~T~(isDestroy)
        +GetPanel~T~(callBack)
        +GetLayerFather(layer)
        内部：panelDic + 四层遮罩
    }
    class BasePanel {
        <<abstract>> MonoBehaviour 面板基类
        +GetControl~T~(name) T
        +ShowMe() / HideMe()（抽象，子类实现）
        #Awake() 扫控件 + 接事件
        #ClickBtn(name) 钩子
        #SliderValueChange(name, v) 钩子
        #ToggleValueChanged(name, v) 钩子
    }
    class E_UILayer {
        <<enumeration>> UI 层级
        +Bottom / Middle / Top / System
    }
    class BasePanelInfo {
        <<abstract>> 面板登记信息基类（私有嵌套）
    }
    class PanelInfoT {
        面板登记信息（私有嵌套）
        +panel T / +callBack / +isHide
    }

    UIManager ..> BasePanel : 加载并调用 ShowMe/HideMe
    UIManager ..> E_UILayer : 按层放置
    UIManager *-- BasePanelInfo : panelDic
    BasePanelInfo <|-- PanelInfoT

    note for UIManager "面板预制体约定 Resources/UI/面板类型名，且须挂对应 BasePanel 子类脚本"
    note for BasePanel "控件按 GameObject.name 注册；Button/Slider/Toggle 事件在 Awake 自动接线，子类 override 钩子即可"
```

## 7. 状态机与定时器

```mermaid
classDiagram
    direction LR

    class StateMachine {
        [Pool(100)] 状态控制器
        +CurrentStateNum int
        +Init(owner)
        +ChangeState~T~(newStateNum, reCurrentState) bool
        +Stop() / Destroy() / ResetInfo()
    }
    class StateBase {
        <<abstract>> [Pool(100)] 状态基类
        +Init(owner, machine)
        +UnInit() / ResetInfo()
        +Enter() / Exit()
        +FixedUpdate() / Update() / LateUpdate()
    }
    class IStateMachineOwner {
        <<interface>> 宿主标记
    }
    class TimerManager {
        定时器管理（单例）
        +CreateTimer(allTime, overCallBack, intervalTime, callBack) int
        +CreateRealTimer(...) int
        +Remove/Reset/Start/Stop Timer(keyID)
        +StartTimer() / StartRealTimer() / StopAllTimer()
    }
    class TimerItem {
        [Pool(100)] 单个定时器
        +keyID / allTime / intervalTime / isRunning
        +overCallBack / callBack UnityAction
        +InitInfo(...) / ResetInfo()
    }

    IStateMachineOwner <.. StateMachine : 宿主
    StateMachine *-- StateBase : stateDic
    StateMachine ..> StateBase : ChangeState~T~
    TimerManager *-- TimerItem : 两个字典

    note for StateMachine "切换 = 旧状态 Exit+摘帧监听 → 新状态 Enter+挂帧监听；状态从对象池/字典复用"
    note for TimerManager "10Hz 心跳协程驱动；timerDic 受 timeScale 影响，realTimerDic 不受"
```

## 8. 配置 / 存档 / 场景切换

```mermaid
classDiagram
    direction LR

    class ConfigManager {
        配置读取（单例）
        +GetConfig~T~(configTypeName, id) T
    }
    class ConfigSetting {
        「外部」Odin SerializedScriptableObject
        +configDict Dictionary(string, Dictionary(int, ConfigBase))
        +GetConfig~T~(name, id) T
    }
    class ConfigBase {
        「外部」Odin SerializedScriptableObject
        配置数据基类
    }
    class SaveManager {
        存档管理（单例）
        +SaveObject / LoadObject~T~
        +SaveSetting / LoadSetting~T~
        +CreateSaveItem / GetSaveItem / DeleteSaveItem
        +GetAllSaveItem(...)
    }
    class SaveItem {
        存档槽
        +saveID / +lastSaveTime
        +UpdateTime(DateTime)
    }
    class ChangeSceneManager {
        场景切换（单例）
        +LoadScene(name, callBack)
        +LoadSceneAsync(name, callBack)
    }
    class ResManager {
        资源加载
    }
    class MonoManager {
        协程
    }
    class EventCenter {
        全局事件
    }

    ConfigManager ..> ResManager : 加载 ConfigSetting
    ConfigSetting *-- ConfigBase : configDict
    SaveManager *-- SaveItem : 存档清单
    ChangeSceneManager ..> MonoManager : 协程
    ChangeSceneManager ..> EventCenter : 进度 E_SceneLoadChange
```

## 9. 工具与运行时监视

```mermaid
classDiagram
    direction LR

    class MathUtil {
        静态工具（全 static）
        +Deg2Rad / Rad2Deg
        +GetObjDistanceXZ / XY
        +CheckObjDistanceXZ / XY
        +IsWorldPosOutScreen
        +IsInSectorRangeXZ
        +RayCast / RayCastAll
        +OverlapBox~T~ / OverlapSphere~T~
    }
    class RuntimeMonitor {
        <<ScriptableObject>> 编辑器监视器
        +autoRefresh bool
        监视：池配置 / 资源缓存 / 全局事件 / 组件事件
    }
    class EditorDebugInfo {
        <<Serializable>> 行数据模型
        ResCacheDebugInfo / PoolConfigDebugInfo
        EventCenterDebugInfo / EventListenerDebugInfo
    }

    RuntimeMonitor ..> EditorDebugInfo : 生成表格行

    note for RuntimeMonitor "整个文件包在 #if UNITY_EDITOR 内，只影响编辑器，不进运行时包体"
```

## 注意事项

- **事件**：`EventCenter` 同一 `EventType` 的委托参数个数必须固定（0/1/2/3 参对应不同 `EventInfo` 子类），混用会强转失败。
- **对象池**：入池须同时满足 `[Pool(maxNum)]` 特性 + 实现 `IPoolObject`（提供 `ResetInfo`），缺一会打 Warning 拒绝入池；`PushGameObj` 用 `obj.name` 作字典键，取/还必须同一资源名。
- **单例**：`BaseManager<T>` 子类必须声明**私有无参构造**（供反射调用）；`SingletonMono<T>` 无懒加载，场景加载完成前 `Instance` 可能为 null。
- **状态机**：宿主须实现 `IStateMachineOwner`；`ChangeState<T>` 要求 `T : StateBase, new()`，状态经对象池复用。
- **定时器**：10Hz（100ms）心跳离散累减，毫秒值需能被 100 整除才精确；`timerDic` 受 timeScale 影响，`realTimerDic` 不受。
- **UI**：面板预制体须放 `Resources/UI/面板类型名` 且挂对应 `BasePanel` 子类；控件以 `GameObject.name` 为键，同名只登记第一个。
- **音频**：`SoundObj` 走对象池，池满归还会被销毁；BGM 的 AudioSource 挂在常驻 GameObject（DontDestroyOnLoad）上。
- **编辑器**：`RuntimeMonitor` 整个文件在 `#if UNITY_EDITOR` 内，不进运行时包体。
- **资源**：`ResManager` 缓存键 = `path + "_" + 类型 FullName`；采用引用计数，异步加载用协程（由 `MonoManager` 托管）。

## 附录 A：类型 → 职责速查

| 功能域 | 类型 | 一句话职责 |
|---|---|---|
| 单例 | `BaseManager<T>` | 纯 C# 单例（双检锁 + 反射私有构造） |
| | `SingletonMono<T>` | MonoBehaviour 单例（手动挂载去重） |
| | `SingletonAutoMono<T>` | MonoBehaviour 单例（自动挂载） |
| 对象池 | `PoolManager` | 池总入口（取/还/清空） |
| | `PoolGameObject` | 一个资源名的 GameObject 抽屉 |
| | `PoolObject<T>` / `PoolObjectBase` | 普通对象抽屉 / 多态基类 |
| | `PoolAttribute` / `IPoolObject` | 容量特性 / 重置接口 |
| 事件 | `EventCenter` + `EventInfo*` | 全局广播（0~3 参） |
| | `EventListener` + `EventListenerExtension` | 组件级监听（挂物体上） |
| 资源 | `ResManager` + `ResInfo<T>` | 资源加载缓存 + 引用计数 |
| 音频 | `AudioManager` / `SoundObj` / `AudioSetting` | 音乐音效 / 池化载体 / 音量设置 |
| UI | `UIManager` / `BasePanel` / `E_UILayer` | 面板管理 / 面板基类 / 层级 |
| 状态机 | `StateMachine` / `StateBase` / `IStateMachineOwner` | 状态切换 / 状态基类 / 宿主标记 |
| 定时器 | `TimerManager` / `TimerItem` | 倒计时管理 / 单条定时器 |
| 配置 | `ConfigManager` / `ConfigSetting` / `ConfigBase` | 配置读取 / 配置资产 / 配置基类 |
| 存档 | `SaveManager` / `SaveItem` | 存档读写 / 存档槽 |
| 场景 | `ChangeSceneManager` | 同步/异步切场景 |
| 驱动 | `MonoManager` | 帧事件广播 + 协程宿主 |
| 扩展 | `Extension` | 通用扩展方法门面 |
| 工具 | `MathUtil` / `RuntimeMonitor` | 数学工具 / 编辑器监视器 |

## 附录 B：读图符号

| 符号 | 含义 |
|---|---|
| `+` / `-` / `#` / `$` | public / private / protected / static |
| `~T~` | 泛型（如 `Load~T~` = `Load<T>`） |
| `<<abstract>> / <<interface>> / <<enumeration>> / <<static>> / <<attribute>>` | 抽象类 / 接口 / 枚举 / 静态类 / 特性 |
| `A <\|-- B` | B 继承 A |
| `A <\|.. B` | B 实现接口 A |
| `A *-- B` | 组合（强持有） |
| `A ..> B` | 依赖 / 调用 |
| `A --> B` | 关联 |
