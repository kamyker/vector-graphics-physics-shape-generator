//Copyright 2018 Kamil Szurant

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SpriteOutlineCreator))]
public class SpriteOutlineEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		SpriteOutlineCreator creator = (SpriteOutlineCreator)target;
		if (GUILayout.Button("Create/Update Outlines"))
		{
			List<GameObject> outlines = creator.UpdateOutlines();
			foreach (var outline in outlines)
				Undo.RegisterCreatedObjectUndo(outline, "Undo creating sprite outline");
		}
	}
}
#endif

[System.Serializable]
public class LineStartEnd
{
	[Range(0, 1)]
	public float LineStartNormalized;
	[Range(0, 1)]
	public float LineEndNormalized = 1;
	public GameObject overrideGameObject;
}

public class SpriteOutlineCreator : MonoBehaviour
{
	public GameObject prefabWithLineRenderer;
	public Vector3 lineOffset = new Vector3(0, 0, -0.001f);
	public LineStartEnd[] lineParts;

	public List<LineRenderer> linesToUpdate;

	public List<GameObject> UpdateOutlines()
	{
		List<List<Vector3>> linesPoints = SpriteBorder.Generate(gameObject, lineOffset);

		List<GameObject> createdLines = new List<GameObject>();

		int lineIdx = 0;
		int linesBeforeCreating = linesToUpdate.Count;
		foreach (var linePoints in linesPoints)
		{
			foreach (var part in lineParts)
			{
				LineRenderer line;
				if (lineIdx < linesBeforeCreating)
				{
					line = linesToUpdate[lineIdx];
					lineIdx++;
				}
				else
				{
					if (part.overrideGameObject != null)
						line = Instantiate(prefabWithLineRenderer, transform.position, transform.rotation, transform).GetComponent<LineRenderer>();
					else
						line = Instantiate(prefabWithLineRenderer, transform.position, transform.rotation, transform).GetComponent<LineRenderer>();
					linesToUpdate.Add(line);
				}

				createdLines.Add(line.gameObject);

				int startToRemove = (int)(linePoints.Count * part.LineStartNormalized);

				int endtoRemoveStartId = (int)(linePoints.Count * (part.LineEndNormalized));
				int endToRemove = linePoints.Count - endtoRemoveStartId;


				List<Vector3> lineCut = new List<Vector3>(linePoints);
				if (part.LineStartNormalized != 0)
					lineCut.RemoveRange(0, startToRemove);

				if (part.LineEndNormalized != 1)
					lineCut.RemoveRange(endtoRemoveStartId - startToRemove, endToRemove);

				line.positionCount = lineCut.Count;
				line.SetPositions(lineCut.ToArray());
			}
		}

		return createdLines;
	}


}

