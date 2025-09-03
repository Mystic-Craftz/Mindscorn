// Assets/Editor/InventoryItemSOEditor.cs
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InventoryItemSO))]
public class InventoryItemSOEditor : Editor
{
    SerializedProperty itemID;
    SerializedProperty itemName;
    SerializedProperty itemDescription;
    SerializedProperty itemIcon;
    SerializedProperty itemType;
    SerializedProperty pickupSound;
    SerializedProperty useSound;
    SerializedProperty isStackable;
    SerializedProperty isUseable;
    SerializedProperty text;

    void OnEnable()
    {
        itemID = serializedObject.FindProperty("itemID");
        itemName = serializedObject.FindProperty("itemName");
        itemDescription = serializedObject.FindProperty("itemDescription");
        itemIcon = serializedObject.FindProperty("itemIcon");
        itemType = serializedObject.FindProperty("itemType");
        pickupSound = serializedObject.FindProperty("pickupSound");
        useSound = serializedObject.FindProperty("useSound");
        isStackable = serializedObject.FindProperty("isStackable");
        isUseable = serializedObject.FindProperty("isUseable");
        text = serializedObject.FindProperty("text");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(itemID);
        EditorGUILayout.PropertyField(itemName);
        EditorGUILayout.PropertyField(itemDescription);
        EditorGUILayout.PropertyField(itemIcon);
        EditorGUILayout.PropertyField(itemType);
        EditorGUILayout.PropertyField(pickupSound);
        EditorGUILayout.PropertyField(useSound);
        EditorGUILayout.PropertyField(isStackable);
        EditorGUILayout.PropertyField(isUseable);

        // Show `text` only if itemType is File
        if ((ItemType)itemType.enumValueIndex == ItemType.File)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Text Entries", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Entry"))
            {
                text.arraySize++;
            }

            for (int i = 0; i < text.arraySize; i++)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);

                SerializedProperty entry = text.GetArrayElementAtIndex(i);
                entry.stringValue = EditorGUILayout.TextArea(entry.stringValue, GUILayout.MinHeight(60));

                if (GUILayout.Button("Remove Entry"))
                {
                    text.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}