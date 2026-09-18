# 验证记录

2026-09-18，在当前 Windows 环境完成。

## 已通过

- Windows 自带 .NET Framework C# 编译器构建，生成 `dist/winCopy.exe`。
- 去重且保留收藏状态、历史条数限制、过期清理、片段保留。
- Windows DPAPI 加密数据往返、原子替换、关闭普通历史持久化、损坏数据拒绝读取。
- 主窗口和设置窗口实例化及真实控件渲染，检查截图未出现设置控件裁切。
- WM_CLIPBOARDUPDATE 文本与图片捕获、暂停记录、历史还原至剪贴板、自身复制不重复记录。

## 未通过 / 尚未完成

- 自动粘贴集成测试未通过：测试目标窗口未取得前台焦点（foreground 与 target 句柄不同），编辑框未收到粘贴。程序保留剪贴板并走手动粘贴提示分支。不能据此认定真实热键呼出后的自动粘贴已经验证。
- 尚未分别在 Windows 10 / Windows 11 真机完成兼容性验收。
- 多显示器和不同 DPI、开机启动、真实 Clipy 导出文件互操作、跨权限应用、快捷键冲突仍需手工验收。

## 产物

- `winCopy-portable.zip`：仅包含程序和说明。
- `dist/self-test-result.txt`、`dist/integration-result.txt`：测试记录。
- `dist/preview.png`、`dist/settings-preview.png`：窗口截图。
- `build.ps1`：构建；`verify.ps1`：模型自测与 UI 渲染。
- `tests/Integration.cs`：桌面集成测试源码；需单独编译并引用 `dist/winCopy.exe`。

测试会短暂操作剪贴板并尝试恢复原内容，建议在没有进行复制粘贴操作时运行。集成测试禁用数据持久化，测试条目不写入真实历史。
