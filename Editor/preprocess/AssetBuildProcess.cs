using System;
using System.Collections.Generic;
using System.Collections.Generic.Ex;
using System.Text.Ex;
using System.Text.RegularExpressions;
using mulova.commons;
using mulova.unicore;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace mulova.preprocess
{
    public abstract class AssetBuildProcess
	{
        public abstract string title { get; }
        public abstract Type assetType { get; }
        protected abstract void VerifyAsset(string path, Object obj);
        protected abstract void PreprocessAsset(string path, Object obj);
        protected abstract void PostprocessAsset(string path, Object obj);

		private RegexMgr excludeExp = new RegexMgr();
		private RegexMgr includeExp = new RegexMgr();
        protected static readonly BuildLog log = new BuildLog();
		private static object[] globalOptions;

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

		public bool IsOption(object o)
		{
			if (globalOptions == null)
			{
				return false;
			}
			foreach (object option in globalOptions)
			{
				if (option == o)
				{
					return true;
				}
			}
			return false;
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

		public void AddExcludePattern(string regexPattern)
		{
			excludeExp.AddPattern(regexPattern);
		}

		public void AddIncludePattern(string regexPattern)
		{
			includeExp.AddPattern(regexPattern);
		}

		private static List<AssetBuildProcess> GetBuildProcessors()
		{
			// collect BuildProcessors
			if (pool == null)
			{
				pool = new List<AssetBuildProcess>();
				List<Type> bps = ReflectionUtil.FindClasses<AssetBuildProcess>();
				foreach (Type t in bps)
				{
					if (!t.IsAbstract)
					{
						AssetBuildProcess b = Activator.CreateInstance(t) as AssetBuildProcess;
						pool.Add(b);
					}
				}
			}
			return pool;
		}

		public static void Reset()
		{
			pool = null;
            log.Clear();
		}

        public static string GetErrorMessage()
        {
            return log.ToString();
        }

        public static void Process(ProcessStage stage, Object obj, params object[] options)
		{
            Reset();
            globalOptions = options;
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
                            p.VerifyAsset(path, obj);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Log($"{path}: {ex}");
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
                            p.PreprocessAsset(path, obj);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Log($"{path}: {ex}");
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
                            p.PostprocessAsset(path, obj);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Log($"{path}: {ex}");
                    }
                }
            }
            string error = log.ToString();
            if (!error.IsEmpty())
            {
                Debug.LogError(error);
                EditorUtility.DisplayDialog("Verify Fails", error, "OK");
            }
        }
    }
}
