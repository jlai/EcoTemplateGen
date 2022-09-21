# Virtual Filesystem

EcoTemplateGen uses [Zio](https://github.com/xoofx/zio) to provide a virtual filesystem.

## Root system

| Path | Read-only? | Description |
| ---- | ---------- | ----------- |
| /Eco                 | Read-only  | Eco server directory |
| /Eco/Mods/__core__   | Read-only  |
| /Eco/Mods/UserCode   | Write-only |
| /Shared              | Read-only  |
| /Project             | Read-only  |

