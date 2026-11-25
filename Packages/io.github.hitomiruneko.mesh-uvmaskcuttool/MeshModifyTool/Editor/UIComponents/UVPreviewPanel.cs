using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HitomiRuneko.MeshUVMaskCutTool.Core;

namespace HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool.UIComponents
{
    /// <summary>
    /// UV 編集パネル（メインウィンドウ内蔵用）。
    /// 実際の描画は UVCanvas が担当し、独立ウィンドウと状態を共有する。
    /// </summary>
    public class UVPreviewPanel
    {
        private readonly UVCanvas canvas = new UVCanvas();

        private MeshCreater meshCreater;
        private Renderer targetRenderer;
        private List<int> selectedVertices = new List<int>();

        private float previewHeight = 360f;
        private Vector2 hoverUv = new Vector2(float.NaN, float.NaN);

        /// <summary>共有キャンバス（独立ウィンドウからも参照する）</summary>
        public UVCanvas Canvas => canvas;

        /// <summary>UV 上で頂点が選択されたとき（3D 側へ反映する）</summary>
        public event Action<List<int>, UVPickOperation> OnVerticesPicked;

        public UVPreviewPanel()
        {
            canvas.OnVerticesPicked += (vertices, operation) =>
            {
                if (OnVerticesPicked != null)
                {
                    OnVerticesPicked(vertices, operation);
                }
            };

            canvas.OnHoverUV += uv => hoverUv = uv;
        }

        /// <summary>プレビュー対象を初期化する</summary>
        public void InitializePreview(Renderer renderer)
        {
            targetRenderer = renderer;
            canvas.TargetRenderer = renderer;
            canvas.MaterialSlot = 0;
            canvas.RequestFitView();
        }

        /// <summary>毎フレームの対象更新</summary>
        public void UpdateTarget(MeshCreater creater, Renderer renderer, List<int> selected)
        {
            meshCreater = creater;
            targetRenderer = renderer;
            selectedVertices = selected ?? new List<int>();

            if (canvas.MeshCreater != creater)
            {
                canvas.MeshCreater = creater;
                canvas.RequestFitView();
            }

            if (canvas.TargetRenderer != renderer)
            {
                canvas.TargetRenderer = renderer;
            }

            canvas.SetSelectedVertices(selectedVertices);
        }

        /// <summary>パネル全体を描画する</summary>
        public void DrawUVPreview()
        {
            EditorGUILayout.LabelField(Localization.Get("texture_preview"), EditorStyles.boldLabel);

            DrawCanvasControls();

            var rect = EditorGUILayout.GetControlRect(false, previewHeight, GUILayout.ExpandWidth(true));

            canvas.Draw(rect);

            DrawInfoBar();
        }

        /// <summary>情報行を描画（内蔵パネル・独立ウィンドウ共用、レイアウト上のコントロール数は常に1つ）</summary>
        public void DrawInfoBar()
        {
            var infoRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            const float hoverWidth = 170f;

            var hoverText = float.IsNaN(hoverUv.x)
                ? string.Empty
                : Localization.Get("uv_hover", hoverUv.x, hoverUv.y);

            var triangleCount = meshCreater != null && selectedVertices.Count > 0
                ? meshCreater.CountSelectedTriangles(selectedVertices)
                : 0;
            var triText = Localization.Get("uv_selected_triangles", triangleCount);

            GUI.Label(new Rect(infoRect.x, infoRect.y, hoverWidth, infoRect.height), hoverText, EditorStyles.miniLabel);
            GUI.Label(
                new Rect(infoRect.x + hoverWidth + 5f, infoRect.y, Mathf.Max(0f, infoRect.width - hoverWidth - 5f), infoRect.height),
                triText,
                EditorStyles.miniLabel);
        }

