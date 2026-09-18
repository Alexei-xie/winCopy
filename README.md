# winCopy

[![Build Windows installer](https://github.com/Alexei-xie/winCopy/actions/workflows/build.yml/badge.svg)](https://github.com/Alexei-xie/winCopy/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

适用于 Windows 10 / 11 的轻量原生剪贴板管理器。以 [Clipy](https://github.com/Clipy/Clipy) 的历史、片段与快捷键工作流为参考，独立 C# / Windows Forms 实现，不包含 Clipy 的 Swift 代码或图标。

![winCopy 圆角界面](docs/preview.png)

界面采用统一圆角组件、卡片历史列表与主次操作层级，详见 [设计说明](docs/DESIGN.md)。

## 下载与安装

前往 [Releases](https://github.com/Alexei-xie/winCopy/releases/latest)：

- `winCopy-版本-setup.exe`：当前用户安装，无需管理员权限，包含开始菜单入口与卸载程序。
- `winCopy-版本-portable.zip`：解压运行 `winCopy.exe`。
- `SHA256SUMS.txt`：安装包与便携包校验值。

使用 Windows 自带的 .NET Framework 4.x，不需要 Node.js 或浏览器运行时。安装包未进行代码签名，Windows 可能显示未知发布者提示。升级前请右键系统托盘图标退出旧版。卸载保留 `%LOCALAPPDATA%\winCopy` 中的数据。

## 功能

- 文本、HTML/RTF、图片、文件路径历史，支持去重、搜索、分类、收藏和预览。
- 无标题栏圆角弹窗；顶部可拖动，记住位置；按 Esc 或切换应用后收起。
- 常用文本片段、横向分组标签、拖拽排序，超出宽度时滚轮或箭头浏览。
- Clipy 风格片段 XML 导入/导出（folders/folder/title/snippets/snippet/title/content）。
- 托盘暂停记录，历史上限和保留天数，进程排除，快捷键设置及登录启动。
- Windows DPAPI 当前用户加密与原子保存。应用本身不联网，无遥测或云同步。

## 快捷键

| 操作 | 快捷键 |
| --- | --- |
| 呼出面板 | Ctrl + Alt + V（可设置） |
| 搜索 | Ctrl + F |
| 选择列表 | 搜索框内按 ↓ |
| 粘贴选中内容 | Enter / 双击 |
| 只复制 | Ctrl + Enter |
| 纯文本粘贴 | Shift + Enter |
| 收起 | Esc |

点击右上角更多菜单可打开设置、新建片段、暂停记录或退出。右键历史项可编辑、收藏及删除。关闭面板不会退出托盘程序。

## 数据与限制

- 数据文件：`%LOCALAPPDATA%\winCopy\history.dat`，仅当前 Windows 账户可解密。
- 默认 200 条历史、30 天；收藏与片段不自动过期。单张图片上限 8 MB / 2500 万像素；普通历史负载约 64 MB 后淘汰较老内容。
- 关闭历史持久化后，普通历史仅在本次会话保留；收藏和片段仍会保存。
- 文件历史保存路径，不备份原文件；不支持剪贴板中的全部自定义二进制格式。
- 默认排除 KeePass、KeePassXC、1Password、Bitwarden；依据读取时的前台进程识别。尊重部分剪贴板排除标记，不能保证识别全部密码内容。
- 自动粘贴受到 Windows 前台窗口与权限限制。失败时内容保留在剪贴板，可手动 Ctrl+V。
- XML 导出是明文，仅包含片段。取消设置不撤销已经执行的片段导入/导出。
- Windows 10/11 真机、多屏及真实高 DPI 环境仍需持续验收。自动化缩放测试不等同于真实 DPI 验证。

## 从源码构建

Windows PowerShell：

```powershell
.\build.ps1
Start-Process .\dist\winCopy.exe -ArgumentList '--self-test' -Wait
Get-Content .\dist\self-test-result.txt
```

构建使用 Windows 自带 C# 编译器，无需还原第三方 NuGet 包。也可用 Visual Studio 打开 `winCopy.csproj`，需安装 .NET Framework 4.8 开发工具。

构建安装包需要 [Inno Setup 6](https://jrsoftware.org/isinfo.php)：

```powershell
.\package.ps1
# 或指定编译器路径
.\package.ps1 -IsccPath 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
```

产物在 `artifacts/`。`tests/` 包含桌面交互与布局测试，需单独编译并引用构建后的 `winCopy.exe`。其中 `Integration.cs` 会临时操作剪贴板，测试后尝试恢复；不用于无交互 CI。`verify.ps1` 可生成使用示例数据的窗口截图。

## GitHub Actions 与发布

[构建流程](.github/workflows/build.yml) 在推送 `main`、提交 PR 或手动运行时，编译程序、运行存储自测、生成安装包/便携包/校验文件，并执行静默安装和卸载检查。通过后产物上传至 Actions Artifacts，保留 30 天。

发布版本：同步修改 `VERSION` 与 `AssemblyInfo.cs` 中的版本号，提交后推送对应标签：

```powershell
git tag v1.0.0
git push origin v1.0.0
```

标签必须与 `VERSION` 对应。成功构建后自动创建 GitHub Release 并上传三个发布文件；重跑会更新同版本附件。发布权限仅授予 release job。

## 开源许可

[MIT License](LICENSE)，Copyright (c) 2026 Alexei-xie。欢迎提交 Issue 和 Pull Request。
