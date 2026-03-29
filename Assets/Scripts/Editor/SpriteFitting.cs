using UnityEditor;
using UnityEngine;
using System.IO;

public class SpriteFitting
{
    [MenuItem("Assets/SpriteFitting", true)]
    private static bool Validate()
    {
        var obj = Selection.activeObject;
        if (obj == null) return false;
        string path = AssetDatabase.GetAssetPath(obj);
        string ext = Path.GetExtension(path).ToLower();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga";
    }

    [MenuItem("Assets/SpriteFitting")]
    private static void Execute()
    {
        var objects = Selection.objects;
        int count = 0;

        foreach (var obj in objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            string ext = Path.GetExtension(assetPath).ToLower();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".tga")
                continue;

            // Read/Write 활성화
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            bool wasReadable = true;
            if (importer != null && !importer.isReadable)
            {
                wasReadable = false;
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            string fullPath = Path.GetFullPath(assetPath);
            byte[] fileBytes = File.ReadAllBytes(fullPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!tex.LoadImage(fileBytes))
            {
                Debug.LogError($"SpriteFitting: {assetPath} 로드 실패");
                Object.DestroyImmediate(tex);
                continue;
            }

            // 투명 여백의 바운딩 박스 계산
            Color32[] pixels = tex.GetPixels32();
            int w = tex.width;
            int h = tex.height;

            int minX = w, maxX = 0, minY = h, maxY = 0;
            bool hasContent = false;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a > 10)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                        hasContent = true;
                    }
                }
            }

            if (!hasContent)
            {
                Debug.LogWarning($"SpriteFitting: {assetPath} 에 불투명 픽셀이 없습니다.");
                Object.DestroyImmediate(tex);
                continue;
            }

            int cropW = maxX - minX + 1;
            int cropH = maxY - minY + 1;

            if (cropW == w && cropH == h)
            {
                Debug.Log($"SpriteFitting: {assetPath} 이미 여백 없음 ({w}x{h})");
                Object.DestroyImmediate(tex);
                continue;
            }

            // 크롭된 텍스처 생성
            Texture2D cropped = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
            Color[] croppedPixels = tex.GetPixels(minX, minY, cropW, cropH);
            cropped.SetPixels(croppedPixels);
            cropped.Apply();

            // PNG로 저장 (원본 덮어쓰기)
            byte[] pngData = cropped.EncodeToPNG();
            File.WriteAllBytes(fullPath, pngData);

            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(cropped);

            // Reimport
            if (importer != null && !wasReadable)
            {
                importer.isReadable = false;
            }
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"SpriteFitting: {assetPath} ({w}x{h}) -> ({cropW}x{cropH}) 완료");
            count++;
        }

        if (count > 0)
            AssetDatabase.Refresh();

        Debug.Log($"SpriteFitting: 총 {count}개 이미지 처리 완료");
    }
}
