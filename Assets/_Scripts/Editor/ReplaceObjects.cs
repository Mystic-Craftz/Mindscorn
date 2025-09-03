using UnityEditor;
using UnityEngine;

public class ReplaceObjects : EditorWindow
{

    private static readonly Vector2Int size = new Vector2Int(250, 100);

    GameObject replacementObject;

    [MenuItem("Custom Tools/Replace Objects")]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow<ReplaceObjects>();
        window.minSize = size;
        window.maxSize = size;
    }


    private void OnGUI()
    {
        replacementObject = (GameObject)EditorGUILayout.ObjectField("Replacement Object", replacementObject, typeof(GameObject), false);
        if (GUILayout.Button("Replace"))
        {
            foreach (var selectedObject in Selection.gameObjects)
            {
                GameObject newObject = Instantiate(replacementObject, selectedObject.transform.position, selectedObject.transform.rotation);
                newObject.transform.position = selectedObject.transform.position;
                newObject.transform.rotation = selectedObject.transform.rotation;
                newObject.transform.localScale = selectedObject.transform.localScale;
                DestroyImmediate(selectedObject);
            }
        }
    }
}