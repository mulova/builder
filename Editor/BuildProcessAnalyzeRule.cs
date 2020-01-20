#if UNITY_2019_3_OR_NEWER
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using LogType = UnityEngine.LogType;

namespace mulova.preprocess
{
    public class BuildProcessAnalyzeRule : AnalyzeRule
    {
        public override string ruleName => "VerifyAttribute and ComponentBuildProcess";
        public override bool CanFix { get => false; }
        
        public override void ClearAnalysis()
        {
            base.ClearAnalysis();
        }
        
        public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            var results = new List<AnalyzeResult>();
            var log = new BuildLog();
            AssetBuildProcess.log = log;
            ComponentBuildProcess.log = log;
            foreach (var g in settings.groups)
            {
                var entry = new List<AddressableAssetEntry>();
#if UNITY_2019_1_OR_NEWER
                g.GatherAllAssets(entry, true, true);
#else
                g.GatherAllAssets(entry, true, true, false);
#endif
                foreach (var e in entry)
                {
                    Object a = AssetDatabase.LoadAssetAtPath<Object>(e.AssetPath);
                    AssetBuildProcess.Process(ProcessStage.Verify, a);
                    ComponentBuildProcess.Process(ProcessStage.Verify, a);
                    if (!log.isEmpty)
                    {
                        foreach (var l in log.logs)
                        {
                            var r = new AnalyzeResult();
                            r.resultName = $"[GROUP]{g.name}  [ADDR]{e.address}  [LOG]{l.ToString()}";
                            switch (l.logType)
                            {
                                case LogType.Error:
                                    r.severity = MessageType.Error;
                                    break;
                                case LogType.Assert:
                                    r.severity = MessageType.Error;
                                    break;
                                case LogType.Warning:
                                    r.severity = MessageType.Warning;
                                    break;
                                case LogType.Log:
                                    r.severity = MessageType.Info;
                                    break;
                                case LogType.Exception:
                                    r.severity = MessageType.Error;
                                    break;
                            }
                            results.Add(r);
                        }
                        log.Clear();
                    }
                }
            }
            AssetBuildProcess.log = null;
            ComponentBuildProcess.log = null;

            return results;
        }
    }

    [InitializeOnLoad]
    class RegisterMyRule
    {
        static RegisterMyRule()
        {
            AnalyzeWindow.RegisterNewRule<BuildProcessAnalyzeRule>();
        }
    }
}
#endif