//Copyright 2018 Kamil Szurant

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VectorGraphics.Editor;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using UnityEngine;

public class SVGGeneratePhysicsShape : EditorWindow
{
	static object spriteWindowInstance;
	static Type spriteWindow;
	float outlineTolerance = 0.1f;
	bool svgSelected;
	bool useUnityGenerator;

	[MenuItem("Assets/Sprites/GenerateSVGPhysicsShape")]
	public static void ShowWindow()
	{
		EditorWindow.GetWindow(typeof(SVGGeneratePhysicsShape));
	}

	void OnGUI()
	{
		GUILayout.Label("Generate Custom Physics Shape For Selected Objects", EditorStyles.boldLabel);
		EditorGUILayout.Space();

		useUnityGenerator = EditorGUILayout.BeginToggleGroup("Use Unity generator", useUnityGenerator);
		outlineTolerance = EditorGUILayout.Slider("Outline Tolerance", outlineTolerance, 0, 1);
		EditorGUILayout.EndToggleGroup();

		CheckSvgSelected();
		EditorGUI.BeginDisabledGroup(!svgSelected);

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (useUnityGenerator)
			if (GUILayout.Button("Generate", GUILayout.ExpandWidth(false)))
				GenerateShapes();
			else if (GUILayout.Button("Generate based on mesh", GUILayout.ExpandWidth(false)))
				GenerateShapesBasedOnMesh();
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();

		EditorGUI.EndDisabledGroup();

		if (!svgSelected)
			EditorGUILayout.HelpBox("Select SVG sprite", MessageType.Info);
	}

	void OnSelectionChange()
	{
		CheckSvgSelected();
		Repaint();
	}

