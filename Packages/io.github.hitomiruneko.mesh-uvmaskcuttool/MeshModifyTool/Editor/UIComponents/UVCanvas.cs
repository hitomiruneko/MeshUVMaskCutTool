using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HitomiRuneko.MeshUVMaskCutTool.Core;

namespace HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool.UIComponents
{
    /// <summary>UV キャンバスでのピッキング操作種別</summary>
    public enum UVPickOperation
    {
        Replace,
        Add,
        Remove
    }

    /// <summary>
    /// UV マップの表示と操作を行うキャンバス。
    /// テクスチャ / UV ライン / 選択三角面 / マスクを重ねて表示し、
    /// ズーム・パンと、UV からの頂点選択（3D への逆方向リンク）に対応する。
    /// 内蔵パネルと独立ウィンドウの両方から使う。
    /// </summary>
    public class UVCanvas
    {
        private const float MinScale = 0.05f;
        private const float MaxScale = 16f;
        private const float ScrollFactor = 0.12f;
        private const float DefaultUnitSize = 1024f;

        // ---- 表示対象 ----
        private MeshCreater meshCreater;
        private Renderer targetRenderer;
        private int materialSlot;
        private readonly HashSet<int> selectedVertices = new HashSet<int>();
        private Texture2D maskTexture;

        public MeshCreater MeshCreater
        {
            get => meshCreater;
            set { meshCreater = value; MarkDirty(); }
        }

        public Renderer TargetRenderer
        {
            get => targetRenderer;
            set { targetRenderer = value; MarkDirty(); }
        }

        public int MaterialSlot
        {
            get => materialSlot;
            set { materialSlot = Mathf.Max(0, value); MarkDirty(); }
        }

        public Texture2D MaskTexture
        {
            get => maskTexture;
            set => maskTexture = value;
        }

        // ---- 表示設定 ----
        public bool ShowTexture = true;
        public bool ShowUvLines = true;
        public bool ShowSelectedOnly = false;
        public bool ShowMask = false;
        public bool ShowSelectedTriangles = true;
        public float TextureOpacity = 0.9f;

        // ---- ビュー状態 ----
        public Vector2 ViewPosition = Vector2.zero;
        public float ViewScale = 1f;
        private bool requestFitView = true;

        // ---- 色 ----
        public Color UvLineColor = new Color(1f, 1f, 1f, 0.8f);
        public Color UvLineShadowColor = new Color(0f, 0f, 0f, 0.6f);
        public Color SelectedFillColor = new Color(1f, 0.22f, 0.28f, 0.35f);
        public Color SelectedLineColor = new Color(1f, 0.12f, 0.18f, 1f);
        public Color BoundsColor = new Color(1f, 1f, 1f, 0.3f);

        // ---- ピッキング（UV -> 3D） ----
        public bool EnablePicking = false;
        public event Action<List<int>, UVPickOperation> OnVerticesPicked;
        public event Action<Vector2> OnHoverUV;

        // ---- キャッシュ ----
        private bool dirty = true;
        private int cachedTopologyHash;
        private Vector2[] uvPoints = new Vector2[0];
        private int[] lineIndices = new int[0];
        private List<int[]> slotTriangles = new List<int[]>();
        private List<int[]> selectedTriangles = new List<int[]>();
        private Material[] cachedMaterials = new Material[0];
        private Vector3[] drawBuffer = new Vector3[0];
        private int cachedSlot = -1;

        // ---- 操作状態 ----
        private bool isBoxSelecting;
        private bool isPanning;
        private Vector2 boxStartWindow;
        private Vector2 boxStartContent;
        private Vector2 boxEndContent;
        private UVPickOperation boxOperation = UVPickOperation.Replace;

        public bool IsDragging => isBoxSelecting || isPanning;

        public void MarkDirty()
        {
            dirty = true;
        }

        public void RequestFitView()
        {
            requestFitView = true;
        }

