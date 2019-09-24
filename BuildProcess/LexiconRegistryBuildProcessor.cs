﻿using System.Collections.Generic;
using System.Text.Ex;
using mulova.commons;
using mulova.comunity;
using mulova.unicore;
using UnityEditor;
using UnityEngine;

namespace mulova.build
{
    public class LexiconRegistryBuildProcessor : AssetBuildProcess
	{
		public LexiconRegistryBuildProcessor(): base("Lexicon Registry", typeof(LexiconRegistry))
		{
		}

		protected override void PreprocessAsset(string path, UnityEngine.Object obj)
		{
			LexiconRegistry reg = obj as LexiconRegistry;
			if (reg.assetDir.isValid)
			{
				string dir = AssetDatabase.GUIDToAssetPath(reg.assetDir);
				if (!dir.IsEmpty())
				{
					dir = EditorAssetUtil.GetAssetRelativePath(dir);
					TextAsset[] assets = EditorAssetUtil.ListAssets<TextAsset>(dir, FileType.Text);
					AddAssets(reg, assets);
				}
			}
		}

		private void AddAssets(LexiconRegistry reg, TextAsset[] assets)
		{
			List<AssetRef> list = new List<AssetRef>();
			foreach (var a in assets)
			{
				if (a != reg.initial)
				{
					var r = new AssetRef();
					r.SetPath(a);
					list.Add(r);
				}
			}
			reg.assets = list.ToArray();
			SetDirty(reg);
		}

		protected override void VerifyAsset(string path, UnityEngine.Object obj)
		{
		}
	}
}
