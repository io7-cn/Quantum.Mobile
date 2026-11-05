# 安装与更新

[English](../en/releases.md)

从[客户端发行页](https://github.com/XmmShp/Quantum/releases)下载可用的 Windows 便携归档或 Android [APK](glossary.md#apk)，每个版本列出适用架构及文件。应用检查该发行源，设置中提供自动与手动检查。

下载文件在打开前核对声明长度与 [SHA-256](glossary.md#sha256) 摘要。只接受更高语义版本或允许的更高构建号，Android 不提供构建号降级。Android 通过操作系统安装器明确确认安装，Windows 打开便携归档后需关闭应用并手动替换。Apple 目标使用其配置的分发渠道。

## 打包

Windows 执行 `.ci/Build-MobileRelease.ps1 -Version X.Y.Z -Target win-x64 -BuildNumber N`，签名 Android 构建使用 `-Target android`。构建号必须为正整数，Android 升级时递增。脚本在安装包旁生成 `update.<channel>.json` 和 `SHA256SUMS.<channel>.txt`。

| 环境变量 | 类型与要求 | 敏感级别 |
| --- | --- | --- |
| `MOBILE_ANDROID_KEYSTORE_PATH` | Android 打包必填，持久签名库路径 | 秘密位置 |
| `MOBILE_ANDROID_KEYSTORE_PASSWORD` | Android 打包必填，签名库密码 | 秘密 |
| `ANDROID_HOME` 或 `ANDROID_SDK_ROOT` | 用于验证签名的 Android 开发工具路径，优先 `ANDROID_HOME` | 非秘密 |

签名别名为 `quantum-mobile`，保留已安装客户端使用的签名身份。签名输入从外部注入，打包排除调试符号，并包含项目许可证和版权归属。持有签名凭据的发行流水线获取本仓库的固定提交，本仓库自身的[持续集成](glossary.md#ci)使用非生产签名验证构建。
