using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace mulova.preprocess
{
    public class PrebuildSettings : ScriptableObject
    {
        public enum Type
        {
            None, Verify, Preprocess
        }
        
        public const string SETTING_PATH = "Assets/Editor/PrebuildSettings.asset";
        
        [SerializeField]
        private Type type;
        
        public static PrebuildSettings Get()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PrebuildSettings>(SETTING_PATH);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PrebuildSettings>();
                settings.type = Type.None;
                var dir = Directory.GetParent(SETTING_PATH);
                if (!dir.Exists)
                {
                    dir.Create();
                }
                AssetDatabase.CreateAsset(settings, SETTING_PATH);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
        
        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(Get());
        }
    }

    /*
// Register a SettingsProvider using UIElements for the drawing framework:
    static class PrebuildSettingsUIElementsRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreatePrebuildSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
            var provider = new SettingsProvider("Project/PrebuildSettings", SettingsScope.Project)
            {
                label = "Prebuild Settings",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = (searchContext, rootElement) =>
                {
                    var settings = PrebuildSettings.GetSerializedSettings();
                    
                    // rootElement is a VisualElement. If you add any children to it, the OnGUI function
                    // isn't called because the SettingsProvider uses the UIElements drawing framework.
                    //rootElement.AddStyleSheetPath("Assets/Editor/settings_ui.uss");
                    var title = new Label()
                    {
                        text = "Prebuild Settings"
                    };
                    title.AddToClassList("title");
                    rootElement.Add(title);
                    
                    var properties = new VisualElement()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Column
                        }
                    };
                    properties.AddToClassList("property-list");
                    rootElement.Add(properties);
                    
                    var options = ((PrebuildSettings.Type[])Enum.GetValues(typeof(PrebuildSettings.Type))).ToList();
                    var pf = new PopupField<PrebuildSettings.Type>(options, settings.FindProperty("type").enumValueIndex);
                    pf.AddToClassList("property-value");
                    properties.Add(pf);
                },
                
                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Prebuild", "Verify", "Preprocess" })
            };
            
            return provider;
        }
    }
    */

    // Create PrebuildSettingsProvider by deriving from SettingsProvider:
    class PrebuildSettingsProvider : SettingsProvider
    {
        private SerializedObject setting;
        
        class Styles
        {
            public static GUIContent type = new GUIContent("Type");
        }
        
        const string SETTING_PATH = "Assets/Editor/PrebuildSettings.asset";
        public PrebuildSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope, new[] { "Prebuild", "Verify", "Preprocess" }) { }
        
        public static bool IsSettingsAvailable()
        {
            return File.Exists(SETTING_PATH);
        }
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // This function is called when the user clicks on the Prebuild element in the Settings window.
            setting = PrebuildSettings.GetSerializedSettings();
        }
        
        public override void OnGUI(string searchContext)
        {
            // Use IMGUI to display UI:
            setting.Update();
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(setting.FindProperty("type"), Styles.type);
                if (change.changed)
                {
                    setting.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                }
            }
        }
        
        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            if (IsSettingsAvailable())
            {
                var provider = new PrebuildSettingsProvider("Project/Prebuild Settings", SettingsScope.Project);
                
                // Automatically extract all keywords from the Styles.
                //provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
                return provider;
            }
            
            // Settings Asset doesn't exist yet; no need to display anything in the Settings window.
            return null;
        }
    }
}