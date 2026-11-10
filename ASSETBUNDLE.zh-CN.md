# 本地 AssetBundle

本分支基于 main，仅替换本地资源加载；不包含远程下载、Catalog 更新或 Bootstrap。Title、Battle 继续作为内置场景由 SceneManager 加载。此前资源更新演示保留在 feature/addressables-content-update 分支。

## 资源与构建

LocalAssetCatalog 统一维护六个 key 与路径：三个 MasterData 二进制、TitleScreen、BattleHud、ResultPopup。Editor 和打包器使用同一份映射。当前将这些条目及其依赖打入一个 localcontent 包，不需要跨包依赖解析。角色和场景中直接引用的资源仍随内置场景打包。

Unity 菜单 Tools > AssetBundles > Build Local Bundles 会先将 Excel 转成 CSV/Bytes，再按当前目标平台构建 LZ4 Bundle。输出 Assets/StreamingAssets/AssetBundles/localcontent。使用 Unity Build / Build And Run 时，构建预处理器会自动执行，防止打入旧平台资源包。产物不提交 Git，克隆后重新构建即可。

## 加载与释放

Editor 直接通过 AssetDatabase 加载，不需要先构建 Bundle，不会调用 Addressables。Player 从 StreamingAssets 加载实际 Bundle；普通本地路径使用 LoadFromFileAsync，Android APK 内的 jar 路径等使用 UnityWebRequestAssetBundle。

IAssetService.Load 每次取得一份引用；Release 释放相应引用，UIViewAssetLease 在 UI 实例销毁时释放，MasterData 在解析完成后释放。串行门保证并发加载不会重复打开同一个包。所有引用释放后执行 Unload(false)，不强制销毁已加载对象；残留对象由 Unity 的未使用资源回收机制处理，这不等同于立即清空所有资源内存。

link.xml 保留可能仅由 Bundle 内 Prefab 引用的运行时和 UI 类型，避免 Player 代码裁剪导致组件丢失。

## 验证

Editor 运行 Title 可验证映射和 UI，但不能替代真实 Bundle 验证。用 Build And Run 生成 Player 后，检查标题、两种战斗模式、结算、语言切换及返回标题。停止 Python 服务器也应正常运行；本版无网络资源更新。

本次已完成独立 C# 编译和静态资源映射检查，尚未在 Unity 中构建 Bundle 或运行 Player。
