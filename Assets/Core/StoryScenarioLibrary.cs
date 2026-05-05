using System.Collections.Generic;

namespace Core
{
    public class DialogueChoice
    {
        public string OptionText;
        public List<DialogueNode> ReactionDialogues;
    }

    public class DialogueNode
    {
        public string Text;
        public List<DialogueChoice> Choices;

        public static implicit operator DialogueNode(string text)
        {
            return new DialogueNode { Text = text, Choices = null };
        }
    }

    public sealed class StoryChapterConfig
    {
        public int ChapterId;
        public string ChapterName;
        public string SceneDescription;
        public List<DialogueNode> PreludeDialogues = new List<DialogueNode>();
        public List<DialogueNode> MiniGameGuideDialogues = new List<DialogueNode>();
        public MiniGameConfig MiniGame;
        public List<DialogueNode> PostMiniGameDialogues = new List<DialogueNode>();
        public List<string> UnlockedCards = new List<string>();
        public string TransitionDialogue;
    }

    public sealed class MiniGameConfig
    {
        public string MiniGameId;
        public string Name;
        public string GameplayCore;
        public int DurationSeconds;
        public string SceneElements;
        public string VictoryCondition;
        public string FailureFeedback;
        public string MemoryAdaptationRule;
        public string DataIsolationRule;
        public bool InfiniteRetry;
    }

    public static class StoryScenarioLibrary
    {
        public static readonly List<DialogueNode> PrologueDialogues = new List<DialogueNode>
        {
            "【生态研究员】你好，欢迎加入淮畔科创调研团，我是本次生态调研的负责人。",
            "【产业工程师】我负责皖北绿色智造板块的调研讲解。",
            "【区域协同专员】我将带你了解长三角科创协同的发展成果。",
            new DialogueNode
            {
                Text = "【生态研究员】在出发前，你想先了解哪方面的内容？",
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice 
                    { 
                        OptionText = "生态保护", 
                        ReactionDialogues = new List<DialogueNode> { "【生态研究员】生态是发展的底色，我们第一站就会去淮河湿地。" } 
                    },
                    new DialogueChoice 
                    { 
                        OptionText = "产业升级", 
                        ReactionDialogues = new List<DialogueNode> { "【产业工程师】产业是经济的命脉，我们会带你去看现代化的绿色工厂。" } 
                    },
                    new DialogueChoice 
                    { 
                        OptionText = "区域协同", 
                        ReactionDialogues = new List<DialogueNode> { "【区域协同专员】协同能汇聚更强的发展动能，这是长三角的重要课题。" } 
                    }
                }
            },
            "【系统】你的身份是「科创观察员」，将依次完成三大主题调研，每完成一个主题的小游戏挑战，即可解锁对应科普成果。",
            "【生态研究员】准备好了吗？我们第一站，前往淮河湿地生态保护区。"
        };

        public static readonly List<DialogueNode> EndingDialogues = new List<DialogueNode>
        {
            "【系统】恭喜你！圆满完成本次淮畔科创行全部调研任务！",
            "【生态研究员】从淮河湿地的水清岸绿，我们看到了生态保护的安徽实践。",
            "【产业工程师】从皖北工厂的绿色智造，我们看到了产业升级的安徽力量。",
            "【区域协同专员】从长三角的科创协同，我们看到了一体化发展的安徽担当。",
            new DialogueNode
            {
                Text = "【生态研究员】回首这趟旅程，你觉得最重要的是什么？",
                Choices = new List<DialogueChoice>
                {
                    new DialogueChoice { OptionText = "科技与生态并重", ReactionDialogues = new List<DialogueNode> { "【生态研究员】没错，绿水青山就是金山银山。" } },
                    new DialogueChoice { OptionText = "协同与创新并举", ReactionDialogues = new List<DialogueNode> { "【生态研究员】说的好，创新离不开开放与协同。" } }
                }
            },
            "【系统】你的全部调研成果、解锁的科普知识，已汇总生成《科创观察员专属报告》，永久保存在当前账户中。",
            "【系统】你可以选择【重新开始调研】，或【切换账户】开启新的调研之旅。"
        };

        public static List<StoryChapterConfig> BuildMainChapters()
        {
            return new List<StoryChapterConfig>
            {
                BuildChapter1(),
                BuildChapter2(),
                BuildChapter3()
            };
        }

