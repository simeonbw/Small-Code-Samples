using System;
using System.Collections.Generic;
using UnityEngine;
using Utility.Inventories;

public class Inventory : MonoBehaviour
{

    protected List<ItemInstance> _inventorySlots = new List<ItemInstance>();

    public List<ItemInstance> InventorySlots { get => _inventorySlots; }

    [SerializeField] protected int _inventorySize;
    public int InventorySize { get => _inventorySlots.Count; }

    public event Action<int> SlotUpdated;

    private void Awake()
    {
        InitInventory();
    }

    protected virtual void InitInventory()
    {
        for (int i = 0; i < _inventorySize; i++)
        {
            _inventorySlots.Add(new ItemInstance(null, -1, null));
        }
    }

    /// <summary>
    /// Invoke the delegate to update the inventory slot. This is mainly used for UI
    /// </summary>
    public void UpdateSlot(int index)
    {
        SlotUpdated?.Invoke(index);
    }

    /// <summary>
    /// Add item to inventory and outputs remaining if not all could fit
    /// </summary>
    /// <returns>
    /// True if it was successfully added
    /// </returns>
    public virtual bool AddItem(ItemInstance item, out int remaining)
    {
        if (item.item.maxStackSize > 1)
        {
            if (FindFreeStack(item.item, out int indx))
            {
                if (_inventorySlots[indx].amount + item.amount > item.item.maxStackSize)
                {
                    _inventorySlots[indx] = new ItemInstance(item.item, item.item.maxStackSize, item.dynamic);
                    UpdateSlot(indx);
                    return AddItem(new ItemInstance(item.item, (_inventorySlots[indx].amount + item.amount) - item.item.maxStackSize, item.dynamic), out remaining);
                }

                _inventorySlots[indx] = new ItemInstance(item.item, _inventorySlots[indx].amount + item.amount);
                UpdateSlot(indx);
                remaining = 0;
                return true;
            }
            else
            {
                if (FindEmptySlot(out indx))
                {
                    if (item.amount > item.item.maxStackSize)
                    {
                        _inventorySlots[indx] = new ItemInstance(item.item, item.item.maxStackSize, item.dynamic);
                        UpdateSlot(indx);
                        return AddItem(new ItemInstance(item.item, item.amount - item.item.maxStackSize, item.dynamic), out remaining);
                    }

                    _inventorySlots[indx] = item;
                    UpdateSlot(indx);
                    remaining = 0;
                    return true;
                }
                else
                {
                    remaining = item.amount;
                    return false;
                }
            }
        }
        else
        {
            if (FindEmptySlot(out int indx))
            {
                _inventorySlots[indx] = new ItemInstance(item.item, 1, item.dynamic);
                UpdateSlot(indx);

                if (item.amount > 1)
                {
                    return AddItem(new ItemInstance(item.item, item.amount - 1, item.dynamic), out remaining);
                }

                remaining = 0;
                return true;
            }
            else
            {
                remaining = item.amount;
                return false;
            }
        }
    }

    /// <summary>
    /// Add item to specific inventory slot index and outputs remaining if not all could fit
    /// </summary>
    /// <returns>
    /// True if it was successfully added
    /// </returns>
    public virtual bool AddItemToIndex(ItemInstance item, int index, out int remaining)
    {
        if (IsSlotEmpty(index) && item.amount <= item.item.maxStackSize)
        {
            _inventorySlots[index] = item;
            UpdateSlot(index);
            remaining = 0;
            return true;
        }

        if(_inventorySlots[index].item == item.item)
        {
            if(item.amount + _inventorySlots[index].amount > item.item.maxStackSize)
            {
                remaining = Math.Abs(item.amount - _inventorySlots[index].amount);

                _inventorySlots[index].amount = item.item.maxStackSize;
                UpdateSlot(index);
                return true;
            }

            _inventorySlots[index].amount += item.amount;
            UpdateSlot(index);
            remaining = 0;
            return true;
        }

        remaining = item.amount;
        return false;
    }

