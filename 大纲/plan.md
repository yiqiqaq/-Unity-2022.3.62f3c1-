## Plan: 淮畔科创行 (Unity 2022.3.62f3c1) 全场景研发排期

依照《燕云十六声》极简留白风格以及单场景叠加、表现层解耦架构，制定从启动流到三大调研章节的完整开发排期。

**TL;DR** - 基座常驻 + 9 个薄叠加场景。启动流（Base → Startup → Login/Register → Intro）打通账户体系后，进入序章 → 三章调研（各含一个科普小游戏）→ 结局生成报告。UI 全解耦，EventBus 发布订阅，TransitionManager 黑幕过渡。

---

### 场景总览

| 场景名 | 类型 | 关联脚本 |
|---|---|---|
| BaseScene | 基座常驻 | BaseSceneBootstrap, GameManager, AccountManager, TransitionManager |
| StartupScene | UI 薄场景 | StartupSceneBootstrap, StartupUI |
| LoginScene | UI 薄场景 | LoginSceneBootstrap, LoginUI |
| RegisterScene | UI 薄场景 | RegisterSceneBootstrap, RegisterUI |
| IntroScene | UI 薄场景 | IntroSceneBootstrap, IntroUI |
| PrologueScene | 剧情场景 | PrologueSceneBootstrap, DialogueUI |
| Chapter1Scene | 章节场景 | Chapter1SceneBootstrap, Chapter1Handler, WaterPurificationMiniGame |
| Chapter2Scene | 章节场景 | Chapter2SceneBootstrap, Chapter2Handler, GreenFactorySortingMiniGame |
| Chapter3Scene | 章节场景 | Chapter3SceneBootstrap, Chapter3Handler, InnovationMatchingMiniGame |
| EndingScene | 结局场景 | EndingSceneBootstrap, EndingUI |

---

### Phase 1: 核心基础与基座通信层建设

1. **环境准备与目录划分**：配置项目建立 `Core`、`Logic`、`Presentation` 文件夹与对应的 `.asmdef` 程序集，确保依赖方向 Presentation → Logic → Core。所有 Canvas 渲染模式设为 `Screen Space - Camera`。
2. **基座场景 (Base Scene) 搭建**：创建挂载 `DontDestroyOnLoad` 的 `GameManager`（状态机）与 `AccountManager`（数据单例）。BaseSceneBootstrap 运行时程序化创建 `[GameSystems]` 根节点并注入 TransitionManager + 黑幕 Canvas。
3. **事件总线 (EventBus) 开发**：全局底层事件分发器（基于 `Dictionary<Type, Delegate>`），UI 按钮只 `EventBus.Publish(new IntentEvent(...))`，由 Logic 层接管流转。
4. **场景转换管理器 (TransitionManager) 开发**：实现"黑幕淡入 → 卸载旧叠加场景 → Additive 加载新场景 → 黑幕淡出"的协程序无缝黑场过渡。`LoadSceneAsync` 失败时自动恢复黑幕 alpha=0 防止卡死。

### Phase 2: 启动序列页面级表现层开发

5. *依赖 Phase 1* **Startup Scene（启动页）**：
   - 极简标题 Logo，底部三键："选择存档"、"了解本次科考"、"告别淮畔"。
   - 半透明退出弹窗："此去山高水长，后会有期？"
6. **Login Scene（存档选择页）**：
   - 3 卡槽列表组件，展现"玩家昵称 | 时长 | 科考进度"。
   - 空槽位显示"成为科考观察员"引导新建。
7. **Registration Scene（注册页）**：
   - 极细下划线 InputField + 错误提示（默认隐藏）+ "落笔"确认键。
8. **Intro Scene（游戏介绍页）**：
   - 逐字 Typewriter 缓动打印科考背景科普文。
   - 右下角"跳过"与"正式开启科考"文字按键。

### Phase 3: 业务逻辑横向贯通与 JSON 持久化

9. *并行与 Phase 2* **账户本地数据 (JSON) 持久化**：`SaveManager` + `FileUtils` 基于 `Application.persistentDataPath` 读写 3 存档位的 JSON。数据模型包括 `AccountData`、`ChapterProgress`、`MiniGameResult`、`FinalReportData`。
10. **信号接管与场景串联联动**：Logic 层监听 IntentEvent 总线，处理完整流转：
    - `GoToLogin` → LoginScene
    - `GoToRegister` → RegisterScene（记录待建槽位号）
    - `LoadAccount` → 读取档案 → IntroScene
    - `CreateAccount` → 新建档案 → IntroScene
    - `GoToMainGame` → 根据存档进度加载对应章节场景
    - `GoToStartup` → 返回启动页
11. **真机性能锁**：`QualitySettings.SetQualityLevel(0)` + `Application.targetFrameRate = 30`。

