using System;
using System.Collections.Generic;
using System.Collections.Generic.Ex;
using System.Text.Ex;
using mulova.commons;
using LogType = UnityEngine.LogType;
using Object = UnityEngine.Object;

namespace mulova.preprocess
{
    public class BuildLog
	{
        public class Entry
        {
            public readonly Object obj;
            public readonly Exception error;
            public readonly string msg;
            public readonly LogType logType;

            public Entry(LogType logType, string msg = null, Exception err = null, Object obj = null)
            {
                this.logType = logType;
                this.msg = msg;
                this.error = err;
                this.obj = obj;
            }

            public override string ToString()
            {
                if (error != null)
                {
                    if (msg != null)
                    {
                        return string.Join(msg, "\n", error.ToString());
                    } else
                    {
                        return error.ToString();
                    }
                } else
                {
                    if (msg != null)
                    {
                        return msg;
                    } else
                    {
                        return "";
                    }
                }
            }

            public override int GetHashCode()
            {
                if (msg != null)
                {
                    return msg.GetHashCode();
                }
                else
                {
                    return error.GetHashCode();
                }
            }
        }

        public readonly HashSet<Entry> logs = new HashSet<Entry>();

        public void Clear()
        {
            logs.Clear();
        }

        public void Log(LogType logType, object msg = null, Exception err = null, Object obj = null)
		{
            if (msg == null && err == null)
            {
                return;
            }
			logs.Add(new Entry(logType, msg?.ToString(), err, obj));
		}

        public void LogError(Exception err = null, Object obj = null)
        {
            Log(LogType.Error, err: err, obj: obj);
        }

        public void Log(object msg = null, Object obj = null)
        {
            Log(LogType.Log, msg: msg, obj: obj);
        }

        public override string ToString()
        {
			if (!logs.IsEmpty())
			{
				return logs.Join("\n");
			} else
			{
				return string.Empty;
			}
		}

        public bool isEmpty
        {
            get
            {
                return logs.IsEmpty();
            }
        }

        public void Merge(BuildLog other)
        {
            this.logs.AddAll(other.logs);
        }
    }
}
