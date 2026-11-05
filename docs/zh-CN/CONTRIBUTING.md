# 贡献指南

[English](../en/CONTRIBUTING.md)

从 `main` 创建分支，按约定式提交记录聚焦的改动。测试放在源码归属的测试项目中，保持相对目录对应。双语文档一同维护，命名、布局、大括号、var 和表达式方法遵循 `.editorconfig`。

还原后运行 `./.ci/Format-Mobile.ps1 -Verify` 和 `dotnet test .ci/Quantum.Mobile.Checks.slnx -c Release`。原生改动还需要构建受影响目标。还原及格式化 `Quantum.Mobile.slnx` 时，将 `QuantumMobileTargetFramework` 设为相应原生目标。完整解决方案包含原生应用，检查用解决方案与平台无关。

渲染或导航改动使用隔离 Debug 检查，确认 Release 继续排除调试接口。移动代码时保留序列化消息和会话存储键，新增实现前优先采用原生平台接口和已有框架传输。

贡献采用 [Apache License 2.0](../../LICENSE)，保留 [NOTICE](../../NOTICE) 中适用的版权归属及第三方条款。技术术语通过[术语表](glossary.md)解释，普通问题提交到 [GitHub Issues](https://github.com/io7-cn/Quantum.Mobile/issues)。