    /// <summary>
    /// Increase amount of item at specified index
    /// </summary>
    /// <returns>
    /// True if it was successfully increased
    /// </returns>
    public bool IncreaseAmountAtIndex(int index, int amount)
    {
        if (IsSlotEmpty(index) && _inventorySlots[index].amount + amount <= _inventorySlots[index].item.maxStackSize)
        {
            _inventorySlots[index].amount += amount;
            UpdateSlot(index);
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Remove item at specified index
    /// </summary>
    /// <returns>
    /// True if it was successfully removed
    /// </returns>
    public virtual bool RemoveItemAtIndex(int index)
    {
        if (IsSlotEmpty(index))
        {
            return false;
        }

        ClearInventorySlot(index);
        UpdateSlot(index);
        return true;
    }

    /// <summary>
    /// Clear inventory slot at index. This will remove the item completely
    /// </summary>
    public void ClearInventorySlot(int index)
    {
        _inventorySlots[index].item = null;
        _inventorySlots[index].amount = -1;
        _inventorySlots[index].dynamic = null;
    }

    /// <summary>
    /// Remove specified amount of item from inventory. If index is specified it will try to remove it from the index
    /// </summary>
    /// <returns>
    /// True if it was successfully removed
    /// </returns>
    public virtual bool RemoveItem(Item item, int amount, int index = -1)
    {
        if (index != -1)
        {
            if (_inventorySlots[index].item != null && amount > 0)
            {
                if (amount >= _inventorySlots[index].amount)
                {
                    ClearInventorySlot(index);
                    UpdateSlot(index);
                }
                else
                {
                    _inventorySlots[index].amount -= amount;
                    UpdateSlot(index);
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            int total = GetTotalAmountOfItem(item);

            if (total >= amount)
            {
                List<ItemInstance> list = _inventorySlots.FindAll(i => i.item != null && i.item == item);

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].amount < total)
                    {
                        total -= list[i].amount;

                        ClearInventorySlot(_inventorySlots.IndexOf(list[i]));
                        UpdateSlot(_inventorySlots.IndexOf(list[i]));
                    }
                    else
                    {
                        _inventorySlots[_inventorySlots.IndexOf(list[i])].amount -= total;
                        UpdateSlot(_inventorySlots.IndexOf(list[i]));
                        break;
                    }

                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Splits a stack of items at the specified index and amount
    /// </summary>
    /// <returns>
    /// Item Instance which can be used to attach to player pointer
    /// </returns>
    public virtual ItemInstance SplitStack(int stackIndex, int amount)
    {
        if (IsSlotEmpty(stackIndex) || (_inventorySlots[stackIndex].item.maxStackSize <= 1 && _inventorySlots[stackIndex].amount > amount))
        {
            return null;
        }

        _inventorySlots[stackIndex].amount -= amount;
        UpdateSlot(stackIndex);

        return new ItemInstance(_inventorySlots[stackIndex].item, amount, _inventorySlots[stackIndex].dynamic);
    }

    /// <summary>
    /// Returns true if the inventory slot at specified index is empty
    /// </summary>
    public bool IsSlotEmpty(int index)
    {
        return _inventorySlots[index].item == null;
    }

    /// <summary>
    /// Find the first available empty inventory slot and output the index
    /// </summary>
    /// <returns>
    /// True if it was successfully in finding an empty slot
    /// </returns>
    public virtual bool FindEmptySlot(out int index)
    {
        index = _inventorySlots.FindIndex(i => i.item == null);
        return index != -1;
    }

    /// <summary>
    /// Find the first stack of specified item that has not reached its max stack size and output the index
    /// </summary>
    /// <returns>
    /// True if it was successfully in finding a free stack
    /// </returns>
    public bool FindFreeStack(Item item, out int index)
    {
        index = _inventorySlots.FindIndex(i => i != null && i.item == item && i.amount < i.item.maxStackSize);
        return index != -1;

    }

    /// <summary>
    /// Return the total amount of specified item in inventory
    /// </summary>
    public int GetTotalAmountOfItem(Item item)
    {
        List<ItemInstance> result = _inventorySlots.FindAll(i => i != null && i.item == item);

        if (result.Count > 0)
        {
            int total = 0;
            foreach (ItemInstance iS in result)
            {
                total += iS.amount;
            }
            return total;
        }

        return 0;
    }

    /// <summary>
    /// Returns true if the specified index is within the inventory size
    /// </summary>
    public bool IsIndexInBounds(int index)
    {
        return index >= 0 && index < _inventorySlots.Count;
    }

    /// <summary>
    /// Returns all indices of items of the specified type in the inventory
    /// </summary>
    public List<int> GetItemsByType(Item_Category type)
    {
        List<int> indices = new List<int>();

        foreach (ItemInstance item in _inventorySlots)
        {
            if (item.item.category == type)
            {
                indices.Add(_inventorySlots.IndexOf(item));
            }
        }

        return indices;
    }
}
