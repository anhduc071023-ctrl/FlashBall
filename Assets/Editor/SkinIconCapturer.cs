#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Công cụ Editor giúp chụp lại toàn bộ 6 ảnh đại diện Skin (Icon_Skin_*.png) trực tiếp từ Player.prefab trong Unity URP.
/// </summary>
public static class SkinIconCapturer
{
    [MenuItem("Tools/FlashBall/Capture In-Game Skin Icons")]
    public static void CaptureAllSkinIcons()
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefaps/Player.prefab");
        if (playerPrefab == null)
        {
            Debug.LogError("[SkinIconCapturer] Khong tim thay Assets/Prefaps/Player.prefab!");
            return;
        }

        string outDir = Path.Combine(Application.dataPath, "Resources/UI");
        Directory.CreateDirectory(outDir);

        SkinDefinition[] all = SkinCatalog.All;
        int captured = 0;

        for (int i = 0; i < all.Length; i++)
        {
            SkinDefinition def = all[i];
            Texture2D tex = SkinPortraitRenderer.RenderSingleSkinToTexture(playerPrefab, def.type, 512);
            if (tex == null) continue;

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string filePath = Path.Combine(outDir, def.iconResource + ".png");
            File.WriteAllBytes(filePath, png);
            captured++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[SkinIconCapturer] Da chup & luu {captured}/{all.Length} icon ngoai trang giong 100% gameplay!");
    }
}
#endif
