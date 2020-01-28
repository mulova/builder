using System;
using System.Collections.Generic;
using System.Collections.Generic.Ex;
using System.Text.Ex;
using mulova.commons;
using mulova.unicore;
using UnityEditor;
using UnityEngine;
using UnityEngine.Ex;
using UnityEngine.SceneManagement;
using LogType = UnityEngine.LogType;
using Object = UnityEngine.Object;
using System.Ex;

namespace mulova.preprocess
{
    public abstract class ComponentBuildProcess : IComparable<ComponentBuildProcess>
    {
        public abstract Type compType { get; }
        public int order = int.MaxValue;
		protected abstract void Verify(Component comp);
		protected abstract void Preprocess(Component comp);
		protected abstract void Postprocess(Component comp);
		private Object currentObj;

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

        private static FieldAttributeRegistry<VerifyAttribute> fieldAttributeReg = new FieldAttributeRegistry<VerifyAttribute>();
        private static MethodAttributeRegistry<MethodVerifyAttribute> methodAttributeReg = new MethodAttributeRegistry<MethodVerifyAttribute>();

		//protected bool isCdnAsset 
		//{ 
		//	get
		//	{
		//		return AssetBundlePath.inst.IsCdnAsset(currentObj);
		//	}
		//}

		public virtual string title
		{
			get
			{
				return GetType().Name;
			}
		}

		public bool isAsset
		{
			get
			{
				return !AssetDatabase.GetAssetPath(currentObj).IsEmpty();
			}
		}

		public string path
		{
			get
			{
				string p = AssetDatabase.GetAssetPath(currentObj);
				if (p != null)
				{
					return p;
				}
				if (currentObj is GameObject)
				{
					return string.Format("[{0}]{1}", SceneManager.GetActiveScene().path, (currentObj as GameObject).transform.GetScenePath());
				}
				return SceneManager.GetActiveScene().path;
			}
		}

        public static void SetOptions(params object[] o)
        {
            if (o == null)
            {
                globalOptions = null;
            } else
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

        public bool IsApplicable(Component comp)
        {
            if (compType == null ^ comp == null)
            {
                return false;
            }
            if (compType != null && !compType.IsAssignableFrom(comp.GetType()))
            {
                return false;
            }
            return true;
        }

        public void Verify(Object obj, Component comp)
		{
            if (!IsApplicable(comp))
            {
                return;
            }
			try
			{
				this.currentObj = obj;
                Verify(comp);
			} catch (Exception ex)
			{
                log.Log(LogType.Error, path, ex);
            }
		}

		public void Preprocess(Object obj, Component comp)
		{
			if (compType == null ^ comp == null)
			{
				return;
			}
			if (compType != null && !compType.IsAssignableFrom(comp.GetType()))
			{
				return;
			}
			try
			{
				this.currentObj = obj;
				Preprocess(comp);
			} catch (Exception ex)
			{
                log.Log(LogType.Error, path, ex);
            }
		}

		public void Postprocess(Object obj, Component comp)
		{
			if (compType == null ^ comp == null)
			{
				return;
			}
			if (compType != null && !compType.IsAssignableFrom(comp.GetType()))
			{
				return;
			}
			try
			{
				this.currentObj = obj;
				Postprocess(comp);
			} catch (Exception ex)
			{
                log.Log(LogType.Error, path, ex);
            }
		}

		private static MultiMap<Type, ComponentBuildProcess> processPool;

		private static List<ComponentBuildProcess> GetBuildProcessor(Type type)
		{
			// collect BuildProcessors
			if (processPool == null)
			{
				processPool = new MultiMap<Type, ComponentBuildProcess>();
                List<Type> bps = typeof(ComponentBuildProcess).FindClasses();
				foreach (Type t in bps)
				{
					if (!t.IsAbstract)
					{
						ComponentBuildProcess b = Activator.CreateInstance(t) as ComponentBuildProcess;
						processPool.Add(b.compType, b);
					}
				}
			}
			if (processPool.ContainsKey(type))
			{
				return processPool[type];
			} else
			{
				// handle inheritance
				Type baseType = type;
				while (baseType != null)
				{
					if (processPool.ContainsKey(baseType))
					{
						List<ComponentBuildProcess> b = processPool.GetSlot(baseType);
						if (b != null)
						{
							processPool.AddRange(type, b);
						}
                        b.Sort();
						return b;
					}
					baseType = baseType.BaseType;
				}
				processPool[type] = null;
				return null;
			}
		}

        public static void Process(ProcessStage stage, Object o)
        {
            if (o is Component c)
            {
                ProcessComponent(stage, o, c);
			} else if (o is GameObject obj)
			{
				foreach (Component comp in (o as GameObject).GetComponentsInChildren<Component>(true))
                {
                    ProcessComponent(stage, o, comp);
                }
            }
        }

        private static void ProcessComponent(ProcessStage stage, Object o, Component c)
        {
            if (c != null && !c.CompareTag("EditorOnly"))
            {
                List<ComponentBuildProcess> processes = GetBuildProcessor(c.GetType());
                if (processes != null)
                {
                    foreach (var p in processes)
                    {
                        if (p.IsApplicable(c))
                        {
                            if ((stage & ProcessStage.Verify) != 0)
                            {
                                p.Verify(o, c);
                            }
                            if ((stage & ProcessStage.Preprocess) != 0)
                            {
                                p.Preprocess(o, c);
                            }
                            if ((stage & ProcessStage.Postprocess) != 0)
                            {
                                p.Postprocess(o, c);
                            }
                        }
                    }
                }
            }
            else
            {
                // missing component
            }
            fieldAttributeReg.ForEach(c, (attr, f, val) =>
            {
                if (!attr.IsValid(c, f))
                {
                    log.Log(LogType.Error, $"{c.transform.GetScenePath()}${attr.TypeId}.{f.Name}", null, o);
                }
            });
            methodAttributeReg.ForEach(c, (attr, m, obj) =>
            {
                if (!attr.IsValid(obj, m))
                {
                    log.Log(LogType.Error, $"{c.transform.GetScenePath()}${attr.TypeId}.{m.Name}", null, o);
                }
            });
        }

        public int CompareTo(ComponentBuildProcess other)
        {
            return this.order - other.order;
        }
    }
}
