# 五个 Excel 维护 MasterData

默认流程：**Excel → CSV → 校验 → MasterMemory `.bytes` → Addressables → 运行时**。

## 文件与字段

在 `Assets/MasterData/Excel` 编辑这五个文件：

| 文件 | 内容 | 字段 |
|---|---|---|
| Characters.xlsx | 角色 | Id、Name、MaxHp、DefaultSkillId |
| Skills.xlsx | 技能 | Id、Name、Power |
| Stages.xlsx | 关卡 | Id、PlayerCharacterId、EnemyCharacterId、EnemyPattern、DisplayName |
| UI.xlsx | UI定义 | Id、PrefabKey、Type、Cache、BlockInput |
| Localization.xlsx | 英日文本 | Key、English、Japanese |

五个文件均使用 **Data** 工作表。第1行是固定字段名，第2行开始是数据；可以筛选或调整列顺序，导入器按字段名识别。枚举列有下拉菜单，布尔列使用Excel布尔值。筛选和隐藏只改变显示，导出仍包含全部数据记录。

## 日常使用

1. Unity 菜单 **Tools > MasterData > Open Excel Folder**，打开要修改的表格。
2. 编辑或追加记录，保存Excel。保持文件名、Data工作表名和字段名不变，不合并单元格。
3. 点击 **Tools > MasterData > Build From Excel (Excel - CSV - Bytes)**。
4. 停止并重新运行Play，避免继续使用缓存的数据库。使用打包资源时，请重新构建Addressables内容。

原来的 **Build Demo MasterData** 现在也执行完整的Excel流程，不再默认读取ScriptableObject。不需要安装Excel插件、COM、Python或额外NuGet包。

读取的是磁盘上的已保存文件，不是Excel窗口中尚未保存的内容。如文件被独占或保存过程中读取失败，请关闭Excel后重试。

## 分步菜单

- **Export Excel To CSV**：转换并校验五个Excel，只更新CSV，不更新`.bytes`。
- **Build From CSV**：读取现有五个CSV，校验并生成三份数据库，不重新读取Excel。需要先确认CSV已更新。

CSV输出在 `Assets/MasterData/CSV`：Characters.csv、Skills.csv、Stages.csv、UI.csv、Localization.csv。

Characters / Skills / Stages 合并为 `master_core.bytes`，UI为`master_ui.bytes`，Localization为`master_localization.bytes`。保留既有输出的`.meta`和Addressables地址。

## 编辑约定

- 角色／技能／关卡Id为正整数，表内不能重复；UI的Id为字符串。不要为了调整顺序修改已有Id。
- MaxHp、Power为正整数。DefaultSkillId和关卡中的角色Id必须引用已有记录。
- EnemyPattern：NormalOnly、EveryThirdTurn、BossCycle。
- 显示层级由 Type 自动推导，不需要单独填写。
- Type：Screen、Popup、Hud、Overlay、Toast。
- 枚举也接受已定义的对应整数。布尔CSV值接受TRUE/FALSE或1/0。
- Japanese可以留空以使用英文回退。Key不能重复，English不能为空，英日占位符索引需一致。

文本支持单元格内换行、逗号、双引号、日文、TMP富文本和占位符。生成的CSV为UTF-8带BOM，使用标准引号转义。字符串首尾空格保留，数字和枚举周围的空格忽略。

仅支持普通`.xlsx`里的直接值。**不支持公式**：先计算并粘贴为值；导入器会拒绝公式，不读取可能过期的缓存。其他工作表不参与导出。不读取`.xls`、宏或外部数据。

既有重复ID、外键、必要关卡与UI、本地化格式校验继续执行。全部校验和数据库序列化成功后才开始写生成文件；磁盘写入是逐文件操作，不提供跨文件事务。

## 数据源

MasterData只使用Excel/CSV。旧ScriptableObject资产、专用Inspector和Legacy菜单已删除。MasterDataSourceData及其SourceRow类是CSV转换所需的普通C#数据对象，并不继承ScriptableObject。战斗按钮的BattleActionDefinition属于另外的UI配置，保持原样。

## 本次确认

- 当前4角色、4技能、3关卡、3UI、61组文本，共75条记录逐字段迁移一致。
- 实际C#代码跑通五个Excel→五个CSV→三份MasterMemory数据库，并用MemoryDatabase读回；CSV重新加载生成的二进制与Excel路线一致。
- 15项解析与转换测试通过，覆盖多行、引号、BOM、布尔值、共享字符串、稀疏单元格、追加行、公式拒绝及无效数据。
- 编辑器代码针对当前Unity引用编译成功，五个表格已渲染检查。
- 本次未操作真实Excel应用或启动Unity点击菜单，最终菜单和画面请在Unity中确认。

建议提交`.xlsx`、生成的`.csv`、`.bytes`及对应`.meta`和代码。

UI 的每个 ID 同时只打开一个实例，再次打开时复用。Cache 只决定关闭后是否保留实例，不控制是否允许重复打开。

Layer 不再单独配置，由 Type 自动决定：Hud→Hud、Screen→Screen、Popup→Popup、Overlay→Overlay、Toast→Top。
