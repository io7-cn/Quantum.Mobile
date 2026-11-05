# 安全

[English](../en/security.md)

通过[仓库安全页面](https://github.com/io7-cn/Quantum.Mobile/security)的私密漏洞报告入口提交安全问题。非敏感缺陷使用 [Issues](https://github.com/io7-cn/Quantum.Mobile/issues)。报告包含受影响版本、目标平台和最小复现，去除密码、账号令牌、设备秘密及个人内容。

应用使用操作系统[安全存储](glossary.md#secure-storage)保存凭据，要求加密的平台连接，验证声明的页面控件及附件长度和完整性。插件操作由 Desktop 执行，自动重试保留原操作标识，未知结果保留到完成核对。

调试自动化需要显式开启，只监听[回环地址](glossary.md#loopback)，使用每次启动单独的令牌。Release 排除该传输。更新下载限制长度，并与配置发行源提供的 [SHA-256](glossary.md#sha256) 摘要比对，Android 安装需要操作系统中的明确确认。
