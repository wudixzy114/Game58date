# Game58date

> **基于 Stride 引擎的"哲学模拟 + 英雄之旅"3D 开放世界原型**：以微体素地形为底座，用世界规律引擎、征兆系统、玩家意图输入和 AI 剧情导演，把传统任务 UI 替换为"环境叙事 + 自然语言反馈"。

## 项目定位 / 背景

Game58date 是一个**单人 3D 开放世界 / 哲学模拟 / 涌现式叙事**的独立游戏原型。它想要颠覆传统"任务栏 + 地图标记"的设计，把"英雄之旅"12 阶段叙事的推进权完全交给一个后台的"剧情管家"——玩家看不到任务列表，但会通过自然征兆（天气骤变、远方港口通航、神秘 NPC、篝火占卜）和主动输入的意图来引导世界。核心设计语言是《易经》式的"萌芽-发展-鼎盛-衰退-转化"循环，把它物化到数值系统里就成了"物极必反"、"塞翁失马"、"因果报应"三个机制。

技术上，项目用 **Stride 4.3 (.NET 10 / `net10.0-windows`)** 作为引擎，整个世界建立在**确定性程序化体素地形**之上——颗粒度比 Minecraft 细很多，能刻画树叶的繁茂和水流的细腻。地形由 11 个独立的 perlin-like 噪声场叠加：continental、erosion、ridge、moisture、temperature、warpX、warpZ、overhangA/B、caveA/B；按 `WorldFieldSampler` 的平滑权重场混合生成 biome；网格走的是"贪心网格"路径；区块后台构建、串行上传、独立水体网格、独立碰撞体、生物群落装饰器、方块覆盖存储与脏区块重建都已经串成完整链路。`PrototypeRuntimeGame` 还会通过 P/Invoke + Win32 强制做无边框全屏，并通过 `WindowsFullscreenController` 处理 DPI 缩放下的 client size 同步。

到目前为止，项目处在"底层世界层已经搭好，上层玩法与叙事系统正在落地"的阶段——文档《系统总设计方案》明确把世界规律引擎、玩家意图、英雄之旅、征兆反馈、存档与性能作为 P0/P1 优先级。

## 仓库结构

```
Game58date/
├── Game58date/                        # 核心游戏项目（net10.0-windows, Stride 4.3）
│   ├── PrototypeRuntimeGame.cs       # 自定义 Game 入口：启动链、模式路由、Win32 全屏
│   ├── WindowsFullscreenController.cs # 无边框全屏 + DPI 同步（SetWindowLong/SetWindowPos）
│   ├── HeroJourneyPrototypeScript.cs # 旧原型（已隔离到独立运行模式）
│   ├── DevSceneRouterScript.cs       # 开发用场景路由：Terrain / Prototype / UI Showcase / MainMenu
│   ├── Gameplay/                     # 上层玩法系统
│   │   ├── WorldLawEngine.cs         # 世界规律引擎（物极必反 / 塞翁失马 / 因果报应）
│   │   ├── WorldLawModel.cs          # WorldLawState + PlayerBehaviorProfile + 因果记录
│   │   ├── WorldLawRuntimeController.cs # 把 WorldLawEngine 装到 SceneComponent
│   │   ├── WorldLawSaveMapper.cs     # 序列化世界规律状态到存档
│   │   ├── HeroJourneyDirector.cs    # 12 阶段状态机
│   │   ├── OmenDirector.cs           # 征兆触发与调度
│   │   ├── OmenPresentationController.cs # 征兆表现层（动态点光源、标记实体）
│   │   ├── PlayerIntentSystem.cs     # 自然语言意图采集 + 关键词意图解析
│   │   ├── PerceptionSkillController.cs # 感知技能开关
│   │   ├── RuntimeMode.cs / RuntimeLaunchTarget.cs / RuntimeSceneLauncher.cs
│   │   └── UI/                       # GameUiComposer、GameUiTheme、MainMenuComposer 等
│   ├── Terrain/                      # 底层世界生成
│   │   ├── TerrainChunkGenerator.cs  # 区块生成（WorldFieldSampler → VoxelChunkData）
│   │   ├── VoxelChunkMesher.cs       # 贪心网格（6 个面显式几何 + 绕序自检）
│   │   ├── VoxelChunkCollisionBuilder.cs # 区块 AABB 碰撞体
│   │   ├── VoxelChunkOverrideStore.cs # 方块覆盖（玩家编辑）持久化
│   │   ├── VoxelTerrainWorldRuntime.cs # 世界运行时：区块流式加载/卸载
│   │   ├── VoxelTerrainRuntimeScript.cs # 启动链入口
│   │   ├── TerrainEnvironmentDecorator.cs # 植被/结构/动物规则化装饰
│   │   ├── TerrainEnvironmentRuleSet.cs # 三类规则（vegetation/structure/animal）
│   │   ├── TerrainTextureAtlasFactory.cs # 程序化纹理图集
│   │   ├── FirstPersonCharacterController.cs # 自定义体素角色控制器（观察者/第一人称统一坐标）
│   │   ├── WeatherAmbienceController.cs # 天气/雾效/极光/丁达尔光
│   │   ├── WorldFieldSampler.cs      # 11 噪声场合成 biome + 高度 + 坡度
│   │   └── Noise/DeterministicNoise.cs # Perlin-like + FBM
│   └── Save/                         # GameSaveData / GameSaveRepository（Schema v2）
├── Game58date.Windows/                # Windows 启动器（仅 6 行，new PrototypeRuntimeGame().Run()）
├── ReflectPhysics/                   # 小工具：反射读取 Stride Physics 程序集
├── ReflectTmp/                       # 小工具：反射读取某个 Stride 内部类型
├── Tools/PrefabGen/                  # 控制台：批量生成 Stride 预制体
├── 说明文档/                          # 策划文档（系统总设计方案 / 任务清单 / 质量要求）
├── 过往问题复盘/                       # 体素地形网格问题复盘（绕序、瑞士奶酪、世界坐标错位等）
├── start-claude.ps1                  # PowerShell 启动 Claude Code
└── 开发说明文档.md
```

