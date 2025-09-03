using UnityEditor;
using UnityEngine;

public class Randomize : EditorWindow
{

    private static readonly Vector2Int size = new Vector2Int(250, 100);

    [MenuItem("Custom Tools/Random Rotation")]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow<Randomize>();
        window.minSize = size;
        window.maxSize = size;
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Rotate Selected"))
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                selectedObjects[i].transform.rotation = Quaternion.Euler(Vector3.up * Random.Range(0f, 360f));
            }
        }
        if (GUILayout.Button("Scale Children"))
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                selectedObjects[i].transform.localScale = Vector3.one * Random.Range(0f, 1f);
            }
        }
    }
}