        /// <summary>キャンバスの操作 UI（内蔵パネルと独立ウィンドウで共用）</summary>
        public void DrawCanvasControls(bool includePreviewHeight = true)
        {
            // レイヤー切替
            using (new EditorGUILayout.HorizontalScope())
            {
                var newShowTexture = GUILayout.Toggle(canvas.ShowTexture, Localization.Get("uv_layer_texture"), "Button");
                var newShowUv = GUILayout.Toggle(canvas.ShowUvLines, Localization.Get("uv_layer_uv"), "Button");
                bool newShowMask;
                using (new EditorGUI.DisabledScope(!canvas.HasMask))
                {
                    newShowMask = GUILayout.Toggle(canvas.ShowMask && canvas.HasMask, Localization.Get("uv_layer_mask"), "Button");
                }

                if (newShowTexture != canvas.ShowTexture)
                {
                    canvas.ShowTexture = newShowTexture;
                }

                if (newShowUv != canvas.ShowUvLines)
                {
                    canvas.ShowUvLines = newShowUv;
                }

                if (canvas.HasMask && newShowMask != canvas.ShowMask)
                {
                    canvas.ShowMask = newShowMask;
                }
            }

            // 表示範囲
            using (new EditorGUILayout.HorizontalScope())
            {
                var texts = new[]
                {
                    Localization.Get("uv_show_all"),
                    Localization.Get("uv_show_selected")
                };
                var index = GUILayout.Toolbar(canvas.ShowSelectedOnly ? 1 : 0, texts);
                canvas.ShowSelectedOnly = index == 1;
            }

            // マテリアルスロット（常に同一のコントロール数で描画）
            var materials = targetRenderer != null ? targetRenderer.sharedMaterials : null;
            using (new EditorGUI.DisabledScope(materials == null || materials.Length <= 1))
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Localization.Get("uv_material_slot"), GUILayout.Width(90));

                string[] options;
                if (materials == null || materials.Length == 0)
                {
                    options = new[] { "-" };
                }
                else
                {
                    options = new string[materials.Length];
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var matName = materials[i] != null ? materials[i].name : "(None)";
                        options[i] = string.Format("{0}: {1}", i, matName);
                    }
                }

                var currentSlot = Mathf.Clamp(canvas.MaterialSlot, 0, options.Length - 1);
                var newSlot = EditorGUILayout.Popup(currentSlot, options);
                if (materials != null && materials.Length > 1 && newSlot != canvas.MaterialSlot)
                {
                    canvas.MaterialSlot = newSlot;
                }
            }

            // 表示倍率 / 不透明度
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newScale = EditorGUILayout.Slider(Localization.Get("uv_view_scale"), canvas.ViewScale, 0.05f, 16f);
                if (check.changed)
                {
                    canvas.SetViewScaleCentered(newScale);
                }
            }
            canvas.TextureOpacity = EditorGUILayout.Slider(Localization.Get("uv_view_opacity"), canvas.TextureOpacity, 0.05f, 1f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Localization.Get("uv_fit_view")))
                {
                    canvas.RequestFitView();
                }

                if (GUILayout.Button(Localization.Get("uv_open_window")))
                {
                    UVEditorWindow.Open(this, canvas);
                }
            }

            // プレビュー高さ（内蔵パネル向け）
            if (includePreviewHeight)
            {
                previewHeight = EditorGUILayout.Slider(
                    Localization.Get("uv_preview_height"), previewHeight, 160f, 720f);
            }

            // UV から 3D への逆方向選択
            canvas.EnablePicking = EditorGUILayout.ToggleLeft(
                Localization.Get("uv_pick_mode"), canvas.EnablePicking);

            var hintRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2f);
            GUI.Label(
                hintRect,
                canvas.EnablePicking ? Localization.Get("uv_reverse_hint") : string.Empty,
                EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>マスク画像を設定する（マスク生成機能から呼ぶ）</summary>
        public void SetMaskTexture(Texture2D texture)
        {
            var oldMask = canvas.MaskTexture;
            if (oldMask != null && oldMask != texture && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(oldMask)))
            {
                UnityEngine.Object.DestroyImmediate(oldMask);
            }

            canvas.MaskTexture = texture;
            if (texture != null)
            {
                canvas.ShowMask = true;
            }
        }

        public void Cleanup()
        {
            canvas.MeshCreater = null;
        }
    }
}
