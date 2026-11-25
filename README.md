# MeshUVMaskCutTool

A non-destructive VRChat avatar mesh cutting, material slot splitting, interactive UV viewer/picker, and UV mask generation tool for Unity 2022.3.

**中文 / English / 日本語** — Built-in trilingual editor UI (`日本語` / `English` / `中文`).

- **Menu Path**: `Tools -> Hitomi's PowerEye -> Mesh-UVMaskCutTool`
- **Package ID**: `io.github.hitomiruneko.mesh-uvmaskcuttool`
- **License**: [MIT License](LICENSE)

---

## One-Click Install (VCC / ALCOM)

- **VPM Listing Page**: https://hitomiruneko.github.io/MeshUVMaskCutTool/
- **Direct VCC Add Link**: `vcc://vpm/addRepo?url=https%3A%2F%2Fhitomiruneko.github.io%2FMeshUVMaskCutTool%2Findex.json`

### Requirements
- Unity `2022.3` (VRChat Creator Companion / ALCOM project)
- `nadena.dev.ndmf` (`>= 1.9.0 < 2.0.0`)
- `nadena.dev.modular-avatar` (`>= 1.14.0 < 2.0.0`)

---

## Features & Enhancements (vs. Upstream MeshUVCutTool)

### 1. Redesigned Compact Editor UI
- **Collapsible Mesh List**: Shows active mesh name and selected vertex/triangle counts in a permanent summary header; supports search filtering, adaptive 1–4 column layout, and a vertical resize handle without auto-collapsing on selection.
- **Standalone Selection Strip**: Mode tools (Select Island / Select Face / Deselect Island / Deselect Face), batch buttons (Select All / Deselect All / Invert), and inline options (`Front-facing only`, `Include overlapping`) stay visible across tabs.
- **Dual-Tab Workflow (`Mesh Edit` / `UV Edit`)**: Separates mesh operations and UV/mask operations cleanly, with an unsaved temporary mesh indicator dot (`●`) and an integrated **Save Temporary Mesh** card.

### 2. Mesh Deletion & Non-Destructive Splitting
- **Remove Selected Vertices (NDMF Non-Destructive)**: Registers `MUMCTMeshRemovalData` for non-destructive removal at build time (with optional Modular Avatar MeshCutter integration).
- **Remove Selected Vertices (New Temp Mesh)** *(New)*: Deletes triangles whose 3 vertices are all selected and outputs a `_Cut` temporary mesh preserving **all remaining material slots, bone weights, 8 UV channels, and BlendShapes**.
- **Separate Material Slots (Temporary Mesh)** *(New)*: Moves selected triangles into new material slots grouped by their original material (`_Slot`), keeping original slot order and original assets untouched.

### 3. Interactive UV Editor & UV→3D Reverse Picking
- **Mouse-Centered Zoom & Pan**: Scroll wheel zooms around cursor; middle/right drag (or left drag when picking is off) pans; `Fit View` resets framing.
- **Independent Layers**: Toggle Texture, deduplicated UV wireframe, and generated Mask layers independently; switch between `All UV` and `Selected Only`, and switch material slots on multi-material meshes.
- **Exact Triangle Highlight & UV→3D Picking**: Selected triangles highlight individually in UV space; enabling `Select from UV` supports single click, box drag, `Shift` add, and `Ctrl` remove, syncing directly back to 3D selection.
- **Synced Separate Window**: Open a larger standalone UV Editor window sharing the same canvas state.

### 4. Exact UV Mask Generator
- Rasterizes selected triangles directly in UV space (follows source texture resolution or fixed `512` / `1024` / `2048` / `4096`), supports inversion, previews on the UV canvas, and exports PNGs (`sRGB` off, `Uncompressed`, no mipmaps).

### 5. Safe Co-Existence with Original MeshUVCutTool
- Uses an independent package ID (`io.github.hitomiruneko.mesh-uvmaskcuttool`), namespace (`HitomiRuneko.MeshUVMaskCutTool`), assembly definitions, NDMF plugin ID, shader names, `EditorPrefs` key, and freshly regenerated `.meta` GUIDs so it can coexist in the same Unity project alongside upstream `jp.vrc-yue.meshuvcuttool`.

---

## 中文简介

面向 VRChat Avatar 的 Unity 编辑器改模工具，支持**非破坏性网格删除 / 新临时网格删面、按原材质分组分离材质槽、交互式 UV 面板与 UV→3D 反选、UV 空间精准蒙版生成**，内置简中 / 英文 / 日文三语界面。

- **打开路径**：`Tools → Hitomi's PowerEye → Mesh-UVMaskCutTool`
- **核心新增与改进**：
  1. **UI 重构**：可收起/搜索/自适应多列网格列表（顶部常驻当前网格与已选顶点/三角面统计）、独立共享「网格选择」工具条、「网格编辑 / UV 编辑」双分页、保存模块并入网格编辑页。
  2. **删除选中顶点（新临时网格）**：按完整选中三角面生成 `_Cut` 临时网格，保留剩余材质槽、权重、8 组 UV 与 BlendShape。
  3. **分离材质槽（临时网格）**：按原材质分组追加新槽并自动复制新槽材质，不改动原网格与原材质。
  4. **交互式 UV 面板与独立大窗口**：滚轮居中缩放、平移、贴图/UV/蒙版图层开关、多材质槽切换、逐三角面精确高亮、UV→3D 点选与框选。
  5. **UV 蒙版生成**：按三角面光栅化生成黑白蒙版（跟随贴图分辨率或 512–4096），自动以非 sRGB 无压缩 PNG 保存到源目录。

---

## 日本語概要

VRChat アバター向けの非破壊メッシュ分割・マテリアルスロット分離・インタラクティブ UV エディタ・UV マスク生成ツールです（日本語 / English / 中文 対応）。

- **メニュー**: `Tools -> Hitomi's PowerEye -> Mesh-UVMaskCutTool`
- **主な追加・改良点**:
  - 折りたたみ・検索・可変列数対応のメッシュ一覧、独立した選択ツールバー、「メッシュ編集 / UV編集」タブ構成、一時メッシュ保存の統合。
  - 選択三角面を削除した新規一時メッシュ作成（`_Cut`、マテリアルスロット・ウェイト・8ch UV・BlendShape を保持）。
  - 元マテリアル別にグループ化したマテリアルスロット分離（`_Slot`）。
  - ズーム・パン・レイヤー切替・UV からの 3D 逆選択・独立大ウィンドウ・UV マスク PNG 出力。

---

## Credits & Attribution

This project is a derivative work released under the **MIT License**. It builds directly upon:

1. **MeshUVCutTool (ちゅんちゅんメッシュカッター)** by **@vrcyue**
   - GitHub: https://github.com/vrcyue/MeshUVCutTool
   - Booth: https://vrc-yue.booth.pm/items/7259465
2. **AvatarModifyTools (にゃんにゃんメッシュエディター)** by **@HhotateA_xR**
   - GitHub: https://github.com/HhotateA/AvatarModifyTools

UX inspiration for UV canvas zooming, panning, and wireframe overlay rendering also came from **MaskTextureEditor** by **nekobako**.

---

## Disclaimer / 免責事項 / 免责声明

- Released under the [MIT License](LICENSE), free to use, modify, and redistribute with copyright and license notices preserved.
- Provided "as is", without warranty of any kind. Please back up your Unity project / avatar prefabs before editing meshes.
- The authors are not responsible for any issues caused by using this tool or by future Unity / VRChat SDK updates.
