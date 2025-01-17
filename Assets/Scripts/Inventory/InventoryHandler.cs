﻿using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InventoryHandler : MonoBehaviour
{
    private static InventoryHandler _inv;
    private CharacterManager _characterManager;
    private InventoryManager _inventoryManager;
    private int _playerSlots;
    private GUIManager _GUIManager;

    private ItemDatabase _itemDatabase;

    //private bool _updateInventory;
    private bool _inTerrain = true;
    private ItemMixture _itemMixture;
    private ResearchSlot _researchingSlot;
    private GameObject _inventoryPanel;
    private GameObject _slotPanel;
    
    private GameObject _inventorySlot;
    private GameObject _inventorySlotBroken;
    private GameObject _inventoryItem;

    public Sprite LockSprite;

    private List<ItemIns> _userInventory ;
    internal List<GameObject> InvSlots = new List<GameObject>();
    internal SlotEquipment[] EquiSlots = new SlotEquipment[14];

    private int _slotAmount =30;

    // Use this for initialization
    void Start()
    {
        Debug.Log("***IH*** Start!");
        //Instance
        _inv = InventoryHandler.Instance();
        _itemDatabase = ItemDatabase.Instance();
        _characterManager = CharacterManager.Instance();
        _inventoryManager = InventoryManager.Instance();
        _itemMixture = ItemMixture.Instance();
        _researchingSlot = ResearchSlot.Instance();
        _GUIManager = GUIManager.Instance();

        _inventoryPanel = GameObject.Find("Inventory Panel");
        _slotPanel = _inventoryPanel.transform.Find("Slot Panel").gameObject;

        _inventorySlotBroken = Resources.Load<GameObject>("Prefabs/SlotInventoryBroken");
        _inventorySlot = Resources.Load<GameObject>("Prefabs/SlotInventory");
        _inventoryItem = Resources.Load<GameObject>("Prefabs/Item");
      
        //Disable All buttons inside building 
        var insideBuilding = GameObject.Find("Building Interior");
        if (insideBuilding != null)
        {
            _inTerrain = false;
            GameObject.Find("ButtonShop").GetComponent<Button>().interactable = false;
            GameObject.Find("ButtonSetting").GetComponent<Button>().interactable = false;
            GameObject.Find("ButtonAbout").GetComponent<Button>().interactable = false;
            GameObject.Find("Item Mixture").GetComponent<Button>().interactable = false;
            GameObject.Find("Research Slot").GetComponent<Button>().interactable = false;
            GameObject.Find("PlayerPic").GetComponent<Button>().interactable = false;
            GameObject.Find("CharacterPic").GetComponent<Button>().interactable = false;
        }
        _playerSlots = _characterManager.CharacterSetting.CarryCnt;
        _userInventory = _inventoryManager.UserInvItems;
        //Debug.Log("IH-UserInventory.Count = " + _userInventory.Count);
        //foreach (var item in _userInventory) item.Print();
        //Equipment
        EquiSlots = _inventoryPanel.GetComponentsInChildren<SlotEquipment>();
        for (int i = 0; i < EquiSlots.Length; i++)
        {
            EquiSlots[i].name = "Slot " + EquiSlots[i].EquType;
            EquiSlots[i].GetComponentInChildren<TextMeshProUGUI>().text = EquiSlots[i].EquType.ToString();
            ItemEquipment equipmentItem = EquiSlots[i].GetComponentInChildren<ItemEquipment>();
            equipmentItem.ItemIns = null;
            equipmentItem.name = "Empty";
            foreach (var equipmentIns in _userInventory)
            {
                if (!equipmentIns.UserItem.Equipped)
                    continue;
                if ( ( (equipmentIns.Item.Type == ItemContainer.ItemType.Weapon || equipmentIns.Item.Type == ItemContainer.ItemType.Tool)
                      &&
                       ( (EquiSlots[i].EquType == ItemContainer.PlaceType.Right && equipmentIns.UserItem.Order == (int)ItemContainer.PlaceType.Right) || 
                         (EquiSlots[i].EquType == ItemContainer.PlaceType.Left  && equipmentIns.UserItem.Order == (int)ItemContainer.PlaceType.Left) ))
                    ||
                     (equipmentIns.Item.PlaceHolder == EquiSlots[i].EquType && equipmentIns.Item.Type == ItemContainer.ItemType.Equipment)
                    )
                {
                    equipmentItem.ItemIns = equipmentIns;
                    equipmentItem.name = equipmentIns.Item.Name;
                    equipmentItem.GetComponent<Image>().sprite = equipmentIns.Item.GetSprite();
                    break;
                }
            }
        }
        //Item Mixture
        InitMixture(_characterManager.CharacterMixture);
        //Researching
        InitResearching(_characterManager.CharacterResearching);
        //Inventory
        for (int i = 0; i < _slotAmount; i++)
        {
            if (i < _playerSlots)
            {
                InvSlots.Add(Instantiate(_inventorySlot));
                InvSlots[i].GetComponent<SlotData>().SlotIndex = i;
            }
            else
                InvSlots.Add(Instantiate(_inventorySlotBroken));
            InvSlots[i].transform.SetParent(_slotPanel.transform);
            if (i < _playerSlots)
            {
                GameObject itemObject = Instantiate(_inventoryItem);
                ItemData data = itemObject.GetComponent<ItemData>();
                itemObject.transform.SetParent(InvSlots[i].transform);
                data.SlotIndex = i;

                foreach (var itemIns in _userInventory)
                {
                    if (itemIns.UserItem.Equipped || itemIns.UserItem.Stored)
                        continue;
                    if (itemIns.UserItem.Order == i)
                    {
                        data.ItemIns = itemIns;
                        itemObject.transform.position = Vector2.zero;
                        InvSlots[i].name = itemObject.name = itemIns.Item.Name;
                        itemObject.GetComponent<Image>().sprite = itemIns.Item.GetSprite();
                        itemObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = itemIns.UserItem.StackCnt > 1 ? itemIns.UserItem.ToString() : "";
                        break;
                    }

                }
            }
            else
            {
                if (i == _playerSlots)
                {
                    Button button = InvSlots[i].GetComponentInChildren<Button>();
                    button.GetComponent<Image>().sprite = LockSprite;
                    InvSlots[i].name = button.name = "Lock";
                    if (_inTerrain)
                        button.interactable = true;
                }
            }
            InvSlots[i].transform.localScale = Vector3.one;
        }
        _inventoryManager.PrintInventory();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_inventoryPanel.activeSelf)
                _inventoryPanel.SetActive(false);
            else
                _inventoryPanel.SetActive(true);
        }
    }
    public bool UseItem(ItemIns item)
    {
        if (item == null)
            return false;
        //Use 1/10 of Max energy to use an item
        if (_characterManager.UseEnergy(_characterManager.CharacterSetting.MaxEnergy / 10))
        {
            string itemStatus = _characterManager.CharacterSettingUseItem(item, true);
            if (itemStatus != "")
                PrintMessage(itemStatus, Color.yellow);
            return true;
        }
        PrintMessage("Not enough energy to use this item", Color.yellow);
        return false;
    }
    public void UnUseItem(ItemIns item)
    {
        if (item == null)
            return;
        _characterManager.CharacterSettingUnuseItem(item, true);
    }
    internal bool HaveAvailableSlot()
    {
        var carryItems = _userInventory.FindAll(l => !l.UserItem.Equipped && !l.UserItem.Stored).Count;
        if (carryItems < _playerSlots)
            return true;
        return false;
    }
    internal int GetAvailableSlot()
    {
        //print("AvailableSlot: " + _invCarry.Count +" "+_playerSlots);
        int availableSlot = -1;
        if (!HaveAvailableSlot())
            return availableSlot;
        for (int i = 0; i < _playerSlots; i++)
        {
            availableSlot = i;
            foreach (var itemIns in _userInventory)
            {
                if (itemIns.UserItem.Equipped || itemIns.UserItem.Stored)
                    continue;
                if (itemIns.UserItem.Order == i)
                {
                    availableSlot = -1;
                    break;
                }

            }
            if (availableSlot == i)
                break;
        }
        return availableSlot;
    }
    internal bool UseEnergy(int amount)
    {
        return _characterManager.UseEnergy(amount);
    }
    public void UpdateInventory()
    {
        Debug.Log("IH-Lets Save Inv at" + DateTime.Now);
        _inventoryManager.UpdateInventory = true;
    }
    public void DeleteFromInventory(ItemIns itemIns)
    {
        _inventoryManager.DeleteItemFromInventory(itemIns);
    }
    //Should match with the AddItemToInventory in CharacterManager
    public bool AddItemToInventory(ItemContainer item, int stackCnt =1,int order=-1)
    {
        print("AddItemToInventory: " +item.MyInfo()+ " stackCnt= " + stackCnt+ " order="+ order);
        if (item.MaxStackCnt < stackCnt)
            throw new Exception("IH-Invalid Item!!! MaxStackCnt < stackCnt ");
        if (ItemIndexInInventory(item.Id)!= -1)
        {
            if (!TryStackInInventory(item.Id, stackCnt, order))
            {
                if (item.Unique)
                {
                    PrintMessage("You Can Only Carry one of this item!", Color.yellow);
                    return false;
                }
            }
            else
                return true;
        }
        if (order == -1)
        {
            order = GetAvailableSlot();
            if (order == -1)
            {
                PrintMessage("Not Enough room in inventory", Color.red);
                return false;
            }
        }
        var itemData = InvSlots[order].transform.GetComponentInChildren<ItemData>();
        itemData.ItemIns = new ItemIns(item, new UserItem(item,_characterManager.UserPlayer.Id, stackCnt, order));
        itemData.LoadItem();
        _inventoryManager.AddItemToInventory(itemData.ItemIns);
        return true;
    }
    private int ItemIndexInInventory(int itemId)
    {
        foreach (var itemIns in _userInventory)
        {
            if (itemIns.UserItem.Equipped|| itemIns.UserItem.Stored)
                continue;
            if (itemIns.Item.Id == itemId)
                return itemIns.UserItem.Order;
        }
        return -1;
    }
    private bool TryStackInInventory(int itemId, int stackCnt,int order)
    {
        foreach (var itemIns in _userInventory)
        {
            if (itemIns.UserItem.Equipped || itemIns.UserItem.Stored)
                continue;
            if (itemIns.Item.Id == itemId && itemIns.Item.MaxStackCnt >= stackCnt + itemIns.UserItem.StackCnt)
            {
                if (order == -1 || itemIns.UserItem.Order == order)
                {
                    itemIns.UserItem.StackCnt += stackCnt;
                    return true;
                }
            }
        }
        return false;
    }
    private ItemIns GetItemInInventory(int itemId)
    {
        foreach (var itemIns in _userInventory)
        {
            if (itemIns.UserItem.Equipped || itemIns.UserItem.Stored)
                continue;
            if (itemIns.Item.Id == itemId)
                return itemIns;
        }
        return null;
    }
    public bool InventoryPanelStat()
    {
        return _inventoryPanel.activeSelf;
    }
    public void GoToRecipeScene()
    {
        _itemMixture = ItemMixture.Instance();
        if (!_itemMixture.ItemLocked)
        {
            if (_itemMixture.IsEmpty())
            {
                BuildTrainStarter();
                SceneManager.LoadScene(SceneSettings.SceneIdForRecipes);
            }
        }
    }
    internal void SetInventoryPanel(bool value)
    {
        _inventoryPanel.SetActive(value);
    }
    public void GoToResearchScene()
    {
        _researchingSlot = ResearchSlot.Instance();
        if (!_researchingSlot.ItemLocked)
        {
            if (_researchingSlot.IsEmpty())
            {
                BuildTrainStarter();
                SceneManager.LoadScene(SceneSettings.SceneIdForResearch);
            }
        }
    }
    public void GoToMailScene()
    {
        BuildTrainStarter();
        //switch the scene
        SceneManager.LoadScene(SceneSettings.SceneIdForMailMessage);
    }
    public void GoToProfileScene()
    {
        BuildTrainStarter();
        //switch the scene
        SceneManager.LoadScene(SceneSettings.SceneIdForProfile);
    }
    public void GoToMenuSceneOption()
    {
        BuildTrainStarter("InventoryHandler","Option");
        //switch the scene
        SceneManager.LoadScene(SceneSettings.SceneIdForMenu);
    }

    public void GoToMenuSceneSocial()
    {
        BuildTrainStarter("InventoryHandler", "Social");
        //switch the scene
        SceneManager.LoadScene(SceneSettings.SceneIdForMenu);
    }
    public void GoToCreditScene()
    {
        BuildTrainStarter();
        //switch the scene
        SceneManager.LoadScene(SceneSettings.SceneIdForCredits);
    }
    public void GoToStoreScene()
    {
        //OfferListHandler
        BuildTrainStarter("InventoryHandlerStarter", "Terrain");        
        //switch the scene
        SceneManager.LoadScene(SceneSettings.SceneIdForStore);
    }
    public void GoToSlotShop()
    {
        //OfferListHandler
        BuildTrainStarter("InventoryHandlerStarter", "Inventory");
        //switch the scene
        SceneManager.LoadScene(SceneSettings.SceneIdForStore);
    }
    private void BuildTrainStarter(string domain = null,string content = null)
    {
        //Preparing to return to terrain
        GameObject go = new GameObject();
        //Make go unDestroyable
        GameObject.DontDestroyOnLoad(go);
        var starter = go.AddComponent<SceneStarter>();
        Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        starter.PreviousPosition = player.position;
        starter.ShowInventory = true;
        go.name = domain ?? "Unknown in Inv";
        starter.Content = content ?? "Unknown in Inv";
    }
    private void InitResearching(CharacterResearching researching)
    {
        if (researching == null)
            _researchingSlot.LoadEmpty();
        else if (researching.Id==0)
            _researchingSlot.LoadEmpty();
        else 
            _researchingSlot.LoadResearch(researching);
    }
    private void InitMixture(CharacterMixture playerMixture)
    {
        if (playerMixture == null)
            _itemMixture.LoadEmpty();
        else if (playerMixture.Id == 0)
            _itemMixture.LoadEmpty();
        else
            _itemMixture.LoadItem(playerMixture);
    }
    public void SaveCharacterMixture(int itemId, int stackCnt, DateTime time)
    {
        _characterManager.SaveCharacterMixture(itemId, stackCnt, time);
    }
    public Recipe CheckRecipes(int first, int second)
    {
        return _characterManager.FindUserRecipes( first,  second);
    }
    internal void PrintMessage(string message,Color color)
    {
        _GUIManager.PrintMessage(message, color);
    }
    public ItemContainer BuildItemFromDatabase(int id)
    {
        return _itemDatabase.GetItemById(id);
    }
    public bool ElementToolUse(ElementIns element=null)
    {
        ElementIns.ElementType targetType = element != null ? element.Type : ElementIns.ElementType.Hole;
        for (int i = 0; i < EquiSlots.Length; i++)
        {
            var toolEquipment = EquiSlots[i].GetComponentInChildren<ItemEquipment>();
            if (toolEquipment==null)
                continue;
            var toolIns = toolEquipment.ItemIns;
            if (toolIns == null)
                continue;
            if (toolIns.Item.Type == ItemContainer.ItemType.Tool
                &&
                (EquiSlots[i].EquType == ItemContainer.PlaceType.Right && toolIns.UserItem.Order == (int) ItemContainer.PlaceType.Right ||
                 EquiSlots[i].EquType == ItemContainer.PlaceType.Left && toolIns.UserItem.Order == (int) ItemContainer.PlaceType.Left)
                &&
                toolIns.UserItem.TimeToUse > 0 
                && 
                targetType == toolIns.Item.FavoriteElement
                    )
                {
                    toolEquipment.UseItem(1);
                    UpdateInventory();
                    return true;
                }
        }
        PrintMessage("You don't have a right tool to use", Color.yellow);
        return false;
    }
    //Middle Man
    public void AddCharacterSetting(string field, float value)
    {
        _characterManager.AddCharacterSetting("Experience", value);
    }
    internal float GetCrafting()
    {
        return _characterManager.GetCharacterAttribute("Crafting");
    }
    public static InventoryHandler Instance()
    {
        if (!_inv)
        {
            _inv = FindObjectOfType(typeof(InventoryHandler)) as InventoryHandler;
            if (!_inv)
                Debug.LogError("There needs to be one active InventoryHandler script on a GameObject in your scene.");
        }
        return _inv;
    }
}