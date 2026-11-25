using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool.Models;
using HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool;
using nadena.dev.modular_avatar.core;
using nadena.dev.modular_avatar.core.vertex_filters;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool.UIComponents
{
    /// <summary>
    /// メッシュリストパネル（折りたたみ可能・検索・レスポンシブ多列・リサイズ対応）
    /// </summary>
    public class MeshListPanel
    {
        private Vector2 rendsScroll = Vector2.zero;
        private List<MUMCTMeshRemovalData> selectedForMerge = new List<MUMCTMeshRemovalData>();

        // 折りたたみ・検索・高さ状態
        private bool isExpanded = true;
        private string searchQuery = string.Empty;
        private float listHeight = 190f;
        private bool isResizingHeight = false;

        // IMGUI Layout/Repaint 整合用キャッシュ（Layout イベント時のみ更新）
        private bool cachedExpanded = true;
        private string cachedSearchQuery = string.Empty;
        private int cachedSelectedIndex = -1;
        private int cachedColumns = 1;
        private bool cachedHasSelectedTempMesh = false;
        private bool cachedHasSelectedRemovalData = false;
        private bool cachedShowMergeButton = false;

        public event Action<int> OnMeshSelected;
        public event Action<int, bool> OnMeshVisibilityChanged;
        public event Action<int, MUMCTMeshRemovalData, int> OnRemovalDataVerticesButtonClicked;
        public event Action<MUMCTMeshRemovalData, bool> OnRemovalDataEnabledChanged;
        public event Action<List<MUMCTMeshRemovalData>> OnMergeRemovalDataRequested;
        public event Action<MUMCTMeshRemovalData> OnRemovalDataDeleteRequested;
        public event Action<MUMCTMeshRemovalData> OnSetupModularAvatarRequested;
        public event Action<MUMCTMeshRemovalData> OnRemoveModularAvatarRequested;
        public event Action<Renderer> OnTemporaryMeshDeleted;

        /// <summary>互換オーバーロード</summary>
        public void DrawMeshList(Renderer[] renderers, int selectedIndex, TemporaryAssetManager assetManager)
        {
            DrawMeshList(renderers, selectedIndex, assetManager, 0, 0, 400f);
        }

        /// <summary>メッシュリストを描画（現在メッシュサマリーバー＋折りたたみ式多列グリッド）</summary>
        public void DrawMeshList(Renderer[] renderers, int selectedIndex, TemporaryAssetManager assetManager,
            int selectedVertexCount, int selectedTriangleCount, float availableWidth)
        {
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            if (Event.current != null && Event.current.type == EventType.Layout)
            {
                cachedExpanded = isExpanded;
                cachedSearchQuery = searchQuery ?? string.Empty;
                cachedSelectedIndex = (selectedIndex >= 0 && selectedIndex < renderers.Length) ? selectedIndex : -1;
                cachedColumns = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(200f, availableWidth - 28f) / 185f), 1, 4);
                cachedShowMergeButton = selectedForMerge.Count >= 2;

                Renderer selRend = cachedSelectedIndex >= 0 ? renderers[cachedSelectedIndex] : null;
                cachedHasSelectedTempMesh = selRend != null && FindTempMesh(selRend, assetManager) != null;
                cachedHasSelectedRemovalData = selRend != null && CountRemovalItems(selRend) > 0;
            }

            // 常時表示サマリーバー（クリックで開閉＋現在メッシュ名・選択統計を表示）
            DrawSummaryHeader(renderers, selectedIndex, selectedVertexCount, selectedTriangleCount);

            if (!cachedExpanded)
            {
                return;
            }

            // 検索バー
            var matched = FilterRenderers(renderers, cachedSearchQuery);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4f);
                searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
                var searchRect = GUILayoutUtility.GetLastRect();
                if (Event.current != null && Event.current.type == EventType.Repaint && string.IsNullOrEmpty(searchQuery))
                {
                    var prevCol = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 0.45f);
                    GUI.Label(new Rect(searchRect.x + 16f, searchRect.y, Mathf.Max(0f, searchRect.width - 18f), searchRect.height),
                        Localization.Get("mesh_list_search"), EditorStyles.miniLabel);
                    GUI.color = prevCol;
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(searchQuery)))
                {
                    if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(22f), GUILayout.Height(18f)))
                    {
                        searchQuery = string.Empty;
                        GUI.FocusControl(null);
                    }
                }

                EditorGUILayout.LabelField(
                    string.Format("{0}/{1}", matched.Count, renderers.Length),
                    EditorStyles.centeredGreyMiniLabel,
                    GUILayout.Width(44f));
                GUILayout.Space(4f);
            }

            // 多列グリッド本体
            rendsScroll = EditorGUILayout.BeginScrollView(rendsScroll, false, false, GUIStyle.none,
                GUI.skin.verticalScrollbar, GUI.skin.scrollView, GUILayout.Height(listHeight));

            if (matched.Count == 0)
            {
                GUILayout.Space(12f);
                EditorGUILayout.LabelField(Localization.Get("mesh_list_no_match"), EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                int cols = Mathf.Max(1, cachedColumns);
                float tileWidth = Mathf.Max(140f, (availableWidth - 38f - (cols - 1) * 4f) / cols);

                for (int rowStart = 0; rowStart < matched.Count; rowStart += cols)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            int listIdx = rowStart + c;
                            if (listIdx < matched.Count)
                            {
                                var entry = matched[listIdx];
                                DrawMeshTile(entry.renderer, entry.index, selectedIndex, assetManager, tileWidth);
                            }
                            else
                            {
                                GUILayout.Space(tileWidth + 4f);
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            // 選択中メッシュの補足ストリップ（一時メッシュ削除・NDMF削除データ操作）
            if (cachedSelectedIndex >= 0 && cachedSelectedIndex < renderers.Length &&
                (cachedHasSelectedTempMesh || cachedHasSelectedRemovalData))
            {
                var selectedRenderer = renderers[cachedSelectedIndex];
                DrawSelectedMeshDetails(selectedRenderer, cachedSelectedIndex, assetManager);
            }

            // 下端リサイズハンドル
            DrawHeightSplitter();
        }

        private void DrawSummaryHeader(Renderer[] renderers, int selectedIndex, int selectedVertexCount, int selectedTriangleCount)
        {
            var headerRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(26f));
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                var bg = EditorGUIUtility.isProSkin
                    ? new Color(0.18f, 0.18f, 0.20f, 1f)
                    : new Color(0.78f, 0.78f, 0.80f, 1f);
                var border = EditorGUIUtility.isProSkin
                    ? new Color(0.10f, 0.10f, 0.11f, 1f)
                    : new Color(0.58f, 0.58f, 0.60f, 1f);
                EditorGUI.DrawRect(headerRect, bg);
                EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f), border);
            }

            GUILayout.Space(4f);
            string titleText = string.Format("{0} {1} ({2})",
                isExpanded ? "▾" : "▸",
                Localization.Get("mesh_list_title"),
                renderers.Length);

            if (GUILayout.Button(titleText, EditorStyles.boldLabel, GUILayout.Width(135f), GUILayout.Height(22f)))
            {
                isExpanded = !isExpanded;
            }

            Renderer activeRenderer = (selectedIndex >= 0 && selectedIndex < renderers.Length) ? renderers[selectedIndex] : null;
            if (activeRenderer != null)
            {
                var prevColor = GUI.contentColor;
                GUI.contentColor = EditorGUIUtility.isProSkin
                    ? new Color(0.55f, 0.80f, 1f, 1f)
                    : new Color(0.08f, 0.35f, 0.75f, 1f);
                EditorGUILayout.LabelField(
                    Localization.Get("mesh_list_current_prefix", activeRenderer.name),
                    EditorStyles.boldLabel,
                    GUILayout.MinWidth(90f));
                GUI.contentColor = prevColor;

                EditorGUILayout.LabelField(
                    Localization.Get("mesh_list_stats", selectedVertexCount, selectedTriangleCount),
                    EditorStyles.miniLabel,
                    GUILayout.Width(175f));
            }
            else
            {
                EditorGUILayout.LabelField(Localization.Get("mesh_list_none_selected"), EditorStyles.centeredGreyMiniLabel);
            }

            string toggleLabel = isExpanded ? Localization.Get("mesh_list_collapse") : Localization.Get("mesh_list_expand");
            if (GUILayout.Button(toggleLabel, EditorStyles.miniButton, GUILayout.Width(54f), GUILayout.Height(20f)))
            {
                isExpanded = !isExpanded;
            }
            GUILayout.Space(4f);
            EditorGUILayout.EndHorizontal();
        }

        private List<(int index, Renderer renderer)> FilterRenderers(Renderer[] renderers, string query)
        {
            var result = new List<(int, Renderer)>();
            bool hasQuery = !string.IsNullOrEmpty(query);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (hasQuery && r.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                result.Add((i, r));
            }

            return result;
        }

        private void DrawMeshTile(Renderer renderer, int index, int selectedIndex, TemporaryAssetManager assetManager, float tileWidth)
        {
            if (renderer == null) return;

            bool isSelected = selectedIndex == index;
            bool isTemp = FindTempMesh(renderer, assetManager) != null;
            int removalCount = CountRemovalItems(renderer);

            using (new EditorGUILayout.HorizontalScope(GUI.skin.box, GUILayout.Width(tileWidth), GUILayout.Height(26f)))
            {
                bool isActive = renderer.gameObject.activeSelf;
                bool newActive = EditorGUILayout.Toggle(isActive, GUILayout.Width(16f));
                if (newActive != isActive)
                {
                    OnMeshVisibilityChanged?.Invoke(index, newActive);
                }

                Color originalColor = GUI.backgroundColor;
                if (isSelected)
                {
                    GUI.backgroundColor = new Color(0.28f, 0.58f, 0.96f, 1f);
                }
                else if (isTemp)
                {
                    GUI.backgroundColor = new Color(1f, 0.85f, 0.52f, 1f);
                }

                string suffix = string.Empty;
                if (isTemp)
                {
                    suffix += " [" + Localization.Get("mesh_list_temp_badge") + "]";
                }
                if (removalCount > 0)
                {
                    suffix += " [" + Localization.Get("mesh_list_removal_badge", removalCount) + "]";
                }

                string buttonText = (isSelected ? "▶ " : string.Empty) + renderer.name + suffix;
                var content = new GUIContent(buttonText, renderer.name + suffix);
                if (GUILayout.Button(content, GUILayout.ExpandWidth(true), GUILayout.Height(20f)))
                {
                    OnMeshSelected?.Invoke(index);
                }

                GUI.backgroundColor = originalColor;
            }
        }

        private void DrawSelectedMeshDetails(Renderer renderer, int meshIndex, TemporaryAssetManager assetManager)
        {
            if (cachedHasSelectedTempMesh)
            {
                var tempMesh = renderer != null ? FindTempMesh(renderer, assetManager) : null;
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    var prev = GUI.contentColor;
                    GUI.contentColor = new Color(1f, 0.75f, 0.25f, 1f);
                    EditorGUILayout.LabelField(Localization.Get("unsaved"), EditorStyles.boldLabel);
                    GUI.contentColor = prev;

                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(tempMesh == null))
                    {
                        GUI.backgroundColor = new Color(0.95f, 0.35f, 0.35f, 1f);
                        if (GUILayout.Button(Localization.Get("btn_delete_temp_mesh"), GUILayout.Width(64f), GUILayout.Height(20f)))
                        {
                            if (tempMesh.RelatedGameObject != null)
                            {
                                UnityEngine.Object.DestroyImmediate(tempMesh.RelatedGameObject);
                            }

                            assetManager.RemoveTemporaryMesh(tempMesh);
                            OnTemporaryMeshDeleted?.Invoke(renderer);
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }

            if (cachedHasSelectedRemovalData)
            {
                var removalDataComponents = renderer != null
                    ? renderer.GetComponents<MUMCTMeshRemovalData>()
                    : Array.Empty<MUMCTMeshRemovalData>();

                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(
                    string.Format(Localization.Get("removal_data_section"), CountRemovalItems(renderer)),
                    EditorStyles.boldLabel);
                if (removalDataComponents != null && removalDataComponents.Length > 0 && renderer != null)
                {
                    DrawRemovalDataList(removalDataComponents, renderer, meshIndex);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawHeightSplitter()
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(7f));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);

            var e = Event.current;
            if (e != null)
            {
                if (e.type == EventType.Repaint)
                {
                    var lineBg = EditorGUIUtility.isProSkin
                        ? new Color(0.14f, 0.14f, 0.15f, 1f)
                        : new Color(0.72f, 0.72f, 0.74f, 1f);
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y + 2f, rect.width, 1f), lineBg);

                    var gripColor = EditorGUIUtility.isProSkin
                        ? new Color(0.50f, 0.50f, 0.55f, 0.9f)
                        : new Color(0.42f, 0.42f, 0.46f, 0.9f);
                    EditorGUI.DrawRect(new Rect(rect.center.x - 18f, rect.y + 3f, 36f, 2f), gripColor);
                }
                else if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
                {
                    isResizingHeight = true;
                    GUIUtility.hotControl = controlId;
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && isResizingHeight && GUIUtility.hotControl == controlId)
                {
                    listHeight = Mathf.Clamp(listHeight + e.delta.y, 90f, 440f);
                    e.Use();
                }
                else if ((e.type == EventType.MouseUp || e.type == EventType.Ignore) && isResizingHeight && GUIUtility.hotControl == controlId)
                {
                    isResizingHeight = false;
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
            }
        }

        private static TemporaryAssetManager.TemporaryAsset<Mesh> FindTempMesh(Renderer renderer, TemporaryAssetManager assetManager)
        {
            if (renderer == null || assetManager == null) return null;
            return assetManager.TempMeshes.FirstOrDefault(m =>
                m.RelatedGameObject == renderer.gameObject ||
                (renderer.transform.parent != null && m.RelatedGameObject == renderer.transform.parent.gameObject));
        }

        private static int CountRemovalItems(Renderer renderer)
        {
            if (renderer == null) return 0;
            var components = renderer.GetComponents<MUMCTMeshRemovalData>();
            if (components == null || components.Length == 0) return 0;

            int count = 0;
            for (int i = 0; i < components.Length; i++)
            {
                var data = components[i];
                if (data == null || data.removalInfos == null) continue;
                for (int j = 0; j < data.removalInfos.Count; j++)
                {
                    var info = data.removalInfos[j];
                    if (info != null && info.targetRenderer == renderer && info.verticesToRemove.Count > 0)
                    {
                        count++;
                    }
                }
            }

            return count;
        }
        
        private void DrawRemovalDataList(MUMCTMeshRemovalData[] removalDataComponents, Renderer renderer, int meshIndex)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var removalData in removalDataComponents)
                {
                    if (removalData != null && removalData.removalInfos.Count > 0)
                    {
                        for (int infoIndex = 0; infoIndex < removalData.removalInfos.Count; infoIndex++)
                        {
                            var info = removalData.removalInfos[infoIndex];
                            if (info != null && info.targetRenderer == renderer && info.verticesToRemove.Count > 0)
                            {
                                // ModularAvatarモード切り替え
                                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                                {
                                    EditorGUILayout.LabelField(L("削除方法:", "Removal Method:", "删除方式："), GUILayout.Width(80));
                                    
                                    if (!removalData.UseModularAvatar)
                                    {
                                        EditorGUILayout.LabelField(L("通常削除モード", "Normal Delete Mode", "通常删除模式"), EditorStyles.miniLabel);
                                        if (GUILayout.Button(L("MA連携を有効化", "Enable MA Integration", "启用 MA 联动"), GUILayout.Width(120)))
                                        {
                                            OnSetupModularAvatarRequested?.Invoke(removalData);
                                        }
                                    }
                                    else
                                    {
                                        EditorGUILayout.LabelField(L("MA連携モード", "MA Integration Mode", "MA 联动模式"), EditorStyles.miniLabel);
                                        if (GUILayout.Button(L("通常削除に戻す", "Return to Normal", "恢复为通常删除"), GUILayout.Width(120)))
                                        {
                                            OnRemoveModularAvatarRequested?.Invoke(removalData);
                                        }
                                    }
                                }
                                
                                EditorGUILayout.Space(2);
                                
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    // [ON/OFF]ボタン
                                    bool isEnabled = removalData.enabled;
                                    string enabledText = isEnabled ? Localization.Get("btn_enable") : Localization.Get("btn_disable");
                                    GUI.backgroundColor = isEnabled ? new Color(0.5f, 1f, 0.5f) : Color.gray;
                                    if (GUILayout.Button(new GUIContent(enabledText, Localization.Get("toggle_enable_disable")), GUILayout.Width(50)))
                                    {
                                        OnRemovalDataEnabledChanged?.Invoke(removalData, !isEnabled);
                                    }
                                    GUI.backgroundColor = Color.white;
                                    
                                    // >> 頂点を選択ボタン
                                    string selectButtonText = string.Format(Localization.Get("btn_select_vertices"), info.verticesToRemove.Count);
                                    if (GUILayout.Button(selectButtonText, GUILayout.Width(180)))
                                    {
                                        OnRemovalDataVerticesButtonClicked?.Invoke(meshIndex, removalData, infoIndex);
                                    }
                                    
                                    GUILayout.FlexibleSpace();
                                    
                                    // 統合対象ボタン
                                    bool isSelectedForMerge = selectedForMerge.Contains(removalData);
                                    string mergeButtonText = isSelectedForMerge ? Localization.Get("btn_remove_from_merge") : Localization.Get("btn_add_to_merge");
                                    GUI.backgroundColor = isSelectedForMerge ? Color.yellow : Color.white;
                                    if (GUILayout.Button(mergeButtonText, GUILayout.Width(120)))
                                    {
                                        if (isSelectedForMerge)
                                        {
                                            selectedForMerge.Remove(removalData);
                                        }
                                        else
                                        {
                                            selectedForMerge.Add(removalData);
                                        }
                                    }
                                    GUI.backgroundColor = Color.white;
                                    
                                    // 削除ボタン
                                    GUI.backgroundColor = Color.red;
                                    if (GUILayout.Button(Localization.Get("btn_delete"), GUILayout.Width(50)))
                                    {
                                        if (EditorUtility.DisplayDialog(
                                            Localization.Get("confirm_delete_title"),
                                            Localization.Get("confirm_delete_message"),
                                            Localization.Get("btn_delete"),
                                            Localization.Get("btn_cancel")))
                                        {
                                            OnRemovalDataDeleteRequested?.Invoke(removalData);
                                            // 統合対象リストからも削除
                                            selectedForMerge.Remove(removalData);
                                        }
                                    }
                                    GUI.backgroundColor = Color.white;
                                }
                            }
                        }
                    }
                }
                
                // ローカライズメソッド（Chinese 未指定時は英語へフォールバック）
                string L(string ja, string en, string zh = null)
                {
                    return Localization.L(ja, en, zh);
                }
                
                // 統合ボタン（フォールドアウト内の最下部）
                if (cachedShowMergeButton)
                {
                    EditorGUILayout.Space(5);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        string mergeButtonText = string.Format(Localization.Get("btn_merge_selected"), selectedForMerge.Count);
                        if (GUILayout.Button(mergeButtonText, GUILayout.Width(150)))
                        {
                            OnMergeRemovalDataRequested?.Invoke(new List<MUMCTMeshRemovalData>(selectedForMerge));
                            selectedForMerge.Clear();
                        }
                    }
                }
            }
        }
    }
}