### Phase 4: 序章场景（PrologueScene）

12. **对话系统 UI 开发**：创建可复用的 `DialogueUI` 组件，支持：
    - 逐句展示对话文本（Typewriter 效果）
    - 分支选择按钮（根据 `DialogueNode.Choices` 动态生成）
    - 点击推进 / 自动推进模式
13. **序章对话流程**：读取 `StoryScenarioLibrary.PrologueDialogues`，依次展示：
    - 三位 NPC 自我介绍（生态研究员、产业工程师、区域协同专员）
    - 分支选择（"生态保护" / "产业升级" / "区域协同"）
    - 玩家身份确认（「科创观察员」）
    - 过渡至第一章
14. **进度记录**：序章完成后调用 `StoryProgressService.MarkPrologueCompleted()` 并自动存档。TransitionManager 加载 Chapter1Scene。

### Phase 5: 第一章・淮河生态保护（Chapter1Scene）

15. **章节对话 UI**：读取 `StoryScenarioLibrary.BuildChapter1()` 返回的 `StoryChapterConfig`：
    - PreludeDialogues：湿地实景描述 + 污染治理分支选择
    - SceneDescription："淮河湿地实景、河岸监测站、动态水面"
16. **引导对话**：MiniGameGuideDialogues 展示操作说明（拖拽清理、90 秒、80% 阈值）。
17. **水质净化小游戏 (WaterPurificationMiniGame)**：
    - 拖拽式清理水面污染物（塑料瓶、塑料袋、油污块）至回收桶
    - 避免误触水生生物（小鱼、芦苇、水草），允许 maxMisTouches=2 容错
    - 90 秒倒计时，清理率 ≥80% 即通关
    - 失败可无限重试（InfiniteRetry=true）
    - 独立预制体加载，通关后 Destroy + Resources.UnloadUnusedAssets + GC
18. **通关后对话**：PostMiniGameDialogues + 分支选择（"湿地像地球之肾" / "污染很容易反弹"）。
19. **解锁知识卡片**：`UnlockedCards` = {"淮河生态治理核心措施", "湿地生态功能", "水污染防治基础知识"}。写入 `AccountData.UnlockedKnowledgeCards`，数据隔离仅写当前账户。
20. **章节过渡**：TransitionDialogue 引出第二章。调用 `StoryFlowManager.CompleteChapter1()` → TransitionManager 加载 Chapter2Scene。

### Phase 6: 第二章・皖北绿色智造（Chapter2Scene）

21. **章节对话 UI**：读取 `StoryScenarioLibrary.BuildChapter2()`：
    - PreludeDialogues：绿色工厂介绍 + 制造环节分支选择
    - SceneDescription："现代化绿色工厂、智能生产流水线、光伏能源展示区"
22. **引导对话**：MiniGameGuideDialogues 展示操作说明（点击分拣、75 秒、85% 准确率）。
23. **绿色工厂分拣小游戏 (GreenFactorySortingMiniGame)**：
    - 传送带持续送出物料，点击分拣至合格入库区 / 回收处理区
    - 合格品：光伏组件、环保建材、可循环塑料
    - 废弃品：工业废渣、高污染废料、不合格零件
    - 75 秒倒计时，分拣 ≥50 件且准确率 ≥85% 即通关
    - 失败可无限重试
24. **通关后对话**：PostMiniGameDialogues + 分支选择。
25. **解锁知识卡片**：{"绿色制造与循环经济", "清洁能源应用场景", "皖北产业升级成果"}。
26. **章节过渡**：TransitionDialogue 引出第三章。TransitionManager 加载 Chapter3Scene。

### Phase 7: 第三章・长三角科创协同（Chapter3Scene）

27. **章节对话 UI**：读取 `StoryScenarioLibrary.BuildChapter3()`：
    - PreludeDialogues：展厅介绍 + 协同关键分支选择
    - SceneDescription："长三角科创展厅、区域协同数字大屏、科创成果展示墙"
28. **引导对话**：MiniGameGuideDialogues 展示操作说明（三列连线匹配、90 秒、8 组）。
29. **科创资源匹配小游戏 (InnovationMatchingMiniGame)**：
    - 三列匹配：城市 ↔ 核心产业 ↔ 科创资源（如合肥 ↔ 量子信息 ↔ 量子科学实验室）
    - 点击式连线，90 秒完成 8 组正确匹配
    - 错误匹配惩罚：-6 秒时间 + 进度回退 1
    - 失败可无限重试
30. **通关后对话**：PostMiniGameDialogues + 分支选择。
31. **解锁知识卡片**：{"长三角一体化发展战略", "区域科创协同机制", "安徽在长三角的核心定位"}。
32. **章节过渡**：TransitionDialogue 为空（最终章），调用 `StoryFlowManager.CompleteChapter3()` → TransitionManager 加载 EndingScene。

