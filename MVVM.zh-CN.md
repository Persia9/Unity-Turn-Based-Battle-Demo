# UI 的 MVVM 分工

所有界面通过 R3 订阅 ViewModel 的显示状态，并将点击事件转发给 ViewModel。静态对象、颜色、字号和位置在 Prefab 中配置。

| View | ViewModel | 负责的状态与操作 |
|---|---|---|
| TitleScreenView | TitleScreenViewModel | 标题文本、语言、开始游戏、加载状态、错误提示 |
| BattleHudView / BattleActionButton | BattleViewModel | HP、回合、战斗信息、日志、行动可用性、提交行动 |
| ResultPopupView | ResultPopupViewModel | 战绩、升级选择、重试、返回标题、失败恢复 |
| PerformanceView | PerformanceViewModel | 面板开关、采样频率、性能文本 |

View 不再直接依赖 UseCase、RunSession、IBattleSession 或本地化服务。字体应用和日志滚动依赖 Unity 控件，因此保留在 View 中。ViewModel 调用 UseCase，不承担战斗规则；性能数据通过 IPerformanceMetrics 获取，UnityPerformanceMetrics 负责调用 Unity 的统计接口。

标题、结算和性能 ViewModel 在对应场景的 Installer 中注册，BattleViewModel 继续由 BattleSceneInstaller 管理。View 打开时订阅，关闭时解除订阅；场景释放时，Zenject 调用 ViewModel.Dispose。异步操作结束前若场景已经释放，不再向已释放的显示状态写入数据。

标题与结算页面在等待场景加载时拒绝重复操作。结算页保留本次战绩快照；升级已经应用但下一场景加载失败时，只允许重试同一个升级，实际避免重复加成仍由 RunFlowUseCase 保证。

本次在 Unity 外运行了 9 组 ViewModel 行为验证：重复操作、错误恢复、语言刷新、升级重试、最终结算、性能采样、HUD 显示投影与释放生命周期。运行时程序集编译通过；Unity 内的画面、按钮交互和场景切换仍需实际确认。
