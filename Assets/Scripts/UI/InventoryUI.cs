using System.Collections.Generic;

public class InventoryUI : BaseBehaviour
{
    public ItemUI[] Slots;

    public List<Item> Inventory => Player.Instance.Inventory;
    
    public override void OnEnable()
    {
        base.OnEnable();

        int Inventory_Len = Inventory.Count;
        int Slots_Len     = Slots.Length;

        for (int i = 0; i < Slots_Len; i++)
        {
            ItemUI Slot = Slots[i];
            if (i >= Inventory_Len)
            {
                //Make sure the slot is hidden
                Slot.Root.gameObject.SetActive(false);
                continue;
            }
            
            //Show the Slot
            Item Slot_Item = Inventory[i];
            Slot.Root.gameObject.SetActive(true);
            Slot.Icon.texture = Slot_Item.ItemIcon;
            Slot.Title.text   = Slot_Item.ItemName;
        }
    }
}