        public static StoryChapterConfig BuildChapter1()
        {
            return new StoryChapterConfig
            {
                ChapterId = ChapterIds.HuaiheEco,
                ChapterName = "第一章・淮河生态保护",
                SceneDescription = "淮河湿地实景、河岸监测站、动态水面",
                PreludeDialogues = new List<DialogueNode>
                {
                    "【生态研究员】我们现在所在的，是淮河干流的湿地保护区。",
                    "【生态研究员】淮河是安徽的母亲河，过去的水污染问题，曾严重影响沿岸生态与居民生活。",
                    new DialogueNode
                    {
                        Text = "【生态研究员】面对这些污染问题，你认为治理的首要突破口在哪儿？",
                        Choices = new List<DialogueChoice>
                        {
                            new DialogueChoice { OptionText = "严查排污口门", ReactionDialogues = new List<DialogueNode> { "【生态研究员】源头控制确实是最根本的方法。" } },
                            new DialogueChoice { OptionText = "修复湿地植被", ReactionDialogues = new List<DialogueNode> { "【生态研究员】恢复生态自净能力是长远之计。" } },
                            new DialogueChoice { OptionText = "水面拉网清漂", ReactionDialogues = new List<DialogueNode> { "【生态研究员】快速清理能立刻见效，是应急关键。" } }
                        }
                    },
                    "【生态研究员】无论采用哪种方法，经过多年的系统治理，淮河水质已经稳定向好，但零星的垃圾污染、油污排放，依然会威胁水生态安全。",
                    "【生态研究员】现在，需要你协助我们完成一次水面应急净化，守护淮河湿地的生态平衡。"
                },
                MiniGameGuideDialogues = new List<DialogueNode>
                {
                    "【生态研究员】操作很简单：拖拽水面上的污染物，放到右侧的环保回收桶里。",
                    "【生态研究员】注意尽量不要误触水里的鱼虾、水草，它们是湿地生态的重要部分，系统允许少量误触容错。",
                    "【生态研究员】90 秒内清理完 80% 以上的污染物，即可完成任务。",
                    "【系统】是否开始挑战？【开始挑战】按钮"
                },
                MiniGame = new MiniGameConfig
                {
                    MiniGameId = "water_purification",
                    Name = "水质净化",
                    GameplayCore = "拖拽式清理，轻量点击操作",
                    DurationSeconds = 90,
                    SceneElements = "动态水面、随机污染物（塑料瓶、塑料袋、油污块）、水生生物（小鱼、芦苇、水草）",
                    VictoryCondition = "90 秒内清理≥80%污染物，误触次数不超过容错上限，可提前达标通关",
                    FailureFeedback = "时间结束未达标或误触超出容错上限，弹出【重试挑战】按钮；不扣进度不卡主线",
                    MemoryAdaptationRule = "独立预制体进入章节时加载，通关/退出立即Destroy；结束后执行资源回收",
                    DataIsolationRule = "通关状态仅写入当前激活账户，不跨账户读取或修改",
                    InfiniteRetry = true
                },
                PostMiniGameDialogues = new List<DialogueNode>
                {
                    "【生态研究员】太棒了！水面净化任务圆满完成。",
                    new DialogueNode
                    {
                        Text = "【生态研究员】看着恢复清澈的水面，你有什么感想？",
                        Choices = new List<DialogueChoice>
                        {
                            new DialogueChoice { OptionText = "湿地像地球之肾", ReactionDialogues = new List<DialogueNode> { "【生态研究员】这个比喻非常贴切，它持续过滤着污染。" } },
                            new DialogueChoice { OptionText = "污染很容易反弹", ReactionDialogues = new List<DialogueNode> { "【生态研究员】对，所以长效的监管和治理方案不可或缺。" } }
                        }
                    },
                    "【生态研究员】你刚才做的，正是淮河生态治理的缩影：通过岸线清理、污水治理、湿地修复，我们一步步让淮河恢复了水清岸绿的模样。",
                    "【生态研究员】现在，淮河干流国考断面水质优良比例已经超过 90%，湿地生态系统也在持续恢复。"
                },
                UnlockedCards = new List<string>
                {
                    "淮河生态治理核心措施",
                    "湿地生态功能",
                    "水污染防治基础知识"
                },
                TransitionDialogue = "【产业工程师】淮河生态的底色，离不开绿色产业的支撑。接下来，我们一起去看看皖北的绿色智造工厂。"
            };
        }

