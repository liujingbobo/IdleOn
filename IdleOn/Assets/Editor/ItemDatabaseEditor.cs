using UnityEditor;
using UnityEngine;
using IdleOn.Items;

[CustomEditor(typeof(ItemDatabase))]
public class ItemDatabaseEditor : Editor
{
    private const string ScanRoot = "Assets/_assets/ScriptableObjects/Items";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Sync Items from Folder"))
            SyncItems((ItemDatabase)target);
    }

    [MenuItem("Tools/IdleOn/Sync Item Database")]
    private static void SyncFromMenu()
    {
        var guids = AssetDatabase.FindAssets("t:ItemDatabase");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[ItemDatabase] No ItemDatabase asset found in project.");
            return;
        }

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var db   = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
        SyncItems(db);
    }

    private static void SyncItems(ItemDatabase db)
    {
        var so       = new SerializedObject(db);
        var listProp = so.FindProperty("allItems");

        var guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { ScanRoot });
        int added = 0;

        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var item      = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
            if (item == null) continue;

            if (!IsInList(listProp, item))
            {
                int index = listProp.arraySize;
                listProp.InsertArrayElementAtIndex(index);
                listProp.GetArrayElementAtIndex(index).objectReferenceValue = item;
                added++;
            }
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log($"[ItemDatabase] Sync complete — {added} item(s) added, {listProp.arraySize} total.");
    }

    private static bool IsInList(SerializedProperty listProp, Object target)
    {
        for (int i = 0; i < listProp.arraySize; i++)
        {
            if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == target)
                return true;
        }
        return false;
    }
}