        public void SetViewScaleCentered(float newScale)
        {
            var prev = ViewScale;
            ViewScale = Mathf.Clamp(newScale, MinScale, MaxScale);
            if (Mathf.Approximately(prev, ViewScale) || prev <= 0f) return;

            ViewPosition -= ViewPosition * (1f - ViewScale / prev);
            var contentSize = GetUnitSize() * ViewScale;
            ViewPosition = Vector2.Max(Vector2.zero, Vector2.Min(ViewPosition, contentSize));
        }

        public void SetSelectedVertices(IEnumerable<int> vertices)
        {
            selectedVertices.Clear();
            if (vertices != null)
            {
                foreach (var v in vertices)
                {
                    selectedVertices.Add(v);
                }
            }

            RebuildSelectedTriangles();
        }

        // ===================================================================
        // 描画
        // ===================================================================

        /// <summary>キャンバスを描画する（viewportRect は描画に使える領域）</summary>
        public void Draw(Rect viewportRect)
        {
            if (Event.current == null) return;

            var controlId = GUIUtility.GetControlID(FocusType.Passive);

            // スクロールバー分を差し引いた表示領域
            var innerRect = new Rect(
                viewportRect.x,
                viewportRect.y,
                Mathf.Max(10f, viewportRect.width - GUI.skin.verticalScrollbar.fixedWidth),
                Mathf.Max(10f, viewportRect.height - GUI.skin.horizontalScrollbar.fixedHeight));

            if (meshCreater == null)
            {
                using (var scroll = new GUI.ScrollViewScope(viewportRect, Vector2.zero, new Rect(Vector2.zero, innerRect.size), true, true))
                {
                    scroll.handleScrollWheel = false;
                    var bgRect = new Rect(Vector2.zero, innerRect.size);
                    EditorGUI.DrawRect(bgRect, new Color(0.13f, 0.13f, 0.13f, 1f));
                    EditorGUI.HelpBox(
                        new Rect(6f, 6f, Mathf.Max(10f, innerRect.width - 12f), 40f),
                        Localization.Get("msg_select_preview"),
                        MessageType.None);
                }

                if (Event.current.type != EventType.Layout && OnHoverUV != null)
                {
                    OnHoverUV(new Vector2(float.NaN, float.NaN));
                }

                return;
            }

            EnsureCache();

            var unit = GetUnitSize();
            if (requestFitView && Event.current.type == EventType.Repaint && innerRect.width > 10f && innerRect.height > 10f)
            {
                FitToSize(innerRect.size, unit);
            }

            var contentSize = new Vector2(unit.x * ViewScale, unit.y * ViewScale);
            var viewRect = new Rect(Vector2.zero, contentSize + innerRect.size);
            var contentRect = new Rect(innerRect.size * 0.5f, contentSize);

            using (var scroll = new GUI.ScrollViewScope(viewportRect, ViewPosition, viewRect, true, true))
            {
                scroll.handleScrollWheel = false;
                ViewPosition = scroll.scrollPosition;

                DrawContent(contentRect);

                if (EnablePicking && isBoxSelecting && Event.current.type == EventType.Repaint)
                {
                    var r = Rect.MinMaxRect(
                        Mathf.Min(boxStartContent.x, boxEndContent.x), Mathf.Min(boxStartContent.y, boxEndContent.y),
                        Mathf.Max(boxStartContent.x, boxEndContent.x), Mathf.Max(boxStartContent.y, boxEndContent.y));
                    EditorGUI.DrawRect(r, new Color(0.3f, 0.6f, 1f, 0.15f));
                    Handles.color = new Color(0.4f, 0.7f, 1f, 0.9f);
                    Handles.DrawAAPolyLine(2f,
                        new Vector3(r.xMin, r.yMin, 0), new Vector3(r.xMax, r.yMin, 0),
                        new Vector3(r.xMax, r.yMax, 0), new Vector3(r.xMin, r.yMax, 0),
                        new Vector3(r.xMin, r.yMin, 0));
                }
            }

            HandleEvents(controlId, innerRect, contentRect);
            ViewPosition = Vector2.Max(Vector2.zero, Vector2.Min(ViewPosition, contentSize));
        }

