using UnityEngine;

public enum ItemType { Inventory, KeyItem, File }

[System.Serializable]
public class InventoryItem
{
    public InventoryItemSO data;
    public int quantity = 1;
}
