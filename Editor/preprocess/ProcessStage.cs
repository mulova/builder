using System;

namespace mulova.build
{
    [Flags]
    public enum ProcessStage
    {
        Verify = 1 << 0,
        Preprocess = 1 << 1,
        Postprocess = 1 << 2
    }
}
