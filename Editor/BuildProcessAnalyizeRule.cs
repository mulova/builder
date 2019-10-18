using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace mulova.preprocess
{
    public class BuildPreprocessAnalyzeRule : AnalyzeRule
    {
        public override string ruleName => "Verify Attributes";
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
                g.GatherAllAssets(entry, true, true, true);
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
                            r.resultName = $"[group]{g.name} [address]{e.address} [log] {l.ToString()}";
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
            AnalyzeWindow.RegisterNewRule<BuildPreprocessAnalyzeRule>();
        }
    }
}
