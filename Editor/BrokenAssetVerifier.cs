using System;
using System.Text.Ex;
using mulova.commons;
using Object = UnityEngine.Object;
using LogType = UnityEngine.LogType;

namespace mulova.build
{
    public class BrokenAssetVerifier : AssetBuildProcess
    {

        public override Type assetType => typeof(Object);
        public override string title => "Broken Asset";

        protected override void Verify(string path, Object obj)
        {
            if (obj == null)
            {
                if (path.Is(FileType.Prefab) && obj == null)
                {
                    log.Log(LogType.Error, path);
                } else if (path.Is(FileType.Asset) && obj == null)
                {
                    log.Log(LogType.Error, path);
                }
            }
        }
        
        protected override void Preprocess(string path, UnityEngine.Object obj)
        {
        }

        protected override void Postprocess(string path, Object obj)
        {
            throw new NotImplementedException();
        }
    }
}
