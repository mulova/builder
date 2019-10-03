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
        public const string VERIFY_ONLY = ComponentBuildProcess.VERIFY_ONLY;

        public abstract string title { get; }
        public abstract Type assetType { get; }
        protected abstract void PreprocessAsset(string path, Object obj);
        protected abstract void VerifyAsset(string path, Object obj);

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

		public void Preprocess(string path, Object obj)
		{
			try
			{
				if (obj != null&&!assetType.IsAssignableFrom(obj.GetType()))
				{
					return;
				}
				if (excludePath != null&&excludePath.IsMatch(path))
				{
					return;
				}
				if (includePath != null&&!includePath.IsMatch(path))
				{
					return;
				}
				VerifyAsset(path, obj);
				if (!IsOption(VERIFY_ONLY))
				{
					PreprocessAsset(path, obj);
				}
			} catch (Exception ex)
			{
                log.Log($"{path}: {ex}");
			}
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

		public static void PreprocessAssets(string path, Object obj, params object[] options)
		{
            globalOptions = options;
			foreach (AssetBuildProcess p in GetBuildProcessors())
			{
				p.Preprocess(path, obj);
			}
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

        public static void Verify(List<Object> list)
		{
			if (!list.IsEmpty())
			{
				Reset();
				foreach (var o in list)
				{
					PreprocessAssets(AssetDatabase.GetAssetPath(o), o);
				}
				string verifyError = log.ToString();
				if (!verifyError.IsEmpty())
				{
					Debug.LogError(verifyError);
					EditorUtility.DisplayDialog("Verify Fails", verifyError, "OK");
				}
			}
		}
	}
}
