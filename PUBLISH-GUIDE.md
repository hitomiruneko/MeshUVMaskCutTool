# MeshUVMaskCutTool GitHub + VCC 发布操作指南

本地仓库已经为你初始化并提交好 `v1.0.0`，只需要在 GitHub 网页和终端里做以下 **4 步**（首次约 3 分钟）即可上线开源并生成 VCC 一键安装链接。

---

## 第 1 步：在 GitHub 新建空仓库

1. 登录你的 GitHub 账号 **`hitomiruneko`**，点击右上角 `+` → **New repository**。
2. 填写信息：
   - **Repository name**：`MeshUVMaskCutTool`
   - **Visibility**：`Public`（公开仓库才能免费使用 GitHub Pages 作为 VCC 安装源）
   - **⚠️ 以下全部不要勾选**：不要勾 `Add a README file`、不要选 `.gitignore`、不要选 `License`（本地仓库都已经配置好了）。
3. 点击 **Create repository**。

---

## 第 2 步：首次推送代码与版本标签

在 PowerShell 中依次执行下面 3 条命令（如浏览器弹出 GitHub 登录授权窗口，点击 **Authorize** 即可）：

```powershell
cd "D:\vrc modeloar\unity project\_publish\MeshUVMaskCutTool"
git remote add origin https://github.com/hitomiruneko/MeshUVMaskCutTool.git
git push -u origin main
git push origin v1.0.0
```

> 推送 `v1.0.0` 标签会自动触发 `.github/workflows/release-vpm.yml` 工作流，自动创建 GitHub Release 并生成 VCC 源。

---

## 第 3 步：启用 GitHub Pages（开启 VCC 一键安装页）

1. 打开仓库页面 `https://github.com/hitomiruneko/MeshUVMaskCutTool`。
2. 进入 **Settings** → 左侧栏 **Pages**。
3. 在 **Build and deployment → Source** 下拉框选择 **GitHub Actions**。
4. 进入仓库顶部的 **Actions** 标签页；如果刚才推送 `v1.0.0` 时工作流因 Pages 未启用而失败，点进该次运行右上角 **Re-run all jobs** 即可。

---

## 第 4 步：验证上线

1. 等待 Actions 绿勾完成后，访问：
   - **GitHub Release**：`https://github.com/hitomiruneko/MeshUVMaskCutTool/releases/tag/v1.0.0`
   - **VCC 一键安装网页**：`https://hitomiruneko.github.io/MeshUVMaskCutTool/`
   - **VCC 订阅地址**：`https://hitomiruneko.github.io/MeshUVMaskCutTool/index.json`
2. 强烈建议新建一个**空白 VRChat Avatar 工程**，用 VCC 通过上述链接安装一次（并可再装一次原版 `MeshUVCutTool` 一起测试），确认 `Tools → Hitomi's PowerEye → Mesh-UVMaskCutTool` 菜单正常、0 报错即可对外分享。

---

## 以后如何发布新版本（例如 v1.0.1）

1. 修改 `Packages\io.github.hitomiruneko.mesh-uvmaskcuttool\package.json` 和 `EnvironmentVariable.cs` 中的版本号为 `1.0.1`，更新 `CHANGELOG.md`。
2. 提交并打新 Tag 推送：
   ```powershell
   git add .
   git commit -m "Release v1.0.1"
   git tag -a v1.0.1 -m "v1.0.1"
   git push origin main
   git push origin v1.0.1
   ```
   GitHub Actions 会自动更新 Release 与 VCC 安装源。