### Phase 8: 结局场景（EndingScene）

33. **结局对话流程**：读取 `StoryScenarioLibrary.EndingDialogues`：
    - 系统祝贺 + 三位 NPC 总结陈词
    - 分支选择（"科技与生态并重" / "协同与创新并举"）
    - 生成《科创观察员专属报告》
34. **最终报告生成**：调用 `StoryProgressService.GenerateFinalReport()`，基于完成章节数评定 S/A/B/C 等级。写入 `AccountData.FinalReport`。
35. **结束操作**：提供"重新开始调研"（回 IntroScene）或"切换账户"（AccountManager.SwitchAccount → LoginScene）选项。

---

### 关键数据流

```
StartupUI ──EventBus──→ TransitionManager ──→ LoginScene / IntroScene
                                                        │
LoginUI ──EventBus──→ LoadAccount ──→ IntroScene       │
RegisterUI ──EventBus──→ CreateAccount ──→ IntroScene   │
                                                        ↓
IntroUI ──EventBus──→ GoToMainGame ──→ PrologueScene
                                                        │
PrologueDialogues ──→ StoryFlowManager.CompletePrologue │
                                                        ↓
Chapter1Scene ──→ WaterPurificationMiniGame ──→ CompleteChapter1
                                                        ↓
Chapter2Scene ──→ GreenFactorySortingMiniGame ──→ CompleteChapter2
                                                        ↓
Chapter3Scene ──→ InnovationMatchingMiniGame ──→ CompleteChapter3
                                                        ↓
EndingScene ──→ GenerateFinalReport ──→ SwitchAccount / Restart
```

### Relevant files

- `Assets/Core/EventBus.cs` — UI 解耦的指令抛出通道
- `Assets/Core/StoryScenarioLibrary.cs` — 全部对话与小游戏配置数据
- `Assets/Core/SaveDataModels.cs` — 存档数据模型 (AccountData, ChapterProgress, MiniGameResult, FinalReportData)
- `Assets/Core/UIEvents.cs` — IntentEvent 定义
- `Assets/Logic/TransitionManager.cs` — 黑幕场景管理器
- `Assets/Logic/AccountManager.cs` — 存档 IO 与账户管理
- `Assets/Logic/StoryFlowManager.cs` — 章节流转控制器
- `Assets/Logic/StoryProgressService.cs` — 进度标记与报告生成
- `Assets/Logic/GameManager.cs` — 全局状态机
- `Assets/Presentation/Chapter/ChapterBase.cs` — 章节基类
- `Assets/Presentation/MiniGame/MiniGameBase.cs` — 小游戏基类
- `Assets/Presentation/MiniGame/WaterPurificationMiniGame.cs` — 水质净化
- `Assets/Presentation/MiniGame/GreenFactorySortingMiniGame.cs` — 绿色工厂分拣
- `Assets/Presentation/MiniGame/InnovationMatchingMiniGame.cs` — 科创资源匹配
- `Assets/Presentation/UI/StartupUI.cs` — 启动页
- `Assets/Presentation/UI/LoginUI.cs` — 存档选择页
- `Assets/Presentation/UI/RegisterUI.cs` — 注册页
- `Assets/Presentation/UI/IntroUI.cs` — 游戏介绍页

### Verification

1. **启动流验证**：BaseScene → 点击"选择存档" → LoginScene 正常加载，3 卡槽显示正确。
2. **存档流验证**：空槽位点击 → RegisterScene → 输入代号 → "落笔" → 创建档案 → IntroScene。
3. **介绍页验证**：Typewriter 逐字打印 → 点击"正式开启科考" → PrologueScene 正常加载。
4. **章节流验证**：Prologue → Chapter1 → 小游戏通关 → Chapter2 → Chapter3 → Ending，全程黑幕无缝过渡。
5. **数据隔离验证**：切换账户后，前一账户的通关进度和知识卡片不可见。
6. **小游戏重试验证**：小游戏失败后点击"重试挑战"，状态正确重置，可无限重试。
7. **内存管控验证**：每个小游戏通关后执行 Destroy + UnloadUnusedAssets + GC，无内存泄漏。

### Decisions

- UI 事件全部通过 EventBus 解耦：UI 执行 `EventBus.Publish(new IntentEvent(...))`，Logic 层接管流转。
- 小游戏使用独立预制体 + 独立场景，通关后立即 Destroy 并卸载场景，控制低端机内存。
- 通关状态仅写入当前激活账户（`AccountData.MiniGameResult` / `ChapterProgress`），不跨账户读写。
- 三章小游戏均支持无限重试（InfiniteRetry=true），失败不卡主线、不扣进度。