	void CheckSvgSelected()
	{
		svgSelected = Selection.objects.Any(go => AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(go)) as SVGImporter != null);
	}

	void GenerateShapes()
	{
		var selected = Selection.gameObjects;
		foreach (var gameObject in selected)
		{
			string path = AssetDatabase.GetAssetPath(gameObject);
			SVGImporter importer = AssetImporter.GetAtPath(path) as SVGImporter;
			if (importer != null)
			{
				var sprite = (Sprite)importer.targetObject;

				Selection.activeObject = sprite;

				spriteWindow = Type.GetType("UnityEditor.SpriteEditorWindow, UnityEditor", true);
				var getWindowMethod = spriteWindow.GetMethod("GetWindow", BindingFlags.Public | BindingFlags.Static);
				getWindowMethod.Invoke(null, null);

				//if (spriteWindowInstance == null)
				spriteWindowInstance = spriteWindow.GetField("s_Instance").GetValue(null);
				SpriteWindowMethod("OnSelectionChange");
				//string selecionPath = SpriteWindowMethod("GetSelectionAssetPath") as string;

				//Choose custom outline mode
				SpriteWindowMethod("SetupModule", (object)1);

				Type spritePhysicsShapeModule = Type.GetType("UnityEditor.U2D.SpritePhysicsShapeModule, UnityEditor", true);
				object outlineWindowInstance = SpriteWindowField("m_CurrentModule");
				Type spriteOutlineModule = spritePhysicsShapeModule.BaseType;
				//Method(Type.GetType("UnityEditor.Experimental.U2D.SpriteEditorModuleBase, UnityEditor", true), outlineWindowInstance, "OnModuleActivate", null);

				object m_Outline = spriteOutlineModule.GetField("m_Outline", bf).GetValue(outlineWindowInstance);
				Type spriteOutlineModel = Type.GetType("UnityEditor.U2D.SpriteOutlineModel, UnityEditor", true);

				object m_SpriteOutlineList = spriteOutlineModel.InvokeMember("", BindingFlags.GetProperty, null, m_Outline, new object[] { 0 });
				Type SpriteOutlineList = Type.GetType("UnityEditor.U2D.SpriteOutlineList, UnityEditor", true);

				SpriteOutlineList.GetField("m_TessellationDetail", bf).SetValue(m_SpriteOutlineList, 0.3f);


				//3. this.SetupShapeEditorOutline(this.m_Selected);
				SpriteRect m_Selected = spriteOutlineModule.GetField("m_Selected", bf).GetValue(outlineWindowInstance) as SpriteRect;
				Method(spriteOutlineModule, outlineWindowInstance, "SetupShapeEditorOutline", m_Selected);

				List<Vector2[]> outlines = new List<Vector2[]>((List<Vector2[]>)Method(SpriteOutlineList, m_SpriteOutlineList, "ToListVector"));
				float tessellation = (float)SpriteOutlineList.GetField("m_TessellationDetail", bf).GetValue(m_SpriteOutlineList);


				Method(Type.GetType("UnityEditor.Experimental.U2D.SpriteEditorModuleBase, UnityEditor", true), outlineWindowInstance, "ApplyRevert", true);
				//Method(Type.GetType("UnityEditor.Experimental.U2D.SpriteEditorModuleBase, UnityEditor", true), outlineWindowInstance, "OnModuleDeactivate", null);
				object ISpriteEditor = spritePhysicsShapeModule.GetProperty("spriteEditorWindow", bf).GetValue(outlineWindowInstance);
				Method(Type.GetType("UnityEditor.Experimental.U2D.ISpriteEditor, UnityEditor", true), ISpriteEditor, "ApplyOrRevertModification", true);
				//Method(Type.GetType("UnityEditor.Experimental.U2D.ISpriteEditor, UnityEditor", true), ISpriteEditor, "SetDataModified");
				//spriteOutlineModule.GetField("shapeEditorDirty").SetValue(outlineWindowInstance, true);
				Debug.Log(outlines[0].Length + " count");
				//AssetDatabase.Refresh();
				//EditorUtility.SetDirty(sprite);
				//AssetDatabase.SaveAssets();
				//OverwriteOutline(sprite, tessellation, outlines);

			}
		}
	}
	void GenerateShapesBasedOnMesh()
	{
		var selected = Selection.gameObjects;
		foreach (var gameObject in selected)
		{
			string path = AssetDatabase.GetAssetPath(gameObject);
			SVGImporter importer = AssetImporter.GetAtPath(path) as SVGImporter;
			if (importer != null)
			{
				var sprite = (Sprite)importer.targetObject;

				//SpriteBorder.Generate()

				Selection.activeObject = sprite;

				spriteWindow = Type.GetType("UnityEditor.SpriteEditorWindow, UnityEditor", true);
				var getWindowMethod = spriteWindow.GetMethod("GetWindow", BindingFlags.Public | BindingFlags.Static);
				getWindowMethod.Invoke(null, null);

				//if (spriteWindowInstance == null)
				spriteWindowInstance = spriteWindow.GetField("s_Instance").GetValue(null);
				SpriteWindowMethod("OnSelectionChange");
				//string selecionPath = SpriteWindowMethod("GetSelectionAssetPath") as string;

				//Choose custom outline mode
				SpriteWindowMethod("SetupModule", (object)1);

				Type spritePhysicsShapeModule = Type.GetType("UnityEditor.U2D.SpritePhysicsShapeModule, UnityEditor", true);
				object outlineWindowInstance = SpriteWindowField("m_CurrentModule");
				Type spriteOutlineModule = spritePhysicsShapeModule.BaseType;
				//Method(Type.GetType("UnityEditor.Experimental.U2D.SpriteEditorModuleBase, UnityEditor", true), outlineWindowInstance, "OnModuleActivate", null);

				object m_Outline = Field(spriteOutlineModule, outlineWindowInstance, "m_Outline");
				Type spriteOutlineModel = Type.GetType("UnityEditor.U2D.SpriteOutlineModel, UnityEditor", true);

				object m_SpriteOutlineList = spriteOutlineModel.InvokeMember("", BindingFlags.GetProperty, null, m_Outline, new object[] { 0 });
				Type SpriteOutlineList = Type.GetType("UnityEditor.U2D.SpriteOutlineList, UnityEditor", true);

				SpriteOutlineList.GetField("m_TessellationDetail", bf).SetValue(m_SpriteOutlineList, 0.3f);


				//3. this.SetupShapeEditorOutline(this.m_Selected);
				SpriteRect m_Selected = spriteOutlineModule.GetField("m_Selected", bf).GetValue(outlineWindowInstance) as SpriteRect;
				Method(spriteOutlineModule, outlineWindowInstance, "SetupShapeEditorOutline", m_Selected);

				List<Vector2[]> outlines = new List<Vector2[]>((List<Vector2[]>)Method(SpriteOutlineList, m_SpriteOutlineList, "ToListVector"));
				float tessellation = (float)SpriteOutlineList.GetField("m_TessellationDetail", bf).GetValue(m_SpriteOutlineList);


				Method(Type.GetType("UnityEditor.Experimental.U2D.SpriteEditorModuleBase, UnityEditor", true), outlineWindowInstance, "ApplyRevert", true);
				//Method(Type.GetType("UnityEditor.Experimental.U2D.SpriteEditorModuleBase, UnityEditor", true), outlineWindowInstance, "OnModuleDeactivate", null);
				object ISpriteEditor = spritePhysicsShapeModule.GetProperty("spriteEditorWindow", bf).GetValue(outlineWindowInstance);
				Method(Type.GetType("UnityEditor.Experimental.U2D.ISpriteEditor, UnityEditor", true), ISpriteEditor, "ApplyOrRevertModification", true);
				//Method(Type.GetType("UnityEditor.Experimental.U2D.ISpriteEditor, UnityEditor", true), ISpriteEditor, "SetDataModified");
				//spriteOutlineModule.GetField("shapeEditorDirty").SetValue(outlineWindowInstance, true);
				Debug.Log(outlines[0].Length + " count");
				//AssetDatabase.Refresh();
				//EditorUtility.SetDirty(sprite);
				//AssetDatabase.SaveAssets();
				//OverwriteOutline(sprite, tessellation, outlines);

			}
		}
	}
	//private static void OverwriteOutline(Sprite mainSprite, float tessellationDetail, List<Vector2[]> outlines)
	//{
	//	ISpriteEditorDataProvider mainSpriteDataProvider = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mainSprite)) as ISpriteEditorDataProvider;
	//	mainSpriteDataProvider.InitSpriteEditorDataProvider();
	//	ISpritePhysicsOutlineDataProvider meshDataProvider = mainSpriteDataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();


	//	GUID mainGuid = mainSprite.GetSpriteID();

	//	meshDataProvider.SetOutlines(mainGuid, outlines);
	//	meshDataProvider.SetTessellationDetail(mainGuid, tessellationDetail);
	//	mainSpriteDataProvider.Apply();


	//	//AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(mainSprite));

	//	Debug.Log("Outlines overwritten for " + mainSprite.name);
	//}

	static BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

	static object SpriteWindowMethod(string method)
	{
		return spriteWindow.GetMethod(method, bf).Invoke(spriteWindowInstance, null);
	}
	static object SpriteWindowMethod(string method, params object[] args)
	{
		return spriteWindow.GetMethod(method, bf).Invoke(spriteWindowInstance, args);
	}

	static object SpriteWindowField(string field)
	{
		return spriteWindow.GetField(field, bf).GetValue(spriteWindowInstance);
	}

	static object Method(Type type, object obj, string method)
	{
		return type.GetMethod(method, bf).Invoke(obj, null);
	}
	static object Method(Type type, object obj, string method, params object[] args)
	{
		return type.GetMethod(method, bf).Invoke(obj, args);
	}

	static object Field(Type type, object obj, string field)
	{
		FieldInfo info = type.GetField(field, bf);
		return info.GetValue(obj);
	}
}

