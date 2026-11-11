# HybridCLR 代码与资源更新

当前工程固定使用 Unity 6000.3.12f1、HybridCLR 9.1.0，沿用原生 AssetBundle，不使用 Addressables。官方包来自 focus-creative-games/hybridclr_unity 的 v9.1.0；该版本内置 Unity 6000.3.x 的原生实现版本映射。

## 程序集边界

| 程序集 | 职责 | 发布方式 |
| --- | --- | --- |
| BattleDemo.Startup | 启动场景、本地加载界面、版本选择、下载校验、DLL / AOT 元数据 / Bundle 加载、稳定的 IAssetService / 场景加载 / 日志接口、最小 ProjectInstaller | AOT，随客户端发布 |
| BattleDemo.Domain | 战斗规则、状态、冷却、敌人行为、周回和强化规则 | 热更新 DLL |
| BattleDemo.Runtime | Application、Repository、MasterData 解析、MVVM、角色和摄像机表现、业务绑定与场景 Installer | 热更新 DLL |
| BattleDemo.Editor | Excel 转换、资源构建、客户端与更新发布菜单 | 仅 Editor |

AOT 不引用 Runtime 或 Domain；业务代码显式引用 Startup。程序集名称 Runtime 保持不变，以保留原 Prefab / 场景脚本关联；IAssetService、日志和 ProjectInstaller 移动时保留原 .meta GUID。

Zenject、UniTask、R3、MessagePipe、MasterMemory 等基础库作为固定 AOT 依赖。业务绑定在加载 DLL 后由 HotUpdate.GameEntry.Start 执行。ProjectContext 已初始化后追加的 IDisposable 显式交给 DisposableManager；MobileRuntimeSettings 显式初始化。场景内部的生命周期仍由各 SceneContext 管理。

## 启动顺序

Startup → 创建常驻本地加载遮罩 → 读取随包 manifest → 检查可用缓存 / 可选远程版本 → 校验并下载完整版本 → 加载补充 AOT 元数据 → 依次 Assembly.Load(Domain, Runtime) → 通过反射调用 GameEntry.Start → 注册业务依赖 → 加载 Title 的场景 Bundle → 初始化 MasterData 和 UI → 下一帧隐藏遮罩。

Title、Battle 不再放进 Player 场景列表，必须在 DLL 加载后从 AssetBundle 打开。Editor 请同样从 Startup 运行；Editor 分支使用已经编译的业务程序集、AssetDatabase 和 LoadSceneAsyncInPlayMode，不重复 Assembly.Load，也不下载服务器资源。因此 Editor 能运行不等于已经验证 IL2CPP 代码热更新。

## 首次准备与出包

1. 打开 Unity，等待 Package Manager 解析 HybridCLR 和代码编译完成。
2. 执行 Tools > HybridCLR Demo > 1 Configure。设置 IL2CPP、.NET Framework、Development Build 和 Startup 单启动场景。
3. 打开 HybridCLR > Installer，点击 Install，确认 Installed。只使用项目 Library/HybridCLR 下的本地 IL2CPP，不替换全局 Unity 安装。
4. 如要测试远程更新，在 Startup 场景的 StartupLauncher 中配置 Remote Root，例如 http://127.0.0.1:8080/HybridCLR/[BuildTarget]。留空则仅使用随包或已验证缓存。
5. 执行 Tools > HybridCLR Demo > 2 Prepare Client Content。这一步会运行官方 Generate/All，生成裁剪 AOT 元数据、桥接函数和保留列表，然后转换 Excel、编译 DLL、构建场景及资源 Bundle，保存不可覆盖的客户端基线。
6. 等待 Editor 完成生成文件的编译，然后执行 Tools > HybridCLR Demo > 3 Build Demo Player。

Windows 需要 Unity Hub 的 Windows Build Support (IL2CPP) 与 Visual Studio C++ 工具链。当前菜单支持 Windows x64 和 Android；iOS 没有配置为可直接使用的演示出包流程。

Configure 将演示包设为 Development，供本地 HTTP 测试；正式发布应另行配置 HTTPS 和发布构建流程。更换平台必须为该平台重新准备客户端，不能共用 DLL / 裁剪 AOT 元数据 / Bundle。

## 文件位置

