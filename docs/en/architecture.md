# Architecture

[简体中文](../zh-CN/architecture.md)

## Dependency direction

The native host depends on Core. Core depends on the protocol project and [NOF](glossary.md#nof) Contract. The protocol project depends only on the .NET runtime libraries. Native controls, permissions, secure storage and lifecycle callbacks stay in the host. Tests reference production assemblies. The host references the dedicated debug transport assembly only in Debug builds. Its tests reference that same production assembly.

The existing `Quantum.Mobile.Internal` namespace and serialized field names form the wire compatibility boundary. Renaming folders or assemblies must preserve these messages. Desktop, Mobile and Platform maintain local protocol copies, so a message change requires coordinated compatibility verification.

## Platform transport

`PlatformRpcClient` reuses the [NOF](glossary.md#nof) [JSON](glossary.md#json) [RPC](glossary.md#rpc) client for login and session operations. `PlatformSession` owns refresh coordination and retry rules. `BridgeClient` owns the mobile control endpoint, response-size limits and server-clock tracking. Plugin calls execute on Desktop.

The page coordinates account, desktop and navigation state. Rendering stays in native controls. The [outbox](glossary.md#outbox) persists a complete command before clearing submitted fields. Retries retain its original identifier and payload. An unknown outcome requires confirmation on Desktop. New draft input survives delayed results.

## Attachments and audio

`OutgoingAttachmentStore` streams selected bytes into a private temporary file, applies the size limit while reading, validates metadata and the [SHA-256](glossary.md#sha256) digest, then publishes the file atomically. Failed or cancelled saves remove the incomplete file. Old committed attachment files expire after 24 hours.

Attachment input supports up to four files, eight MiB per file and sixteen MiB per action. Uploads use repeatable 128 KiB chunks. Server and Desktop verify ownership, length and digest before execution.

Native recording requires explicit microphone permission. It produces mono 16 kHz, 16-bit [PCM](glossary.md#pcm) audio in a wave container and stops after 45 seconds or when the app moves to the background. Android uses native capture with a small wave-header encoder. Windows and Apple targets use their native recorders.

## Development transport

The opt-in debug server listens on [loopback](glossary.md#loopback), authenticates a per-launch token and exposes an allowlist of [UI](glossary.md#ui) operations. It rejects browser origins and oversized requests. The transport and preview code are excluded from Release. The [development guide](development.md) lists the routes and checks.
