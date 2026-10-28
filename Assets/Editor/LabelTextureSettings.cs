using UnityEditor;
using UnityEngine;

/// <summary>Ajusta automaticamente las texturas de Resources/Labels (nitidez en las etiquetas, transparencia en manchas).</summary>
public class LabelTextureSettings : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace("\\", "/").Contains("/Resources/Labels/")) return;
        TextureImporter ti = (TextureImporter)assetImporter;
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);

        ti.textureType = TextureImporterType.Default;
        ti.anisoLevel = 8;
        ti.mipmapEnabled = true;
        ti.maxTextureSize = 2048;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;

        if (file.StartsWith("stain_") || file == "handprint" || file == "dust")
        {
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
        }
        if (file == "price_strip" || file == "floor_tile" || file == "wall" || file == "ceiling_tile" || file == "asphalt")
            ti.wrapMode = TextureWrapMode.Repeat;
    }
}
