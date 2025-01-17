﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using TMPro;

public class OfferListHandler : MonoBehaviour {
    private ItemDatabase _itemDatabase;
    private MessagePanelHandler _messagePanelHandler;

    public GameObject OfferContent;
    public Sprite CoinSprite;
    public Sprite GemSprite;
    public Sprite MoneySprite;
    public Sprite CarryCntSprite;
    public Sprite Life;

    private GameObject _contentPanel;
    private GameObject _clickedButton;
    private CharacterManager _characterManager;
    private InventoryManager _inventoryManager;
    private List<Offer> _offers = new List<Offer>();

    private string _backScene;
    void Awake()
    {
        _itemDatabase = ItemDatabase.Instance();
        _messagePanelHandler = MessagePanelHandler.Instance();
        _characterManager =CharacterManager.Instance();
        _inventoryManager = InventoryManager.Instance();
        _contentPanel = GameObject.Find("ContentPanel");
    }

    void Start()
    {
        List<Offer> offers = _itemDatabase.GetOffers();
        var offerCnt = 0;
        if (offers.Count == 0)
            throw new Exception("Offers count is ZERO");
        SetBackScene();
        _offers = offers.OrderBy(o => o.PayProd).ThenBy(o => o.IsSpecial).ToList();
        for (int i = 0; i < _offers.Count; i++)
        {
            if (!_offers[i].IsEnable)
                continue;
            _offers[i] = NormalizeOffer(_offers[i]);
            if (!_offers[i].IsEnable)
                continue;
            GameObject offerObject = Instantiate(OfferContent);
            offerObject.transform.SetParent(_contentPanel.transform);
            offerObject.transform.name = _offers[i].NameGenerator();

            var images = offerObject.GetComponentsInChildren<Image>();
            var texts = offerObject.GetComponentsInChildren<TextMeshProUGUI>();
            var buttons = offerObject.GetComponentsInChildren<Button>();

            images[1].sprite = GetSprite(_offers[i].SellProd);
            texts[0].text = _offers[i].SellAmount.ToString();

            buttons[0].name = i.ToString();
            buttons[0].onClick.AddListener(ShopOffer);
            images[3].sprite = GetSprite(_offers[i].PayProd);
            texts[1].text = _offers[i].PayProd == "Money" ? 
                (_offers[i].PayAmount - 0.01f).ToString(CultureInfo.InvariantCulture) //make it XX.99$
                : _offers[i].PayAmount.ToString();
            offerObject.transform.localScale = Vector3.one;
            offerCnt++;
        }
        RectTransform rt = _contentPanel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, offerCnt/5 *100);
    }
    private Offer NormalizeOffer(Offer offer)
    {
        switch (offer.SellProd)
        {
            case "CarryCnt":
                if (_characterManager.CharacterSetting.CarryCnt >25)
                    offer.IsEnable = false;
                else
                    offer.PayAmount = (_characterManager.CharacterSetting.CarryCnt) * offer.PayAmount;
                break;
            case "Life":
                if (_characterManager.CharacterSetting.Life > 0)
                    offer.IsEnable = false;
                else
                    offer.PayAmount = (_characterManager.CharacterSetting.Level) * 10 + offer.PayAmount;
                break;
        }
        if (_backScene == "Gem" && offer.SellProd != "Gem")
            offer.IsEnable = false;
        if (_backScene == "Wait" && offer.SellProd != "Gem")
            offer.IsEnable = false;
        if (_backScene == "Inventory" &&  offer.SellProd != "CarryCnt" && offer.SellProd != "Gem")
            offer.IsEnable = false;
        if (_backScene == "GameOver" && offer.SellProd != "Life" && offer.SellProd != "Gem")
            offer.IsEnable = false;
        return offer;
    }
    void ShopOffer()
    {
        _clickedButton = EventSystem.current.currentSelectedGameObject;
        string buttonName = _clickedButton.name;
        Offer offer = _offers[Int32.Parse(buttonName)];

        //AcceptSellTerms
        switch (offer.SellProd)
        {
            case "CarryCnt":
                _messagePanelHandler.ShowMessage("Are you sure you want to acquire this slot at this level for " + offer.PayAmount +" " + offer.PayProd + "(s)?", MessagePanel.PanelType.YesNo, () => { DoShop(offer); });
                return;
            case "Life":
                _messagePanelHandler.ShowMessage("Are you sure you want to buy a life now for "+ offer.PayAmount + " " + offer.PayProd + "(s)?", MessagePanel.PanelType.YesNo, () => { DoShop(offer); });
                return;
        }
        switch (offer.PayProd)
        {
            case "Money":
                _messagePanelHandler.ShowMessage("Are you sure you want to Spend " + offer.PayAmount + " cash?", MessagePanel.PanelType.YesNo, () => { DoShop(offer); });
                return;

        }
        _messagePanelHandler.ShowMessage("Are you sure you want to Spend " + offer.PayAmount + " " + offer.PayProd + "(s)?", MessagePanel.PanelType.YesNo, () => { DoShop(offer); });
    }

    private void DoShop(Offer offer)
    {
        //Check criteria
        if (Regex.IsMatch(offer.SellProd, @"\d"))
        {
            //checked for available spot in inv
            if (!_inventoryManager.HaveAvailableSlot())
            {
                _messagePanelHandler.ShowMessage("Not enough room in inventory! ", MessagePanel.PanelType.Ok);
                return;
            }
        }
        if (ProcessThePay(offer.PayProd, offer.PayAmount))
        {
            //Items SellProd is an number (ID)
            if (Regex.IsMatch(offer.SellProd, @"\d"))
            {
                var item = _itemDatabase.GetItemById(Int32.Parse(offer.SellProd));
                item.Print();
                if (!_characterManager.AddItemToInventory(item, offer.SellAmount))
                {
                    //Refund the value 
                    ProcessThePay(offer.PayProd, -offer.PayAmount);
                    if (item.Unique)
                        _messagePanelHandler.ShowMessage("You Can not Carry more than one of this item!", MessagePanel.PanelType.Ok);
                }
            }
            else
                ProcessTheSell(offer.SellProd, offer.SellAmount);
            //To disable the Button after shopping that way not the same rate get applied 
            if (offer.IsSpecial)
            {
                Button pressButton = _clickedButton.GetComponent<Button>();
                pressButton.interactable = false;
            }
        }
    }
    private void ProcessTheSell(string sellProd, int sellAmount)
    {
        switch (sellProd)
        {
            case "Coin":
                _characterManager.AddCharacterSetting(sellProd, sellAmount);
                break;
            case "Gem":
                _characterManager.AddCharacterSetting(sellProd, sellAmount);
                break;
            case "CarryCnt":
                _characterManager.AddCharacterSetting(sellProd, sellAmount);
                break;
            case "Life":
                _characterManager.AddCharacterSetting(sellProd, sellAmount);
                break;
        }
    }
    private bool ProcessThePay(string payProd, int payAmount)
    {
        switch (payProd)
        {
            case "Coin":
                if (_characterManager.CharacterSetting.Coin > payAmount)
                {
                    _characterManager.AddCharacterSetting(payProd, -payAmount);
                    return true;
                }
                _messagePanelHandler.ShowMessage("You don't have enough Coin ! ", MessagePanel.PanelType.Ok);
                return false;
            case "Gem":
                print("process GEM" + _characterManager.UserPlayer.Gem + " " + payAmount);
                if (_characterManager.UserPlayer.Gem > payAmount)
                {
                    _characterManager.AddCharacterSetting(payProd, -payAmount);
                    return true;
                }
                _messagePanelHandler.ShowMessage("You don't have enough Gem ! ", MessagePanel.PanelType.Ok);
                return false;
            case "Money":
                if (ProcessThePayment(payAmount))
                    return true;
                print("Your Purchase didn't process ");
                _messagePanelHandler.ShowMessage("Your Purchase didn't process ", MessagePanel.PanelType.Ok);
                return false;
        }
        _messagePanelHandler.ShowMessage("Something went wrong", MessagePanel.PanelType.Ok);
        return false;
    }
    private bool ProcessThePayment(int payAmount)
    {
        //todo:send to Apple store
        return true;
    }
    private Sprite GetSprite(string spriteName)
    {
        switch (spriteName)
        {
            case "Coin":
                return CoinSprite;
            case "Gem":
                return GemSprite;
            case "Money":
                return MoneySprite;
            case "CarryCnt":
                return CarryCntSprite;
            case "Life":
                return Life;
            default:
                if (Regex.IsMatch(spriteName, @"\d"))
                {
                    var item = _itemDatabase.GetItemById(Int32.Parse(spriteName));
                    return item.GetSprite();
                }
                return null;
        }
    }
    private void SetBackScene()
    {
        var starter = GameObject.FindObjectOfType<SceneStarter>();
        if (starter != null)
            _backScene = starter.Content;
    }
    public void BackToMainScene()
    {
        switch (_backScene)
        {
            case "Wait":
                SceneManager.LoadScene(SceneSettings.SceneIdForWait);
                break;
            case "GameOver":
                SceneManager.LoadScene(SceneSettings.SceneIdForGameOver);
                break;
            case "Inside":
                SceneManager.LoadScene(SceneSettings.SceneIdForInsideBuilding);
                break;
            default:
                SceneManager.LoadScene(SceneSettings.SceneIdForTerrainView);
                break;
        }
    }
}