        public static StoryChapterConfig BuildChapter2()
        {
            return new StoryChapterConfig
            {
                ChapterId = ChapterIds.WanbeiManufacturing,
                ChapterName = "第二章・皖北绿色智造",
                SceneDescription = "现代化绿色工厂、智能生产流水线、光伏能源展示区",
                PreludeDialogues = new List<DialogueNode>
                {
                    "【产业工程师】这里是皖北的标杆绿色工厂，也是安徽产业升级的缩影。",
                    "【产业工程师】过去，皖北的传统产业能耗高、排放大，如今我们通过技术升级，实现了绿色制造、循环经济、智能制造的三位一体。",
                    new DialogueNode
                    {
                        Text = "【产业工程师】提到绿色工厂，你想到最重要的环节是哪一环？",
                        Choices = new List<DialogueChoice>
                        {
                            new DialogueChoice { OptionText = "清洁能源供应", ReactionDialogues = new List<DialogueNode> { "【产业工程师】正是！利用光伏和风能代替火电，能大幅减排。" } },
                            new DialogueChoice { OptionText = "智能制造生产", ReactionDialogues = new List<DialogueNode> { "【产业工程师】没错，智能控制可使能耗和原料损耗降到最低。" } },
                            new DialogueChoice { OptionText = "废料回收再造", ReactionDialogues = new List<DialogueNode> { "【产业工程师】很对，实现资源的闭环循环是核心标志。" } }
                        }
                    },
                    "【产业工程师】确实，这些环节缺一不可。绿色生产的核心，就是从源头把控，做好资源分类、循环利用，减少浪费与排放。",
                    "【产业工程师】现在，需要你协助我们完成生产线上的物料分拣，守住绿色制造的第一道关口。"
                },
                MiniGameGuideDialogues = new List<DialogueNode>
                {
                    "【产业工程师】规则很简单：传送带上会持续送出物料，你需要点击分拣。",
                    "【产业工程师】清洁能源产品、环保材料、可循环利用件，送入左侧的【合格入库区】；工业废料、高污染件、不合格品，送入右侧的【回收处理区】。",
                    "【产业工程师】75 秒内分拣准确率达到 85% 以上，且分拣数量达标即可完成任务，支持提前通关。",
                    "【系统】是否开始挑战？【开始挑战】按钮"
                },
                MiniGame = new MiniGameConfig
                {
                    MiniGameId = "green_factory_sorting",
                    Name = "绿色工厂分拣",
                    GameplayCore = "点击式分类，极简操作，无复杂逻辑",
                    DurationSeconds = 75,
                    SceneElements = "循环传送带、随机物料（光伏组件、环保建材、可循环塑料、工业废渣、高污染废料、不合格零件）",
                    VictoryCondition = "75 秒内分拣≥50件且准确率≥85%，满足条件即可提前通关",
                    FailureFeedback = "时间结束未达标/准确率不足，弹出【重试挑战】按钮；不扣进度不卡主线",
                    MemoryAdaptationRule = "独立预制体进入章节时加载，通关/退出立即Destroy；结束后执行资源回收",
                    DataIsolationRule = "通关状态仅写入当前激活账户，不跨账户读取或修改",
                    InfiniteRetry = true
                },
                PostMiniGameDialogues = new List<DialogueNode>
                {
                    "【产业工程师】完美！分拣准确率完全符合绿色工厂的生产标准。",
                    new DialogueNode
                    {
                        Text = "【产业工程师】看到这些合格和不合格的产品被有效处理，你认为这其中最大的价值在哪？",
                        Choices = new List<DialogueChoice>
                        {
                            new DialogueChoice { OptionText = "资源最大化利用", ReactionDialogues = new List<DialogueNode> { "【产业工程师】正是这样，变废为宝是循环经济的核心。" } },
                            new DialogueChoice { OptionText = "减少自然环境破坏", ReactionDialogues = new List<DialogueNode> { "【产业工程师】是的，这样能避免工业废渣进一步污染土地。" } }
                        }
                    },
                    "【产业工程师】你刚才体验的，正是皖北产业升级的核心逻辑：用绿色技术改造传统产业，用循环经济降低能耗排放，用智能制造提升生产效率。",
                    "【产业工程师】如今，皖北已经形成了新能源、新材料、高端装备制造等一批绿色产业集群，成为安徽高质量发展的重要增长极。"
                },
                UnlockedCards = new List<string>
                {
                    "绿色制造与循环经济",
                    "清洁能源应用场景",
                    "皖北产业升级成果"
                },
                TransitionDialogue = "【区域协同专员】皖北的产业升级，离不开长三角一体化的科创赋能。最后一站，我们去看看长三角科创协同的成果。"
            };
        }

