# Quantum Mobile

[English](../en/README.md)

Quantum Mobile 是 [Quantum Desktop](https://github.com/io7-cn/Quantum.Desktop) 的原生伴随客户端，由艾欧柒（杭州）智能科技有限公司产品研发团队维护。它显示插件声明的页面，将操作发送给登录同一平台账号的电脑，业务操作由桌面插件执行。应用采用 [.NET MAUI](glossary.md#maui)，包含 Android、iOS、Windows 和 Mac Catalyst 目标。

## 源码组织

| 目录 | 职责 |
| --- | --- |
| `src/Quantum.Mobile/` | 原生控件、页面编排、平台接口和存储适配 |
| `src/Quantum.Mobile.Core/` | 平台通信、导航状态、附件存储和更新 |
| `src/Quantum.Mobile.Protocol/` | 移动消息、页面模型、序列化和校验 |
| `tests/` | 对应的宿主、Core、协议测试和页面资源 |
| `docs/en/` 与 `docs/zh-CN/` | 维护中的双语文档 |
| `.ci/` | 验证与打包辅助工具 |

源码项目自行还原依赖，协议与 Core 从本仓库构建。插件作者使用独立发布的 [Quantum SDK](https://github.com/io7-cn/Quantum.Sdk)，[SDK](glossary.md#sdk) 是软件开发工具包。

## 构建与验证

使用 `global.json` 选择的 .NET 10 开发工具、目标平台工作负载及原生平台工具。Android 需要 Android 开发工具和 Java Development Kit 21。Apple 目标需要 macOS 和兼容版本的 Xcode。

```powershell
dotnet test .ci/Quantum.Mobile.Checks.slnx -c Release
dotnet build src/Quantum.Mobile/Quantum.Mobile.csproj -c Debug -f net10.0-windows10.0.19041.0 -p:QuantumMobileTargetFramework=net10.0-windows10.0.19041.0
dotnet build src/Quantum.Mobile/Quantum.Mobile.csproj -c Debug -f net10.0-android -p:QuantumMobileTargetFramework=net10.0-android
```

使用相同项目、目标和属性执行 `dotnet run` 可启动原生目标。Windows 和 Android 构建纳入[持续集成](glossary.md#ci)，Apple 目标需要对应设备和工具链验证。

## 连接

启动 Desktop，登录 Quantum Platform 账号并安装声明了移动页面的插件。Mobile 登录同一账号后选择在线电脑。账号菜单提供电脑切换与设置，底部导航打开插件页面，下拉页面可刷新。切换电脑前应先核对待确认的操作。

默认平台为 `https://quantum.io-vii.com/`。高级设置接受自部署平台的根地址。除[回环地址](glossary.md#loopback)开发连接外，传输要求 HTTPS。账号和设备凭据使用[安全存储](glossary.md#secure-storage)。详见[登录会话](login-sessions.md)和[配置](configuration.md)。

Debug 构建提供隔离预览与本地自动化接口，设置方式见[开发指南](development.md)。Release 构建排除这些工具。

另见[架构](architecture.md)、[安装与更新](releases.md)、[贡献指南](CONTRIBUTING.md)及[安全问题报告](security.md)。普通缺陷和需求提交到 [Issues](https://github.com/io7-cn/Quantum.Mobile/issues)。

项目采用 [Apache License 2.0](../../LICENSE)，版权归属及第三方条款见 [NOTICE](../../NOTICE)。
