/*
MeshUVMaskCutTool
https://github.com/hitomiruneko/MeshUVMaskCutTool

Copyright (c) 2026 @hitomiruneko
Released under the MIT License.

Modified from MeshUVCutTool:
https://github.com/vrcyue/MeshUVCutTool
Copyright (c) 2025 @vrcyue
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php

以下のコードをベースに実装

AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using HitomiRuneko.MeshUVMaskCutTool.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool.Models;
using HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool.Core;
using HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool.UIComponents;
using HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool.Utilities;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool
{
    public class MeshModifyTool : EditorWindow
    {
        [MenuItem("Tools/Hitomi's PowerEye/Mesh-UVMaskCutTool")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<MeshModifyTool>();
            wnd.titleContent = new GUIContent(Localization.Get("window_title"));
            wnd.minSize = new Vector2(800, 400);
        }

        // コア機能
        private MeshEditingCore editingCore;
        private SceneViewIntegration sceneViewIntegration;
        private AssetSaveManager saveManager;
        
        // UIコンポーネント
        private MeshListPanel meshListPanel;
        private UVPreviewPanel uvPreviewPanel;
        private ToolSettingsPanel toolSettingsPanel;
        private MeshCreationOptionsPanel meshCreationOptionsPanel;
        
        // 一時アセット管理
        private TemporaryAssetManager assetManager = new TemporaryAssetManager();
        
        // UI状態
        private int activeTab = 0; // 0 = 网格编辑, 1 = UV 编辑
        private bool extendMeshOption = false;
        private bool extendUvTools = false;
        private bool extendAdjustUVEdit = false;

        // IMGUI Layout/Repaint 整合用キャッシュ（Layout 時のみ更新）
        private int cachedActiveTab = 0;
        private bool cachedHasUnsavedMesh = false;
        private bool cachedExtendMeshOption = false;
        private bool cachedExtendMaskSection = true;
        private bool cachedExtendUvTools = false;
        private bool cachedExtendAdjustUVEdit = false;
        
        // カメラビュー表示フラグ
        private bool showCameraView = false;
        
        // アバター情報
        private GameObject avatar;
        private AvatarMonitor avatarMonitor;
        
        // ボタン設定
        private int drawButton = 0;
        private int rotateButton = 1;
        private int moveButton = 2;
        
        // ペンツール設定
        private MeshPenTool.ExtraTool penMode = MeshPenTool.ExtraTool.SelectVertex;
        private float brushPower = 0.001f;
        private float brushWidth = 0.03f;
        private float brushStrength = 1f;
        
        // サフィックス
        private string assetSuffix = "_New";

        // マテリアルスロット分離時に元メッシュを自動で隠すか
        private bool hideOriginalMeshAfterSlotSplit = true;

        // UV マスク生成設定
        private bool extendMaskSection = true;
        private bool maskFollowTexture = true;
        private int maskFixedSize = 1024;
        private bool maskInvert = false;
        private bool maskAutoSave = true;
        private string lastMaskPath = string.Empty;
        private bool cachedShowSavedWarning = false;
        
        // 選択設定
        public enum MeshCreateOption
        {
            Normal,                 // 通常（選択部分のみ）
            CreateInverse,          // 選択範囲外も作成
            RemoveBoneWeights,      // ボーンウェイトを削除
            CopyUsedBones,          // 選択部分のみ作成し、メッシュが依存するボーンをコピー
            CopyUsedBonesWithoutParent,  // 選択部分のみ作成し、使用ボーンのみコピー（親を除く）
            CopyUsedBonesWithoutParentAdjusted,  // 選択部分のみ作成し、使用ボーンのみコピー（親を除く・ルート位置調整）
            CopyUsedBonesWithoutParentTopY,      // 選択部分のみ作成し、使用ボーンのみコピー（親を除く・最高Y座標）
            CopyUsedBonesWithoutParentBottomY    // 選択部分のみ作成し、使用ボーンのみコピー（親を除く・最低Y座標）
        }
        private MeshCreateOption meshCreateOption = MeshCreateOption.Normal;
        private float sidebarWidth = 400f;
        private bool isResizing = false;
        private const float MIN_SIDEBAR_WIDTH = 200f;
        private const float MAX_SIDEBAR_WIDTH = 600f;

        // ショートカット取得
        private bool keyboardShortcut = false;
        private bool keyboardShift = false;
        private bool keyboardCtr = false;
        private bool keyboardAlt = false;

        // 編集ツールプリセット
        private MeshPenTool[] _penTools;

        // 拡張編集ツールプリセット
        private MeshPenTool[] _extraTools;
        
        // エラー管理
        private System.Exception lastError = null;
        
        // スクロール位置
        private Vector2 meshEditScrollPosition = Vector2.zero;

        MeshPenTool[] extraTools
        {
            get
            {
                if (_extraTools == null)
                {
                    _extraTools = new MeshPenTool[4]
                    {
                        new MeshPenTool(EnvironmentGUIDs.selectLandToolIcon,"SelectLand",MeshPenTool.ExtraTool.SelectLand,null,null,0f),
                        new MeshPenTool(EnvironmentGUIDs.selectVertexToolIcon, "SelectVertex",
                            MeshPenTool.ExtraTool.SelectVertex, null, null, 0f),
                        new MeshPenTool(EnvironmentGUIDs.unSelectLandToolIcon,"UnSelectLand",MeshPenTool.ExtraTool.UnSelectLand,null,null,0f),
                        new MeshPenTool(EnvironmentGUIDs.unSelectVertexToolIcon, "UnSelectVertex",
                            MeshPenTool.ExtraTool.UnSelectVertex, null, null, 0f),
                    };
                }

                return _extraTools;
            }
        }

        
        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            if (editingCore == null)
            {
                editingCore = new MeshEditingCore(assetManager);
                saveManager = new AssetSaveManager(assetManager);
                sceneViewIntegration = new SceneViewIntegration(editingCore);
                meshListPanel = new MeshListPanel();
                uvPreviewPanel = new UVPreviewPanel();
                toolSettingsPanel = new ToolSettingsPanel();
                meshCreationOptionsPanel = new MeshCreationOptionsPanel();
                
                // イベント登録
                RegisterEvents();
            }
        }
        
        /// <summary>
        /// イベント登録
        /// </summary>
        private void RegisterEvents()
        {
            // メッシュリストパネル
            meshListPanel.OnMeshSelected += (index) =>
            {
                // 現在選択中のメッシュと同じ場合は選択解除（トグル）
                if (editingCore.GetEditIndex() == index)
                {
                    editingCore.SelectMesh(-1);
                    
                    // SceneView統合を無効化
                    if (sceneViewIntegration != null)
                    {
                        sceneViewIntegration.Disable();
                    }
                }
                else
                {
                    editingCore.GetRenderers()[index].gameObject.SetActive(true);
                    editingCore.SelectMesh(index);
                    
                    // UVプレビューパネルを初期化
                    var renderer = editingCore.GetRenderers()[index];
                    if (renderer != null)
                    {
                        uvPreviewPanel.InitializePreview(renderer);
                    }
                    
                    // SceneView統合を有効化
                    if (sceneViewIntegration != null && avatarMonitor != null)
                    {
                        sceneViewIntegration.Enable(avatarMonitor);
                    }
                }
            };
            
            meshListPanel.OnMeshVisibilityChanged += (index, isActive) =>
            {
                editingCore.GetRenderers()[index].gameObject.SetActive(isActive);
            };
            
            meshListPanel.OnTemporaryMeshDeleted += (deletedRenderer) =>
            {
                // 削除されたレンダラーのインデックスを探す
                var renderers = editingCore.GetRenderers();
                int indexToRemove = -1;
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == deletedRenderer)
                    {
                        indexToRemove = i;
                        break;
                    }
                }
                
                // レンダラー配列から削除
                if (indexToRemove != -1)
                {
                    editingCore.RemoveRenderer(indexToRemove);
                }
                
                // メッシュリストを更新
                Repaint();
            };
            
            meshListPanel.OnRemovalDataVerticesButtonClicked += (index, removalData, infoIndex) =>
            {
                // まず対象のメッシュを選択
                editingCore.SelectMesh(index);
                
                // RemovalDataから頂点リストを取得して選択
                if (removalData != null && infoIndex < removalData.removalInfos.Count)
                {
                    var info = removalData.removalInfos[infoIndex];
                    if (info != null && info.verticesToRemove != null && info.verticesToRemove.Count > 0)
                    {
                        var selectionHandler = editingCore.GetSelectionHandler();
                        selectionHandler.SetSelectedVertices(info.verticesToRemove);
                    }
                }
            };
            
            meshListPanel.OnRemovalDataEnabledChanged += (removalData, enabled) =>
            {
                if (removalData != null)
                {
                    removalData.enabled = enabled;
                    EditorUtility.SetDirty(removalData);
                    
                    // NDMFプレビューを更新
                    MeshRemovalUtility.RefreshNDMFPreview();
                }
            };
            
            meshListPanel.OnMergeRemovalDataRequested += (removalDataList) =>
            {
                MeshRemovalUtility.MergeRemovalData(removalDataList);
                Repaint(); // UIを更新
            };
            
            meshListPanel.OnRemovalDataDeleteRequested += (removalData) =>
            {
                if (removalData != null)
                {
                    DestroyImmediate(removalData);
                    Repaint();
                    
                    // NDMFプレビューを更新
                    MeshRemovalUtility.RefreshNDMFPreview();
                }
            };
            
            // ModularAvatar関連イベント
            meshListPanel.OnSetupModularAvatarRequested += (removalData) =>
            {
                if (removalData != null)
                {
                    var editor = Editor.CreateEditor(removalData) as MeshRemovalDataEditor;
                    if (editor != null)
                    {
                        editor.SetupModularAvatarComponentsPublic(removalData);
                        DestroyImmediate(editor);
                    }
                }
            };
            
            meshListPanel.OnRemoveModularAvatarRequested += (removalData) =>
            {
                if (removalData != null)
                {
                    var editor = Editor.CreateEditor(removalData) as MeshRemovalDataEditor;
                    if (editor != null)
                    {
                        editor.RemoveModularAvatarComponentsPublic(removalData);
                        DestroyImmediate(editor);
                    }
                }
            };
            
            // 選択ハンドラ
            var selectionHandler = editingCore.GetSelectionHandler();
            selectionHandler.OnSelectionChanged += (vertices) =>
            {
                editingCore.UpdateVertexSelection(vertices);
            };
            
            // UV パネルからの逆方向選択（UV → 3D 頂点）
            uvPreviewPanel.OnVerticesPicked += (vertices, operation) =>
            {
                if (editingCore == null) return;
                var handler = editingCore.GetSelectionHandler();
                if (handler == null) return;

                var set = new HashSet<int>(handler.GetSelectedVertices());

                if (operation == UVPickOperation.Replace)
                {
                    set.Clear();
                }

                if (operation == UVPickOperation.Remove)
                {
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        set.Remove(vertices[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        set.Add(vertices[i]);
                    }
                }

                var newList = new List<int>(set);
                handler.SetSelectedVertices(newList);
                uvPreviewPanel.Canvas.SetSelectedVertices(newList);
                SceneView.RepaintAll();
                Repaint();
            };

            // 設定パネル
            toolSettingsPanel.OnWireFrameColorChanged += (color) =>
            {
                var previewController = editingCore.GetPreviewController();
                previewController.WireFrameColor = color;
            };
            
            toolSettingsPanel.OnNormalAlphaChanged += (alpha) =>
            {
                var previewController = editingCore.GetPreviewController();
                previewController.NormalAlpha = alpha;
                if (editingCore.GetEditIndex() != -1)
                {
                    previewController.ApplyRenderMode(editingCore.GetRenderers()[editingCore.GetEditIndex()]);
                }
            };
            
            toolSettingsPanel.OnUVAlphaChanged += (alpha) =>
            {
                var previewController = editingCore.GetPreviewController();
                previewController.UVAlpha = alpha;
                if (editingCore.GetEditIndex() != -1)
                {
                    previewController.ApplyRenderMode(editingCore.GetRenderers()[editingCore.GetEditIndex()]);
                }
            };
            
            toolSettingsPanel.OnCameraViewChanged += (show) =>
            {
                showCameraView = show;
                if (avatarMonitor != null)
                {
                    avatarMonitor.SetSceneViewMode(!showCameraView);
                }
            };
            
            // メッシュ作成オプションパネル
            meshCreationOptionsPanel.OnOptionChanged += (baseMode, weightBoneMode, advancedBoneMode, rootPositionMode) =>
            {
                // 新しい選択を従来のenumに変換
                meshCreateOption = MeshCreationOptionsPanel.ConvertToLegacyOption(baseMode, weightBoneMode, advancedBoneMode, rootPositionMode);
            };
        }

        /// <summary>
        /// ウィンドウタイトルを現在の言語設定に同期する
        /// </summary>
        private void SyncWindowTitle()
        {
            var title = Localization.Get("window_title");
            if (titleContent == null || titleContent.text != title)
            {
                titleContent = new GUIContent(title);
            }
        }




        /// <summary>
        /// 表示部，実装は置かないこと
        /// </summary>
        private void OnGUI()
        {
            try
            {
                OnGUIInternal();
            }
            catch (ExitGUIException)
            {
                // ExitGUIExceptionは正常な動作なので再スローする
                throw;
            }
            catch (System.Exception e)
            {
                lastError = e;
                Debug.LogError("[MeshModifyTool] OnGUI Error: " + e.ToString());
                throw;
            }
        }
        
        /// <summary>
        /// OnGUIの実際の処理
        /// </summary>
        private void OnGUIInternal()
        {
            Initialize();
            SyncWindowTitle();
            wantsMouseMove = (activeTab == 1);
            
            // エラーがある場合は最初に表示
            if (lastError != null)
            {
                EditorGUILayout.HelpBox(Localization.Get("msg_error_occurred") + lastError.Message, MessageType.Error);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Localization.Get("btn_restart_window")))
                    {
                        Close();
                        EditorApplication.delayCall += () =>
                        {
                            ShowWindow();
                        };
                    }
                    
                    if (GUILayout.Button(Localization.Get("btn_close_window")))
                    {
                        Close();
                    }
                }
                return;
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (editingCore.GetRenderers() == null || editingCore.GetRenderers().Length == 0)
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        // 言語切り替えボタンを右上に配置
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.LabelField(Localization.Get("language_label"), GUILayout.Width(180));
                            Localization.DrawLanguageSelector(210f);
                        }
                        
                        WindowBase.TitleStyle(Localization.Get("window_title"));
                        WindowBase.DetailStyle(Localization.Get("window_description"));
                            
                            //, EnvironmentGUIDs.readme);
                        avatar = EditorGUILayout.ObjectField("", avatar, typeof(GameObject), true) as GameObject;
                        if (GUILayout.Button(Localization.Get("btn_setup")))
                        {
                            Setup(avatar);
                        }

                        // VRCAvatarDescriptorを持つアクティブなオブジェクトを検索して表示
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField(Localization.Get("active_avatars_description"));
                        
#if VRC_SDK_VRCSDK3
                        var avatarDescriptors = FindObjectsOfType<VRCAvatarDescriptor>();
                        var activeAvatars = new List<VRCAvatarDescriptor>();
                        
                        foreach (var desc in avatarDescriptors)
                        {
                            if (desc.gameObject.activeInHierarchy)
                            {
                                activeAvatars.Add(desc);
                            }
                        }
                        
                        if (activeAvatars.Count > 0)
                        {
                            using (var scrollView = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.MaxHeight(200)))
                            {
                                foreach (var avatarDesc in activeAvatars)
                                {
                                    using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                                    {
                                        EditorGUILayout.LabelField(avatarDesc.gameObject.name, GUILayout.ExpandWidth(true));
                                        if (GUILayout.Button(Localization.Get("btn_select"), GUILayout.Width(60)))
                                        {
                                            avatar = avatarDesc.gameObject;
                                            Setup(avatar);
                                        }
                                    }
                                }
                            }
                        }
#endif

                        WindowBase.Signature();
                        return;
                    }
                }

                // ウィンドウ左側
                using (showCameraView 
                    ? new EditorGUILayout.VerticalScope(GUILayout.Width(sidebarWidth))
                    : new EditorGUILayout.VerticalScope())
                {
                    // 戻るボタン・アバター名・言語切替をコンパクトなヘッダーに配置
                    bool backClicked = false;
                    var headerRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(30f));
                    if (Event.current != null && Event.current.type == EventType.Repaint)
                    {
                        EditorGUI.DrawRect(headerRect, ToolUI.HeaderBg);
                        EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.yMax - 1f, headerRect.width, 1f), ToolUI.Divider);
                    }
                    GUILayout.Space(6f);
                    if (GUILayout.Button("← " + Localization.Get("btn_back"), GUILayout.Width(68f), GUILayout.Height(22f)))
                    {
                        backClicked = true;
                    }
                    GUILayout.Space(8f);
                    EditorGUILayout.LabelField(
                        Localization.Get("avatar_label", avatar != null ? avatar.name : "-"),
                        EditorStyles.boldLabel,
                        GUILayout.MinWidth(120f),
                        GUILayout.Height(22f));
                    GUILayout.FlexibleSpace();
                    Localization.DrawLanguageSelector(110f, GUILayout.Height(20f));
                    GUILayout.Space(6f);
                    EditorGUILayout.EndHorizontal();

                    if (backClicked)
                    {
                        // 初期状態に戻す
                        avatar = null;
                        editingCore.Cleanup();

                        // 一時アセットのクリーンアップ
                        assetManager.CleanupTemporaryAssets();

                        if (avatarMonitor != null)
                        {
                            avatarMonitor.Release();
                            avatarMonitor = null;
                        }
                        sceneViewIntegration.Disable();
                        return; // 戻るボタンが押されたらOnGUIの残りの処理をスキップ
                    }

                    // カメラビュー表示時のみリサイズハンドルを表示
                    if (showCameraView)
                    {
                        // リサイズハンドルの処理
                        var resizeRect = new Rect(sidebarWidth - 5, 0, 10, position.height);
                        EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);

                        if (Event.current.type == EventType.MouseDown && resizeRect.Contains(Event.current.mousePosition))
                        {
                            isResizing = true;
                        }
                        else if (Event.current.type == EventType.MouseUp)
                        {
                            isResizing = false;
                        }
                        else if (Event.current.type == EventType.MouseDrag && isResizing)
                        {
                            sidebarWidth = Mathf.Clamp(Event.current.mousePosition.x, MIN_SIDEBAR_WIDTH, MAX_SIDEBAR_WIDTH);
                            Repaint();
                        }
                    }


                    // 選択中オブジェクトが非アクティブになったら，選択解除
                    var currentEditIndex = editingCore.GetEditIndex();
                    if (currentEditIndex != -1)
                    {
                        var renderers = editingCore.GetRenderers();
                        if (currentEditIndex < renderers.Length && renderers[currentEditIndex] != null)
                        {
                            if (!renderers[currentEditIndex].gameObject.activeSelf)
                            {
                                editingCore.SelectMesh(-1);
                                
                                // SceneView統合を無効化
                                if (sceneViewIntegration != null)
                                {
                                    sceneViewIntegration.Disable();
                                }
                            }
                        }
                        else
                        {
                            // レンダラーが削除されている場合も選択解除
                            editingCore.SelectMesh(-1);
                            
                            // SceneView統合を無効化
                            if (sceneViewIntegration != null)
                            {
                                sceneViewIntegration.Disable();
                            }
                        }
                    }

                    // メッシュリストパネル（折りたたみ可能・現在メッシュ統計表示）
                    var headerSelectionHandler = editingCore.GetSelectionHandler();
                    var headerCreater = editingCore.EditMeshCreater;
                    var headerSelectedVerts = headerSelectionHandler != null ? headerSelectionHandler.GetSelectedVertices() : new List<int>();
                    int headerSelectedVertCount = headerSelectedVerts.Count;
                    int headerSelectedTriCount = headerCreater != null && headerSelectedVertCount > 0
                        ? headerCreater.CountSelectedTriangles(headerSelectedVerts)
                        : 0;
                    float listAvailableWidth = showCameraView ? sidebarWidth : position.width;

                    meshListPanel.DrawMeshList(
                        editingCore.GetRenderers(),
                        editingCore.GetEditIndex(),
                        assetManager,
                        headerSelectedVertCount,
                        headerSelectedTriCount,
                        listAvailableWidth);

                    // 独立した共有「メッシュ選択」モジュール（タブ切替の影響を受けない）
                    DrawSharedSelectionModule();

                    // タブ切替状態と折りたたみ状態を Layout 段階で確定させる
                    bool liveHasUnsavedMesh = assetManager.TempMeshes.Any(m => m.Asset != null && !AssetDatabase.Contains(m.Asset));
                    if (Event.current != null && Event.current.type == EventType.Layout)
                    {
                        cachedHasUnsavedMesh = liveHasUnsavedMesh;
                        cachedActiveTab = activeTab;
                        cachedExtendMeshOption = extendMeshOption;
                        cachedExtendMaskSection = extendMaskSection;
                        cachedExtendUvTools = extendUvTools;
                        cachedExtendAdjustUVEdit = extendAdjustUVEdit;
                    }

                    activeTab = ToolUI.DrawTabBar(
                        activeTab,
                        new[] { Localization.Get("tab_mesh_edit"), Localization.Get("tab_uv_edit") },
                        new[] { cachedHasUnsavedMesh, false });

                    // スクロールビューの開始（選択中のタブ内容を表示）
                    meshEditScrollPosition = EditorGUILayout.BeginScrollView(
                        meshEditScrollPosition,
                        GUILayout.ExpandHeight(true) // 利用可能な高さ全体を使用
                    );

                    if (cachedActiveTab == 0)
                    {
                        DrawMeshEditTab();
                    }
                    else
                    {
                        DrawUVEditTab();
                    }

                    EditorGUILayout.EndScrollView();

                }

                using (new EditorGUILayout.VerticalScope())
                {
                    if (avatarMonitor != null && showCameraView)
                    {
                        var avatarMonitorWidth = 300;
                        int positionDrag = keyboardShortcut && keyboardAlt ? drawButton : moveButton;
                        bool canNotTouch = keyboardShortcut && (keyboardShift || keyboardCtr);
                        bool canNotWheel = keyboardShortcut && (keyboardShift || keyboardAlt);
                        if (keyboardShortcut && keyboardAlt)
                        {
                            avatarMonitor.SetSpeed(0.1f, 0.5f, 0.3f);
                        }
                        else
                        {
                            avatarMonitor.SetSpeed();
                        }

                        avatarMonitor.Display((int)position.width - avatarMonitorWidth, (int)position.height - 10,
                            rotateButton, positionDrag, !canNotTouch, !canNotWheel);
                        if (editingCore.GetEditIndex() != -1)
                        {
                            HandleAvatarMonitorTouch();
                        }
                    }
                }
            }

            HandleVertexSelection();
            UpdateUVPreview();
        }

        /// <summary>
        /// 毎フレーム更新する
        /// </summary>
        private void Update()
        {
            Repaint();
        }
        
        private void OnEnable()
        {
            Initialize();
            if (sceneViewIntegration != null && avatarMonitor != null)
            {
                sceneViewIntegration.Enable(avatarMonitor);
            }
        }
        
        private void OnDisable()
        {
            if (sceneViewIntegration != null)
            {
                sceneViewIntegration.Disable();
            }
        }
        /// <summary>
        /// ハンドル頂点選択
        /// </summary>
        private void HandleVertexSelection()
        {
            if (editingCore == null || editingCore.GetEditIndex() == -1) return;
            
            var selectionHandler = editingCore.GetSelectionHandler();
            if (!selectionHandler.IsSelectionMode) return;
            
            var e = Event.current;
            var previewController = editingCore.GetPreviewController();
            var meshCollider = previewController.GetEditMeshCollider();
            
            if (meshCollider == null) return;
            
            // マウスドラッグ開始（メッシュチェックなしで無条件に開始）
            if (e.type == EventType.MouseDown && e.button == drawButton &&
                avatarMonitor.IsInDisplay(e.mousePosition))
            {
                avatarMonitor.BeginDrag(e.mousePosition);
                
                // ドラッグではなく単純なクリックの場合のために、ここでも頂点選択処理を呼ぶ
                // （ドラッグが開始されなかった場合のフォールバック）
                selectionHandler.HandleVertexClick(
                    editingCore.EditMeshCreater,
                    meshCollider,
                    avatarMonitor,
                    e.mousePosition);
            }
            
            // マウスドラッグ終了
            if (e.type == EventType.MouseUp && e.button == drawButton)
            {
                var rect = avatarMonitor.GetSelectionRect();
                if (rect.size.magnitude > 0)
                {
                    selectionHandler.HandleDragSelection(
                        editingCore.EditMeshCreater,
                        meshCollider,
                        avatarMonitor,
                        rect);
                }
                avatarMonitor.EndDrag();
            }
            
            // 選択範囲の描画
            avatarMonitor.DrawSelectionRect();
            Repaint();
        }
        
        /// <summary>
        /// アバターモニタータッチ処理
        /// </summary>
        private void HandleAvatarMonitorTouch()
        {
            // この処理はHandleVertexSelectionに統合されたため、何もしない
            // 互換性のためにメソッドは残しておく
        }
        
        /// <summary>
        /// 選択中の三角面から UV マスクを生成する
        /// </summary>
        private void GenerateMask()
        {
            var editIndex = editingCore.GetEditIndex();
            if (editIndex < 0) return;

            var meshCreater = editingCore.EditMeshCreater;
            if (meshCreater == null) return;

            var selected = editingCore.GetSelectionHandler().GetSelectedVertices();
            if (meshCreater.CountSelectedTriangles(selected) == 0)
            {
                EditorUtility.DisplayDialog(
                    Localization.Get("dialog_title_warning"),
                    Localization.Get("mask_no_selection"),
                    Localization.Get("dialog_ok"));
                return;
            }

            var renderers = editingCore.GetRenderers();
            Renderer targetRenderer = (editIndex < renderers.Length) ? renderers[editIndex] : null;

            var slot = uvPreviewPanel != null ? uvPreviewPanel.Canvas.MaterialSlot : 0;
            Texture sourceTexture = null;
            if (targetRenderer != null)
            {
                var materials = targetRenderer.sharedMaterials;
                if (materials != null && materials.Length > 0)
                {
                    var material = materials[Mathf.Clamp(slot, 0, materials.Length - 1)];
                    if (material != null)
                    {
                        sourceTexture = material.mainTexture;
                    }
                }
            }

            Texture2D mask = maskFollowTexture
                ? UVMaskGenerator.GenerateFollowingTexture(meshCreater, selected, sourceTexture, maskInvert)
                : UVMaskGenerator.Generate(meshCreater, selected, maskFixedSize, maskFixedSize, maskInvert);

            if (mask == null) return;

            if (uvPreviewPanel != null)
            {
                uvPreviewPanel.SetMaskTexture(mask);
            }

            Debug.Log(Localization.Get("mask_generated", mask.width, mask.height));

            if (maskAutoSave)
            {
                var directory = GetMaskSaveDirectory(sourceTexture, meshCreater);
                var baseName = !string.IsNullOrEmpty(meshCreater.name) ? meshCreater.name : "UVMask";
                if (sourceTexture != null && !string.IsNullOrEmpty(sourceTexture.name))
                {
                    baseName = sourceTexture.name;
                }

                var path = UVMaskGenerator.SaveToPng(mask, directory, baseName);
                if (!string.IsNullOrEmpty(path))
                {
                    lastMaskPath = path;
                    Debug.Log(Localization.Get("mask_saved", path));
                    var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (asset != null)
                    {
                        EditorGUIUtility.PingObject(asset);
                    }
                }
            }

            Repaint();
        }

        /// <summary>マスク PNG の保存先（元テクスチャと同じフォルダ）を求める</summary>
        private string GetMaskSaveDirectory(Texture sourceTexture, MeshCreater meshCreater)
        {
            if (sourceTexture != null)
            {
                var texturePath = AssetDatabase.GetAssetPath(sourceTexture);
                if (!string.IsNullOrEmpty(texturePath))
                {
                    return Path.GetDirectoryName(texturePath);
                }
            }

            if (meshCreater != null && meshCreater.RendBone != null)
            {
                var renderer = meshCreater.RendBone.GetComponent<Renderer>();
                var skinned = renderer as SkinnedMeshRenderer;
                if (skinned != null && skinned.sharedMesh != null)
                {
                    var meshPath = AssetDatabase.GetAssetPath(skinned.sharedMesh);
                    if (!string.IsNullOrEmpty(meshPath))
                    {
                        return Path.GetDirectoryName(meshPath);
                    }
                }
            }

            return "Assets/Temp";
        }

        /// <summary>
        /// UVプレビューを更新
        /// </summary>
        private void UpdateUVPreview()
        {
            if (editingCore == null || editingCore.EditMeshCreater == null)
            {
                uvPreviewPanel?.UpdateTarget(null, null, null);
                return;
            }
            
            var selectionHandler = editingCore.GetSelectionHandler();
            var selectedVertices = selectionHandler.GetSelectedVertices();
            
            // 現在の編集インデックスから最新のレンダラーを取得
            Renderer currentRenderer = null;
            var editIndex = editingCore.GetEditIndex();
            if (editIndex >= 0 && editIndex < editingCore.GetRenderers().Length)
            {
                currentRenderer = editingCore.GetRenderers()[editIndex];
            }

            uvPreviewPanel.UpdateTarget(editingCore.EditMeshCreater, currentRenderer, selectedVertices);
        }



        private void OnDestroy()
        {
            // まずコア機能のクリーンアップで参照をクリア
            editingCore?.Cleanup();
            
            // その後、一時アセットを削除
            assetManager.CleanupTemporaryAssets();
            
            // AvatarMonitorのクリーンアップ
            avatarMonitor?.Release();
            avatarMonitor = null;
            
            // UVプレビューパネルのクリーンアップ
            uvPreviewPanel?.Cleanup();
        }

        void Setup(GameObject anim)
        {
            this.avatar = anim;
            
            if (avatarMonitor != null) avatarMonitor.Release();
            avatarMonitor = new AvatarMonitor(anim.transform);
            
            // カメラビューがOFFならSceneViewモードをON（反転関係）
            avatarMonitor.SetSceneViewMode(!showCameraView);
            
            // コア機能のセットアップ
            Initialize();
            editingCore.Setup(anim, avatarMonitor);
            
            // SceneView統合を有効化
            sceneViewIntegration.Enable(avatarMonitor);
        }

        /// <summary>共有「メッシュ選択」ツールストリップ（タブ切替に依存せず常時表示）</summary>
        private void DrawSharedSelectionModule()
        {
            var selectionHandler = editingCore.GetSelectionHandler();
            var editMeshCreater = editingCore.EditMeshCreater;

            ToolUI.BeginCard(Localization.Get("mesh_select"));

            using (new EditorGUILayout.HorizontalScope())
            {
                string[] toolTips = new[]
                {
                    Localization.Get("tool_select_land"),
                    Localization.Get("tool_select_vertex"),
                    Localization.Get("tool_unselect_land"),
                    Localization.Get("tool_unselect_vertex")
                };

                for (int i = 0; i < extraTools.Length; i++)
                {
                    if (extraTools[i].ButtonCompact(ref penMode, ref brushPower, ref brushWidth, ref brushStrength,
                        toolTips[i], ToolUI.AccentBlue, GUILayout.Width(44f), GUILayout.Height(28f)))
                    {
                        switch (penMode)
                        {
                            case MeshPenTool.ExtraTool.SelectLand:
                                selectionHandler.CurrentMode = MeshSelectionHandler.SelectionMode.SelectLand;
                                break;
                            case MeshPenTool.ExtraTool.UnSelectLand:
                                selectionHandler.CurrentMode = MeshSelectionHandler.SelectionMode.UnSelectLand;
                                break;
                            case MeshPenTool.ExtraTool.SelectVertex:
                                selectionHandler.CurrentMode = MeshSelectionHandler.SelectionMode.SelectVertex;
                                break;
                            case MeshPenTool.ExtraTool.UnSelectVertex:
                                selectionHandler.CurrentMode = MeshSelectionHandler.SelectionMode.UnSelectVertex;
                                break;
                        }
                    }
                }

                GUILayout.Space(8f);

                bool disableBatch = editMeshCreater == null || editMeshCreater.IsComputeLandVertexes();
                using (new EditorGUI.DisabledScope(disableBatch))
                {
                    if (GUILayout.Button(Localization.Get("btn_select_all"), GUILayout.Height(28f), GUILayout.ExpandWidth(true)))
                    {
                        selectionHandler.SelectAll(editMeshCreater.VertexsCount());
                    }

                    if (GUILayout.Button(Localization.Get("btn_select_none"), GUILayout.Height(28f), GUILayout.ExpandWidth(true)))
                    {
                        selectionHandler.ClearSelection();
                    }

                    if (GUILayout.Button(Localization.Get("btn_revert_select"), GUILayout.Height(28f), GUILayout.ExpandWidth(true)))
                    {
                        selectionHandler.InvertSelection(editMeshCreater.VertexsCount());
                    }
                }
            }

            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                var backfaceCulling = selectionHandler.BackfaceCulling;
                var newBackfaceCulling = EditorGUILayout.ToggleLeft(
                    Localization.Get("opt_front_only_short"), backfaceCulling, GUILayout.MinWidth(130f));
                if (newBackfaceCulling != backfaceCulling)
                {
                    selectionHandler.BackfaceCulling = newBackfaceCulling;
                    if (avatarMonitor != null)
                    {
                        avatarMonitor.EnableBackfaceCulling = newBackfaceCulling;
                    }
                }

                var isSelectOverlapping = selectionHandler.IsSelectOverlappingVertices;
                var newSelectOverlapping = EditorGUILayout.ToggleLeft(
                    Localization.Get("opt_overlapping_short"), isSelectOverlapping, GUILayout.MinWidth(140f));
                if (newSelectOverlapping != isSelectOverlapping)
                {
                    selectionHandler.IsSelectOverlappingVertices = newSelectOverlapping;
                }

                GUILayout.FlexibleSpace();
            }

            ToolUI.EndCard();
        }

        /// <summary>「メッシュ編集」タブ（削除 / 抽出・材質スロット分離 / 一時メッシュ保存）</summary>
        private void DrawMeshEditTab()
        {
            var selectionHandler = editingCore.GetSelectionHandler();
            var editMeshCreater = editingCore.EditMeshCreater;
            var meshOperations = editingCore.GetMeshOperations();
            var selectedVertices = selectionHandler.GetSelectedVertices();
            bool noSelection = editMeshCreater == null || selectedVertices.Count == 0;

            // ---- カード 1: 選択頂点の削除 ----
            ToolUI.BeginCard(Localization.Get("card_delete_vertices"));
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(noSelection))
                {
                    if (GUILayout.Button(Localization.Get("btn_remove_mesh_ndmf"), GUILayout.Height(30f), GUILayout.ExpandWidth(true)))
                    {
                        var renderer = editMeshCreater.RendBone.GetComponent<Renderer>();
                        saveManager.RegisterMeshRemoval(avatar, renderer, selectedVertices);
                        editingCore.SelectMesh(editingCore.GetEditIndex());
                    }
                }

                using (new EditorGUI.DisabledScope(noSelection || cachedHasUnsavedMesh))
                {
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(1f, 0.78f, 0.45f, 1f);
                    bool clickedTempRemove = GUILayout.Button(
                        Localization.Get("btn_remove_mesh_temp"),
                        GUILayout.Height(30f),
                        GUILayout.ExpandWidth(true));
                    GUI.backgroundColor = prevBg;

                    if (clickedTempRemove)
                    {
                        int selectedTriangleCount = editMeshCreater.CountSelectedTriangles(selectedVertices);
                        if (selectedTriangleCount == 0)
                        {
                            EditorUtility.DisplayDialog(
                                Localization.Get("dialog_title_warning"),
                                Localization.Get("msg_no_selected_triangle"),
                                Localization.Get("dialog_ok"));
                        }
                        else if (selectedTriangleCount >= editMeshCreater.CountTotalTriangles())
                        {
                            EditorUtility.DisplayDialog(
                                Localization.Get("dialog_title_warning"),
                                Localization.Get("msg_all_triangles_selected"),
                                Localization.Get("dialog_ok"));
                        }
                        else
                        {
                            var sourceRenderer = editMeshCreater.RendBone != null
                                ? editMeshCreater.RendBone.GetComponent<Renderer>()
                                : null;

                            var cutObj = meshOperations.CreateMeshWithRemovedVertices(
                                editMeshCreater, selectedVertices, avatar, "_Cut");

                            if (cutObj != null)
                            {
                                var newRenderer = cutObj.GetComponent<Renderer>();
                                if (newRenderer != null)
                                {
                                    editingCore.AddRenderer(newRenderer, false);

                                    if (hideOriginalMeshAfterSlotSplit && sourceRenderer != null)
                                    {
                                        sourceRenderer.gameObject.SetActive(false);
                                    }

                                    var createdMesh = newRenderer.GetMesh();
                                    int remainingVerts = createdMesh != null ? createdMesh.vertexCount : 0;
                                    Debug.Log(Localization.Get("msg_temp_removed_created", remainingVerts));
                                }
                            }

                            editingCore.SelectMesh(-1);
                            if (sceneViewIntegration != null)
                            {
                                sceneViewIntegration.Disable();
                            }
                            UpdateUVPreview();
                            Repaint();
                        }
                    }
                }
            }
            ToolUI.EndCard();

            // ---- カード 2: 抽出と材質スロット分離 ----
            ToolUI.BeginCard(Localization.Get("card_extract_split"));

            if (cachedHasUnsavedMesh)
            {
                EditorGUILayout.HelpBox(Localization.Get("msg_unsaved_mesh_warning"), MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(noSelection || cachedHasUnsavedMesh))
            {
                if (GUILayout.Button(Localization.Get("btn_create_mesh"), GUILayout.Height(30f), GUILayout.ExpandWidth(true)))
                {
                    var options = new HitomiRuneko.MeshUVMaskCutTool.Core.MeshCreationOptions();
                    switch (meshCreateOption)
                    {
                        case MeshCreateOption.Normal:
                            break;
                        case MeshCreateOption.CreateInverse:
                            options.Inverse = true;
                            break;
                        case MeshCreateOption.RemoveBoneWeights:
                            options.RemoveBoneWeights = true;
                            break;
                        case MeshCreateOption.CopyUsedBones:
                            options.CopyUsedBonesOnly = true;
                            break;
                        case MeshCreateOption.CopyUsedBonesWithoutParent:
                            options.CopyUsedBonesOnly = true;
                            options.ExcludeParentBones = true;
                            break;
                        case MeshCreateOption.CopyUsedBonesWithoutParentAdjusted:
                            options.CopyUsedBonesOnly = true;
                            options.ExcludeParentBones = true;
                            options.RootAdjustment = HitomiRuneko.MeshUVMaskCutTool.Core.BoneRootAdjustment.Center;
                            break;
                        case MeshCreateOption.CopyUsedBonesWithoutParentTopY:
                            options.CopyUsedBonesOnly = true;
                            options.ExcludeParentBones = true;
                            options.RootAdjustment = HitomiRuneko.MeshUVMaskCutTool.Core.BoneRootAdjustment.TopY;
                            break;
                        case MeshCreateOption.CopyUsedBonesWithoutParentBottomY:
                            options.CopyUsedBonesOnly = true;
                            options.ExcludeParentBones = true;
                            options.RootAdjustment = HitomiRuneko.MeshUVMaskCutTool.Core.BoneRootAdjustment.BottomY;
                            break;
                    }

                    if (options.Inverse)
                    {
                        var allVertices = Enumerable.Range(0, editMeshCreater.VertexsCount()).ToList();
                        var inverseVertices = allVertices.Except(selectedVertices).ToList();
                        var inverseOptions = new HitomiRuneko.MeshUVMaskCutTool.Core.MeshCreationOptions();
                        var inverseObj = meshOperations.CreateMeshFromSelection(editMeshCreater, inverseVertices, avatar, "_Temp", inverseOptions);
                        if (inverseObj != null)
                        {
                            var inverseRenderer = inverseObj.GetComponent<SkinnedMeshRenderer>();
                            if (inverseRenderer != null)
                                editingCore.AddRenderer(inverseRenderer, false);
                        }
                    }

                    var newObj = meshOperations.CreateMeshFromSelection(editMeshCreater, selectedVertices, avatar, "_Temp", options);
                    if (newObj != null)
                    {
                        Renderer newRenderer = null;
                        if (options.CopyUsedBonesOnly && !options.RemoveBoneWeights)
                        {
                            newRenderer = newObj.GetComponentInChildren<SkinnedMeshRenderer>();
                        }
                        else
                        {
                            newRenderer = options.RemoveBoneWeights
                                ? newObj.GetComponent<MeshRenderer>()
                                : newObj.GetComponent<SkinnedMeshRenderer>();
                        }

                        if (newRenderer != null)
                            editingCore.AddRenderer(newRenderer, false);
                    }

                    editingCore.SelectMesh(-1);
                    if (sceneViewIntegration != null)
                    {
                        sceneViewIntegration.Disable();
                    }
                    UpdateUVPreview();
                    Repaint();
                }

                if (GUILayout.Button(Localization.Get("btn_separate_material_slot"), GUILayout.Height(30f), GUILayout.ExpandWidth(true)))
                {
                    int selectedTriangleCount = editMeshCreater.CountSelectedTriangles(selectedVertices);
                    if (selectedTriangleCount == 0)
                    {
                        EditorUtility.DisplayDialog(
                            Localization.Get("dialog_title_warning"),
                            Localization.Get("msg_no_selected_triangle"),
                            Localization.Get("dialog_ok"));
                    }
                    else
                    {
                        var sourceRenderer = editMeshCreater.RendBone != null
                            ? editMeshCreater.RendBone.GetComponent<Renderer>()
                            : null;

                        var slotObj = meshOperations.CreateMeshWithSeparatedMaterialSlots(
                            editMeshCreater, selectedVertices, avatar, "_Slot");

                        if (slotObj != null)
                        {
                            var slotRenderer = slotObj.GetComponent<SkinnedMeshRenderer>();
                            if (slotRenderer != null)
                            {
                                editingCore.AddRenderer(slotRenderer, false);

                                if (hideOriginalMeshAfterSlotSplit && sourceRenderer != null)
                                {
                                    sourceRenderer.gameObject.SetActive(false);
                                }

                                int slotCount = slotRenderer.sharedMaterials != null
                                    ? slotRenderer.sharedMaterials.Length
                                    : 0;
                                Debug.Log(string.Format(Localization.Get("msg_material_slot_created"), slotCount));
                            }
                        }

                        editingCore.SelectMesh(-1);
                        if (sceneViewIntegration != null)
                        {
                            sceneViewIntegration.Disable();
                        }
                        UpdateUVPreview();
                        Repaint();
                    }
                }
            }

            GUILayout.Space(4f);
            hideOriginalMeshAfterSlotSplit = EditorGUILayout.ToggleLeft(
                Localization.Get("opt_hide_original_after_temp"), hideOriginalMeshAfterSlotSplit);

            extendMeshOption = EditorGUILayout.Foldout(extendMeshOption, Localization.Get("mesh_create_option"), true);
            if (cachedExtendMeshOption)
            {
                meshCreationOptionsPanel.SetFromLegacyOption(meshCreateOption);
                meshCreationOptionsPanel.DrawOptionsPanel();

                GUILayout.Space(4f);
                EditorGUILayout.LabelField(Localization.Get("normal_offset"));
                var normalOffset = meshOperations.NormalOffset;
                var newNormalOffset = EditorGUILayout.Slider(normalOffset, -1f, 1f);
                if (Math.Abs(newNormalOffset - normalOffset) > 0.001f)
                {
                    meshOperations.NormalOffset = newNormalOffset;
                }
            }
            ToolUI.EndCard();

            // ---- カード 3: 一時メッシュの保存（メッシュ編集タブへ統合） ----
            int unsavedCount = assetManager.TempMeshes.Count(m => m.Asset != null && !AssetDatabase.Contains(m.Asset));
            string saveBadge = cachedHasUnsavedMesh ? Localization.Get("save_unsaved_badge", unsavedCount) : null;
            ToolUI.BeginCard(Localization.Get("section_save"), saveBadge, ToolUI.AccentAmber);

            if (!cachedHasUnsavedMesh)
            {
                EditorGUILayout.LabelField(Localization.Get("save_no_temp_hint"), EditorStyles.miniLabel);
            }
            else
            {
                int targetIndex = FindTargetTempMeshIndex(out string targetName);
                EditorGUILayout.LabelField(Localization.Get("save_target_label", targetName), EditorStyles.boldLabel);
                EditorGUILayout.LabelField(Localization.Get("save_description"), EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(4f);

                bool hasSelectedAssets = assetManager.TempMeshes.Any(m => m.IsSelected) ||
                    assetManager.TempMaterials.Any(m => m.IsSelected) ||
                    assetManager.TempTextures.Any(t => t.IsSelected);
                bool canSave = targetIndex != -1 && hasSelectedAssets;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(Localization.Get("save_suffix"), GUILayout.Width(72f));
                    assetSuffix = EditorGUILayout.TextField(assetSuffix, GUILayout.Width(110f));
                    GUILayout.Space(6f);

                    using (new EditorGUI.DisabledScope(!canSave))
                    {
                        var prevBg = GUI.backgroundColor;
                        GUI.backgroundColor = ToolUI.AccentGreen;
                        bool saveClicked = GUILayout.Button(
                            Localization.Get("save_selected"),
                            GUILayout.Height(26f),
                            GUILayout.ExpandWidth(true));
                        GUI.backgroundColor = prevBg;

                        if (saveClicked)
                        {
                            saveManager.SaveTemporaryAssetsToFiles(targetIndex, editingCore.GetRenderers(), assetSuffix);
                            editingCore.SelectMesh(-1);
                            if (sceneViewIntegration != null)
                            {
                                sceneViewIntegration.Disable();
                            }
                            UpdateUVPreview();
                            Repaint();
                        }
                    }
                }
            }
            ToolUI.EndCard();
        }

        /// <summary>「UV 編集」タブ（UV キャンバス / マスク生成 / UV 変換）</summary>
        private void DrawUVEditTab()
        {
            // ---- カード 1: UV プレビュー & 反転選択 ----
            ToolUI.BeginCard();
            uvPreviewPanel.DrawUVPreview();
            ToolUI.EndCard();

            // ---- カード 2: マスク生成 ----
            ToolUI.BeginCard();
            extendMaskSection = EditorGUILayout.Foldout(
                extendMaskSection, Localization.Get("mask_section"), true, EditorStyles.foldoutHeader);
            if (cachedExtendMaskSection)
            {
                GUILayout.Space(2f);
                maskFollowTexture = EditorGUILayout.ToggleLeft(Localization.Get("mask_follow_texture"), maskFollowTexture);

                using (new EditorGUI.DisabledScope(maskFollowTexture))
                {
                    maskFixedSize = EditorGUILayout.IntPopup(
                        Localization.Get("mask_size"),
                        maskFixedSize,
                        new[] { "512", "1024", "2048", "4096" },
                        new[] { 512, 1024, 2048, 4096 });
                }

                maskInvert = EditorGUILayout.ToggleLeft(Localization.Get("mask_invert"), maskInvert);
                maskAutoSave = EditorGUILayout.ToggleLeft(Localization.Get("mask_autosave"), maskAutoSave);

                using (new EditorGUI.DisabledScope(editingCore.GetEditIndex() == -1))
                {
                    if (GUILayout.Button(Localization.Get("mask_generate"), GUILayout.Height(26f)))
                    {
                        GenerateMask();
                    }
                }

                EditorGUILayout.LabelField(
                    Localization.Get("mask_current"),
                    string.IsNullOrEmpty(lastMaskPath) ? "-" : lastMaskPath,
                    EditorStyles.miniLabel);
            }
            ToolUI.EndCard();

            // ---- カード 3: UV とテクスチャの変換（低頻度操作） ----
            ToolUI.BeginCard();
            extendUvTools = EditorGUILayout.Foldout(
                extendUvTools, Localization.Get("uv_texture_edit"), true, EditorStyles.foldoutHeader);

            var editIdx = editingCore.GetEditIndex();
            bool isTempMesh = true;
            if (editIdx != -1)
            {
                var renderers = editingCore.GetRenderers();
                if (editIdx < renderers.Length && renderers[editIdx] != null)
                {
                    var rendererGameObject = renderers[editIdx].gameObject;
                    isTempMesh = assetManager.TempMeshes.Any(m =>
                        m.RelatedGameObject == rendererGameObject ||
                        (rendererGameObject.transform.parent != null &&
                         m.RelatedGameObject == rendererGameObject.transform.parent.gameObject));
                }
            }

            if (Event.current != null && Event.current.type == EventType.Layout)
            {
                cachedShowSavedWarning = !isTempMesh;
            }

            if (cachedExtendUvTools)
            {
                GUILayout.Space(2f);
                if (cachedShowSavedWarning)
                {
                    EditorGUILayout.HelpBox(Localization.Get("msg_saved_object_warning"), MessageType.Warning);
                }

                using (new EditorGUI.DisabledScope(editingCore.EditMeshCreater == null))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(Localization.Get("uv_fit"), GUILayout.Height(26f), GUILayout.ExpandWidth(true)))
                        {
                            if (ConfirmOverwriteIfSaved(isTempMesh))
                            {
                                var ops = editingCore.GetMeshOperations();
                                ops.UpdateMeshUV(editingCore.EditMeshCreater, true, false);
                                UpdateUVPreview();
                                Repaint();
                            }
                        }

                        if (GUILayout.Button(Localization.Get("uv_reconstruct"), GUILayout.Height(26f), GUILayout.ExpandWidth(true)))
                        {
                            if (ConfirmOverwriteIfSaved(isTempMesh))
                            {
                                var ops = editingCore.GetMeshOperations();
                                ops.UpdateMeshUV(editingCore.EditMeshCreater, false, true);
                                UpdateUVPreview();
                                Repaint();
                            }
                        }
                    }

                    GUILayout.Space(4f);
                    extendAdjustUVEdit = EditorGUILayout.Foldout(extendAdjustUVEdit, Localization.Get("uv_adjust"), true);
                    if (cachedExtendAdjustUVEdit)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(Localization.Get("flip_horizontal")))
                            {
                                if (ConfirmOverwriteIfSaved(isTempMesh))
                                {
                                    editingCore.GetMeshOperations().TransformUV(editingCore.EditMeshCreater, MeshOperations.TransformType.FlipHorizontal);
                                    UpdateUVPreview();
                                    Repaint();
                                }
                            }

                            if (GUILayout.Button(Localization.Get("flip_vertical")))
                            {
                                if (ConfirmOverwriteIfSaved(isTempMesh))
                                {
                                    editingCore.GetMeshOperations().TransformUV(editingCore.EditMeshCreater, MeshOperations.TransformType.FlipVertical);
                                    UpdateUVPreview();
                                    Repaint();
                                }
                            }
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(Localization.Get("rotate_90")))
                            {
                                if (ConfirmOverwriteIfSaved(isTempMesh))
                                {
                                    editingCore.GetMeshOperations().TransformUV(editingCore.EditMeshCreater, MeshOperations.TransformType.Rotate90);
                                    UpdateUVPreview();
                                    Repaint();
                                }
                            }

                            if (GUILayout.Button(Localization.Get("rotate_minus_90")))
                            {
                                if (ConfirmOverwriteIfSaved(isTempMesh))
                                {
                                    editingCore.GetMeshOperations().TransformUV(editingCore.EditMeshCreater, MeshOperations.TransformType.RotateMinus90);
                                    UpdateUVPreview();
                                    Repaint();
                                }
                            }
                        }
                    }
                }
            }
            ToolUI.EndCard();
        }

        private bool ConfirmOverwriteIfSaved(bool isTempMesh)
        {
            if (isTempMesh) return true;
            return EditorUtility.DisplayDialog(
                Localization.Get("dialog_title_warning"),
                Localization.Get("msg_saved_object_warning"),
                Localization.Get("dialog_ok"),
                Localization.Get("dialog_cancel"));
        }

        private int FindTargetTempMeshIndex(out string targetName)
        {
            targetName = "-";
            var renderers = editingCore.GetRenderers();
            if (renderers == null || renderers.Length == 0) return -1;

            int editIdx = editingCore.GetEditIndex();
            if (editIdx >= 0 && editIdx < renderers.Length && renderers[editIdx] != null)
            {
                var rendGo = renderers[editIdx].gameObject;
                var match = assetManager.TempMeshes.FirstOrDefault(m =>
                    m.Asset != null && !AssetDatabase.Contains(m.Asset) &&
                    (m.RelatedGameObject == rendGo ||
                     (rendGo.transform.parent != null && m.RelatedGameObject == rendGo.transform.parent.gameObject)));
                if (match != null)
                {
                    targetName = renderers[editIdx].name;
                    return editIdx;
                }
            }

            var firstTemp = assetManager.TempMeshes.FirstOrDefault(m => m.Asset != null && !AssetDatabase.Contains(m.Asset));
            if (firstTemp == null) return -1;

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (firstTemp.RelatedGameObject == r.gameObject ||
                    (r.transform.parent != null && firstTemp.RelatedGameObject == r.transform.parent.gameObject))
                {
                    targetName = r.name;
                    return i;
                }
            }

            return -1;
        }
    }

    internal static class ToolUI
    {
        public static Color HeaderBg => EditorGUIUtility.isProSkin
            ? new Color(0.18f, 0.18f, 0.20f, 1f)
            : new Color(0.79f, 0.79f, 0.81f, 1f);

        public static Color CardBg => EditorGUIUtility.isProSkin
            ? new Color(0.24f, 0.24f, 0.26f, 1f)
            : new Color(0.84f, 0.84f, 0.86f, 1f);

        public static Color CardBorder => EditorGUIUtility.isProSkin
            ? new Color(0.14f, 0.14f, 0.16f, 1f)
            : new Color(0.64f, 0.64f, 0.67f, 1f);

        public static Color Divider => EditorGUIUtility.isProSkin
            ? new Color(0.12f, 0.12f, 0.13f, 1f)
            : new Color(0.60f, 0.60f, 0.62f, 1f);

        public static Color AccentBlue => new Color(0.28f, 0.58f, 0.96f, 1f);
        public static Color AccentAmber => new Color(0.96f, 0.70f, 0.24f, 1f);
        public static Color AccentGreen => new Color(0.38f, 0.78f, 0.48f, 1f);

        private static GUIStyle cardStyle;
        private static Texture2D cardBgTex;
        private static bool cardTexProSkin;

        private static GUIStyle GetCardStyle()
        {
            if (cardStyle == null || cardBgTex == null || cardTexProSkin != EditorGUIUtility.isProSkin)
            {
                cardTexProSkin = EditorGUIUtility.isProSkin;
                cardBgTex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                cardBgTex.SetPixel(0, 0, CardBg);
                cardBgTex.Apply();

                cardStyle = new GUIStyle
                {
                    normal = { background = cardBgTex },
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(4, 4, 4, 4)
                };
            }

            return cardStyle;
        }

        public static void BeginCard(string title = null, string badgeText = null, Color? badgeColor = null)
        {
            var rect = EditorGUILayout.BeginVertical(GetCardStyle());
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                var border = CardBorder;
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), border);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), border);
                EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);
            }

            if (!string.IsNullOrEmpty(title))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    if (!string.IsNullOrEmpty(badgeText))
                    {
                        var prev = GUI.contentColor;
                        GUI.contentColor = badgeColor ?? AccentAmber;
                        EditorGUILayout.LabelField("● " + badgeText, EditorStyles.boldLabel, GUILayout.Width(100f));
                        GUI.contentColor = prev;
                    }
                }
                GUILayout.Space(4f);
            }
        }

        public static void EndCard()
        {
            EditorGUILayout.EndVertical();
        }

        public static int DrawTabBar(int currentTab, string[] labels, bool[] dots)
        {
            var barRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(28f));
            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(barRect, HeaderBg);
                EditorGUI.DrawRect(new Rect(barRect.x, barRect.yMax - 1f, barRect.width, 1f), Divider);
            }

            int nextTab = currentTab;
            for (int i = 0; i < labels.Length; i++)
            {
                bool active = i == currentTab;
                bool showDot = dots != null && i < dots.Length && dots[i];
                string text = showDot ? labels[i] + "  ●" : labels[i];

                var prevContent = GUI.contentColor;
                if (active)
                {
                    GUI.contentColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                }
                else
                {
                    GUI.contentColor = showDot
                        ? AccentAmber
                        : (EditorGUIUtility.isProSkin ? new Color(0.72f, 0.72f, 0.75f, 1f) : new Color(0.32f, 0.32f, 0.35f, 1f));
                }

                var style = active ? EditorStyles.boldLabel : EditorStyles.label;
                var tabStyle = new GUIStyle(style)
                {
                    alignment = TextAnchor.MiddleCenter
                };

                if (GUILayout.Button(text, tabStyle, GUILayout.Height(26f), GUILayout.ExpandWidth(true)))
                {
                    nextTab = i;
                }

                var btnRect = GUILayoutUtility.GetLastRect();
                if (Event.current != null && Event.current.type == EventType.Repaint && active)
                {
                    EditorGUI.DrawRect(new Rect(btnRect.x + 8f, btnRect.yMax - 2.5f, Mathf.Max(0f, btnRect.width - 16f), 2.5f), AccentBlue);
                }

                GUI.contentColor = prevContent;
            }

            EditorGUILayout.EndHorizontal();
            return nextTab;
        }
    }
}