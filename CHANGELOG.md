# Changelog

All notable changes to **MeshUVMaskCutTool** are documented in this file.

## [1.0.0] - 2026-11-25

Initial public derivative release based on [MeshUVCutTool](https://github.com/vrcyue/MeshUVCutTool) (@vrcyue) and [AvatarModifyTools](https://github.com/HhotateA/AvatarModifyTools) (@HhotateA_xR).

### Added
- **Trilingual Localization (`日本語` / `English` / `中文`)**: Full UI translation including `MeshListPanel` and `MeshRemovalDataEditor`, persisted via `MeshUVMaskCutTool_Language`.
- **Remove Selected Vertices (New Temp Mesh)**: Creates a non-destructive `_Cut` temporary mesh deleting fully selected triangles while preserving remaining material slots, bone weights, 8 UV channels, and BlendShapes.
- **Separate Material Slots (Temporary Mesh)**: Groups selected triangles by original material into new slots (`_Slot`) with duplicated materials while preserving original slot indices.
- **Interactive UV Canvas & Separate UV Window**: Mouse-centered zoom, panning, Texture/UV/Mask layer toggles, per-slot material switching, exact per-triangle highlight, and UV→3D reverse picking (click, box drag, Shift add, Ctrl remove).
- **UV Mask Generator**: Rasterizes selected triangles in UV space (following source texture resolution or 512–4096), supports inversion, and exports uncompressed non-sRGB PNG masks.
- **Full Co-Existence Isolation**: Dedicated package ID (`io.github.hitomiruneko.mesh-uvmaskcuttool`), namespace (`HitomiRuneko.MeshUVMaskCutTool`), assembly definitions, NDMF plugin ID, shader names, and regenerated `.meta` GUIDs.

### Changed
- **Redesigned Editor Window Layout**:
  - Collapsible, searchable, adaptive multi-column Mesh List with permanent selected-mesh summary and vertex/triangle counters.
  - Standalone shared Mesh Selection toolbar visible across all tabs.
  - Dual-tab layout (`Mesh Edit` / `UV Edit`) with an unsaved-temp-mesh indicator dot and integrated Save card.
  - Menu path placed under `Tools/Hitomi's PowerEye/Mesh-UVMaskCutTool`.
