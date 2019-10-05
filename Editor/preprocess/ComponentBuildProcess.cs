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
using Object = UnityEngine.Object;

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
        protected static readonly BuildLog log = new BuildLog();

        public static object[] globalOptions;
        private static FieldAttributeRegistry<VerifyAttribute> attributeReg = new FieldAttributeRegistry<VerifyAttribute>();

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

        public bool IsApplicable(Object obj, Component comp)
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
            if (!IsApplicable(obj, comp))
            {
                return;
            }
			try
			{
				this.currentObj = obj;
                Verify(comp);
                attributeReg.ForEach(obj, (attr, f, val) =>
                {
                    if (!attr.IsValid(obj, f))
                    {
                        log.Log($"[{path}] {attr.GetType().FullName} fails to verify {obj.GetType().FullName}.{f.Name}");
                    }
                });
			} catch (Exception ex)
			{
                log.Log($"{path}: {ex}");
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
                log.Log($"{path}: {ex}");
            }
		}

		public void PreprocessOver(Object obj, Component comp)
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
                log.Log($"{path}: {ex}");
            }
		}

		private static MultiMap<Type, ComponentBuildProcess> processPool;

		private static List<ComponentBuildProcess> GetBuildProcessor(Type type)
		{
			// collect BuildProcessors
			if (processPool == null)
			{
				processPool = new MultiMap<Type, ComponentBuildProcess>();
				List<Type> bps = ReflectionUtil.FindClasses<ComponentBuildProcess>();
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

		public static void Reset()
		{
			processPool = null;
		}

        public static string GetErrorMessage()
        {
            return log.ToString();
        }

        public static void Process(ProcessStage stage, Object o, params object[] options)
        {
            globalOptions = options;

            if (o is Component)
            {
                var c = o as Component;
                var obj = c.gameObject;
                if (!c.CompareTag("EditorOnly"))
                {
                    List<ComponentBuildProcess> processes = GetBuildProcessor(c.GetType());
                    if (processes != null)
                    {
                        foreach (ComponentBuildProcess p in processes)
                        {
                            if (p.IsApplicable(obj, c))
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
                                    p.PreprocessOver(o, c);
                                }
                            }
                        }
					}
				}
			} else if (o is GameObject obj)
			{
				foreach (Component c in (o as GameObject).GetComponentsInChildren<Component>(true))
				{
					if (c != null&&!c.CompareTag("EditorOnly"))
					{
						List<ComponentBuildProcess> processes = GetBuildProcessor(c.GetType());
						if (processes != null)
						{
							foreach (var p in processes)
							{
                                if (p.IsApplicable(obj, c))
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
                                        p.PreprocessOver(o, c);
                                    }
                                }
                            }
						}
					} else
					{
						// missing component
					}
				}
			}
		}

        public int CompareTo(ComponentBuildProcess other)
        {
            return this.order - other.order;
        }
    }
}
