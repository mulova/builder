﻿using System;
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
    public abstract class ComponentBuildProcess
	{
        public const string VERIFY_ONLY = "verify_only";
        public abstract Type compType { get; }

		protected abstract void VerifyComponent(Component comp);
		protected abstract void PreprocessComponent(Component comp);
		protected abstract void PreprocessOver(Component comp);

		private Object currentObj;
        private static readonly BuildLog log = new BuildLog();
        public static object[] globalOptions;

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

		public void Verify(Object obj, Component comp)
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
				VerifyComponent(comp);
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
				if (!IsOption(VERIFY_ONLY))
				{
					PreprocessComponent(comp);
				}
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
				if (!IsOption(VERIFY_ONLY))
				{
					PreprocessOver(comp);
				}
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

        public static void VerifyComponents(Object obj, params object[] options)
		{
			ProcessComponents((p,o,c)=>p.Verify(o, c), obj);
		}

		public static void PreprocessComponents(Object obj, params object[] options)
		{
            globalOptions = options;
			ProcessComponents((p,o,c)=>p.Preprocess(o, c), obj);
		}

		public static void PreprocessOver(Object obj, params object[] options)
		{
            globalOptions = options;
			ProcessComponents((p,o,c)=>p.PreprocessOver(o, c), obj);
		}

		private static void ProcessComponents(Action<ComponentBuildProcess, Object, Component> action, Object obj)
		{
			if (obj is Component)
			{
				Component c = obj as Component;
				if (!c.CompareTag("EditorOnly"))
				{
					List<ComponentBuildProcess> processes = GetBuildProcessor(c.GetType());
					if (processes != null)
					{
						foreach (ComponentBuildProcess p in processes)
						{
							action(p, obj, c);
						}
					}
				}
			} else if (obj is GameObject)
			{
				foreach (Component c in (obj as GameObject).GetComponentsInChildren<Component>(true))
				{
					if (c != null&&!c.CompareTag("EditorOnly"))
					{
						List<ComponentBuildProcess> processes = GetBuildProcessor(c.GetType());
						if (processes != null)
						{
							foreach (var p in processes)
							{
								action(p, obj, c);
							}
						}
					} else
					{
						// missing component
					}
				}
			}
		}
	}
}