using UnityEngine;
using mulova.commons;
using System.Text.Ex;
using mulova.preprocess;
using System;
using Object = UnityEngine.Object;

namespace mulova.build
{
    public class BrokenAssetVerifier : AssetBuildProcess
    {

        public override Type assetType => typeof(Object);
        public override string title => "Broken Asset";

        protected override void VerifyAsset(string path, Object obj)
        {
            if (obj == null)
            {
                if (path.Is(FileType.Prefab) && obj == null)
                {
                    log.Log(path);
                } else if (path.Is(FileType.Asset) && obj == null)
                {
                    log.Log(path);
                }
            }
        }
        
        protected override void PreprocessAsset(string path, UnityEngine.Object obj)
        {
        }
    }
}
