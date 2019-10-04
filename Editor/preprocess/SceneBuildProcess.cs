/*
using System;
using System.Collections.Generic;
using mulova.commons;
using UnityEngine;

namespace mulova.preprocess
{
    public abstract class SceneBuildProcess
    {
        protected abstract void VerifyScene(IEnumerable<Transform> sceneRoots);
        protected abstract void PreprocessScene(IEnumerable<Transform> sceneRoots);
        protected abstract void Postprocess(IEnumerable<Transform> sceneRoots);

        private static readonly BuildLog log = new BuildLog();
        public static object[] globalOptions;
        protected string scene { get; private set; }

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
        
        public void Process(ProcessStage stage, IEnumerable<Transform> sceneRoots)
        {
            try
            {
                this.scene = scene;
                if ((stage & ProcessStage.Verify) != 0)
                {
                    VerifyScene(sceneRoots);
                }
                if ((stage & ProcessStage.Preprocess) != 0)
                {
                    PreprocessScene(sceneRoots);
                }
                if ((stage & ProcessStage.Postprocess) != 0)
                {
                    Postprocess(sceneRoots);
                }
            } catch (Exception ex)
            {
                log.Log($"{scene}: {ex}");
            }
        }

        public static string GetErrorMessage()
        {
            return log.ToString();
        }

        private static List<SceneBuildProcess> pool;

        protected static List<SceneBuildProcess> processPool
        {
            get
            {
                // collect BuildProcessors
                if (pool == null)
                {
                    pool = new List<SceneBuildProcess>();
                    foreach (Type t in ReflectionUtil.FindClasses<SceneBuildProcess>())
                    {
                        if (!t.IsAbstract)
                        {
                            SceneBuildProcess b = Activator.CreateInstance(t) as SceneBuildProcess;
                            pool.Add(b);
                        }
                    }
                }
                return pool;
            }
        }

        public static void Reset()
        {
            pool = null;
        }

        public static void ProcessScenes(ProcessStage stage, Transform sceneRoots, params object[] options)
        {
            globalOptions = options;
            foreach (SceneBuildProcess p in processPool)
            {
                if ((stage & ProcessStage.Verify) != 0)
                {
                    p.VerifyScene(sceneRoots);
                }
            }
            foreach (SceneBuildProcess p in processPool)
            {
                if ((stage & ProcessStage.Preprocess) != 0)
                {
                    p.PreprocessScene(sceneRoots);
                }
            }
            foreach (SceneBuildProcess p in processPool)
            {
                if ((stage & ProcessStage.Postprocess) != 0)
                {
                    p.Postprocess(sceneRoots);
                }
            }
        }
    }
}
*/