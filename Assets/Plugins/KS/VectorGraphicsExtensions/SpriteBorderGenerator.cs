//Copyright 2018 Kamil Szurant

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using UnityEngine;

public class SpriteBorder
{
	public static List<List<Vector3>> Generate(GameObject spriteGo, Vector3 lineOffset)
	{
		ushort[] triangles = spriteGo.GetComponent<SpriteRenderer>().sprite.triangles;

		List<KeyValuePair<int, int>> edges = new List<KeyValuePair<int, int>>();
		for (int i = 0; i < triangles.Length; i += 3)
		{
			for (int e = 0; e < 3; e++)
			{
				int vert1 = triangles[i + e];
				int vert2 = triangles[i + e + 1 > i + 2 ? i : i + e + 1];

				if (edges.Contains(new KeyValuePair<int, int>(vert1, vert2)))
					edges.Remove(new KeyValuePair<int, int>(vert1, vert2));
				else if (edges.Contains(new KeyValuePair<int, int>(vert2, vert1)))
					edges.Remove(new KeyValuePair<int, int>(vert2, vert1));
				else
					edges.Add(new KeyValuePair<int, int>(vert1, vert2));
			}
		}

		Dictionary<int, int> edgeStartEnd = new Dictionary<int, int>();
		foreach (KeyValuePair<int, int> edge in edges)
			if (edgeStartEnd.ContainsKey(edge.Key) == false)
				edgeStartEnd.Add(edge.Key, edge.Value);

		List<List<Vector3>> linesPoints = new List<List<Vector3>>();

		linesPoints.Add(new List<Vector3>());
		int startVert = 0;
		int nextVert = 0;
		int highestVert = 0;
		int lineId = 0;

		Vector2[] vertices = spriteGo.GetComponent<SpriteRenderer>().sprite.vertices;
		while (true)
		{
			linesPoints[lineId].Add(new Vector3(vertices[nextVert].x, vertices[nextVert].y, spriteGo.transform.position.z) + lineOffset);

			nextVert = edgeStartEnd[nextVert];

			if (nextVert > highestVert)
				highestVert = nextVert;

			if (nextVert == startVert)
			{
				linesPoints[lineId].Add(new Vector3(vertices[nextVert].x, vertices[nextVert].y, spriteGo.transform.position.z) + lineOffset);

				if (edgeStartEnd.ContainsKey(highestVert + 1))
				{
					linesPoints.Add(new List<Vector3>());
					startVert = highestVert + 1;
					nextVert = startVert;
					lineId++;

					continue;
				}
				else
					break;
			}
		}

		return linesPoints;
	}
}