- Artifacts/HybridCLR/<平台>/<App Version>/Baseline/state.json：客户端 ID、AOT 源码 / 固定依赖指纹、构建模式和初始清单哈希。
- 同目录 Baseline/Content：原始客户端配套的 manifest、DLL、AOT 元数据和 Bundle 备份。
- Windows 客户端输出到 Artifacts/Player/Win64/<App Version>/CubeBattle.exe，以缩短随包文件的读取路径。Android 的 CubeBattle.apk 仍在基线同级的 Player 目录。
- 同级 Updates/<时间>：每次发布的代码 / 资源完整版本。
- ServerData/HybridCLR/<平台>：资源服务器实际提供的文件。
- Assets/StreamingAssets/HotUpdate：随客户端安装的初始版本，不随发布更新而替换。
- Library/HybridCLR：官方生成的原生工具链、编译 DLL 和裁剪元数据。
- Assets/Startup/Generated/link.xml：根据业务引用生成的 AOT 保留列表。
- Assets/HybridCLRGenerated/Editor/AOTGenericReferences.cs：AOT 泛型需求记录，放在 Editor 目录以避免改变启动程序集的运行时结构；打包器读取其中的补充元数据列表。

旧 Assets/StreamingAssets/AssetBundles 会在 Prepare 时移到 Artifacts/LegacyAssetBundles 下备份，避免旧本地版 Bundle 被重复打进新 Player。不会删除原来的构建备份。

## 发布更新并验证

先构建一次原始 Player，并保留它。之后修改 Runtime / Domain 的代码，或修改 UI、场景、Excel 数据，再执行 Tools > HybridCLR Demo > 4 Build Code And Resource Update。

在项目目录运行：

```powershell
python Tools/serve_content.py
```

重启原来的 Player，Startup 会检查清单并下载新版本；不要重新构建 / 安装客户端，也不要清空缓存。可修改 BattleRules 的攻击倍率及相应 UI 文案，观察原客户端的伤害行为变化，证明更新了代码而不仅仅是资源。

手机测试：Remote Root 使用电脑局域网 IP，并运行 python Tools/serve_content.py --bind 0.0.0.0。电脑和手机在可互通的网络上。手机的 127.0.0.1 指向手机自身。

## 版本一致性与失败处理

- 每个客户端基线有独立 clientId 和平台标识。新客户端使用新的 App Version 建立新基线；原基线不会被更新覆盖。
- 所有文件使用 SHA-256 命名并校验大小与哈希；服务器先发布文件，最后发布 manifest，旧的哈希文件仍保留。
- 下载写入独立缓存版本，成功打开 Title 并初始化 UI 后才提交 active.txt。缓存位于 Application.persistentDataPath/HotUpdate/<clientId>。
- 网络不可用、文件校验失败、平台 / 客户端不匹配时，优先使用已验证的上次版本，再回退到随包版本。
- 如果 DLL 加载或业务启动失败，该缓存版本会被隔离。由于同一进程不能安全撤销已经加载的程序集，需要退出重启，再使用上次成功或随包版本；不会在进程中强行替换已加载 DLL。
- 更新复用原客户端的 AOT 补充元数据。固定 AOT 代码、第三方依赖、API 保留范围发生变化，或新增未准备的 AOT 泛型元数据需求时，菜单会要求重新发布客户端。
- 更新生效点是下次启动，不支持战斗进行中替换程序集。原生 SDK、引擎和平台权限配置仍需重新出包。

## 已验证与未验证

已完成基于 Unity 实际程序集、.NET Framework API 的 AOT / 热更新层 Player 与 Editor 条件编译、Editor 构建菜单编译，以及版本事务行为检查（本地启动、下载校验、提交时机、缓存复用、离线回退、坏版本隔离、缓存损坏、跨客户端 / AOT 元数据拒绝、路径与依赖检查、取消）。测试使用控制的文件与下载替身，不等同于真实网络或 AssetBundle 执行。

在当前执行环境尝试 Unity 批处理时，Unity 在导入项目前因 Windows WMI 初始化断言退出（work/hybridclr-configure.log）。尚未完成原生 HybridCLR Installer、Generate/All、真实 Bundle 构建或 IL2CPP Player 运行，不能视为真机热更新已通过。请按首次准备步骤在本机 Unity 完成验证。
