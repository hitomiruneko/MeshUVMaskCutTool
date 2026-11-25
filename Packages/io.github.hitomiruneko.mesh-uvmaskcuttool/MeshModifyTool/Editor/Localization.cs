using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool
{
    public static class Localization
    {
        public enum Language
        {
            Japanese = 0,
            English = 1,
            Chinese = 2
        }

        private const string LanguagePrefsKey = "MeshUVMaskCutTool_Language";

        private static Language currentLanguage = Language.Japanese;

        public static Language CurrentLanguage
        {
            get => currentLanguage;
            set
            {
                currentLanguage = value;
                EditorPrefs.SetInt(LanguagePrefsKey, (int)value);
            }
        }

        static Localization()
        {
            // 初回起動時はOSの言語から既定値を決める（以降は EditorPrefs の記憶を優先）
            var systemLanguage = Application.systemLanguage;
            var defaultLanguage = (systemLanguage == SystemLanguage.ChineseSimplified ||
                                   systemLanguage == SystemLanguage.ChineseTraditional ||
                                   systemLanguage == SystemLanguage.Chinese)
                ? Language.Chinese
                : Language.Japanese;

            var stored = EditorPrefs.GetInt(LanguagePrefsKey, (int)defaultLanguage);
            if (stored < (int)Language.Japanese || stored > (int)Language.Chinese)
            {
                stored = (int)Language.Japanese;
            }

            currentLanguage = (Language)stored;
        }

        /// <summary>言語名（切替UI用）</summary>
        public static string GetLanguageDisplayName(Language language)
        {
            switch (language)
            {
                case Language.Japanese: return "日本語";
                case Language.English: return "English";
                case Language.Chinese: return "中文";
                default: return language.ToString();
            }
        }

        /// <summary>言語切替ポップアップを描画</summary>
        public static void DrawLanguageSelector(float width = 210f, params GUILayoutOption[] extraOptions)
        {
            var languages = new[] { Language.Japanese, Language.English, Language.Chinese };
            var names = new string[languages.Length];
            for (int i = 0; i < languages.Length; i++)
            {
                names[i] = GetLanguageDisplayName(languages[i]);
            }

            var currentIndex = System.Array.IndexOf(languages, currentLanguage);
            if (currentIndex < 0) currentIndex = 0;

            var options = new List<GUILayoutOption>(extraOptions ?? new GUILayoutOption[0]);
            if (width > 0f) options.Add(GUILayout.Width(width));

            var newIndex = EditorGUILayout.Popup(currentIndex, names, options.ToArray());
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < languages.Length)
            {
                CurrentLanguage = languages[newIndex];
            }
        }

        private static Dictionary<string, Dictionary<Language, string>> texts = new Dictionary<string, Dictionary<Language, string>>
        {
            // ===== ウィンドウ =====
            ["window_title"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "MeshUVMaskCutTool",
                [Language.English] = "MeshUVMaskCutTool",
                [Language.Chinese] = "MeshUVMaskCutTool"
            },
            ["window_description"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "Unity上でアバターのメッシュ分割・マテリアルスロット分離・UV編集・マスク生成ができるツールです",
                [Language.English] = "Unity tool for avatar mesh cutting, material slot splitting, interactive UV editing & UV mask generation",
                [Language.Chinese] = "在 Unity 内即可完成 Avatar 网格切割、材质槽分离、交互式 UV 编辑与 UV 蒙版生成的工具"
            },

            // ===== セクション =====
            ["section_mesh_edit"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ編集",
                [Language.English] = "Mesh Editing",
                [Language.Chinese] = "网格编辑"
            },
            ["section_uv_edit"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UV編集",
                [Language.English] = "UV Editing",
                [Language.Chinese] = "UV 编辑"
            },
            ["section_save"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "一時的に作成したメッシュを保存",
                [Language.English] = "Save Temporarily Created Mesh",
                [Language.Chinese] = "保存临时创建的网格"
            },

            // ===== メッシュ選択 =====
            ["mesh_select"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ選択",
                [Language.English] = "Mesh Selection",
                [Language.Chinese] = "网格选择"
            },
            ["backface_culling"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ドラッグ時、表面のメッシュのみ選択",
                [Language.English] = "Select only front-facing mesh when dragging",
                [Language.Chinese] = "拖拽时仅选择正面网格"
            },
            ["select_overlapping"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "一括選択時、重複頂点も選択（実行に時間がかかる場合があります）",
                [Language.English] = "Include overlapping vertices in batch selection (May take time to execute)",
                [Language.Chinese] = "批量选择时同时选中重叠顶点（执行可能需要时间）"
            },

            // ===== ボタン =====
            ["btn_select_all"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "すべて選択",
                [Language.English] = "Select All",
                [Language.Chinese] = "全选"
            },
            ["btn_select_none"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "全選択解除",
                [Language.English] = "Deselect All",
                [Language.Chinese] = "取消全选"
            },
            ["btn_revert_select"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択反転",
                [Language.English] = "Invert Selection",
                [Language.Chinese] = "反选"
            },
            ["btn_create_mesh"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択頂点のメッシュを作成（一時的）",
                [Language.English] = "Create Mesh from Selected Vertices (Temporary)",
                [Language.Chinese] = "由选中顶点创建网格（临时）"
            },
            ["temp_mesh_description"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "一時的なメッシュはこのウィンドウを閉じると削除されます\n下の「一時的に作成したメッシュを保存」を押すと、アセットとして保存されます",
                [Language.English] = "Temporary meshes will be deleted when this window is closed\nPress \"Save Temporarily Created Mesh\" below to save as assets",
                [Language.Chinese] = "临时网格会在关闭本窗口时删除\n按下方「保存临时创建的网格」可保存为资产"
            },
            ["btn_remove_mesh_ndmf"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択頂点を削除（NDMF非破壊削除）",
                [Language.English] = "Remove Selected (NDMF Non-Destructive)",
                [Language.Chinese] = "删除选中顶点（NDMF 非破坏性删除）"
            },
            ["btn_back"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "戻る",
                [Language.English] = "Back",
                [Language.Chinese] = "返回"
            },

            // ===== メッシュ作成オプション =====
            ["create_mesh"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ編集",
                [Language.English] = "Mesh Edit",
                [Language.Chinese] = "网格编辑"
            },
            ["mesh_create_option"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ作成オプション",
                [Language.English] = "Mesh Creation Options",
                [Language.Chinese] = "网格创建选项"
            },
            ["create_option"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "作成オプション",
                [Language.English] = "Creation Option",
                [Language.Chinese] = "创建选项"
            },
            ["create_normal"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択部分のみ",
                [Language.English] = "Selected Only",
                [Language.Chinese] = "仅选中部分"
            },
            ["mesh_edit_option"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ選択オプション",
                [Language.English] = "Mesh Selection Options",
                [Language.Chinese] = "网格选择选项"
            },
            ["normal_offset"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "頂点を押し出し",
                [Language.English] = "Push Vertices Along Normals",
                [Language.Chinese] = "沿法线推移顶点"
            },
            ["create_unselected"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択範囲外も含めて作成",
                [Language.English] = "Include Unselected",
                [Language.Chinese] = "包含未选中部分一并创建"
            },
            ["remove_bone_weights"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ボーンウェイトを削除（MeshRenderer出力）",
                [Language.English] = "Remove Bone Weights (MeshRenderer)",
                [Language.Chinese] = "删除骨骼权重（输出 MeshRenderer）"
            },
            ["copy_used_bones"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "依存するボーンをコピー",
                [Language.English] = "Copy Dependent Bones",
                [Language.Chinese] = "复制依赖的骨骼"
            },
            ["copy_used_bones_without_parent"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "使用ボーンのみ（親を除く）",
                [Language.English] = "Used Bones Only (Exclude Parents)",
                [Language.Chinese] = "仅使用骨骼（排除父级）"
            },
            ["copy_used_bones_without_parent_adjusted"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "使用ボーンのみ（親を除く・ルート位置調整）",
                [Language.English] = "Used Bones Only (Exclude Parents, Adjust Root)",
                [Language.Chinese] = "仅使用骨骼（排除父级・调整根位置）"
            },
            ["copy_used_bones_without_parent_top_y"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "使用ボーンのみ（親を除く・最高Y座標）",
                [Language.English] = "Used Bones Only (Exclude Parents, Top Y)",
                [Language.Chinese] = "仅使用骨骼（排除父级・最高 Y）"
            },
            ["copy_used_bones_without_parent_bottom_y"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "使用ボーンのみ（親を除く・最低Y座標）",
                [Language.English] = "Used Bones Only (Exclude Parents, Bottom Y)",
                [Language.Chinese] = "仅使用骨骼（排除父级・最低 Y）"
            },
            ["show_camera_view"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "独自カメラビューを表示",
                [Language.English] = "Show Custom Camera View",
                [Language.Chinese] = "显示独立相机视图"
            },

            // ===== UV編集 =====
            ["texture_preview"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "テクスチャプレビュー",
                [Language.English] = "Texture Preview",
                [Language.Chinese] = "贴图预览"
            },
            ["uv_texture_edit"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UVテクスチャ編集",
                [Language.English] = "UV & Texture Editing",
                [Language.Chinese] = "UV 与贴图编辑"
            },
            ["uv_fit"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UVフィット（0-1範囲に収める）",
                [Language.English] = "Fit UV to 0-1 Range",
                [Language.Chinese] = "UV 适配（缩放到 0–1 范围）"
            },
            ["uv_reconstruct"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UV再構築（実験的機能）",
                [Language.English] = "Rebuild UV (Experimental)",
                [Language.Chinese] = "UV 重建（实验功能）"
            },
            ["uv_adjust"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UV調整（実験的機能）",
                [Language.English] = "UV Adjustments (Experimental)",
                [Language.Chinese] = "UV 调整（实验功能）"
            },
            ["flip_horizontal"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "左右反転",
                [Language.English] = "Flip Horizontal",
                [Language.Chinese] = "左右翻转"
            },
            ["flip_vertical"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "上下反転",
                [Language.English] = "Flip Vertical",
                [Language.Chinese] = "上下翻转"
            },
            ["rotate_90"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "90度回転",
                [Language.English] = "Rotate 90°",
                [Language.Chinese] = "旋转 90°"
            },
            ["rotate_minus_90"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "-90度回転",
                [Language.English] = "Rotate -90°",
                [Language.Chinese] = "旋转 -90°"
            },

            // ===== 保存 =====
            ["save_suffix"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "保存時の接尾辞",
                [Language.English] = "Save Suffix",
                [Language.Chinese] = "保存后缀"
            },
            ["save_selected"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "一時的に作成したメッシュを保存",
                [Language.English] = "Save Temporarily Created Mesh",
                [Language.Chinese] = "保存临时创建的网格"
            },
            ["save_description"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュに関係するテクスチャやマテリアルも保存されます\n保存場所は元のメッシュ、テクスチャ、マテリアルがあった場所と同一です",
                [Language.English] = "Related textures and materials will also be saved\nFiles will be saved in the same location as the original mesh, textures, and materials",
                [Language.Chinese] = "与网格相关的贴图与材质也会一并保存\n保存位置与原网格、贴图、材质所在的目录相同"
            },
            ["unsaved"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "● 未保存",
                [Language.English] = "● Unsaved",
                [Language.Chinese] = "● 未保存"
            },

            // ===== UI設定 =====
            ["ui_settings"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UI設定 & デバッグ",
                [Language.English] = "UI Settings & Debug",
                [Language.Chinese] = "UI 设置与调试"
            },
            ["selected_vertices"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択頂点数:",
                [Language.English] = "Selected Vertices:",
                [Language.Chinese] = "选中顶点数："
            },
            ["not_selected_vertices"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "非選択頂点数:",
                [Language.English] = "Not Selected Vertices:",
                [Language.Chinese] = "未选中顶点数："
            },

            // ===== メッセージ =====
            ["msg_select_preview"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュを選択するとテクスチャプレビューが表示されます",
                [Language.English] = "Select a mesh to display texture preview",
                [Language.Chinese] = "选中网格后将显示贴图预览"
            },
            ["msg_saved_object_warning"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "保存済みのオブジェクトが選択されています。\n元のオブジェクトを上書き変更する場合のみ操作してください",
                [Language.English] = "A saved object is selected.\nProceed only if you want to overwrite the original object",
                [Language.Chinese] = "当前选中的是已保存的对象。\n仅在需要覆盖修改原对象时才进行操作"
            },
            ["msg_save_complete"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択したアセットを保存しました",
                [Language.English] = "Successfully saved selected assets",
                [Language.Chinese] = "已保存选中的资产"
            },
            ["msg_asset_saved"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "アセットの保存",
                [Language.English] = "Save Assets",
                [Language.Chinese] = "保存资产"
            },

            // ===== その他UI =====
            ["wire_frame_color"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ワイヤーフレーム色",
                [Language.English] = "Wire Frame Color",
                [Language.Chinese] = "线框颜色"
            },
            ["normal"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "法線表示",
                [Language.English] = "Normal Display",
                [Language.Chinese] = "法线显示"
            },
            ["uv"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UV表示",
                [Language.English] = "UV Display",
                [Language.Chinese] = "UV 显示"
            },
            ["debug"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "デバッグ",
                [Language.English] = "Debug",
                [Language.Chinese] = "调试"
            },
            ["msg_select_mesh_prompt"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "以下のメッシュをクリックして編集を開始してください",
                [Language.English] = "Click on a mesh below to start editing",
                [Language.Chinese] = "点击下方网格开始编辑"
            },
            ["btn_select_removal_vertices_count"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除対象頂点選択 ({0}頂点)",
                [Language.English] = "Select Removal Vertices ({0})",
                [Language.Chinese] = "选择删除顶点（{0} 个）"
            },
            ["language_label"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "Change Language / 言語変更:",
                [Language.English] = "Change Language / 言語変更:",
                [Language.Chinese] = "切换语言 / Change Language："
            },
            ["msg_removal_registered"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除情報を登録しました",
                [Language.English] = "Removal Info Registered",
                [Language.Chinese] = "已登记删除信息"
            },
            ["msg_removal_info"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ビルド時に以下の頂点が削除されます:",
                [Language.English] = "The following vertices will be removed during build:",
                [Language.Chinese] = "构建时以下顶点将被删除："
            },
            ["active_avatars_description"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "シーン内のアクティブなアバターを選択してすぐに編集を開始できます",
                [Language.English] = "Select an active avatar in the scene to start editing immediately",
                [Language.Chinese] = "选择场景中处于激活状态的 Avatar 即可开始编辑"
            },

            // ===== 削除データUI =====
            ["removal_data_section"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除データ ({0} 項目)",
                [Language.English] = "Removal Data ({0} items)",
                [Language.Chinese] = "删除数据（{0} 项）"
            },
            ["removal_data_select_mesh"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除データ ({0} 項目) - メッシュを選択してください",
                [Language.English] = "Removal Data ({0} items) - Please select the mesh",
                [Language.Chinese] = "删除数据（{0} 项）- 请选择网格"
            },
            ["btn_enable"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "[ON]",
                [Language.English] = "[ON]",
                [Language.Chinese] = "[开]"
            },
            ["btn_disable"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "[OFF]",
                [Language.English] = "[OFF]",
                [Language.Chinese] = "[关]"
            },
            ["btn_select_vertices"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = ">> 頂点を選択 ({0}個)",
                [Language.English] = ">> Select Vertices ({0})",
                [Language.Chinese] = ">> 选择顶点（{0} 个）"
            },
            ["btn_add_to_merge"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "統合対象にする",
                [Language.English] = "Add to Merge",
                [Language.Chinese] = "加入合并目标"
            },
            ["btn_remove_from_merge"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "統合対象から外す",
                [Language.English] = "Remove from Merge",
                [Language.Chinese] = "移出合并目标"
            },
            ["btn_merge_selected"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択した{0}個を統合",
                [Language.English] = "Merge {0} Selected",
                [Language.Chinese] = "合并选中的 {0} 项"
            },
            ["toggle_enable_disable"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "クリックして有効/無効を切り替え",
                [Language.English] = "Click to toggle enable/disable",
                [Language.Chinese] = "点击切换启用/禁用"
            },
            ["btn_delete"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除",
                [Language.English] = "Delete",
                [Language.Chinese] = "删除"
            },
            ["btn_cancel"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "キャンセル",
                [Language.English] = "Cancel",
                [Language.Chinese] = "取消"
            },
            ["confirm_delete_title"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除の確認",
                [Language.English] = "Confirm Delete",
                [Language.Chinese] = "确认删除"
            },
            ["confirm_delete_message"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "この削除データを削除してもよろしいですか？\nこの操作は元に戻せません。",
                [Language.English] = "Are you sure you want to delete this removal data?\nThis action cannot be undone.",
                [Language.Chinese] = "确定要删除这条删除数据吗？\n此操作无法撤销。"
            },
            ["msg_unsaved_mesh_warning"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "新しい一時メッシュを作成する場合は未保存メッシュを保存するか削除してください",
                [Language.English] = "To create a new temporary mesh, please save or delete the unsaved mesh",
                [Language.Chinese] = "若要创建新的临时网格，请先保存或删除未保存的网格"
            },
            ["msg_mesh_selection_mode"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ選択モード中",
                [Language.English] = "Mesh Selection Mode Active",
                [Language.Chinese] = "网格选择模式中"
            },
            ["msg_gizmo_warning"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "※ Gizmo等の操作ができない場合があります",
                [Language.English] = "※ Gizmo operations may be disabled",
                [Language.Chinese] = "※ 可能无法操作 Gizmo 等"
            },
            ["btn_delete_temp_mesh"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除",
                [Language.English] = "Delete",
                [Language.Chinese] = "删除"
            },

            // ===== メッシュ作成オプションUI =====
            ["mesh_creation_mode"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ作成モード",
                [Language.English] = "Mesh Creation Mode",
                [Language.Chinese] = "网格创建模式"
            },
            ["base_creation_mode"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "基本作成モード",
                [Language.English] = "Base Creation Mode",
                [Language.Chinese] = "基本创建模式"
            },
            ["weight_bone_settings"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ウェイト/ボーン設定",
                [Language.English] = "Weight/Bone Settings",
                [Language.Chinese] = "权重/骨骼设置"
            },
            ["advanced_bone_settings"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "高度なボーン設定",
                [Language.English] = "Advanced Bone Settings",
                [Language.Chinese] = "高级骨骼设置"
            },
            ["keep_bone_weights"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "通常（ボーンウェイトを保持）",
                [Language.English] = "Normal (Keep Bone Weights)",
                [Language.Chinese] = "通常（保留骨骼权重）"
            },
            ["include_all_parent_bones"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "親ボーンを含めてボーン階層全体をコピー",
                [Language.English] = "Copy entire bone hierarchy including parent bones",
                [Language.Chinese] = "包含父骨骼，复制整个骨骼层级"
            },
            ["exclude_parent_bones"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "親ボーンを除外（実際に使用されているボーンのみコピー）",
                [Language.English] = "Exclude parent bones (copy only actually used bones)",
                [Language.Chinese] = "排除父骨骼（仅复制实际使用到的骨骼）"
            },
            ["root_position_settings"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ボーンの位置設定",
                [Language.English] = "Bone Position Settings",
                [Language.Chinese] = "骨骼位置设置"
            },
            ["root_no_adjustment"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "位置調整なし（元の位置を維持）",
                [Language.English] = "No adjustment (keep original position)",
                [Language.Chinese] = "不调整（保持原位置）"
            },
            ["root_center_position"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ境界ボックスの中心に配置",
                [Language.English] = "Place at mesh bounding box center",
                [Language.Chinese] = "放置在网格边界框中心"
            },
            ["root_top_y"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュの最高Y座標位置に配置",
                [Language.English] = "Place at mesh top Y position",
                [Language.Chinese] = "放置在网格最高 Y 位置"
            },
            ["root_bottom_y"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュの最低Y座標位置に配置",
                [Language.English] = "Place at mesh bottom Y position",
                [Language.Chinese] = "放置在网格最低 Y 位置"
            },

            // ===== ダイアログ =====
            ["dialog_title_warning"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "警告",
                [Language.English] = "Warning",
                [Language.Chinese] = "警告"
            },
            ["dialog_ok"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "OK",
                [Language.English] = "OK",
                [Language.Chinese] = "确定"
            },
            ["dialog_cancel"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "キャンセル",
                [Language.English] = "Cancel",
                [Language.Chinese] = "取消"
            },

            // ============================================================
            // 以下、カスタマイズ版で追加（lead / 2026-11-18）
            // ============================================================

            // ===== 氐部追加メッシュ作成（エラー表示・戻る） =====
            ["msg_error_occurred"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "エラーが発生しました。動作が不安定な可能性があります。\nウィンドウを閉じて再度開くと改善される場合があります。\n\nエラー詳細: ",
                [Language.English] = "An error occurred. The tool may be unstable.\nClosing and reopening the window may help.\n\nDetails: ",
                [Language.Chinese] = "发生了错误，运行可能不稳定。\n关闭窗口后重新打开可能会改善。\n\n错误详情："
            },
            ["btn_restart_window"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ウィンドウを再起動",
                [Language.English] = "Restart Window",
                [Language.Chinese] = "重启窗口"
            },
            ["btn_close_window"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ウィンドウを閉じる",
                [Language.English] = "Close Window",
                [Language.Chinese] = "关闭窗口"
            },
            ["btn_setup"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "セットアップ",
                [Language.English] = "Setup",
                [Language.Chinese] = "初始化"
            },
            ["btn_select"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択",
                [Language.English] = "Select",
                [Language.Chinese] = "选择"
            },

            // ===== 追加マテリアルスロット分離 =====
            ["btn_separate_material_slot"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択部分のマテリアルスロットを分離（一時的）",
                [Language.English] = "Separate Material Slot for Selection (Temporary)",
                [Language.Chinese] = "分离选中部分的材质槽（临时网格）"
            },
            ["opt_hide_original_mesh"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "作成後に元のメッシュを非表示にする",
                [Language.English] = "Hide the original mesh after creation",
                [Language.Chinese] = "创建后自动隐藏原网格"
            },
            ["msg_no_selected_triangle"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択頂点だけでは三角面を構成できません（三角面の3頂点すべてを選択してください）",
                [Language.English] = "The selected vertices do not form any triangle (all 3 vertices of a triangle must be selected)",
                [Language.Chinese] = "当前选中顶点未构成任何完整三角面（需同时选中三角面的三个顶点）"
            },
            ["msg_material_slot_created"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "マテリアルスロットを追加した一時メッシュを作成しました（スロット数: {0}）",
                [Language.English] = "Created a temporary mesh with additional material slots (slots: {0})",
                [Language.Chinese] = "已创建追加了材质槽的临时网格（材质槽数：{0}）"
            },

            // ===== UVパネル共通 =====
            ["uv_layer_texture"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "テクスチャ",
                [Language.English] = "Texture",
                [Language.Chinese] = "贴图"
            },
            ["uv_layer_uv"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UV",
                [Language.English] = "UV",
                [Language.Chinese] = "UV"
            },
            ["uv_layer_mask"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "マスク",
                [Language.English] = "Mask",
                [Language.Chinese] = "蒙版"
            },
            ["uv_view_scale"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "表示倍率",
                [Language.English] = "View Scale",
                [Language.Chinese] = "显示倍率"
            },
            ["uv_view_opacity"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "表示不透明度",
                [Language.English] = "View Opacity",
                [Language.Chinese] = "显示不透明度"
            },
            ["uv_fit_view"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "ウィンドウに合わせる",
                [Language.English] = "Fit the View to the Window",
                [Language.Chinese] = "适应窗口"
            },
            ["uv_material_slot"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "マテリアルスロット",
                [Language.English] = "Material Slot",
                [Language.Chinese] = "材质槽"
            },
            ["uv_open_window"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "別ウィンドウで開く",
                [Language.English] = "Open in Separate Window",
                [Language.Chinese] = "在独立窗口打开"
            },
            ["uv_window_title"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UVエディタ（大きな表示）",
                [Language.English] = "UV Editor (Large View)",
                [Language.Chinese] = "UV 编辑器（大窗口）"
            },
            ["uv_show_all"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "すべてのUV",
                [Language.English] = "All UV",
                [Language.Chinese] = "全部 UV"
            },
            ["uv_show_selected"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択のみ",
                [Language.English] = "Selected Only",
                [Language.Chinese] = "仅选中"
            },
            ["uv_selected_triangles"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択三角面: {0}",
                [Language.English] = "Selected Triangles: {0}",
                [Language.Chinese] = "选中三角面：{0}"
            },
            ["uv_reverse_hint"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "オンにすると、UV上でクリック/ドラッグして頂点を選択できます（Shift: 追加 / Ctrl: 除外）",
                [Language.English] = "When enabled, click/drag on the UV view to select vertices (Shift: add / Ctrl: remove)",
                [Language.Chinese] = "开启后可在 UV 上单击/框选来反选顶点（Shift 追加 / Ctrl 去除）"
            },
            ["uv_preview_height"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "プレビューの高さ",
                [Language.English] = "Preview Height",
                [Language.Chinese] = "预览高度"
            },
            ["uv_pick_mode"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UVから選択",
                [Language.English] = "Select from UV",
                [Language.Chinese] = "从 UV 反选 3D"
            },
            ["uv_hover"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UV: ({0:0.000}, {1:0.000})",
                [Language.English] = "UV: ({0:0.000}, {1:0.000})",
                [Language.Chinese] = "UV：({0:0.000}, {1:0.000})"
            },

            // ===== マスク生成 =====
            ["mask_section"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "マスク生成",
                [Language.English] = "Mask Generation",
                [Language.Chinese] = "蒙版生成"
            },
            ["mask_generate"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択三角面からマスクを生成",
                [Language.English] = "Generate Mask from Selected Triangles",
                [Language.Chinese] = "由选中三角面生成蒙版"
            },
            ["mask_size"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "マスクサイズ",
                [Language.English] = "Mask Size",
                [Language.Chinese] = "蒙版尺寸"
            },
            ["mask_follow_texture"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "テクスチャに合わせる",
                [Language.English] = "Follow Texture",
                [Language.Chinese] = "跟随贴图"
            },
            ["mask_invert"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "マスクを反転",
                [Language.English] = "Invert Mask",
                [Language.Chinese] = "反相蒙版"
            },
            ["mask_autosave"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "生成後にPNGとして保存",
                [Language.English] = "Save as PNG after generation",
                [Language.Chinese] = "生成后保存为 PNG"
            },
            ["mask_saved"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "マスクを保存しました:\n{0}",
                [Language.English] = "Mask saved:\n{0}",
                [Language.Chinese] = "蒙版已保存：\n{0}"
            },
            ["mask_no_selection"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "三角面が選択されていません",
                [Language.English] = "No triangle is selected",
                [Language.Chinese] = "未选中任何三角面"
            },
            ["mask_current"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "現在のマスク",
                [Language.English] = "Current Mask",
                [Language.Chinese] = "当前蒙版"
            },
            ["mask_generated"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "マスクを生成しました（{0}x{1}）",
                [Language.English] = "Mask generated ({0}x{1})",
                [Language.Chinese] = "蒙版已生成（{0}×{1}）"
            },

            // ===== UIリニューアル・新機能追加（2026-11-25） =====
            ["tab_mesh_edit"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ編集",
                [Language.English] = "Mesh Edit",
                [Language.Chinese] = "网格编辑"
            },
            ["tab_uv_edit"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "UV編集",
                [Language.English] = "UV Edit",
                [Language.Chinese] = "UV 编辑"
            },
            ["mesh_list_title"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ一覧",
                [Language.English] = "Mesh List",
                [Language.Chinese] = "网格列表"
            },
            ["mesh_list_expand"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "展開",
                [Language.English] = "Expand",
                [Language.Chinese] = "展开"
            },
            ["mesh_list_collapse"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "折りたたむ",
                [Language.English] = "Collapse",
                [Language.Chinese] = "收起"
            },
            ["mesh_list_search"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "メッシュ名で検索...",
                [Language.English] = "Search meshes...",
                [Language.Chinese] = "搜索网格名称..."
            },
            ["mesh_list_none_selected"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "未選択",
                [Language.English] = "None",
                [Language.Chinese] = "未选中"
            },
            ["mesh_list_current_prefix"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "編集中: {0}",
                [Language.English] = "Editing: {0}",
                [Language.Chinese] = "当前：{0}"
            },
            ["mesh_list_stats"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択 {0} 頂点 / {1} 三角面",
                [Language.English] = "Selected {0} verts / {1} tris",
                [Language.Chinese] = "已选 {0} 顶点 / {1} 三角面"
            },
            ["mesh_list_no_match"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "一致するメッシュがありません",
                [Language.English] = "No matching meshes",
                [Language.Chinese] = "没有匹配的网格"
            },
            ["mesh_list_removal_badge"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "削除:{0}",
                [Language.English] = "Del:{0}",
                [Language.Chinese] = "删:{0}"
            },
            ["mesh_list_temp_badge"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "未保存",
                [Language.English] = "Temp",
                [Language.Chinese] = "临时"
            },
            ["card_delete_vertices"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択頂点の削除",
                [Language.English] = "Remove Selected Vertices",
                [Language.Chinese] = "删除选中顶点"
            },
            ["card_extract_split"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "抽出・マテリアルスロット分離",
                [Language.English] = "Extract & Split Material Slots",
                [Language.Chinese] = "提取与材质槽分离"
            },
            ["btn_remove_mesh_temp"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択頂点を削除（新一時メッシュ）",
                [Language.English] = "Remove Selected (New Temp Mesh)",
                [Language.Chinese] = "删除选中顶点（新临时网格）"
            },
            ["msg_temp_removed_created"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "選択三角面を削除した一時メッシュを作成しました（残り頂点: {0}）",
                [Language.English] = "Created temporary mesh with selected triangles removed (remaining vertices: {0})",
                [Language.Chinese] = "已创建删除选中三角面的临时网格（剩余顶点：{0}）"
            },
            ["msg_all_triangles_selected"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "すべての三角面が選択されているため、空のメッシュは作成できません",
                [Language.English] = "All triangles are selected; cannot create an empty mesh",
                [Language.Chinese] = "所有三角面均已选中，无法创建空网格"
            },
            ["opt_hide_original_after_temp"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "一時メッシュ作成後に元のメッシュを非表示にする",
                [Language.English] = "Hide original mesh after creating temporary mesh",
                [Language.Chinese] = "创建临时网格后自动隐藏原网格"
            },
            ["save_unsaved_badge"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "未保存 {0} 件",
                [Language.English] = "{0} Unsaved",
                [Language.Chinese] = "待保存 {0} 项"
            },
            ["save_no_temp_hint"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "未保存の一時メッシュはありません",
                [Language.English] = "No unsaved temporary meshes",
                [Language.Chinese] = "当前没有未保存的临时网格"
            },
            ["save_target_label"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "保存対象: {0}",
                [Language.English] = "Target: {0}",
                [Language.Chinese] = "保存目标：{0}"
            },
            ["tool_select_land"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "アイランド選択（連結面を一括選択）",
                [Language.English] = "Select Island (connected faces)",
                [Language.Chinese] = "选岛（批量选择连通面）"
            },
            ["tool_select_vertex"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "三角面・頂点選択",
                [Language.English] = "Select Face / Vertex",
                [Language.Chinese] = "选择三角面/顶点"
            },
            ["tool_unselect_land"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "アイランド選択解除",
                [Language.English] = "Deselect Island",
                [Language.Chinese] = "取消选岛"
            },
            ["tool_unselect_vertex"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "三角面・頂点選択解除",
                [Language.English] = "Deselect Face / Vertex",
                [Language.Chinese] = "取消选择三角面/顶点"
            },
            ["opt_front_only_short"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "表面のみ選択",
                [Language.English] = "Front-facing only",
                [Language.Chinese] = "仅选择正面"
            },
            ["opt_overlapping_short"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "重複頂点を含む",
                [Language.English] = "Include overlapping",
                [Language.Chinese] = "包含重叠顶点"
            },
            ["avatar_label"] = new Dictionary<Language, string>
            {
                [Language.Japanese] = "アバター: {0}",
                [Language.English] = "Avatar: {0}",
                [Language.Chinese] = "Avatar：{0}"
            }
        };

        /// <summary>日/英/中の三語ローカライズ（zh 未指定時は英語へフォールバック）</summary>
        public static string L(string ja, string en, string zh = null)
        {
            switch (currentLanguage)
            {
                case Language.Chinese:
                    return string.IsNullOrEmpty(zh) ? en : zh;
                case Language.English:
                    return en;
                default:
                    return ja;
            }
        }

        public static string Get(string key)
        {
            if (texts.TryGetValue(key, out var dict))
            {
                if (dict.TryGetValue(currentLanguage, out var text) && !string.IsNullOrEmpty(text))
                {
                    return text;
                }

                // 未翻訳時は英語 → 日本語 → キー の順にフォールバック
                if (dict.TryGetValue(Language.English, out text) && !string.IsNullOrEmpty(text))
                {
                    return text;
                }
                if (dict.TryGetValue(Language.Japanese, out text) && !string.IsNullOrEmpty(text))
                {
                    return text;
                }
            }

            return key; // キーが見つからない場合はキーをそのまま返す
        }

        /// <summary>書式付き取得（{0}, {1} ...）</summary>
        public static string Get(string key, params object[] args)
        {
            var text = Get(key);
            if (args == null || args.Length == 0)
            {
                return text;
            }

            try
            {
                return string.Format(text, args);
            }
            catch (System.FormatException)
            {
                return text;
            }
        }
    }
}
