# Security

[简体中文](../zh-CN/security.md)

Report vulnerabilities privately through the [repository security page](https://github.com/io7-cn/Quantum.Mobile/security). Use [Issues](https://github.com/io7-cn/Quantum.Mobile/issues) for non-sensitive bugs. Send a minimal reproduction with affected versions and targets, excluding passwords, account tokens, device secrets and personal content.

The app stores credentials using OS [secure storage](glossary.md#secure-storage), requires encrypted platform connections, validates declared page controls, and checks attachment bounds and integrity. It executes plugin operations on Desktop. Automatic retries preserve the original action identifier. Unknown outcomes remain visible until checked.

Debug automation requires explicit opt-in and a per-launch token on [loopback](glossary.md#loopback). Release excludes this transport. Update downloads are size-bounded and verified against [SHA-256](glossary.md#sha256) digests from the configured release source. Android installation requires explicit OS approval.
