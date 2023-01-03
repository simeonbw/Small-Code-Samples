using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utility.Inventories;

public class InventorySlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _itemIcon;
    [SerializeField] private Image _durabilityBar;
    [SerializeField] private TMP_Text _amountText;

    [SerializeField] private GameObject _selectedIcon;
    public GameObject SelectedIcon { get => _selectedIcon; }

    private int _slotIndex;
    private Inventory _inventory;
    private InventoryMenu _menu;

    public bool ContainsItem { get => _itemIcon.gameObject.activeSelf; }

    /// <summary>
    /// Initialize the inventory slot by providing its index and the owning inventory
    /// </summary>
    public void InitSlot(int index, Inventory inventory)
    {
        _slotIndex = index;
        _inventory = inventory;
        _menu = InventoryMenu.Instance;
        UpdateSlot();
    }

    /// <summary>
    /// Update the inventory slot data
    /// </summary>
    public void UpdateSlot()
    {
        ItemInstance item = _inventory.InventorySlots[_slotIndex];

        if (item.item == null)
        {
            _itemIcon.sprite = null;
            _itemIcon.gameObject.SetActive(false);
            _durabilityBar.gameObject.SetActive(false);
            _amountText.text = "";
            return;
        }
        
        ItemSlotIcon slotIcon = item.GetIcon();
        _itemIcon.sprite = slotIcon.icon;
        _itemIcon.color = slotIcon.tint;
        _itemIcon.gameObject.SetActive(true);

        if(item.item.category == Item_Category.Weapon || item.item.category == Item_Category.Tool || item.item.category == Item_Category.Armor)
        {
            var equipment = (Item_Equipment)item.item;

            if (item.dynamic != null && item.dynamic.durability != -1)
            {
                _durabilityBar.fillAmount = item.dynamic.durability / equipment.durability;
                _durabilityBar.gameObject.SetActive(true);
            }
            else
            {
                _durabilityBar.gameObject.SetActive(false);
            }
        }

        _amountText.text = item.amount > 1 ? item.amount.ToString() : "";
    }
   
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && (_menu.ItemSelected || _inventory.InventorySlots[_slotIndex].item != null))
        {
            _menu.OnSlotLeftClicked(_slotIndex, _inventory);
        }
        else if (eventData.button == PointerEventData.InputButton.Right && (_menu.ItemSelected || _inventory.InventorySlots[_slotIndex].item != null))
        {
            _menu.OnSlotRightClicked(_slotIndex, _inventory);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ContainsItem)
        {
            _menu.ShowItemDetails(_inventory.InventorySlots[_slotIndex]);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AshMenuController.Instance.DetailsMenu.HideObjectDetails();
    }
}