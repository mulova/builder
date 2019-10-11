using System;
using System.Collections.Generic;
using System.Collections.Generic.Ex;
using System.Ex;
using System.IO;
using System.Reflection;
using System.Text.Ex;
using System.Text.RegularExpressions;
using mulova.commons;
using mulova.unicore;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace mulova.preprocess
{
    public class BuildScript : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
		private static Regex ignoreRegex;
		public static string ignorePattern = ".meta$|.fbx$|.FBX$|/Editor/|Assets/Plugins/";

		private static Regex ignorePath
		{
			get
			{
				if (ignoreRegex == null)
				{
					ignoreRegex = new Regex(ignorePattern);
				}
				return ignoreRegex;
			}
		}

        public int callbackOrder => 1;

        public static void AddIgnorePattern(string regexPattern)
		{
			ignorePattern = string.Format("{0}|{1}", ignorePattern, regexPattern);
			ignoreRegex = null;
		}

		public static readonly Loggerx log = LogManager.GetLogger(typeof(BuildScript));

		private static bool DisplayProgressBar(string title, string info, float progress)
		{
			if (SystemInfo.graphicsDeviceID != 0)
			{
				return EditorUtility.DisplayCancelableProgressBar(title, info, progress);
			}
			log.Debug("{0} ({1:P2})", info, progress);
			return false;
		}

		public static void InitEditorLog()
		{
			BuildScript.log.level = LogLevel.DEBUG;
//            AssetBuilder.log.level = LogLevel.DEBUG;
			Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
			Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.Full);
			Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
			Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
			Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
			PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
			PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.Full);
			PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
			PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
			PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
		}

		public static void ConfigureAndroid()
		{
			string path = Platform.conf.GetString("KEYSTORE_FILE", null);
			if (path.IsEmpty())
			{
				return;
			}
			PlayerSettings.Android.keystoreName = Path.Combine(EditorAssetUtil.GetProjPath(), path);
			PlayerSettings.Android.keystorePass = Platform.conf.GetString("KEYSTORE_PW", string.Empty);
			PlayerSettings.Android.keyaliasName = Platform.conf.GetString("KEYSTORE_ALIAS_NAME", string.Empty);
			PlayerSettings.Android.keyaliasPass = Platform.conf.GetString("KEYSTORE_ALIAS_PW", string.Empty);
		}

		public static void LoadEditorDll()
		{
			foreach (string d in Directory.GetDirectories(Application.dataPath, "*Editor*", SearchOption.AllDirectories))
			{
				string dir = d.ToUnixPath();
				if (dir == "Editor"||dir.StartsWithIgnoreCase("Editor/")||dir.Contains("/Editor/")||dir.EndsWithIgnoreCase("/Editor"))
				{
					DirectoryInfo dirInfo = new DirectoryInfo(dir);
					FileInfo[] files = dirInfo.GetFiles("*.dll", SearchOption.TopDirectoryOnly);
					foreach (FileInfo f in files)
					{
						if (f.Name.StartsWith("FreeType"))
						{
							continue;
						}
						Assembly.LoadFile(f.FullName);
					}
				}
			}
		}

		public static void UpdateProjectSettings(bool versionUp)
		{
			string prjSettingPath = PathUtil.Combine(Application.dataPath, "../ProjectSettings/ProjectSettings.asset");
			if (versionUp)
			{
				string text = File.ReadAllText(prjSettingPath);
				Regex regex = new Regex(@"^(?<vc>\s*bundleVersion:\s*[0-9\.]+)", RegexOptions.Multiline);
				Match m = regex.Match(text);
				string ver = m.Groups["vc"].Value;
				int sep = ver.LastIndexOf('.');
				int minorVersion = int.Parse(ver.Substring(sep+1));
				string newVer = ver.Substring(0, sep+1)+(minorVersion+1);
				text = regex.Replace(text, newVer);
				log.Debug(string.Format("{0} -> {1}", ver, newVer));

				regex = new Regex(@"^(?<ver>\s*AndroidBundleVersionCode:\s*[0-9]+)", RegexOptions.Multiline);
				m = regex.Match(text);
				ver = m.Groups["ver"].Value;
				sep = ver.LastIndexOf(' ');
				if (sep < 0)
				{
					sep = ver.LastIndexOf(':');
				}
				int verCode = int.Parse(ver.Substring(sep+1).Trim());
				newVer = ver.Substring(0, sep+1)+(verCode+1);
				text = regex.Replace(text, newVer);

				log.Debug(string.Format("{0} -> {1}", ver, newVer));
				File.WriteAllText(prjSettingPath, text);
			}
		}

		/*
        * done in configure.sh
    public static void UpdateBuildConfig()
    {
        string file = PathUtil.Combine(Application.dataPath, "Resources/build_config.bytes");
        string text = File.ReadAllText(file);
        text = SetVariable(text, "VERSION", PlayerSettings.bundleVersion);
        text = SetVariable(text, "VERSION_CODE", PlayerSettings.Android.bundleVersionCode);
        text = SetVariable(text, "BUILD_TIME", System.DateTime.Now.Ticks);
        ExecOutput rev = EditorUtil.ExecuteCommand("git", "rev-parse HEAD");
        text = SetVariable(text, "REPO_HASH", rev.stdout.Trim());
        File.WriteAllText(file, text);
        AssetDatabase.ImportAsset("Assets/Resources/build_config.bytes", ImportAssetOptions.ForceUpdate);
    }
        */

		private static string SetVariable(string input, string varName, object value)
		{
			Regex regex = new Regex(@"^\s*"+varName+@"\s*=.*$", RegexOptions.Multiline);
			if (regex.IsMatch(input))
			{
				return regex.Replace(input, varName+"="+value.ToString());
			} else
			{
				return string.Format("{0}\n{1}={2}", input.Trim(), varName, value.ToString());
			}
		}

		public static void DoNothing()
		{
		}

		private static bool sceneProcessing;

		public static void SetDirty(Object o)
		{
			EditorUtil.SetDirty(o);
			if (sceneProcessing)
			{
                EditorSceneManager.MarkAllScenesDirty();
			}
		}

		public static string PrebuildAll(ProcessStage stage, params object[] options)
		{
			LoadEditorDll();
			ResetPrebuilder();
			if (options != null)
			{
				log.Info("Prebuild options: "+ options.Join(", "));
			}
            List<Func<string, string>> func = new List<Func<string, string>>();
            if ((stage & ProcessStage.Verify) != 0)
            {
                func.Add(GetPreprocessFunc(ProcessStage.Verify, options));
            }
            if ((stage & ProcessStage.Preprocess) != 0)
            {
                func.Add(GetPreprocessFunc(ProcessStage.Preprocess, options));
            }
            if ((stage & ProcessStage.Postprocess) != 0)
            {
                func.Add(GetPreprocessFunc(ProcessStage.Postprocess, options));
            }
            EditorTraversal.ForEachAssetPath(FileTypeEx.UNITY_SUPPORTED, func.ToArray());

            EditorTraversal.ForEachScene(roots=> ProcessScene(roots, options));
			AssetDatabase.SaveAssets();

			return GetPrebuildMessage();
		}

        private static Func<string, string> GetPreprocessFunc(ProcessStage stage, params object[] options)
        {
            return path =>
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                ComponentBuildProcess.Process(stage, obj, options);
                AssetBuildProcess.Process(stage, obj, options);
                return null;
            };
        }

        public static void ProcessCurrentScene()
		{
			string err = ProcessScene(SceneManager.GetActiveScene());
			if (!err.IsEmpty())
			{
				throw new Exception(err);
			}
			EditorSceneManager.SaveOpenScenes();
		}

		public static string ProcessScene(Scene scene, params object[] options)
		{
			foreach (var root in scene.GetRootGameObjects())
			{
				var transforms = root.GetComponentsInChildren<Transform>(true);
				foreach (Transform r in transforms)
				{
					ComponentBuildProcess.Process(ProcessStage.Verify, r.gameObject, options);
				}
				foreach (Transform r in transforms)
				{
					ComponentBuildProcess.Process(ProcessStage.Preprocess, r.gameObject, options);
				}
				foreach (Transform r in transforms)
				{
					ComponentBuildProcess.Process(ProcessStage.Postprocess, r.gameObject, options);
				}
			}
			return null;
		}

		private static void PrebuildAssets(string[] allPaths, string progressTitle, Action<Object, string> preprocess)
		{
            EditorGUIUtil.DisplayProgressBar(allPaths, progressTitle, true, path => {
                if (ignorePath.IsMatch(path))
                {
                    return;
                }
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                preprocess(asset, path);
            });
		}

		public static void PrebuildAssets(string[] assetPaths, params object[] options)
		{
			string[] allPaths = AssetDatabase.GetDependencies(assetPaths);
			PrebuildAssets(allPaths, "Verify Asset (1/3)", (a, path)=>
			{
                ComponentBuildProcess.Process(ProcessStage.Verify, a, options);
                AssetBuildProcess.Process(ProcessStage.Verify, a, options);
            });
            PrebuildAssets(allPaths, "Prebuild (2/3)", (a, path)=> {
                ComponentBuildProcess.Process(ProcessStage.Preprocess, a, options);
                AssetBuildProcess.Process(ProcessStage.Preprocess, a, options);
			});
			PrebuildAssets(allPaths, "Prebuild Over (3/3)", (a, path)=> {
                ComponentBuildProcess.Process(ProcessStage.Postprocess, a, options);
				AssetBuildProcess.Process(ProcessStage.Postprocess, a, options);
			});

			AssetDatabase.SaveAssets();
		}

		public static void ResetPrebuilder()
		{
			ComponentBuildProcess.Reset();
			AssetBuildProcess.Reset();
		}

		public static string GetPrebuildMessage()
		{
			string assetErrors = AssetBuildProcess.GetErrorMessage();
			string compErrors = ComponentBuildProcess.GetErrorMessage();
			return string.Join("\n", assetErrors, compErrors);
		}

		public static void TestPostProcessBuild()
		{
			string path = EditorAssetUtil.GetProjPath();
			if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
			{
				path = PathUtil.Combine(path, "test.apk");
			} else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
			{
				path = PathUtil.Combine(path, "Build/iOS");
			}
			OnPostProcessBuild(EditorUserBuildSettings.activeBuildTarget, path);
		}

		public static void OnPostProcessBuild(BuildTarget target, string path)
		{
			Platform.Reset();
			#if UNITY_ANDROID
			string manifestPath = PathUtil.Combine(path, Application.productName, "/unity-android-resources/AndroidManifest.xml");
			if (File.Exists(manifestPath))
			{
				AndroidManifest manifest = new AndroidManifest(manifestPath);
				manifest.Read();
				manifest.packageName = manifest.packageName+"_reslib";
				manifest.Write();
			}
			#elif UNITY_IOS && PBX_PROJECT
			IosPostprocessor proc = new IosPostprocessor(path);
			proc.SetBitCode(Platform.conf.GetBool("XCODE_BITCODE", false));
			var frameworks = Platform.conf.GetString("XCODE_FRAMEWORKS", string.Empty).SplitCSV();
			foreach (var f in frameworks)
			{
			proc.AddSystemFramework(f);
			}
			var frameworkSearchPaths = Platform.conf.GetString("XCODE_FRAMEWORKSEARCHPATHS", string.Empty).SplitCSV();
			foreach (var p in frameworkSearchPaths)
			{
			proc.AddFrameworkSearchPath(p);
			}
			var headerPaths = Platform.conf.GetString("XCODE_HEADERPATHS", string.Empty).SplitCSV();
			foreach (var p in headerPaths)
			{
			proc.AddHeaderPath(p);
			}
			// XCODE_FILES items are start with 'Assets/'
			var files = Platform.conf.GetString("XCODE_FILES", string.Empty).SplitCSV();
			foreach (var f in files)
			{
			string projPath = PathUtil.Combine("Assets/", f);
			if (Directory.Exists(projPath))
			{
			foreach (var file in EditorAssetUtil.ListAssetPaths(projPath, FileType.All, true))
			{
			if (!file.EndsWithIgnoreCase(".meta"))
			{
			proc.AddFile(file);
			}
			}
			proc.AddHeaderPath(f.Substring("Assets/".Length));
			} else
			{
			proc.AddFile("Assets/"+f);
			}
			}
			proc.Save();
			#endif
		}

        public void OnPreprocessBuild(BuildReport report)
        {
            string err = null;
            if (PrebuildSettings.Get().type == PrebuildSettings.Type.Verify)
            {
                err = PrebuildAll(ProcessStage.Verify);
            } else if (PrebuildSettings.Get().type == PrebuildSettings.Type.Preprocess)
            {
                err = PrebuildAll(ProcessStage.Preprocess | ProcessStage.Postprocess);
            }
            else if (PrebuildSettings.Get().type == PrebuildSettings.Type.All)
            {
                err = PrebuildAll(ProcessStage.Verify | ProcessStage.Preprocess | ProcessStage.Postprocess);
            }
            if (!err.IsEmpty())
            {
                throw new Exception(err);
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
        }

        /*
public static void Configure()
{
BuildConfig.Reset();
string zone = CommandLineReader.GetCustomArgument(nameof(BuildConfig.ZONE), BuildConfig.ZONE);
string market = CommandLineReader.GetCustomArgument("MARKET");
string buildTarget = CommandLineReader.GetCustomArgument("BUILD_TARGET", BuildConfig.TARGET_ANDROID);
var runtime = RuntimePlatform.Android;
if (buildTarget == BuildConfig.TARGET_ANDROID)
{
runtime = RuntimePlatform.Android;
}
else if (buildTarget == BuildConfig.TARGET_IOS)
{
runtime = RuntimePlatform.IPhonePlayer;
}
else if (buildTarget == BuildConfig.TARGET_OSX)
{
runtime = RuntimePlatform.OSXPlayer;
}
else if (buildTarget == BuildConfig.TARGET_WIN)
{
runtime = RuntimePlatform.WindowsPlayer;
}
else if (buildTarget == BuildConfig.TARGET_WEBGL)
{
runtime = RuntimePlatform.WebGLPlayer;
}
else
{
runtime = EditorUserBuildSettings.activeBuildTarget.ToRuntimePlatform();
}

string buildConfigPath = string.Format("Assets/Resources/{0}.bytes", BuildConfig.FILE_NAME);
PropertiesReader buildConfig = new PropertiesReader(buildConfigPath);
buildConfig[nameof(BuildConfig.RUNTIME)] = runtime.ToString();
buildConfig[nameof(BuildConfig.PLATFORM)] = runtime.GetPlatformName();
buildConfig[nameof(BuildConfig.TARGET)] = runtime.GetTargetName();
buildConfig[nameof(BuildConfig.ZONE)] = zone;
buildConfig[nameof(BuildConfig.UNITY_VER)] = Application.unityVersion;
buildConfig[nameof(BuildConfig.BUILD_TIME)] = System.DateTime.UtcNow.Ticks.ToString();

ExecOutput rev = EditorUtil.ExecuteCommand("sh", "-c \"git rev-parse HEAD\"");
if (!rev.IsError())
{
buildConfig[nameof(BuildConfig.REVISION)] = rev.stdout.Trim();
}
else
{
throw new Exception(rev.stderr);
}
ExecOutput branch = EditorUtil.ExecuteCommand("sh", "-c \"git rev-parse --abbrev-ref HEAD\"");
if (!branch.IsError())
{
string branchStr = branch.stdout.Trim();
buildConfig[nameof(BuildConfig.DETAIL)] = branchStr;
if (branchStr.StartsWith("release/"))
{
  buildConfig[nameof(BuildConfig.VERSION)] = branchStr.Substring("release/".Length);
}
}
else
{
throw new Exception(rev.stderr);
}

File.WriteAllText(buildConfigPath, buildConfig.ToString());
AssetDatabase.ImportAsset(buildConfigPath, ImportAssetOptions.ForceUpdate);
BuildConfig.Reset();

string platformConfigPath = "Assets/Resources/platform_config.bytes";
PropertiesReader platformConfig = new PropertiesReader();
platformConfig["market"] = market;
// merge platform_config files
platformConfig.LoadFile("Assets/platform/platform_config.bytes");
platformConfig.LoadFile(string.Format("Assets/platform/platform_config_{0}.bytes", zone));
platformConfig.LoadFile(string.Format("Assets/platform/platform_config_{0}.bytes", market));
var file = string.Format("Assets/platform/platform_config_{0}_{1}.bytes", zone, market);
if (File.Exists(file))
{
platformConfig.LoadFile(file);
}
if (File.Exists("Assets/platform/platform_config_test.bytes"))
{
platformConfig.LoadFile("Assets/platform/platform_config_test.bytes");
}
File.WriteAllText(platformConfigPath, platformConfig.ToString());
AssetDatabase.ImportAsset(platformConfigPath, ImportAssetOptions.ForceUpdate);

BuildConfig.Reset();
Platform.Reset();

PlayerSettings.bundleVersion = BuildConfig.VERSION;
if (runtime == RuntimePlatform.Android)
{
PlayerSettings.Android.bundleVersionCode = BuildConfig.VERSION_CODE;
}
#if UNITY_5_6_OR_NEWER
PlayerSettings.applicationIdentifier =
#else
PlayerSettings.bundleIdentifier = 
#endif
Platform.conf.GetString("package_name", PlayerSettings.applicationIdentifier);
AssetDatabase.SaveAssets();
}
*/
    }
}
