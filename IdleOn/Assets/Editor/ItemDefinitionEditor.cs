using UnityEditor;
using UnityEngine;
using IdleOn.Items;
using IdleOn.Core;

[CustomEditor(typeof(ItemDefinition))]
public class ItemDefinitionEditor : Editor
{
    // Identity
    SerializedProperty _itemId;
    SerializedProperty _displayName;
    SerializedProperty _description;
    SerializedProperty _itemType;
    SerializedProperty _icon;
    SerializedProperty _sellValue;

    // Rarity
    SerializedProperty _rarity;

    // Equipment
    SerializedProperty _equipmentSlot;
    SerializedProperty _statBonuses;

    // Consumable
    SerializedProperty _consumableType;

    void OnEnable()
    {
        _itemId          = serializedObject.FindProperty("ItemId");
        _displayName     = serializedObject.FindProperty("DisplayName");
        _description     = serializedObject.FindProperty("Description");
        _itemType        = serializedObject.FindProperty("ItemType");
        _icon            = serializedObject.FindProperty("Icon");
        _sellValue       = serializedObject.FindProperty("SellValue");
        _rarity          = serializedObject.FindProperty("Rarity");
        _equipmentSlot   = serializedObject.FindProperty("EquipmentSlot");
        _statBonuses     = serializedObject.FindProperty("StatBonuses");
        _consumableType  = serializedObject.FindProperty("ConsumableType");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_itemId);
        EditorGUILayout.PropertyField(_displayName);
        EditorGUILayout.PropertyField(_description);
        EditorGUILayout.PropertyField(_icon);
        EditorGUILayout.PropertyField(_sellValue);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rarity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_rarity);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Type", EditorStyles.boldLabel);

        ItemType prevType = (ItemType)_itemType.enumValueIndex;
        EditorGUILayout.PropertyField(_itemType);
        ItemType newType = (ItemType)_itemType.enumValueIndex;

        if (newType != prevType)
            ResetIrrelevantFields(newType);

        switch (newType)
        {
            case ItemType.Equipment:
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Equipment", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_equipmentSlot);
                EditorGUILayout.PropertyField(_statBonuses, includeChildren: true);
                break;

            case ItemType.Consumable:
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Consumable", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_consumableType);
                break;

            case ItemType.Material:
                // No type-specific fields
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    void ResetIrrelevantFields(ItemType newType)
    {
        switch (newType)
        {
            case ItemType.Equipment:
                _consumableType.enumValueIndex = 0;
                break;

            case ItemType.Consumable:
                _equipmentSlot.enumValueIndex = 0;
                ZeroStatBonuses();
                break;

            case ItemType.Material:
                _equipmentSlot.enumValueIndex = 0;
                ZeroStatBonuses();
                _consumableType.enumValueIndex = 0;
                break;
        }
    }

    void ZeroStatBonuses()
    {
        _statBonuses.FindPropertyRelative("STR").intValue        = 0;
        _statBonuses.FindPropertyRelative("AGI").intValue        = 0;
        _statBonuses.FindPropertyRelative("WIS").intValue        = 0;
        _statBonuses.FindPropertyRelative("LUK").intValue        = 0;
        _statBonuses.FindPropertyRelative("MaxHP").floatValue    = 0f;
        _statBonuses.FindPropertyRelative("MaxMP").floatValue    = 0f;
        _statBonuses.FindPropertyRelative("ATKMin").floatValue   = 0f;
        _statBonuses.FindPropertyRelative("ATKMax").floatValue   = 0f;
        _statBonuses.FindPropertyRelative("DEF").floatValue      = 0f;
        _statBonuses.FindPropertyRelative("ACC").floatValue      = 0f;
        _statBonuses.FindPropertyRelative("CRITChance").floatValue = 0f;
        _statBonuses.FindPropertyRelative("MoveSpeed").floatValue  = 0f;
    }
}
