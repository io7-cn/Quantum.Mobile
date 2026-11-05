# 安装与更新

[English](../en/releases.md)

从[客户端发行页](https://github.com/io7-cn/Quantum.Mobile/releases)下载可用的 Windows 便携归档或 Android [APK](glossary.md#apk)，每个版本列出适用架构及文件。应用检查该发行源，设置中提供自动与手动检查。

下载文件在打开前核对声明长度与 [SHA-256](glossary.md#sha256) 摘要。只接受更高语义版本或允许的更高构建号，Android 不提供构建号降级。Android 通过操作系统安装器明确确认安装，Windows 打开便携归档后需关闭应用并手动替换。Apple 目标使用其配置的分发渠道。

## 打包

Windows 执行 `.ci/Build-MobileRelease.ps1 -Version X.Y.Z -Target win-x64 -BuildNumber N`，签名 Android 构建使用 `-Target android`。构建号必须为正整数，Android 升级时递增。脚本在安装包旁生成 `update.<channel>.json` 和 `SHA256SUMS.<channel>.txt`。

| 环境变量 | 类型与要求 | 敏感级别 |
| --- | --- | --- |
| `MOBILE_ANDROID_KEYSTORE_PATH` | Android 打包必填，持久签名库路径 | 秘密位置 |
| `MOBILE_ANDROID_KEYSTORE_PASSWORD` | Android 打包必填，签名库密码 | 秘密 |
| `ANDROID_HOME` 或 `ANDROID_SDK_ROOT` | 用于验证签名的 Android 开发工具路径，优先 `ANDROID_HOME` | 非秘密 |

签名别名为 `quantum-mobile`。签名输入从外部注入，打包排除调试符号，并包含项目许可证和版权归属。Android 安装包必须匹配 [.ci/android-signing-certificate.sha256](../../.ci/android-signing-certificate.sha256) 记录的公开证书摘要。签名库及密码须保存在 Git 之外经批准的加密备份存储中，后续 Android 发行必须复用该身份。

## 独立仓库发行工作流

[Release Mobile](../../.github/workflows/release-mobile.yml) 直接构建本仓库。在已验证的 `main` 历史上推送 `mobile-vX.Y.Z` 标签，构建 Android 与 Windows 安装包、验证校验和后发布完整版本。允许预发行后缀。手动运行接受版本号，只上传工作流产物，不创建发行版本。

配置仓库 Secrets：`MOBILE_ANDROID_KEYSTORE_BASE64`（持久 [PKCS12](glossary.md#pkcs12) 签名库的 base64 编码）和 `MOBILE_ANDROID_KEYSTORE_PASSWORD`（签名库与密钥共用密码）。工作流仅在 Android 运行器上还原签名库，并在打包后删除。常规验证构建不接收发行签名凭据。两个平台共用同一个正整数构建号，取自 2020-01-01 起的 [UTC](../../.standards/docs/glossary.md#utc) 秒数，后续发行必须使用更高构建号。

## 从旧分发方式升级

独立仓库采用新的 Android 签名证书，旧证书签名的安装版本不能直接覆盖升级。请先导出或备份可恢复的应用数据，卸载旧应用，再安装新签名的安装包并重新登录。卸载会删除应用本地数据，操作前必须确认备份有效。后续发行须保持新的签名身份稳定。

仍使用旧发行源的客户端需要从本仓库手动安装一次。Windows 请先关闭应用，在保留用户数据的前提下替换便携版文件；Android 按上述卸载重装流程操作。源代码仓库的迁移不会自动重定向已安装客户端的更新源。
