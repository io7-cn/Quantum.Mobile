# 配置

[English](../en/configuration.md)

| 设置 | 类型与默认值 | 持久化与敏感级别 |
| --- | --- | --- |
| 平台地址 | HTTPS 根地址，默认 `https://quantum.io-vii.com/` | 高级设置，本地偏好 `quantum.mobile.platform`，非秘密 |
| 语言 | `en` 或 `zh`，初始采用系统界面语言 | 登录页语言选择，`quantum.mobile.language`，非秘密 |
| 自动检查更新 | 布尔值，默认 true | 设置，`quantum.mobile.auto-update-check`，非秘密 |
| 账号会话 | 序列化凭据，登录前为空 | 操作系统[安全存储](glossary.md#secure-storage)，按平台隔离，秘密 |
| 设备注册与待发送操作 | 序列化凭据与操作内容，连接或提交前为空 | 操作系统安全存储，按平台与账号隔离，秘密 |
| `QUANTUM_MOBILE_DEBUG_PORT` | 整数 1024–65535，未设置时不开启服务 | 仅 Debug 进程环境，非秘密 |
| `QUANTUM_MOBILE_DEBUG_TOKEN` | 32–256 位可打印且不含空白的 ASCII 字符，启用服务时必填 | 仅 Debug 进程环境，秘密 |
| `QUANTUM_MOBILE_DEBUG_PREVIEW` | `1` 启用隔离预览，未设置时关闭 | 仅 Debug 进程环境，非秘密 |
| `QuantumMobileTargetFramework` | 构建属性，默认按构建系统选择原生目标 | 显式构建参数覆盖自动选择，非秘密 |

平台地址通过验证后才保存，除[回环地址](glossary.md#loopback)外强制 HTTPS。填写 `https://platform.example/` 这样的根地址，路径、凭据和查询参数的处理由 `PlatformAddress.Parse` 定义。认证成功后保存账号秘密，详见[登录会话](login-sessions.md)。

发布签名凭据通过[发行说明](releases.md)中的环境变量注入。Release 忽略调试环境变量。
