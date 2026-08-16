using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace JasonSkillman.AsyncAddressablesManager.Editor
{
	internal sealed class ReferenceViewerWindow : EditorWindow
	{
		private enum ColumnType
		{
			Name,
			Key,
			RefCount,
			Type,
		}
		
		private const string WindowName = "Reference Viewer";

		private Vector2 scrollPosition;
		private string searchFilter = "";
		private GUIStyle rowStyle;
		private GUIStyle headerStyle;
		private GUIStyle searchFieldStyle;

		// Column width settings
		private float nameColumnWidth = 200.0f;
		private float keyColumnWidth = 250.0f;
		private float refCountColumnWidth = 120.0f;
		private float typeColumnWidth = 120.0f;
		
		private bool isDraggingColumn = false;
		private float columnDragStartX;
		private int currentDraggingColumnIndex = -1;

		private bool showEmptyRefs;

		[MenuItem("Tools/Async Addressables Manager/Reference Viewer")]
		private static void OpenWindow()
		{
			GetWindow<ReferenceViewerWindow>(WindowName);
		}

		#region GUI

		private void OnGUI()
		{
			InitializeStyles();
			
			GUILayout.Space(10);

			// Search bar
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Search:", GUILayout.Width(60));
			searchFilter = EditorGUILayout.TextField(searchFilter, searchFieldStyle);
			EditorGUILayout.EndHorizontal();
			
			GUILayout.Space(5);

			showEmptyRefs = EditorGUILayout.Toggle("Show Empty Refs", showEmptyRefs);
			GUILayout.Space(5);

			// Scrollable list
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			{
				DrawHeaderGUI();
				DrawListGUI();
			}
			EditorGUILayout.EndScrollView();
		}

		private void DrawHeaderGUI()
		{
			EditorGUILayout.BeginHorizontal(headerStyle);
			{
				GUILayout.Label("Name", GUILayout.Width(nameColumnWidth));
				DrawDraggableColumnSeparatorGUI((int)ColumnType.Name, ref nameColumnWidth);

				GUILayout.Label("Key", GUILayout.Width(keyColumnWidth));
				DrawDraggableColumnSeparatorGUI((int)ColumnType.Key, ref keyColumnWidth);

				GUILayout.Label("Reference Count", GUILayout.Width(refCountColumnWidth));
				DrawDraggableColumnSeparatorGUI((int)ColumnType.RefCount, ref refCountColumnWidth);

				GUILayout.Label("Type", GUILayout.Width(typeColumnWidth));
				DrawDraggableColumnSeparatorGUI((int)ColumnType.Type, ref typeColumnWidth);
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawListGUI()
		{
			var loadedAssets = AddressablesManager.LoadedAssets;

			if (loadedAssets != null && loadedAssets.Count > 0)
			{
				foreach (var kvp in FilterAssets(searchFilter, AddressablesManager.LoadedAssets))
				{
					if (!showEmptyRefs && kvp.Value.referenceCount == 0)
					{
						continue;
					}
					
					EditorGUILayout.BeginHorizontal(rowStyle);
					{
						GUILayout.TextField(kvp.Value.assetName, GUILayout.Width(nameColumnWidth));

						GUILayout.TextField(kvp.Key?.ToString(), GUILayout.Width(keyColumnWidth));

						GUILayout.Label(kvp.Value.referenceCount.ToString(), GUILayout.Width(refCountColumnWidth));

						GUILayout.Label(kvp.Value.asset?.GetType().Name, GUILayout.Width(typeColumnWidth));
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			else
			{
				GUILayout.Label("No loaded assets found.");
			}
		}
		
		private void DrawDraggableColumnSeparatorGUI(int columnIndex, ref float columnPosition)
		{
			Rect rect = GUILayoutUtility.GetLastRect();

			float Width = 8;
			Rect separatorRect = new Rect(rect.x + rect.width - (Width / 2), rect.y, Width, rect.height);
			EditorGUIUtility.AddCursorRect(separatorRect, MouseCursor.ResizeHorizontal);

			Rect drawRect = separatorRect;
			drawRect.width = 4;
			EditorGUI.DrawRect(drawRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));

			HandleDragEvents(separatorRect, columnIndex, ref columnPosition);
		}

		#endregion
		
		private void HandleDragEvents(in Rect handleRect, int columnIndex, ref float columnPosition)
		{
			Event currentEvent = Event.current;
        
			switch (currentEvent.type)
			{
				case EventType.MouseDown:
					if (handleRect.Contains(currentEvent.mousePosition))
					{
						isDraggingColumn = true;
						columnDragStartX = currentEvent.mousePosition.x;
						currentDraggingColumnIndex = columnIndex;
						currentEvent.Use();
					}
					break;
				case EventType.MouseDrag:
					if (isDraggingColumn &&
					    columnIndex == currentDraggingColumnIndex)
					{
						float deltaX = currentEvent.mousePosition.x - columnDragStartX;
						columnPosition += deltaX;
                    
						// Clamp to reasonable values
						columnPosition = Mathf.Clamp(columnPosition, 50, 300);
                    
						columnDragStartX = currentEvent.mousePosition.x;
						currentEvent.Use();
					}
					break;
				case EventType.MouseUp:
					if (isDraggingColumn &&
					    columnIndex == currentDraggingColumnIndex)
					{
						isDraggingColumn = false;
						currentDraggingColumnIndex = -1;
						currentEvent.Use();
					}
					break;
			}
		}

		private void InitializeStyles()
		{
			searchFieldStyle = new GUIStyle(EditorStyles.textField);
			searchFieldStyle.margin = new RectOffset(5, 5, 5, 5);
			
			
			//headerStyle = new GUIStyle(EditorStyles.textArea);
			headerStyle = new GUIStyle();
			headerStyle.normal.background = MakeColorTexture(40, 40, 40);
			//headerStyle.focused.background = MakeColorTexture(40, 40, 40);
			//headerStyle.active.background = MakeColorTexture(40, 40, 40);
			//headerStyle.hover.background = MakeColorTexture(40, 40, 40);
			headerStyle.margin = new RectOffset(2, 2, 2, 2);
			headerStyle.padding = new RectOffset(5, 5, 5, 5);
			headerStyle.alignment = TextAnchor.MiddleLeft;
			headerStyle.fontStyle = FontStyle.Bold;
			
			
			//rowStyle = new GUIStyle(EditorStyles.textArea);
			rowStyle = new GUIStyle();
			rowStyle.normal.background = MakeColorTexture(25, 25, 25);
			//rowStyle.focused.background = MakeColorTexture(25, 25, 25);
			//rowStyle.active.background = MakeColorTexture(25, 25, 25);
			//rowStyle.hover.background = MakeColorTexture(40, 40, 40);
			rowStyle.margin = new RectOffset(2, 2, 2, 2);
			rowStyle.padding = new RectOffset(5, 5, 5, 5);
			rowStyle.alignment = TextAnchor.MiddleLeft;
			
		}

		[Pure]
		private static IEnumerable<KeyValuePair<object, AddressablesManager.AssetRefCount>> FilterAssets(string searchFilter, Dictionary<object, AddressablesManager.AssetRefCount> dictionary)
		{
			if (string.IsNullOrEmpty(searchFilter))
				return dictionary
					.OrderBy(kvp => kvp.Key?.ToString() ?? "");

			return dictionary
				.Where(kvp => kvp.Key?.ToString()?.ToLower().Contains(searchFilter.ToLower()) == true)
				.OrderBy(kvp => kvp.Key?.ToString() ?? "");
		}

		[Pure]
		private static Texture2D MakeColorTexture(int r, int g, int b)
		{
			Texture2D texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, new Color(r / 255f, g / 255f, b / 255f));
			texture.Apply();
			return texture;
		}
	}
}
