# 多语言统一使用 MasterData

## 数据流

```text
Localization.xlsx → Localization.csv
  → LocalizationSourceValidator
  → LocalizationMaster（Key、English、Japanese）
  → MasterMemory DatabaseBuilder
  → master_localization.bytes
  → AssetBundle：Master/Localization
  → LocalizationRepository
  → UnityLocalizationService / ILocalizationService.Get()
  → View
```

UnityLocalizationService 不再保存英日字典，只负责读取语言偏好、切换语言、保存 PlayerPrefs、发送 LanguageChanged，查询和格式化委托给 LocalizedTextLookup。这里没有使用 Unity 官方 Localization 插件。

## 编辑和导出

1. 打开 Assets/MasterData/Excel/Localization.xlsx。
2. 在 Data 工作表编辑 Key、English、Japanese。Key对应Get()标识，English是回退文本，Japanese是日文。
3. 文本支持换行和TMP富文本；占位符使用 `{0}`、`{1}`、`{0:00}` 等，显示花括号使用 `{{` 和 `}}`。
4. 保存Excel后，选择 Tools > MasterData > Build From Excel (Excel - CSV - Bytes)。转换时会自动校验并生成CSV和数据库。
5. 停止并重新进入 Play，避免继续使用缓存的旧数据库。Player 需要重新构建本地 AssetBundle；Editor 直接读取项目资源。

原服务的61组英日文本已迁移，并生成和注册了 master_localization.bytes。原来的 master_core.bytes / master_ui.bytes 未重写。字体仍由已有 LocalizedFontProvider 处理，本次不修改字体资源。

## 查询和回退

- 英文：读取 English。
- 日文：读取 Japanese；日文为空时回退到 English。
- 没有对应Key：返回Key本身。
- 两种文本都为空的异常记录：返回Key；正常导出时English为空会被阻止。
- 传入格式参数时使用 InvariantCulture 格式化，保持原服务行为；没有参数时返回原始模板。
- 用户语言选择仍保存在 `game.language`，首次默认语言规则不变。

新增加语言需要扩展当前双语言字段、GameLanguage及查询映射；当前实现没有把任意语言数量做成通用配置。

## 校验

导出会检查至少有一条文本、Key非空且不重复、English非空、格式语法有效、英日占位符索引一致。日文允许为空以使用英文回退。占位符索引支持0至31，可按翻译需要调整顺序或重复使用同一个索引。

校验失败不会开始覆盖输出；每一份表都先完成序列化，再写3个文件。和原导出器一样，不提供跨文件的磁盘事务。

## 初始化顺序

InitializeMasterDataUseCase 依次确保 Core、UI、Localization 数据库加载成功，之后才打开标题或战斗HUD。读取文本前必须等待初始化完成；LocalizationRepository 会在未加载时报告明确错误，不会悄悄用硬编码字典代替。

如果第三份本地化数据加载失败，重新调用初始化只重试这一份，已经成功的 Core / UI 不会重复加载。Master/Localization 已加入 LocalAssetCatalog，对应本地 localcontent Bundle。

## 本次验证

- 运行时程序集使用当前Unity引用、MasterMemory和MessagePack源码生成器编译成功。
- 编辑器的数据源、校验和导出代码对更新后的Runtime编译成功。
- 18项查询、回退、格式、校验、失败重试和并发初始化测试在.NET环境通过。
- 使用实际生成的DatabaseBuilder生成本地化二进制，并用MemoryDatabase读回，61组英日文本逐条一致。
- 没有由本次操作启动Unity进行实际的标题显示、语言切换或Inspector导出验证；这些仍需在Unity中检查。

适用文件包括 Scripts及其.meta、DemoMasterData.asset、新的.bytes/.meta、AssetBundle分组和测试。请不要只复制UnityLocalizationService.cs，否则缺少仓库与加载注册。
