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
            None, Verify, Preprocess, All
        }

        public const string SETTING_PATH = "Assets/Editor/PrebuildSettings.asset";

        [SerializeField]
        public Type type;

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
                    pf.RegisterValueChangedCallback(popupChanged);
                    properties.Add(pf);

                    void popupChanged(ChangeEvent<PrebuildSettings.Type> evt)
                    {
                        var t = settings.FindProperty("type");
                        t.enumValueIndex = (int)evt.newValue;
                        settings.ApplyModifiedProperties();
                    }
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Prebuild", "Verify", "Preprocess" })
            };

            return provider;
        }
    }
}