using System.Collections.Generic;
using System.Collections.Generic.Ex;
using System.Text.Ex;

namespace mulova.preprocess
{
    public class BuildLog
	{
        //public static readonly BuildLog inst = new BuildLog();

		private HashSet<string> errors = new HashSet<string>();

        public void Clear()
        {
            errors.Clear();
        }

        public void Log(object msg)
		{
            if (msg is string m && m.IsEmpty())
            {
                return;
            }
            if (msg != null)
			{
				errors.Add(msg.ToString());
			}
		}

        public void LogFormat(string format, params object[] param)
		{
			errors.Add(string.Format(format, param));
		}

        public override string ToString()
        {
			if (!errors.IsEmpty())
			{
				return errors.Join("\n");
			} else
			{
				return string.Empty;
			}
		}
	}
}