        public static StoryChapterConfig BuildChapter3()
        {
            return new StoryChapterConfig
            {
                ChapterId = ChapterIds.YangtzeDelta,
                ChapterName = "第三章・长三角科创协同",
                SceneDescription = "长三角科创展厅、区域协同数字大屏、科创成果展示墙",
                PreludeDialogues = new List<DialogueNode>
                {
                    "【区域协同专员】这里是长三角科创协同成果展厅，长三角是全国科创资源最密集、产业协同最紧密的区域之一。",
                    "【区域协同专员】安徽作为长三角的重要成员，正深度融入一体化发展，实现了科创资源共享、产业链互补、创新成果协同转化。",
                    new DialogueNode
                    {
                        Text = "【区域协同专员】你认为长三角协同的关键是解决什么问题？",
                        Choices = new List<DialogueChoice>
                        {
                            new DialogueChoice { OptionText = "知识产权互认", ReactionDialogues = new List<DialogueNode> { "【区域协同专员】打破地域壁垒是融合的基础。" } },
                            new DialogueChoice { OptionText = "基础设施联通", ReactionDialogues = new List<DialogueNode> { "【区域协同专员】交通网、信息网互联能让要素极速流动。" } },
                            new DialogueChoice { OptionText = "高精尖人才流动", ReactionDialogues = new List<DialogueNode> { "【区域协同专员】人才是第一资源，建立互通的引才机制是活水之源。" } }
                        }
                    },
                    "【区域协同专员】这些都很关键。长三角协同的核心，就是把合适的科创资源、产业优势、城市定位精准匹配，发挥 1+1>3 的效果。",
                    "【区域协同专员】现在，需要你完成一次区域科创资源的匹配，搭建起长三角协同发展的桥梁。"
                },
                MiniGameGuideDialogues = new List<DialogueNode>
                {
                    "【区域协同专员】规则很简单：左侧是长三角城市，中间是核心产业，右侧是科创资源，你需要把三者一一对应连线。",
                    "【区域协同专员】比如：合肥对应量子信息产业，对应量子科学实验室科创资源。",
                    "【区域协同专员】90 秒内完成全部 8 组正确匹配即可完成任务，错误匹配会触发时间与进度惩罚。",
                    "【系统】是否开始挑战？【开始挑战】按钮"
                },
                MiniGame = new MiniGameConfig
                {
                    MiniGameId = "innovation_matching",
                    Name = "科创资源匹配",
                    GameplayCore = "点击式连线匹配，无复杂操作，纯科普向",
                    DurationSeconds = 90,
                    SceneElements = "三列匹配项（城市、产业、科创资源）、动态连线、正确/错误反馈标识",
                    VictoryCondition = "90 秒内完成全部 8 组城市-产业-科创资源正确匹配",
                    FailureFeedback = "时间结束未完成全部匹配，或被错误惩罚耗尽时间，弹出【重试挑战】按钮；不扣进度不卡主线",
                    MemoryAdaptationRule = "独立预制体进入章节时加载，通关/退出立即Destroy；结束后执行资源回收",
                    DataIsolationRule = "通关状态仅写入当前激活账户，不跨账户读取或修改",
                    InfiniteRetry = true
                },
                PostMiniGameDialogues = new List<DialogueNode>
                {
                    "【区域协同专员】太厉害了！你完全掌握了长三角科创协同的核心逻辑。",
                    new DialogueNode
                    {
                        Text = "【区域协同专员】当各方优势互补后，你觉得这种机制未来会怎样反哺社会？",
                        Choices = new List<DialogueChoice>
                        {
                            new DialogueChoice { OptionText = "科技成果加速落地", ReactionDialogues = new List<DialogueNode> { "【区域协同专员】对，能让前沿技术最快投入民用。" } },
                            new DialogueChoice { OptionText = "培育世界级产业群", ReactionDialogues = new List<DialogueNode> { "【区域协同专员】是的，通过合力打造全球竞争力的矩阵。" } }
                        }
                    },
                    "【区域协同专员】通过城市定位、产业优势、科创资源的精准匹配，长三角实现了创新链、产业链、人才链的深度融合。",
                    "【区域协同专员】安徽也在这个过程中，实现了科创实力的快速提升，成为长三角科技创新的重要策源地。"
                },
                UnlockedCards = new List<string>
                {
                    "长三角一体化发展战略",
                    "区域科创协同机制",
                    "安徽在长三角的核心定位"
                },
                TransitionDialogue = string.Empty
            };
        }
    }
}
