using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum ItemType
{
    Currency,
    Consumable
}

public enum ConsumableEffect
{
    DoingNothing,
    HealHP,
    RestoreMana,
    RestoreStamina
}

[CreateAssetMenu(menuName = "Game/Item")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string id;
    [SerializeField] private ItemType type;

    [Header("UI")]
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;

    [Header("Consumable")]
    [SerializeField] private ConsumableEffect effect;
    [SerializeField] private int value;

    public string Id => id;
    public ItemType Type => type;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public ConsumableEffect Effect => effect;
    public int Value => value;

#if UNITY_EDITOR
    public bool EditorApplyDefinition(
        string newId,
        ItemType newType,
        string newDisplayName,
        Sprite newIcon,
        ConsumableEffect newEffect,
        int newValue,
        out string error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(newId))
        {
            error = "Item id is null or empty.";
            return false;
        }

        id = newId.Trim();
        type = newType;
        displayName = newDisplayName ?? string.Empty;
        icon = newIcon;
        effect = newEffect;
        value = newValue;

        EditorUtility.SetDirty(this);
        return true;
    }
#endif
}
