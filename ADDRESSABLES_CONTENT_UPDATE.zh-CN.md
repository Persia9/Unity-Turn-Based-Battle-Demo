# Addressables 资源更新演示

## 当前结构

- 安装包只内置 `Assets/Scenes/Bootstrap.unity`、启动 UI 和已有 C# 代码。
- `Remote Content` 组包含 Title、Battle、UI、角色、平台、三份 MasterData，通过 `RemoteContent` 标签下载。
- 启动顺序：初始化 Addressables → 检查 Catalog → 更新 Catalog → 计算缺失资源 → 下载 → 加载 Title。
- 启动页只显示状态文字、下载大小和进度条，没有操作按钮。失败后按 2、4、8、15 秒的间隔自动重试，之后每 15 秒继续尝试，直到成功或退出；等待使用真实时间，不受 Time.timeScale 影响。
- 每次重试重新检查 Catalog 与缺失资源，已经完整缓存的 Bundle 不会重复下载。检查阶段进度条保持起点，完成下载后显示为满格。
- UI 与角色数据继续使用现有 Prefab、MVVM、Zenject 和 MasterMemory。场景由 Addressables 加载，不再通过 Build Settings 中的名称加载。
- 更新在重新启动客户端时检查。本演示不在战斗中替换已经加载的场景和资源，不包含 C# 代码热更新。

## 第一次构建（Windows 64 位）

1. 在 Unity 6000.3.12f1 打开项目，等待导入与编译完成，切换到 Windows 64 位。
2. `Tools > Content Update > 1 Configure Remote Content`。默认地址为 `http://127.0.0.1:8080/[BuildTarget]`。可在 Addressables Profiles 修改 `Remote.LoadPath`，修改必须发生在初始包构建之前。
3. 执行 `2 Build Baseline Resources`。菜单会先执行 Excel → CSV → MasterData，再构建 Addressables，并保留初始的 `addressables_content_state.bin`。
4. 执行 `3 Build Demo Player From Baseline`。它只打包 Bootstrap，并使用刚才生成的初始 Catalog，不会重新构建 Addressables。此菜单生成 Development Build，允许本地 HTTP。
5. 在项目目录运行 `python Tools/serve_content.py`，保持终端运行。
6. 运行 `Artifacts/ContentUpdate/StandaloneWindows64/1.0.0/Player/CubeBattle.exe`。启动页显示下载状态，随后进入原版标题 `CUBE BATTLE`。退出客户端。

输出位置（`1.0.0` 对应 Player Settings 的版本号）：

```text
ServerData/StandaloneWindows64/                          # 本地服务器实际发布目录
Artifacts/ContentUpdate/StandaloneWindows64/1.0.0/
  Baseline/addressables_content_state.bin                # 原始版本状态，更新构建的输入
  Baseline/ClientData/                                   # 初始客户端 Catalog 与本地 Addressables 数据
  Baseline/ServerData/StandaloneWindows64/                # 初始服务器文件快照
  Player/CubeBattle.exe                                  # 同一份演示客户端
```

初始状态文件存在时，菜单拒绝覆盖它。新安装包版本应修改 Player Settings 的 Version 后重新建立初始版本；同一安装包的资源更新使用下面的更新菜单。

## 使用同一个客户端演示更新

1. 保留上一步生成的客户端与初始状态文件，不重新构建 Player。
2. 执行 `Tools > Content Update > 5 Build Sample Title v2 Update`。
3. 菜单临时把标题 Prefab 的 `Game Title` 文本改为 `CUBE BATTLE v2`，使用原始状态文件构建更新，然后恢复工程中的 Prefab 原文件。
4. `ServerData/StandaloneWindows64` 已被更新，保持服务器运行，重新启动同一个 EXE。
5. 客户端检查新 Catalog、下载有变化的 Bundle，并显示 `CUBE BATTLE v2`。这就是仅更新资源、没有重新安装客户端的演示。

示例更新恢复了工程中的 Prefab，所以 Editor 的 `Use Asset Database` 模式仍会显示工程原文件。验证发布资源请使用上述客户端；若在 Editor 验证，使用 `Use Existing Build` 并从 Bootstrap 启动。

## 更新自己的资源

修改现有 Prefab、场景或 Excel 后，执行 `4 Build Content Update`。它会重新转换 MasterData，并始终以保留的初始状态文件构建更新。

改变的发布文件会另存到：

```text
Artifacts/ContentUpdate/StandaloneWindows64/1.0.0/Updates/<时间>/ServerData/StandaloneWindows64/
```

这些文件是相对初始版本的发布差异。部署时覆盖到原服务器的对应目录，保留旧 Bundle；先上传 Bundle，再上传 Catalog，最后上传 `.hash`。不要把差异目录当作完整服务器目录单独替换。状态文件是构建输入，不需要上传到公开资源服务器。

`Tools/serve_content.py` 对 Catalog 与 hash 使用 `no-cache`，对带内容哈希的 `.bundle` 使用长期缓存。本地默认只监听本机；实体设备演示时，将 Profile 地址改为电脑的局域网 IP，并以 `--bind 0.0.0.0` 启动服务器，然后重新构建初始包。正式发布使用 HTTPS。

## 兼容范围与验证状态

资源必须兼容同一客户端已有的脚本类型、序列化布局和 MasterData schema。修改 C# 类型、升级 Unity 或修改数据结构，需要重新评估并构建客户端初始版本。

运行时与构建菜单已完成独立编译检查。当前 Codex 环境运行 Unity 批处理时，在进入工程构建前因授权 IPC/WMI 初始化失败退出，尚未实际生成 Bundle 或 Player；请在本机 Unity 中执行上述菜单完成打包与运行确认。

### 场景切换遮罩

`ProjectContext` 持有本地 `SceneLoadingOverlay.prefab`，切换场景前显示常驻遮罩。Title / Battle 完成 MasterData 初始化和 UI 创建后，等待下一帧再隐藏；加载失败时解除遮罩。遮罩不会随 Single 场景切换被销毁，且不依赖远程 Bundle。外观在 Prefab 中调整。