## 技术栈

| 领域 | 选型 | 用途 |
|---|---|---|
| 引擎 | Stride 4.3.0.2507（Stride.Engine / Video / Physics / Navigation / Particles / UI） | 渲染、物理、UI 引擎 |
| 运行时 | .NET 10（`net10.0-windows`） | 仅 Windows，目标 Windows 10/11 |
| 渲染 | Stride 内置 PBR + 自定义 PBR 材质 + 程序化纹理图集 | 世界/角色材质 |
| 噪声 | 自研 `DeterministicNoise`（基于 Ken Perlin 改进 + 256 长度 permutation） | 地形、生物群落 |
| 序列化 | Stride ContentManager + `GameSaveData` (Schema v2) | 存档与世界种子 |
| 平台 | Win32 + `SetWindowLong` / `SetWindowPos` / `MonitorFromWindow` | 无边框全屏 + DPI 同步 |
| 工具 | PowerShell、dotnet CLI | 启动 / 编译 / 调试 |

## 核心模块

**`PrototypeRuntimeGame`（启动链 + 模式路由）**
继承 Stride 的 `Game` 类。`LoadContent` 阶段会按 `RuntimeMode`（`Prototype` / `Terrain` / `UiShowcase` / `MainMenu`）选择不同的根实体；`Update` 阶段每帧检查窗口状态，强制无边框全屏并通过 `WindowsFullscreenController.TryApplyBorderlessFullscreen` 同步 presenter 与 `BackBuffer` 的尺寸，规避 Stride 默认全屏在多 DPI 显示器下的尺寸错乱。

**`WorldFieldSampler`（世界场采样）**
对每个 `(worldX, worldZ)` 采样 11 个独立种子的噪声（continental、erosion、ridge、moisture、temperature、warpX、warpZ、overhangA/B、caveA/B），通过权重场混合出 surface height、soil depth、slope、elevation、biome（plains/grassland/desert/forest/wetland/hills/mountains/snowfield/coast/ocean/island/lake）。所有噪声都基于 `int seed` 的 permutation 表，同一 seed 必出同一世界——是存档与世界种子复现的基础。

**`TerrainChunkGenerator` + `VoxelChunkMesher`（体素区块构建）**
`TerrainChunkGenerator` 用 `WorldFieldSampler` 填出 `VoxelChunkData`（`BlockKind` 数组 + 区块坐标），`VoxelChunkMesher` 走"显式 6 面 + 贪心合并"路径：6 个面的顶点表和绕序是显式定义的，启动期会做一次绕序自检；同材质相邻面做横向合并。`VoxelChunkCollisionBuilder` 独立构建 AABB 碰撞体，视觉实体和碰撞实体解耦，避免物理回写 transform 导致的区块上浮。

**`VoxelTerrainWorldRuntime`（运行时流式加载）**
维护区块后台生成队列（`MaxConcurrentChunkBuilds = 2`），按 `MaxChunkUploadsPerFrame = 2` 串行上传到 GPU；`VoxelChunkOverrideStore` 负责玩家方块修改的持久化，脏区块会触发邻区块重建。所有这些链路都有贯穿的运行时日志（请求 / 构建 / 丢弃 / 集成 / 卸载）。

**`TerrainEnvironmentDecorator`（环境装饰）**
基于 `TerrainEnvironmentRuleSet`（vegetation / structure / animal 三类规则）和 `EnvironmentEntityPool` 对象池，按 biome 与 chunk 中心距离放置植被/结构/动物 prop。每个 `Entity` 都遵循 LOD 与距离裁剪。

