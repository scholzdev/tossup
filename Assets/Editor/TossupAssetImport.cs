using UnityEditor;
using UnityEngine;

namespace Tossup.EditorTools
{
    // Import settings that match how the LÖVE version loaded its assets: smooth mipmapped art, nearest-filtered
    // character portraits, uncompressed pixels, the pixel font hinted, and the music streamed.
    public sealed class TossupAssetImport : AssetPostprocessor
    {
        const string Root = "Assets/Resources/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (TextureImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            if (file.StartsWith("cursor_"))
            {
                importer.textureType = TextureImporterType.Cursor;
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                return;
            }
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = false;
            if (assetPath.StartsWith(Root + "characters/") || assetPath.StartsWith(Root + "fonts/"))
            {
                importer.mipmapEnabled = false; // portraits and baked glyphs are pixel art, drawn with nearest filtering
                importer.filterMode = FilterMode.Point;
            }
            else
            {
                importer.mipmapEnabled = true; // a 512px coin drawn small stays smooth
                importer.filterMode = FilterMode.Trilinear;
            }
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            if (assetPath.StartsWith(Root + "music/"))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = .7f;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
            }
            importer.defaultSampleSettings = settings;
        }

        void OnPreprocessAsset()
        {
            if (assetImporter is TrueTypeFontImporter font && assetPath.StartsWith(Root))
            {
                font.fontTextureCase = FontTextureCase.Dynamic;
                font.fontRenderingMode = FontRenderingMode.HintedSmooth; // FreeType with hinting, like LÖVE
                font.includeFontData = true;
            }
        }
    }
}
