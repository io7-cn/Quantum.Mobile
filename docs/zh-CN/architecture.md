# 架构

[English](../en/architecture.md)

## 依赖方向

原生宿主依赖 Core，Core 依赖协议项目和 [NOF](glossary.md#nof) Contract。协议项目只依赖 .NET 运行时库。原生控件、权限、安全存储和生命周期回调归宿主管理。测试引用生产程序集，宿主仅在 Debug 构建中引用独立的调试传输程序集，调试服务测试直接引用同一生产程序集。

现有 `Quantum.Mobile.Internal` 命名空间及序列化字段构成通信兼容边界，目录或程序集改名应保留这些消息。Desktop、Mobile 和 Platform 各自维护协议副本，消息变更需要协调兼容性验证。

## 平台通信

`PlatformRpcClient` 复用 [NOF](glossary.md#nof) 的 [JSON](glossary.md#json) [远程过程调用](glossary.md#rpc)客户端处理登录和会话操作。`PlatformSession` 管理刷新协调及重试规则，`BridgeClient` 管理移动控制端点、响应长度限制和服务端时钟偏移。插件调用在 Desktop 执行。

页面协调账号、电脑和导航状态，渲染由原生控件负责。[发件箱](glossary.md#outbox)在清空已提交字段前保存完整命令，重试保留原标识和内容。未知结果需要在 Desktop 核对，新草稿不会被延迟返回的结果覆盖。

## 附件与录音

`OutgoingAttachmentStore` 将选中文件流式写入私有临时文件，边读取边限制长度，校验元数据和 [SHA-256](glossary.md#sha256) 摘要后原子提交。保存失败或取消时清理不完整文件，已提交附件文件在 24 小时后清理。

附件输入每次最多四个文件，单个八 MiB、单次操作合计十六 MiB。上传采用可重复的 128 KiB 分块，服务端和 Desktop 在执行前核对归属、长度和摘要。

原生录音要求用户主动授权麦克风，输出单声道 16 kHz、16 位 [PCM](glossary.md#pcm) 的 wave 音频，最长 45 秒，退到后台时停止。Android 使用原生采集及小型 wave 文件头编码，Windows 和 Apple 目标使用对应的原生录音器。

## 开发传输

可选调试服务只监听[回环地址](glossary.md#loopback)，验证每次启动单独生成的令牌，只开放列明的界面操作，并拒绝浏览器来源及超长请求。Release 排除调试传输和预览源码，接口和验证方式见[开发指南](development.md)。
