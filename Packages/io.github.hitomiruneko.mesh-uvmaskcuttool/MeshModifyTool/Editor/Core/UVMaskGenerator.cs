using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using HitomiRuneko.MeshUVMaskCutTool.Core;

namespace HitomiRuneko.MeshUVMaskCutTool.MeshModifyTool.Core
{
    /// <summary>
    /// 選択された 3D 三角面から UV マスクを生成する。
    /// UV 空間で三角形をラスタライズするので、塗りではなく形状に沿った正確なマスクになる。
    /// </summary>
    public static class UVMaskGenerator
    {
        /// <summary>マスクを生成する（白=選択 / 透明=非選択）</summary>
        /// <param name="meshCreater">対象メッシュ</param>
        /// <param name="selectedVertices">選択頂点（三角形の3頂点がすべて選択されているものが対象）</param>
        /// <param name="width">出力幅</param>
        /// <param name="height">出力高さ</param>
        /// <param name="invert">反転するか</param>
        public static Texture2D Generate(MeshCreater meshCreater, IList<int> selectedVertices,
            int width, int height, bool invert)
        {
            width = Mathf.Clamp(width, 16, 8192);
            height = Mathf.Clamp(height, 16, 8192);

            var pixels = new Color32[width * height];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = clear;
            }

            if (meshCreater != null && selectedVertices != null && selectedVertices.Count > 0)
            {
                var selectedSet = new HashSet<int>(selectedVertices);
                var uvList = meshCreater.GetUVList();
                var white = new Color32(255, 255, 255, 255);

                for (int sm = 0; sm < meshCreater.SubMeshCount(); sm++)
                {
                    var tris = meshCreater.GetTriangles(sm);
                    for (int t = 0; t < tris.Count; t++)
                    {
                        var tri = tris[t];
                        if (tri[0] < 0 || tri[1] < 0 || tri[2] < 0) continue;
                        if (!selectedSet.Contains(tri[0]) || !selectedSet.Contains(tri[1]) || !selectedSet.Contains(tri[2]))
                        {
                            continue;
                        }

                        var uv0 = GetUV(uvList, tri[0]);
                        var uv1 = GetUV(uvList, tri[1]);
                        var uv2 = GetUV(uvList, tri[2]);

                        // 0-1 の範囲に収まる場合はそのまま、はみ出す場合は -1/0/+1 の複製も描く
                        if (IsInsideUnitRange(uv0, uv1, uv2))
                        {
                            RasterizeTriangle(pixels, width, height, uv0, uv1, uv2, white);
                        }
                        else
                        {
                            for (int ou = -1; ou <= 1; ou++)
                            {
                                for (int ov = -1; ov <= 1; ov++)
                                {
                                    var offset = new Vector2(ou, ov);
                                    RasterizeTriangle(pixels, width, height,
                                        uv0 + offset, uv1 + offset, uv2 + offset, white);
                                }
                            }
                        }
                    }
                }
            }

            if (invert)
            {
                var white = new Color32(255, 255, 255, 255);
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = pixels[i].a > 0 ? clear : white;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "UVMask";
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>元テクスチャと同じ解像度でマスクを生成する</summary>
        public static Texture2D GenerateFollowingTexture(MeshCreater meshCreater, IList<int> selectedVertices,
            Texture sourceTexture, bool invert)
        {
            var width = sourceTexture != null ? sourceTexture.width : 1024;
            var height = sourceTexture != null ? sourceTexture.height : 1024;
            return Generate(meshCreater, selectedVertices, width, height, invert);
        }

        /// <summary>PNG としてアセットに保存し、保存パスを返す</summary>
        public static string SaveToPng(Texture2D texture, string directory, string baseName)
        {
            if (texture == null) return null;

            if (string.IsNullOrEmpty(directory))
            {
                directory = "Assets/Temp";
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fileName = string.IsNullOrEmpty(baseName) ? "UVMask" : baseName;
            var path = Path.Combine(directory, fileName + "_Mask.png").Replace('\\', '/');
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var png = texture.EncodeToPNG();
            if (png == null) return null;

            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.alphaIsTransparency = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            return path;
        }

        private static bool IsInsideUnitRange(Vector2 a, Vector2 b, Vector2 c)
        {
            return a.x >= 0f && a.x <= 1f && a.y >= 0f && a.y <= 1f &&
                   b.x >= 0f && b.x <= 1f && b.y >= 0f && b.y <= 1f &&
                   c.x >= 0f && c.x <= 1f && c.y >= 0f && c.y <= 1f;
        }

        private static Vector2 GetUV(Vector2[][] uvList, int index)
        {
            if (uvList == null || index < 0 || index >= uvList.Length) return Vector2.zero;
            var arr = uvList[index];
            return (arr != null && arr.Length > 0) ? arr[0] : Vector2.zero;
        }

        private static void RasterizeTriangle(Color32[] pixels, int width, int height,
            Vector2 uv0, Vector2 uv1, Vector2 uv2, Color32 color)
        {
            var x0 = uv0.x * width;
            var y0 = uv0.y * height;
            var x1 = uv1.x * width;
            var y1 = uv1.y * height;
            var x2 = uv2.x * width;
            var y2 = uv2.y * height;

            var minU = Mathf.Min(x0, Mathf.Min(x1, x2));
            var maxU = Mathf.Max(x0, Mathf.Max(x1, x2));
            var minV = Mathf.Min(y0, Mathf.Min(y1, y2));
            var maxV = Mathf.Max(y0, Mathf.Max(y1, y2));

            if (maxU < 0f || minU > width || maxV < 0f || minV > height) return;

            var minX = Mathf.Clamp(Mathf.FloorToInt(minU), 0, width - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(maxU), 0, width - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(minV), 0, height - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(maxV), 0, height - 1);

            for (int py = minY; py <= maxY; py++)
            {
                var cy = py + 0.5f;
                var rowOffset = py * width;

                for (int px = minX; px <= maxX; px++)
                {
                    var cx = px + 0.5f;
                    if (PointInTriangle(cx, cy, x0, y0, x1, y1, x2, y2))
                    {
                        pixels[rowOffset + px] = color;
                    }
                }
            }
        }

        private static bool PointInTriangle(float px, float py,
            float x0, float y0, float x1, float y1, float x2, float y2)
        {
            var d1 = (px - x1) * (y0 - y1) - (x0 - x1) * (py - y1);
            var d2 = (px - x2) * (y1 - y2) - (x1 - x2) * (py - y2);
            var d3 = (px - x0) * (y2 - y0) - (x2 - x0) * (py - y0);

            var hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
            var hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
            return !(hasNeg && hasPos);
        }
    }
}
