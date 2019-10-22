using System;
using System.Collections.Generic;
using System.Collections.Generic.Ex;
using System.Text.RegularExpressions;
using mulova.commons;
using UnityEditor;
using LogType = UnityEngine.LogType;
using Object = UnityEngine.Object;
using System.Ex;

namespace mulova.preprocess
{
    public abstract class AssetBuildProcess : IComparable<AssetBuildProcess>
	{
        public abstract string title { get; }
        public abstract Type assetType { get; }
        public int order = int.MaxValue;
        protected abstract void Verify(string path, Object obj);
        protected abstract void Preprocess(string path, Object obj);
        protected abstract void Postprocess(string path, Object obj);

		private RegexMgr excludeExp = new RegexMgr();
		private RegexMgr includeExp = new RegexMgr();
        public static HashSet<object> globalOptions;
        private static BuildLog _log;
        public static BuildLog log
        {
            get
            {
                if (_log == null)
                {
                    _log = new BuildLog();
                }
                return _log;
            }
            set
            {
                _log = value;
            }
        }

        private static List<AssetBuildProcess> pool;

		private Regex excludePath
		{
			get
			{
				return excludeExp.exp;
			}
		}

		private Regex includePath
		{
			get
			{
				return includeExp.exp;
			}
		}

		public virtual bool IsApplicable(string path, Object obj)
		{
            if (obj != null&&!assetType.IsAssignableFrom(obj.GetType()))
            {
                return false;
            }
            if (excludePath != null&&excludePath.IsMatch(path))
            {
                return false;
            }
            if (includePath != null&&!includePath.IsMatch(path))
            {
                return false;
            }
            return true;
		}

		public void AddExcludePattern(string regexPattern)
		{
			excludeExp.AddPattern(regexPattern);
		}

		public void AddIncludePattern(string regexPattern)
		{
			includeExp.AddPattern(regexPattern);
		}

        public static void SetOptions(params object[] o)
        {
            if (o == null)
            {
                globalOptions = null;
            }
            else
            {
                if (globalOptions == null)
                {
                    globalOptions = new HashSet<object>();
                }
                globalOptions.AddAll(o);
            }
        }

        public static void AddOption(object o)
        {
            if (globalOptions == null)
            {
                globalOptions = new HashSet<object>();
            }
            globalOptions.Add(o);
        }

        public bool IsOption(object o)
        {
            if (globalOptions == null)
            {
                return false;
            }
            return globalOptions.Contains(o);
        }

        public T GetOption<T>()
        {
            if (globalOptions != null)
            {
                foreach (object option in globalOptions)
                {
                    if (option is T)
                    {
                        return (T)option;
                    }
                }
            }
            return default(T);
        }

        private static List<AssetBuildProcess> GetBuildProcessors()
		{
			// collect BuildProcessors
			if (pool == null)
			{
				pool = new List<AssetBuildProcess>();
				List<Type> bps = typeof(AssetBuildProcess).FindClasses();
				foreach (Type t in bps)
				{
					if (!t.IsAbstract)
					{
						AssetBuildProcess b = Activator.CreateInstance(t) as AssetBuildProcess;
						pool.Add(b);
					}
				}
                pool.Sort();
			}
			return pool;
		}

        public static void Process(ProcessStage stage, Object obj)
        {
            var processors = GetBuildProcessors();
            var path = AssetDatabase.GetAssetPath(obj);
            foreach (AssetBuildProcess p in processors)
            {
                if (p.IsApplicable(path, obj))
                {
                    try
                    {
                        if ((stage & ProcessStage.Verify) != 0)
                        {
                            p.Verify(path, obj);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Log(LogType.Error, $"{path}.{p.assetType.FullName}", ex, obj);
                    }
                }
            }
            foreach (AssetBuildProcess p in processors)
            {
                if (p.IsApplicable(path, obj))
                {
                    try
                    {
                        if ((stage & ProcessStage.Preprocess) != 0)
                        {
                            p.Preprocess(path, obj);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Log(LogType.Error, $"{path}.{p.assetType.FullName}", ex, obj);
                    }
                }
            }
            foreach (AssetBuildProcess p in processors)
            {
                if (p.IsApplicable(path, obj))
                {
                    try
                    {
                        if ((stage & ProcessStage.Postprocess) != 0)
                        {
                            p.Postprocess(path, obj);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Log(LogType.Error, $"{path}.{p.assetType.FullName}", ex, obj);
                    }
                }
            }
        }

        public int CompareTo(AssetBuildProcess other)
        {
            return order - other.order;
        }
    }
}
