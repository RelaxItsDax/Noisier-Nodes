using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Noise {
    [Serializable]
    public enum NoiseType {
        PERLIN_2D,
        PERLIN_3D,
        SIMPLEX_2D,
        SIMPLEX_3D,
        SIMPLEX_2D_GRADIENT,
        SIMPLEX_3D_GRADIENT,
        VORONOI_2D,
        VORONOI_3D,
        VORONOI_2D_CELLS,
        VORONOI_3D_CELLS,
        VORONOI_2D_PERIODIC,
        VORONOI_3D_PERIODIC,
        VORONOI_2D_PERIODIC_CELLS,
        VORONOI_3D_PERIODIC_CELLS
    }

    [Serializable]
    public struct NoiseGeneratorEntry {
        public NoiseType type;
        public ComputeShader shader;
    }

    [CreateAssetMenu(menuName = "Noisier Nodes/Noise Generator")]
    public class NoiseGeneratorSO : ScriptableObject {
        public List<NoiseGeneratorEntry> noiseGenerators;
        [Header("Output Settings")]
        public NoiseType type;
        public string fileName = "Shaders/Noise/NoiseGeneratorOutput";
        public int height;
        public int width;
        public int depth = 1;
        public bool encodeToPng;
        public string seed;
        
        private readonly float randomBounds = 1000;

        [Header("Perlin/Simplex Inputs")] 
        public float scale;
        [Header("Voronoi Inputs")]
        public float cellDensity;
        public float angleOffset;
        [Header("Output")] 
        public Texture outputTexture;


        private static readonly List<NoiseType> Types2D = new() {
            NoiseType.PERLIN_2D, NoiseType.SIMPLEX_2D, NoiseType.SIMPLEX_2D_GRADIENT, NoiseType.VORONOI_2D,
            NoiseType.VORONOI_2D_CELLS, NoiseType.VORONOI_2D_PERIODIC, NoiseType.VORONOI_2D_PERIODIC_CELLS
        };

        private static readonly List<NoiseType> Types3D = new() {
            NoiseType.PERLIN_3D, NoiseType.SIMPLEX_3D, NoiseType.SIMPLEX_3D_GRADIENT, NoiseType.VORONOI_3D,
            NoiseType.VORONOI_3D_CELLS, NoiseType.VORONOI_3D_PERIODIC, NoiseType.VORONOI_3D_PERIODIC_CELLS
        };


        public void Generate() {
            foreach (NoiseGeneratorEntry entry in noiseGenerators) {
                if (entry.type == type) {
                    if (!entry.shader) {
                        Debug.LogError("Entry of type " + type + " did not have an associated shader!");
                        return;
                    }

                    RenderTextureDescriptor rtd =
                        new RenderTextureDescriptor(width, height, GraphicsFormat.R8G8B8A8_UNorm, 0, 0)
                            {
                                msaaSamples = 1
                            };

                    if (Types2D.Contains(entry.type)) {
                        rtd.dimension = TextureDimension.Tex2D;
                    }
                    else if (Types3D.Contains(entry.type)) {
                        rtd.volumeDepth = depth;
                        rtd.dimension = TextureDimension.Tex3D;
                    }

                    RenderTexture output = new RenderTexture(rtd) {
                        enableRandomWrite = true
                    };
                    output.Create();

                    Unity.Mathematics.Random r = new Unity.Mathematics.Random();
                    r.InitState((uint)seed.GetHashCode());
                    r.NextFloat(-randomBounds, randomBounds);

                    entry.shader.SetTexture(0, Shader.PropertyToID("result"), output);
                    entry.shader.SetVector(Shader.PropertyToID("size"), new Vector4(width, height, depth, 0));
                    entry.shader.SetVector(Shader.PropertyToID("offset"),
                        new Vector4(r.NextFloat(-randomBounds, randomBounds), r.NextFloat(-randomBounds, randomBounds),
                            r.NextFloat(-randomBounds, randomBounds), r.NextFloat(-randomBounds, randomBounds)));
                    entry.shader.SetFloat(Shader.PropertyToID("scale"), scale);
                    entry.shader.SetFloat(Shader.PropertyToID("cellDensity"), cellDensity);
                    entry.shader.SetFloat(Shader.PropertyToID("angleOffset"), angleOffset);
                    entry.shader.Dispatch(0,
                        Mathf.CeilToInt(width / 8.0f),
                        Mathf.CeilToInt(height / 8.0f),
                        Mathf.CeilToInt(depth / 8.0f));

                    if (output.dimension == TextureDimension.Tex2D) {
                        if (encodeToPng) {
                            SaveRT2DToTexture2DAsset(output, fileName, true);
                            outputTexture = null;
                        }
                        else outputTexture = SaveRT2DToTexture2DAsset(output, fileName, false);
                    }
                    if (output.dimension == TextureDimension.Tex3D) outputTexture = SaveRT3DToTexture3DAsset(output, fileName);
                    return;
                }
            }

            Debug.LogError("Could not find generator with type " + type + "!");
        }

        private static Texture2D SaveRT2DToTexture2DAsset(RenderTexture rt2D, string fileName, bool encodeToPng) {
            int width = rt2D.width, height = rt2D.height;
            NativeArray<byte> arr = new NativeArray<byte>(width * height * 4, Allocator.Persistent, // * 4 because of the color format.
                NativeArrayOptions.UninitializedMemory);
            NativeArray<byte> arrRef = arr;
            Texture2D output = new Texture2D(width, height, rt2D.graphicsFormat, TextureCreationFlags.None);
            AsyncGPUReadback.RequestIntoNativeArray(ref arr, rt2D, 0, (_) => {
                output.SetPixelData(arrRef, 0);
                output.Apply(updateMipmaps: false);
                if (encodeToPng) {
                    byte[] png = output.EncodeToPNG();
                    string path = $"Assets/{fileName}.png";
                    File.WriteAllBytes(path, png);
                    AssetDatabase.ImportAsset(path);
                    AssetDatabase.Refresh();
                    Debug.Log($"PNG saved to: " + path);
                    output.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                    DestroyImmediate(output, true);
                }
                else {
                    string path = $"Assets/{fileName}.asset";
                    AssetDatabase.CreateAsset(output, path);
                    AssetDatabase.SaveAssetIfDirty(output);
                    output.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                    Debug.Log($"Texture2D saved to: " + path);
                }

                arrRef.Dispose();
                rt2D.Release();
            });
            return output;
        }

        private static Texture3D SaveRT3DToTexture3DAsset(RenderTexture rt3D, string fileName) {
            int width = rt3D.width, height = rt3D.height, depth = rt3D.volumeDepth;
            NativeArray<byte> arr = new NativeArray<byte>(width * height * depth * 4, Allocator.Persistent, // * 4 because of the color format.
                NativeArrayOptions.UninitializedMemory);
            NativeArray<byte> arrRef = arr;
            Texture3D output = new Texture3D(width, height, depth, rt3D.graphicsFormat, TextureCreationFlags.None);
            AsyncGPUReadback.RequestIntoNativeArray(ref arr, rt3D, 0, (_) => {
                output.SetPixelData(arrRef, 0);
                output.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                AssetDatabase.CreateAsset(output, $"Assets/{fileName}.asset");
                AssetDatabase.SaveAssetIfDirty(output);
                arrRef.Dispose();
                rt3D.Release();
            });
            Debug.Log($"Texture3D saved to: Assets/{fileName}.asset");
            return output;
        }
    }


    [CustomEditor(typeof(NoiseGeneratorSO))]
    public class MyDataSOEditor : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            NoiseGeneratorSO noiseGen = (NoiseGeneratorSO)target;

            if (GUILayout.Button("Generate")) {
                noiseGen.Generate();
                EditorUtility.SetDirty(noiseGen);
            }
        }
    }
}