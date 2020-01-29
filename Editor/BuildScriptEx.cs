/*
using System.Collections.Generic;
using mulova.commons;
using mulova.unicore;
using UnityEditor;
using UnityEngine;

namespace mulova.build
{
    public static class BuildScriptEx {
        
        public static readonly ILog log = BuildScript.log;
        
        /// <summary>
        /// Change automatic compressed texture with alpha to ETC1 for backward compatibility 
        /// </summary>
        public static string SetAndroidTextureFormat(TextureImporterFormat format, params string[] excludes) {
            if (Platform.platform != RuntimePlatform.Android) {
                return null;
            }
            string err = EditorTraversal.ForEachAsset<Texture2D>(FileType.Image, (path,tex)=> {
                if (path.Contains("/Plugins/Android/")
                    || path.Contains("/Plugins/iOS/")
                    || path.Contains("/Editor/")
                    || excludes.Find(p=>path.Contains(p)).IsNotEmpty()) {
                    return null;
                } else if (tex == null) {
                    return "Missing texture "+path;
                }
                
                if (tex.format.HasAlpha()) {
                    TextureImporter i = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (i.textureFormat == TextureImporterFormat.AutomaticCompressed 
                        || i.textureFormat == TextureImporterFormat.AutomaticCrunched) {
                        i.textureFormat = format;
                        EditorUtility.SetDirty(tex);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                }
                return null;
            }, FileType.Image);
            AssetDatabase.SaveAssets();
            return err;
        }
        
        public static void SetAndroidFormat() {
            string err = SetAndroidTextureFormat(TextureImporterFormat.Automatic16bit, "/download/");
            if (!err.IsEmpty()) {
                log.Error(null, err);
            }
        }
        
        public static void Refresh() {
            EditorTraversal.ForEachScene(roots=> {
                foreach (Transform root in roots) {
                    foreach (TexSetter s in root.GetComponentsInChildren<TexSetter>(true)) {
                        foreach (AssetRef r in s.textures) {
                            if (r != null && !r.IsStrong()) {
                                r.reference = null;
                                EditorUtility.SetDirty(s);
                            }
                        }
                    }
                }
                return null;
            });
            EditorTraversal.ForEachAsset<GameObject>(FileType.Prefab, (path,popup)=> {
                foreach (TexSetter s in popup.GetComponentsInChildren<TexSetter>(true)) {
                    foreach (AssetRef r in s.textures) {
                        if (r != null && !r.IsStrong()) {
                            r.reference = null;
                            EditorUtility.SetDirty(s);
                        }
                    }
                }
                return null;
            });
        }
        
        public static void FindScript() {
            List<string> found = new List<string>();
            BuildScript.ForEachAsset<GameObject>(FileType.Prefab, path, popup)=> {
                foreach (TexSetter t in popup.GetComponentsInChildren<TexSetter>(true)) {
                    found.Add(t.transform.GetScenePath());
                }
                return null;
            });
            BuildScript.ForEachScene(roots=> {
                foreach (Transform t in roots) {
                    foreach (TexSetter tex in t.GetComponentsInChildren<TexSetter>(true)) {
                        found.Add(tex.transform.GetScenePath());
                    }
                }
                return null;
            });
            Debug.Log(found.Join("\n"));
        }
    }
}

*/