//public class SVGImporterGenerateOutlinePost : AssetPostprocessor
//{
//	static object spriteWindowInstance;
//	static Type spriteWindow;

//	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
//	{
//		foreach (string str in importedAssets)
//		{
//			//Debug.Log("Reimported Asset: " + str);
//			SVGImporter importer = AssetImporter.GetAtPath(str) as SVGImporter;
//			if (importer != null)
//			{
//				string name = str.ToLower();

//				//Debug.Log(" ->import ");

//				string path = importer.assetPath;
//				FileInfo fileInfo = new FileInfo(path);
//				string directory = fileInfo.Directory.Name;

//				Debug.Log(directory);
//				//if (File.Exists(AssetDatabase.GetTextMetaFilePathFromAssetPath(name)))
//				//	return;

//				if (directory == "OutlineGenerator")
//				{
//					Sprite sprite = AssetDatabase.LoadAssetAtPath(str, typeof(Sprite)) as Sprite;

//					Selection.activeObject = sprite;
//					//Debug.Log(Selection.activeObject.name);

//					spriteWindow = Type.GetType("UnityEditor.SpriteEditorWindow, UnityEditor", true);
//					var getWindowMethod = spriteWindow.GetMethod("GetWindow", BindingFlags.Public | BindingFlags.Static);
//					getWindowMethod.Invoke(null, null);

//					//if (spriteWindowInstance == null)
//					spriteWindowInstance = spriteWindow.GetField("s_Instance").GetValue(null);
//					SpriteWindowMethod("OnSelectionChange");
//					string selecionPath = SpriteWindowMethod("GetSelectionAssetPath") as string;
//					//Choose custom outline mode
//					//SpriteWindowMethod("SetupModule", (object)0);
//					SpriteWindowMethod("SetupModule", (object)1);

//					Type spritePhysicsShapeModule = Type.GetType("UnityEditor.U2D.SpritePhysicsShapeModule, UnityEditor", true);
//					object outlineWindowInstance = SpriteWindowField("m_CurrentModule");
//					Type spriteOutlineModule = spritePhysicsShapeModule.BaseType;
//					//Method(Type.GetType("UnityEditor.Experimental.U2D.SpriteEditorModuleBase, UnityEditor", true), outlineWindowInstance, "OnModuleActivate", null);

//					object m_Outline = spriteOutlineModule.GetField("m_Outline", bf).GetValue(outlineWindowInstance);
//					Type spriteOutlineModel = Type.GetType("UnityEditor.U2D.SpriteOutlineModel, UnityEditor", true);

//					object m_SpriteOutlineList = spriteOutlineModel.InvokeMember("", BindingFlags.GetProperty, null, m_Outline, new object[] { 0 });
//					Type SpriteOutlineList = Type.GetType("UnityEditor.U2D.SpriteOutlineList, UnityEditor", true);

