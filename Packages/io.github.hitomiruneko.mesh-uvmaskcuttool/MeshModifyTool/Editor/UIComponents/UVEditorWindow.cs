using UnityEditor;
using UnityEngine;

namespace HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool.UIComponents
{
    /// <summary>
    /// UV エディタの独立ウィンドウ（大画面で細かい操作をするためのもの）。
    /// UVPreviewPanel と同じ UVCanvas インスタンスを共有するので、内蔵パネルと状態は常に同期する。
    /// </summary>
    public class UVEditorWindow : EditorWindow
    {
        private UVPreviewPanel owner;
        private UVCanvas canvas;

        public static void Open(UVPreviewPanel owner, UVCanvas canvas)
        {
            var window = GetWindow<UVEditorWindow>();
            window.owner = owner;
            window.canvas = canvas;
            window.minSize = new Vector2(640f, 480f);
            window.titleContent = new GUIContent(Localization.Get("uv_window_title"));
            window.Show();
        }

        private void OnGUI()
        {
            wantsMouseMove = true;

            var title = Localization.Get("uv_window_title");
            if (titleContent == null || titleContent.text != title)
            {
                titleContent = new GUIContent(title);
            }

            if (owner == null || canvas == null || canvas.MeshCreater == null)
            {
                EditorGUILayout.HelpBox(Localization.Get("msg_select_preview"), MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                owner.DrawCanvasControls(false);
            }

            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            canvas.Draw(rect);
            owner.DrawInfoBar();

            var e = Event.current;
            if (e != null && (e.type == EventType.MouseMove ||
                              e.type == EventType.MouseDown ||
                              e.type == EventType.MouseDrag ||
                              e.type == EventType.MouseUp ||
                              e.type == EventType.ScrollWheel))
            {
                Repaint();
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