        private void FitToSize(Vector2 viewSize, Vector2 unit)
        {
            var sx = viewSize.x / Mathf.Max(1f, unit.x);
            var sy = viewSize.y / Mathf.Max(1f, unit.y);
            ViewScale = Mathf.Clamp(Mathf.Min(sx, sy) * 0.95f, MinScale, MaxScale);
            ViewPosition = unit * (ViewScale * 0.5f);
            requestFitView = false;
        }

        private void DrawContent(Rect contentRect)
        {
            // 背景（チェッカー風）
            EditorGUI.DrawRect(contentRect, new Color(0.16f, 0.16f, 0.16f, 1f));

            var texture = GetMainTexture();

            if (ShowTexture && texture != null)
            {
                var prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(TextureOpacity));
                EditorGUI.DrawTextureTransparent(contentRect, texture);
                GUI.color = prevColor;
            }

            if (ShowMask && maskTexture != null)
            {
                var prevColor = GUI.color;
                GUI.color = new Color(0.25f, 1f, 0.45f, 0.4f);
                EditorGUI.DrawTextureTransparent(contentRect, maskTexture);
                GUI.color = prevColor;
            }

            // Handles 系は Repaint でのみ描画する
            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            // 0-1 の枠
            Handles.color = BoundsColor;
            var b0 = UvToPoint(contentRect, new Vector2(0f, 0f));
            var b1 = UvToPoint(contentRect, new Vector2(1f, 1f));
            Handles.DrawAAPolyLine(1.5f,
                new Vector3(b0.x, b0.y, 0), new Vector3(b1.x, b0.y, 0),
                new Vector3(b1.x, b1.y, 0), new Vector3(b0.x, b1.y, 0),
                new Vector3(b0.x, b0.y, 0));

            if (ShowUvLines && !ShowSelectedOnly)
            {
                DrawUvLines(contentRect);
            }

            if (ShowSelectedTriangles)
            {
                DrawSelectedTriangles(contentRect);
            }
        }

        private void DrawUvLines(Rect contentRect)
        {
            var count = uvPoints.Length;
            if (count == 0 || lineIndices.Length == 0) return;

            if (drawBuffer.Length < count)
            {
                drawBuffer = new Vector3[count];
            }

            for (int i = 0; i < count; i++)
            {
                var p = UvToPoint(contentRect, uvPoints[i]);
                drawBuffer[i] = new Vector3(p.x, p.y, 0f);
            }

            // 影（黒）
            Handles.color = UvLineShadowColor;
            Handles.DrawLines(drawBuffer, lineIndices);

            // 本体（白）+ 1px ずらし
            for (int i = 0; i < count; i++)
            {
                drawBuffer[i] -= Vector3.one;
            }

            Handles.color = UvLineColor;
            Handles.DrawLines(drawBuffer, lineIndices);
        }

        private void DrawSelectedTriangles(Rect contentRect)
        {
            if (selectedTriangles.Count == 0) return;

            var vertexCount = uvPoints.Length;
            for (int i = 0; i < selectedTriangles.Count; i++)
            {
                var tri = selectedTriangles[i];
                if (tri == null || tri.Length < 3) continue;
                if (tri[0] < 0 || tri[1] < 0 || tri[2] < 0) continue;
                if (tri[0] >= vertexCount || tri[1] >= vertexCount || tri[2] >= vertexCount) continue;

                var a = UvToPoint(contentRect, uvPoints[tri[0]]);
                var b = UvToPoint(contentRect, uvPoints[tri[1]]);
                var c = UvToPoint(contentRect, uvPoints[tri[2]]);

                if (tri[0] == tri[1] || tri[1] == tri[2] || tri[2] == tri[0]) continue;

                Handles.color = SelectedFillColor;
                Handles.DrawAAConvexPolygon(
                    new Vector3(a.x, a.y, 0f),
                    new Vector3(b.x, b.y, 0f),
                    new Vector3(c.x, c.y, 0f));

                Handles.color = SelectedLineColor;
                Handles.DrawAAPolyLine(1.6f,
                    new Vector3(a.x, a.y, 0f),
                    new Vector3(b.x, b.y, 0f),
                    new Vector3(c.x, c.y, 0f),
                    new Vector3(a.x, a.y, 0f));
            }
        }