//					SpriteOutlineList.GetField("m_TessellationDetail", bf).SetValue(m_SpriteOutlineList, 0.3f);


//					//3. this.SetupShapeEditorOutline(this.m_Selected);
//					SpriteRect m_Selected = spriteOutlineModule.GetField("m_Selected", bf).GetValue(outlineWindowInstance) as SpriteRect;
//					Method(spriteOutlineModule, outlineWindowInstance, "SetupShapeEditorOutline", m_Selected);

//					List<Vector2[]> outlines = new List<Vector2[]>((List<Vector2[]>)Method(SpriteOutlineList, m_SpriteOutlineList, "ToListVector"));
//					float tessellation = (float)SpriteOutlineList.GetField("m_TessellationDetail", bf).GetValue(m_SpriteOutlineList);

//					string newPath = path.Replace("/" + directory, "");
//					if (AssetDatabase.MoveAsset(path, newPath) != "")
//					{
//						string fileName = new FileInfo(path).Name;
//						string parentFolder = path.Replace("/" + fileName, "");
//						string newName = UnityEngine.Random.Range(0, 99) + fileName;
//						string error2 = AssetDatabase.RenameAsset(path, newName);
//						string error = AssetDatabase.MoveAsset(parentFolder + newName, newPath);
//						Debug.Log(error);
//					}

//					//Method(Type.GetType("UnityEditor.Experimental.U2D.SpriteEditorModuleBase, UnityEditor", true), outlineWindowInstance, "ApplyRevert", true);
//					//Method(Type.GetType("UnityEditor.Experimental.U2D.SpriteEditorModuleBase, UnityEditor", true), outlineWindowInstance, "OnModuleDeactivate", null);
//					object ISpriteEditor = spritePhysicsShapeModule.GetProperty("spriteEditorWindow", bf).GetValue(outlineWindowInstance);
//					//Method(Type.GetType("UnityEditor.Experimental.U2D.ISpriteEditor, UnityEditor", true), ISpriteEditor, "ApplyOrRevertModification", true);
//					Method(Type.GetType("UnityEditor.Experimental.U2D.ISpriteEditor, UnityEditor", true), ISpriteEditor, "SetDataModified");
//					//spriteOutlineModule.GetField("shapeEditorDirty").SetValue(outlineWindowInstance, true);
//					Debug.Log(outlines[0].Length + " count");
//					//AssetDatabase.Refresh();
//					//EditorUtility.SetDirty(sprite);
//					//AssetDatabase.SaveAssets();

//					OverwriteOutline(sprite, tessellation, outlines);
//					AssetDatabase.Refresh();
//					EditorUtility.SetDirty(sprite);
//					AssetDatabase.SaveAssets();
//					//AssetDatabase.ImportAsset(path.Replace("/" + directory, ""));
//				}
//			}
//		}
//		//AssetDatabase.SaveAssets();
//	}

//	private static void OverwriteOutline(Sprite mainSprite, float tessellationDetail, List<Vector2[]> outlines)
//	{
//		ISpriteEditorDataProvider mainSpriteDataProvider = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mainSprite)) as ISpriteEditorDataProvider;
//		mainSpriteDataProvider.InitSpriteEditorDataProvider();
//		ISpritePhysicsOutlineDataProvider meshDataProvider = mainSpriteDataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();


//		GUID mainGuid = mainSprite.GetSpriteID();

//		meshDataProvider.SetOutlines(mainGuid, outlines);
//		meshDataProvider.SetTessellationDetail(mainGuid, tessellationDetail);
//		mainSpriteDataProvider.Apply();


//		//force SetBindPose
//		AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(mainSprite));

//		Debug.Log("Outlines overwritten for " + mainSprite.name);
//	}

//	static BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

//	static object SpriteWindowMethod(string method)
//	{
//		return spriteWindow.GetMethod(method, bf).Invoke(spriteWindowInstance, null);
//	}
//	static object SpriteWindowMethod(string method, params object[] args)
//	{
//		return spriteWindow.GetMethod(method, bf).Invoke(spriteWindowInstance, args);
//	}

//	static object SpriteWindowField(string field)
//	{
//		return spriteWindow.GetField(field, bf).GetValue(spriteWindowInstance);
//	}

//	static object Method(Type type, object obj, string method)
//	{
//		return type.GetMethod(method, bf).Invoke(obj, null);
//	}
//	static object Method(Type type, object obj, string method, params object[] args)
//	{
//		return type.GetMethod(method, bf).Invoke(obj, args);
//	}

//	static object Field(Type type, object obj, string field)
//	{
//		FieldInfo info = type.GetField(field, bf);
//		return info.GetValue(obj);
//	}
//}