**`WorldLawEngine`（世界规律引擎 / 哲学模拟）**
- `WorldLawState` 维护 `ExplorationDrive` / `BorderLonging` / `LossMemory` / `BlessingWeight` / `ResourcePressure` / `Karma` 等状态
- `PlayerBehaviorProfile` 跟踪玩家 `ExplorationTendency` / `CuriosityTendency` / `Violence` / `IntentActions` / `Losses` / `Blessings` 等画像
- `Tick(deltaTime)` 推进时间、衰减 loss/blessing、根据 violence 累加 resource pressure
- `SubmitIntent(rawText)` 做关键词意图解析（`sea/cross/ship/harbor` → `BorderLonging += 0.45`）并触发对应 `OmenType`
- `RegisterExplorationProgress(distanceMeters)` 把行走距离转化为探索欲望
- `TryAdvanceHeroStage()` 推进 12 阶段状态机
- `AddCausalityRecord` 保留最多 24 条因果记录（支持复盘）

**`PlayerIntentSystem`（玩家意图输入）**
自然语言输入走"关键词 + 上下文"解析，识别 `OmenType.NaturalAnomaly / SocialShift / GuideArrival / Divination / PathRevelation` 等征兆来源；玩家提交意图时同步写入 `BehaviorProfile` 与 `Notebook` 历史。

**`HeroJourneyDirector`（英雄之旅剧情导演）**
12 阶段状态机：`OrdinaryWorld → CallToAdventure → CrossingTheThreshold → RoadOfTrials → MeetingTheMentor → ApproachToTheInmostCave → Transformation → ...`。会监听 `WorldLawEngine` 事件 + `PlayerIntentSystem` 提交 + `OmenDirector` 触发来推进阶段。

**`OmenDirector` + `OmenPresentationController`（征兆系统）**
事件评分 + 分类 + 持久化；表现层会创建征兆标记实体（动态点光源 / 边缘高亮 / 信号栏通知）；与 `WorldLawEngine` 的 `OmenTriggered` 事件完全解耦。

**`GameUiComposer`（正式 UI 主链）**
`GameUiTheme` 统一风格 + `GameUiComposer` 用 `UIComponent` + `Canvas` + 多个 `TextBlock`/`Border`/`Button` 组成"上下文式 HUD"。包括 mode tag、title、stage、biome、omen callout、profile、perception、intent input、narrative、history、meter widgets（karma/blessing/path/danger）、system menu 等十几个子面板。

**`GameSaveData`（存档 / Schema v2）**
`WorldSaveData`（含 Seed）+ `PlayerSaveData`（位置、视角）+ `TerrainSaveData`（方块覆盖）+ `GameplaySaveData`（世界规律 / 行为画像 / 征兆历史 / 英雄之旅阶段）。`GameSaveRepository` 是仓储层，schema 升级有版本号保护。

## 已完成 / 进行中

- ✅ 程序化体素地形：区块生成、贪心网格、水体、碰撞、纹理图集、生物群落装饰
- ✅ 第一人称角色控制器（观察者/第一人称统一坐标）
- ✅ WorldLawEngine 基础版 + 行为画像 + 因果记录
- ✅ 玩家意图输入系统基础版
- ✅ 英雄之旅剧情导演基础版
- ✅ 正式 OmenDirector + OmenPresentationController
- ✅ 感知技能系统基础版
- ✅ 正式 UI 主链（模式 tag / Signal Rail / 意图输入 / 系统菜单 / 主菜单）
- ✅ 开发用场景路由启动器
- ⏳ 正式 UI 第二阶段：动态主菜单、上下文探索 HUD、征兆通知层
- ⏳ 高级地形材质（三向投射 / 材质分层 / 远景 LOD）
- ❌ NPC 生成与对话系统
- ❌ 叙事驱动地图生成（基于剧情自动生成沙漠试炼 / 远古遗迹等）
- ❌ 意图驱动的跨地图过渡（隐藏过图 + 平滑切入）
- ❌ 远景 / LOD / 对象池的完整性能闭环
- ❌ 原型与正式系统彻底解耦（仍在做迁移）

## 本地运行 / 构建

```powershell
# 编译
dotnet build Game58date.Windows\Game58date.Windows.csproj

# 直接以默认 Terrain 模式运行
dotnet run --project Game58date.Windows

# 启动 Claude Code 协作
.\start-claude.ps1
```

> ⚠️ 目标框架是 `net10.0-windows`，需要 .NET 10 SDK。Stride 资产走 ContentManager，加载场景时需要先在 Stride Editor 中 build 资源；当前 `PrototypeRuntimeGame.LoadContent` 用 `LoadUiShowcaseScene` 时才走资源路径，地形模式直接用代码生成实体。

## 状态

**v0.x 原型 / 上层玩法系统正在建设**：地形底层链路完成度较高，能跑出一个完整的可探索体素世界（4 chunk view distance、24×24×96 区块、确定性种子复现、第一/第三人称走跑跳、世界规律 tick、征兆触发、意图输入、UI 通知、存档/读档），但**距离完整可玩的垂直切片**还差叙事驱动的地图生成、NPC、跨地图过渡、正式 UI 第二阶段等 P1/P2 系统。属于"已经能演示核心机制，但还不能作为独立游戏发布"的状态。

## License

未指定 License。当前所有 commit 都来自 `xiezongyu` 个人实验，建议默认按私有项目处理。
