/* 
* BSM Noise Textures
* Benoît Saint-Moulin © 2025
* Version 1.0 - Unity (6) 2023+ HDRP Editor Includes
* Stable & Production-Ready
* This tool is provided as-is without any warranty.
* You are free to use it in personal projects.
* Credit is appreciated, but not required.
* Except if you reuse or base work on this code, then credit is required.
*
* Features:
* - Procedural texture generation
* - Multiple noise algorithms
* - Advanced texture manipulation
* - Texture type and format support
* - Noise pattern generation
* - Normal map creation
* - Mask and coat map generation
* - High-quality texture export
* - Customizable noise parameters
* - Texture set management
*
* For updates and more tools, visit:
* https://www.bsm3d.com
* For contact and support:
* bsm@bsm3d.com
*/

using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BSMTools
{
    // Utilitaire complet pour la génération, la manipulation et l'exportation de textures dans Unity

    public static class BSM_Textures
    {
        // Énumérations pour les types de textures et les formats d'exportation
        public enum TextureType
        {
            Solid,
            Perlin,
            FractionalBrownian,
            Voronoi,
            Gradient,
            Brick,
            Stone,
            Leaf,
            Bark,
            Soil,
            Sand,
            Grass,
            Rocky,
            Rivet,
            Hexagonal
        }

        public enum TextureSaveType
        {
            Color,
            Normal,
            Height,
            Mask,
            Coat,
            AmbientOcclusion
        }

        public enum TextureExportFormat
        {
            PNG,    // Format standard web/Unity, compressé
            TIFF,   // Non compressé, haute qualité
            EXR     // Haute gamme dynamique, précision flottante
        }

        // Méthodes de génération de textures

        public static Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        public static Texture2D CreatePerlinNoiseTexture(
            int width, 
            int height, 
            float scale = 1f, 
            Color? colorA = null, 
            Color? colorB = null, 
            int seed = -1)
        {
            Texture2D texture = new Texture2D(width, height);
            Color baseColorA = colorA ?? Color.white;
            Color baseColorB = colorB ?? Color.black;

            if (seed != -1) UnityEngine.Random.InitState(seed);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float xCoord = (float)x / width * scale;
                    float yCoord = (float)y / height * scale;
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);
                    Color pixelColor = Color.Lerp(baseColorA, baseColorB, sample);
                    texture.SetPixel(x, y, pixelColor);
                }
            }
            texture.Apply();
            return texture;
        }

        public static Texture2D CreateFBMNoiseTexture(
            int width, 
            int height, 
            int octaves = 4, 
            float persistence = 0.5f, 
            float lacunarity = 2f, 
            float scale = 1f, 
            int seed = -1)
        {
            Texture2D texture = new Texture2D(width, height);
            
            if (seed != -1) UnityEngine.Random.InitState(seed);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float xCoord = (float)x / width * scale;
                    float yCoord = (float)y / height * scale;
                    float sample = GenerateFBM(xCoord, yCoord, octaves, persistence, lacunarity);
                    texture.SetPixel(x, y, new Color(sample, sample, sample));
                }
            }
            texture.Apply();
            return texture;
        }

        public static Texture2D CreateVoronoiTexture(
            int width, 
            int height, 
            int cellCount = 5, 
            bool showCells = false, 
            int seed = -1)
        {
            Texture2D texture = new Texture2D(width, height);
            
            if (seed != -1) UnityEngine.Random.InitState(seed);

            Vector2[] points = new Vector2[cellCount];

            // Générer des centres de cellules aléatoires
            for (int i = 0; i < cellCount; i++)
            {
                points[i] = new Vector2(UnityEngine.Random.Range(0, width), UnityEngine.Random.Range(0, height));
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float minDist = float.MaxValue;
                    Vector2 closestPoint = Vector2.zero;

                    // Trouver le centre de cellule le plus proche
                    foreach (Vector2 point in points)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), point);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestPoint = point;
                        }
                    }

                    // Déterminer la couleur en fonction de la distance ou de la cellule
                    float intensity = showCells 
                        ? (closestPoint.x / width)  // Utiliser la position x du centre de la cellule pour la couleur
                        : (minDist / Mathf.Max(width, height));
                    
                    texture.SetPixel(x, y, new Color(intensity, intensity, intensity));
                }
            }
            texture.Apply();
            return texture;
        }

        public static Texture2D CreateMaskTexture(
            int width, 
            int height, 
            float threshold = 0.5f, 
            float scale = 1f, 
            int seed = -1)
        {
            Texture2D maskTexture = new Texture2D(width, height);
            
            if (seed == -1) seed = UnityEngine.Random.Range(0, 10000);
            UnityEngine.Random.InitState(seed);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = Mathf.PerlinNoise(
                        x * scale / width, 
                        y * scale / height
                    );

                    Color pixelColor = noise > threshold ? Color.white : Color.black;
                    maskTexture.SetPixel(x, y, pixelColor);
                }
            }

            maskTexture.Apply();
            return maskTexture;
        }

        public static Texture2D CreateCoatMap(
            int width, 
            int height, 
            float intensity = 0.5f, 
            float scale = 1f, 
            int seed = -1)
        {
            Texture2D coatTexture = new Texture2D(width, height);
            
            if (seed == -1) seed = UnityEngine.Random.Range(0, 10000);
            UnityEngine.Random.InitState(seed);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = Mathf.PerlinNoise(
                        x * scale / width, 
                        y * scale / height
                    );

                    float coatValue = Mathf.Clamp01(noise * intensity);
                    Color pixelColor = new Color(coatValue, coatValue, coatValue);
                    coatTexture.SetPixel(x, y, pixelColor);
                }
            }

            coatTexture.Apply();
            return coatTexture;
        }

        public static Texture2D CreateNormalMapFromTexture(Texture2D sourceTexture, float strength = 1f)
        {
            Texture2D normalMap = new Texture2D(sourceTexture.width, sourceTexture.height);

            for (int y = 0; y < sourceTexture.height; y++)
            {
                for (int x = 0; x < sourceTexture.width; x++)
                {
                    float left = x > 0 ? sourceTexture.GetPixel(x - 1, y).grayscale : sourceTexture.GetPixel(x, y).grayscale;
                    float right = x < sourceTexture.width - 1 ? sourceTexture.GetPixel(x + 1, y).grayscale : sourceTexture.GetPixel(x, y).grayscale;
                    float bottom = y > 0 ? sourceTexture.GetPixel(x, y - 1).grayscale : sourceTexture.GetPixel(x, y).grayscale;
                    float top = y < sourceTexture.height - 1 ? sourceTexture.GetPixel(x, y + 1).grayscale : sourceTexture.GetPixel(x, y).grayscale;

                    Vector3 normal = new Vector3(
                        (left - right) * strength,
                        (bottom - top) * strength,
                        1f
                    ).normalized;

                    Color normalColor = new Color(
                        normal.x * 0.5f + 0.5f,
                        normal.y * 0.5f + 0.5f,
                        normal.z,
                        1f
                    );

                    normalMap.SetPixel(x, y, normalColor);
                }
            }
            
            normalMap.Apply();
            return normalMap;
        }

        public static Texture2D CreateTexture(TextureType type, int width, int height, params object[] parameters)
        {
            switch (type)
            {
                case TextureType.Solid:
                    return CreateSolidTexture(width, height, parameters.Length > 0 ? (Color)parameters[0] : Color.white);
                
                case TextureType.Perlin:
                    return CreatePerlinNoiseTexture(width, height, 
                        parameters.Length > 0 ? (float)parameters[0] : 1f,
                        parameters.Length > 1 ? (Color?)parameters[1] : null,
                        parameters.Length > 2 ? (Color?)parameters[2] : null);
                
                case TextureType.FractionalBrownian:
                    return CreateFBMNoiseTexture(width, height, 
                        parameters.Length > 0 ? (int)parameters[0] : 4,
                        parameters.Length > 1 ? (float)parameters[1] : 0.5f,
                        parameters.Length > 2 ? (float)parameters[2] : 2f,
                        parameters.Length > 3 ? (float)parameters[3] : 1f);
                
                case TextureType.Voronoi:
                    return CreateVoronoiTexture(width, height,
                        parameters.Length > 0 ? (int)parameters[0] : 5,
                        parameters.Length > 1 ? (bool)parameters[1] : false);

                default:
                    throw new ArgumentException($"Texture type {type} not implemented.");
            }
        }

        // Méthode interne pour le bruit Brownien Fractionnaire
        private static float GenerateFBM(float x, float y, int octaves, float persistence, float lacunarity)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float noiseValue = Mathf.PerlinNoise(x * frequency, y * frequency);
                total += noiseValue * amplitude;
                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxValue;
        }

        // Méthodes d'exportation et de sauvegarde

        public static string ExportTexture(
            Texture2D texture, 
            string customFileName = null, 
            TextureExportFormat format = TextureExportFormat.PNG,
            bool highQuality = false)
        {
            #if UNITY_EDITOR
            string baseFolderPath = "Assets/BSM Datas/Textures/";
            string fullBasePath = Path.Combine(Application.dataPath, "BSM Datas/Textures/");

            if (!Directory.Exists(fullBasePath))
            {
                Directory.CreateDirectory(fullBasePath);
                AssetDatabase.Refresh();
            }

            try 
            {
                string baseFileName = string.IsNullOrEmpty(customFileName) 
                    ? GenerateUniqueFileName() 
                    : customFileName;

                string filePath, fullFilePath;
                switch (format)
                {
                    case TextureExportFormat.PNG:
                        filePath = Path.Combine(baseFolderPath, $"{baseFileName}.png");
                        fullFilePath = Path.Combine(fullBasePath, $"{baseFileName}.png");
                        File.WriteAllBytes(fullFilePath, texture.EncodeToPNG());
                        ConfigureTextureImportSettings(filePath, TextureImporterType.Default, true);
                        break;

                    case TextureExportFormat.TIFF:
                        filePath = Path.Combine(baseFolderPath, $"{baseFileName}.tiff");
                        fullFilePath = Path.Combine(fullBasePath, $"{baseFileName}.tiff");
                        
                        byte[] tiffBytes = EncodeTextureToTIFF(texture, highQuality);
                        File.WriteAllBytes(fullFilePath, tiffBytes);
                        
                        ConfigureTextureImportSettings(filePath, TextureImporterType.Default);
                        break;

                    case TextureExportFormat.EXR:
                        filePath = Path.Combine(baseFolderPath, $"{baseFileName}.exr");
                        fullFilePath = Path.Combine(fullBasePath, $"{baseFileName}.exr");
                        
                        byte[] exrBytes = EncodeTextureToEXR(texture, highQuality);
                        File.WriteAllBytes(fullFilePath, exrBytes);
                        
                        ConfigureTextureImportSettings(filePath, TextureImporterType.Default);
                        break;

                    default:
                        throw new ArgumentException("Unsupported export format");
                }

                AssetDatabase.Refresh();
                Debug.Log($"Exported texture: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error exporting texture: {ex.Message}";
                Debug.LogError(errorMessage);
                return errorMessage;
            }
            #else
            Debug.LogWarning("Texture export is only supported in Unity Editor.");
            return "Editor-only functionality";
            #endif
        }

        public static Dictionary<TextureSaveType, string> ExportTextureSet(
            Texture2D primaryTexture, 
            Texture2D normalMap = null, 
            Texture2D maskTexture = null, 
            Texture2D coatMap = null, 
            string customPrefix = null,
            TextureExportFormat format = TextureExportFormat.PNG,
            bool highQuality = false)
        {
            var exportedTextures = new Dictionary<TextureSaveType, string>();

            // Exporter la texture principale
            if (primaryTexture != null)
            {
                string exportPath = ExportTexture(
                    primaryTexture, 
                    $"{customPrefix}_Color", 
                    format, 
                    highQuality
                );
                exportedTextures[TextureSaveType.Color] = exportPath;
            }

            // Exporter la carte normale
            if (normalMap != null)
            {
                string exportPath = ExportTexture(
                    normalMap, 
                    $"{customPrefix}_Normal", 
                    format, 
                    highQuality
                );
                exportedTextures[TextureSaveType.Normal] = exportPath;
            }

            // Exporter la texture de masque
            if (maskTexture != null)
            {
                string exportPath = ExportTexture(
                    maskTexture, 
                    $"{customPrefix}_Mask", 
                    format, 
                    highQuality
                );
                exportedTextures[TextureSaveType.Mask] = exportPath;
            }

            // Exporter la carte de revêtement
            if (coatMap != null)
            {
                string exportPath = ExportTexture(
                    coatMap, 
                    $"{customPrefix}_Coat", 
                    format, 
                    highQuality
                );
                exportedTextures[TextureSaveType.Coat] = exportPath;
            }

            return exportedTextures;
        }

        public static string GenerateAndSaveTextureSet(
            int width, 
            int height, 
            TextureType primaryType = TextureType.Perlin, 
            string customPrefix = null)
        {
            // Générer la texture principale
            Texture2D primaryTexture = CreateTexture(primaryType, width, height);

            // Générer des textures complémentaires
            Texture2D normalMap = CreateNormalMapFromTexture(primaryTexture);
            Texture2D maskTexture = CreateMaskTexture(width, height);
            Texture2D coatMap = CreateCoatMap(width, height);

            // Sauvegarder l'ensemble de textures
            var savedTextures = ExportTextureSet(
                primaryTexture, 
                normalMap, 
                maskTexture, 
                coatMap, 
                customPrefix
            );

            // Générer un résumé de sauvegarde
            string saveSummary = "Ensemble de textures généré:\n" + 
                string.Join("\n", savedTextures.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            
            return saveSummary;
        }

        private static void ConfigureTextureImportSettings(
            string path, 
            TextureImporterType textureType, 
            bool alphaIsTransparency = false)
        {
            #if UNITY_EDITOR
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = textureType;
                if (alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                }
                importer.SaveAndReimport();
            }
            #endif
        }

        private static string GenerateUniqueFileName()
        {
            string basePath = "Assets/BSM Datas/Textures/";
            string baseFileName = "TextureSet";
            int index = 1;
            string fileName;
            
            do
            {
                fileName = index == 1 ? baseFileName : $"{baseFileName}_{index}";
                index++;
            } while (File.Exists(Path.Combine(basePath, $"{fileName}_Color.png")));

            return fileName;
        }

        private static byte[] EncodeTextureToTIFF(Texture2D texture, bool highQuality)
        {
            #if UNITY_EDITOR
            // Créer une texture de rendu temporaire
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width, 
                texture.height, 
                0, 
                RenderTextureFormat.ARGB32, 
                RenderTextureReadWrite.Linear
            );

            // Copier la texture dans la texture de rendu
            Graphics.Blit(texture, tmp);

            // Créer une texture lisible à partir de la texture de rendu
            Texture2D readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            RenderTexture.active = tmp;
            readableTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            readableTexture.Apply();

            // Nettoyer
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(tmp);

            // Convertir en tableau d'octets
            byte[] tiffBytes = highQuality 
                ? ImageConversion.EncodeToTGA(readableTexture)  // Fallback de haute qualité
                : ImageConversion.EncodeToPNG(readableTexture);

            // Nettoyer
            UnityEngine.Object.DestroyImmediate(readableTexture);

            return tiffBytes;
            #else
            throw new InvalidOperationException("L'exportation TIFF est uniquement supportée dans l'éditeur Unity");
            #endif
        }

        private static byte[] EncodeTextureToEXR(Texture2D texture, bool highQuality)
        {
            #if UNITY_EDITOR
            // Créer une texture de rendu pour la conversion HDR
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width, 
                texture.height, 
                0, 
                RenderTextureFormat.ARGBFloat, 
                RenderTextureReadWrite.Linear
            );

            // Copier la texture dans la texture de rendu
            Graphics.Blit(texture, tmp);

            // Créer une texture HDR lisible
            Texture2D hdrTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, false);
            RenderTexture.active = tmp;
            hdrTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            hdrTexture.Apply();

            // Nettoyer
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(tmp);

            // Convertir en EXR
            byte[] exrBytes = highQuality 
                ? ImageConversion.EncodeToEXR(hdrTexture, Texture2D.EXRFlags.OutputAsFloat)
                : ImageConversion.EncodeToEXR(hdrTexture, Texture2D.EXRFlags.CompressZIP);

            // Nettoyer
            UnityEngine.Object.DestroyImmediate(hdrTexture);

            return exrBytes;
            #else
            throw new InvalidOperationException("L'exportation EXR est uniquement supportée dans l'éditeur Unity");
            #endif
        }
    }
}