# AssetBundle 与 HybridCLR

当前版的完整配置与测试方式见 [HybridCLR 说明](HYBRIDCLR.zh-CN.md)。

- LocalAssetCatalog 继续定义三个 UI Prefab 和三份 MasterData 的加载 key，进入 localcontent Bundle。
- Title、Battle 分别进入场景 Bundle。Unity 收集资源依赖，打包器将实际跨包依赖写入 manifest；运行时先递归加载依赖，再加载目标 Bundle。
- Startup 是唯一内置启动场景。DLL 与补充元数据加载完成后，才允许打开包含热更新脚本的场景和 Prefab。
- Player 从随包或经过校验的缓存加载 Bundle。当前实现校验字节后使用 LoadFromMemoryAsync，也能处理 Android APK 中的 StreamingAssets jar 路径。
- 一个进程只使用一个已选定版本。Bundle 容器保留到进程结束，防止业务场景和共享资源仍在使用时被提前卸载；IAssetService 的 Load / Release 记录资源引用，退出时 Unload(false) 不强制销毁场景对象。
- Editor 从 Startup 启动后，通过相同 key 直接读取项目资产，不模拟 DLL 下载或原生 IL2CPP 执行。
- 构建使用 Tools > HybridCLR Demo 菜单。旧 Tools > AssetBundles > Build Local Bundles 现在转到首次客户端内容准备，而不是生成无法独立启动的旧包。

此前仅本地 AssetBundle 的实现保存在提交 543ff28。旧 Addressables 远程更新版保留在 feature/addressables-content-update 分支。