        // ===================================================================
        // 入力処理
        // ===================================================================

        private Vector2 WindowToContent(Rect innerRect, Vector2 windowPos)
        {
            return windowPos - innerRect.position + ViewPosition;
        }

        private void HandleEvents(int controlId, Rect innerRect, Rect contentRect)
        {
            var e = Event.current;
            if (e == null || e.type == EventType.Layout || e.type == EventType.Used) return;

            var inside = innerRect.Contains(e.mousePosition);

            if (OnHoverUV != null)
            {
                if (inside)
                {
                    OnHoverUV(PointToUv(contentRect, WindowToContent(innerRect, e.mousePosition)));
                }
                else if (e.type == EventType.MouseMove || e.type == EventType.Repaint)
                {
                    OnHoverUV(new Vector2(float.NaN, float.NaN));
                }
            }

            switch (e.type)
            {
                case EventType.ScrollWheel:
                {
                    if (!inside) break;
                    var delta = e.delta.y + e.delta.x;
                    if (Mathf.Abs(delta) < 0.0001f) break;
                    ZoomAt(e.mousePosition, innerRect, delta);
                    e.Use();
                    break;
                }

                case EventType.MouseDown:
                {
                    if (!inside) break;

                    var wantsPan = e.button == 2 || e.button == 1 || (e.button == 0 && !EnablePicking);
                    if (wantsPan)
                    {
                        isPanning = true;
                        isBoxSelecting = false;
                        GUIUtility.hotControl = controlId;
                        e.Use();
                    }
                    else if (e.button == 0 && EnablePicking)
                    {
                        boxOperation = e.shift
                            ? UVPickOperation.Add
                            : ((e.control || e.command) ? UVPickOperation.Remove : UVPickOperation.Replace);
                        boxStartWindow = e.mousePosition;
                        var contentPos = WindowToContent(innerRect, e.mousePosition);
                        boxStartContent = contentPos;
                        boxEndContent = contentPos;
                        isBoxSelecting = true;
                        isPanning = false;
                        GUIUtility.hotControl = controlId;
                        e.Use();
                    }

                    break;
                }

                case EventType.MouseDrag:
                {
                    if (GUIUtility.hotControl != controlId) break;

                    if (isPanning)
                    {
                        ViewPosition -= e.delta;
                        e.Use();
                    }
                    else if (isBoxSelecting)
                    {
                        boxEndContent = WindowToContent(innerRect, e.mousePosition);
                        e.Use();
                    }

                    break;
                }

                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl != controlId) break;

                    if (isPanning)
                    {
                        isPanning = false;
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    else if (isBoxSelecting)
                    {
                        isBoxSelecting = false;
                        GUIUtility.hotControl = 0;
                        boxEndContent = WindowToContent(innerRect, e.mousePosition);

                        if ((e.mousePosition - boxStartWindow).sqrMagnitude > 16f)
                        {
                            PickBox(boxStartContent, boxEndContent, contentRect, boxOperation);
                        }
                        else
                        {
                            PickAt(boxEndContent, contentRect, boxOperation);
                        }

                        e.Use();
                    }

                    break;
                }

                case EventType.Ignore:
                {
                    if (GUIUtility.hotControl == controlId && (isPanning || isBoxSelecting))
                    {
                        isPanning = false;
                        isBoxSelecting = false;
                        GUIUtility.hotControl = 0;
                    }

                    break;
                }
            }
        }

        private void ZoomAt(Vector2 mousePosition, Rect innerRect, float delta)
        {
            var prev = ViewScale;
            ViewScale *= 1f - delta * ScrollFactor;
            ViewScale = Mathf.Clamp(ViewScale, MinScale, MaxScale);
            if (Mathf.Approximately(prev, ViewScale)) return;

            var pos = mousePosition - innerRect.center + ViewPosition;
            ViewPosition -= pos * (1f - ViewScale / prev);
            var newContentSize = GetUnitSize() * ViewScale;
            ViewPosition = Vector2.Max(Vector2.zero, Vector2.Min(ViewPosition, newContentSize));
        }

        // ===================================================================
        // ピッキング
        // ===================================================================

        private void PickAt(Vector2 contentPos, Rect contentRect, UVPickOperation operation)
        {
            var uv = PointToUv(contentRect, contentPos);
            var picked = new List<int>();
            var vertexCount = uvPoints.Length;

            for (int i = 0; i < slotTriangles.Count; i++)
            {
                var tri = slotTriangles[i];
                if (tri[0] >= vertexCount || tri[1] >= vertexCount || tri[2] >= vertexCount) continue;
                if (PointInTriangle(uv, uvPoints[tri[0]], uvPoints[tri[1]], uvPoints[tri[2]]))
                {
                    picked.Add(tri[0]);
                    picked.Add(tri[1]);
                    picked.Add(tri[2]);
                    break;
                }
            }

            if (picked.Count > 0)
            {
                NotifyPicked(picked, operation);
            }
            else if (operation == UVPickOperation.Replace)
            {
                // 空白をクリックした場合は選択解除
                NotifyPicked(new List<int>(), operation);
            }
        }

        private void PickBox(Vector2 contentA, Vector2 contentB, Rect contentRect, UVPickOperation operation)
        {
            var min = Vector2.Min(contentA, contentB);
            var max = Vector2.Max(contentA, contentB);
            var center = (min + max) * 0.5f;
            var picked = new List<int>();
            var vertexCount = uvPoints.Length;

            for (int i = 0; i < slotTriangles.Count; i++)
            {
                var tri = slotTriangles[i];
                if (tri[0] >= vertexCount || tri[1] >= vertexCount || tri[2] >= vertexCount) continue;

                var p0 = UvToPoint(contentRect, uvPoints[tri[0]]);
                var p1 = UvToPoint(contentRect, uvPoints[tri[1]]);
                var p2 = UvToPoint(contentRect, uvPoints[tri[2]]);

                var hit = InRect(p0, min, max) || InRect(p1, min, max) || InRect(p2, min, max) ||
                          PointInTriangle(center, p0, p1, p2);

                if (hit)
                {
                    picked.Add(tri[0]);
                    picked.Add(tri[1]);
                    picked.Add(tri[2]);
                }
            }

            if (picked.Count > 0 || operation == UVPickOperation.Replace)
            {
                NotifyPicked(picked, operation);
            }
        }

        private void NotifyPicked(List<int> vertices, UVPickOperation operation)
        {
            if (OnVerticesPicked == null) return;
            OnVerticesPicked(vertices, operation);
        }

        // ===================================================================
        // キャッシュ
        // ===================================================================

        private void EnsureCache()
        {
            if (meshCreater == null) return;

            var hash = meshCreater.GetTopologyHash();
            var needRebuild = dirty || hash != cachedTopologyHash || cachedMaterials.Length == 0;
            if (!needRebuild)
            {
                var slot = Mathf.Clamp(materialSlot, 0, Mathf.Max(0, meshCreater.SubMeshCount() - 1));
                if (slot != cachedSlot)
                {
                    cachedSlot = slot;
                    slotTriangles = meshCreater.GetTriangles(slot);
                    RebuildSlotLines();
                    RebuildSelectedTriangles();
                }

                return;
            }

            dirty = false;
            cachedTopologyHash = hash;

            var vertexCount = meshCreater.VertexsCount();
            var uvList = meshCreater.GetUVList();
            if (uvPoints.Length != vertexCount)
            {
                uvPoints = new Vector2[vertexCount];
            }

            for (int i = 0; i < vertexCount; i++)
            {
                var arr = uvList[i];
                uvPoints[i] = (arr != null && arr.Length > 0) ? arr[0] : Vector2.zero;
            }

            cachedMaterials = meshCreater.GetMaterials();
            cachedSlot = Mathf.Clamp(materialSlot, 0, Mathf.Max(0, meshCreater.SubMeshCount() - 1));
            slotTriangles = meshCreater.GetTriangles(cachedSlot);
            RebuildSlotLines();
            RebuildSelectedTriangles();
        }

        private void RebuildSlotLines()
        {
            var edgeSet = new HashSet<long>();
            var indices = new List<int>();
            for (int t = 0; t < slotTriangles.Count; t++)
            {
                var tri = slotTriangles[t];
                if (tri == null || tri.Length < 3) continue;
                AddEdge(edgeSet, indices, tri[0], tri[1]);
                AddEdge(edgeSet, indices, tri[1], tri[2]);
                AddEdge(edgeSet, indices, tri[2], tri[0]);
            }

            lineIndices = indices.ToArray();
        }

        private void RebuildSelectedTriangles()
        {
            if (meshCreater == null || selectedVertices.Count == 0 || slotTriangles == null || slotTriangles.Count == 0)
            {
                selectedTriangles = new List<int[]>();
                return;
            }

            var result = new List<int[]>();
            for (int t = 0; t < slotTriangles.Count; t++)
            {
                var tri = slotTriangles[t];
                if (tri == null || tri.Length < 3) continue;
                if (selectedVertices.Contains(tri[0]) && selectedVertices.Contains(tri[1]) && selectedVertices.Contains(tri[2]))
                {
                    result.Add(tri);
                }
            }

            selectedTriangles = result;
        }

        private static void AddEdge(HashSet<long> set, List<int> indices, int a, int b)
        {
            if (a < 0 || b < 0 || a == b) return;

            long key = a < b
                ? (((long)a) << 32) | (uint)b
                : (((long)b) << 32) | (uint)a;

            if (set.Add(key))
            {
                indices.Add(a);
                indices.Add(b);
            }
        }

        // ===================================================================
        // 補助
        // ===================================================================

        public Texture GetMainTexture()
        {
            if (targetRenderer == null) return null;

            var materials = targetRenderer.sharedMaterials;
            if (materials == null || materials.Length == 0) return null;

            var index = Mathf.Clamp(materialSlot, 0, materials.Length - 1);
            var material = materials[index];
            return material != null ? material.mainTexture : null;
        }

        public Vector2 GetUnitSize()
        {
            var texture = GetMainTexture();
            if (texture != null && texture.width > 0 && texture.height > 0)
            {
                return new Vector2(texture.width, texture.height);
            }

            return new Vector2(DefaultUnitSize, DefaultUnitSize);
        }

        public static Vector2 UvToPoint(Rect contentRect, Vector2 uv)
        {
            return new Vector2(
                contentRect.x + uv.x * contentRect.width,
                contentRect.y + (1f - uv.y) * contentRect.height);
        }

        public static Vector2 PointToUv(Rect contentRect, Vector2 point)
        {
            if (contentRect.width < 0.0001f || contentRect.height < 0.0001f)
            {
                return Vector2.zero;
            }

            return new Vector2(
                (point.x - contentRect.x) / contentRect.width,
                1f - (point.y - contentRect.y) / contentRect.height);
        }

        public static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            var d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            var d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            var d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);

            var hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
            var hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
            return !(hasNeg && hasPos);
        }

        private static bool InRect(Vector2 p, Vector2 min, Vector2 max)
        {
            return p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;
        }

        /// <summary>マスク画像だけを生成済みか（パネル側の表示切替用）</summary>
        public bool HasMask => maskTexture != null;
    }
}
