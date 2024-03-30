using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyBox;
using System.Collections.Generic;
using System;
using System.Collections;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using System.Linq;
using RainbowArt.CleanFlatUI;

namespace PortableScanner
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance;

        private PlayerInteraction _interaction;
        public MarketShoppingCart cart;

        private RackManager rackManager;

        private ConfigEntry<KeyCode> scanKey;
        private KeyboardShortcut scanKeyFillShelf;
        private ConfigEntry<bool> fillShelfFeature;

        private PopupMessage popupMessage;

        private void Awake()
        {
            Instance = this;

            scanKey = Config.Bind(
                "Keybinds",
                "Scan Button",
                KeyCode.Mouse2,
                "Use this key to use the scanner."
            );

            fillShelfFeature = Config.Bind(
                "Features",
                "Enable Fill Shelf Feature",
                true,
                "Use CTRL + Scan to add the number of boxes required to fill the scanned shelf to the cart."
            );

            if(fillShelfFeature.Value)
            {
                scanKeyFillShelf = new KeyboardShortcut(scanKey.Value, new KeyCode[] { KeyCode.LeftControl });
                scanKey.SettingChanged += (object sender, EventArgs e) =>
                {
                    scanKeyFillShelf = new KeyboardShortcut(scanKey.Value, new KeyCode[] { KeyCode.LeftControl });
                };
            }

            SceneManager.activeSceneChanged += (Scene s1, Scene s2) =>
            {
                _interaction = Singleton<PlayerInteraction>.Instance;
                rackManager = Singleton<RackManager>.Instance;
            };

            Harmony.CreateAndPatchAll(typeof(Patches));

            popupMessage = gameObject.GetOrAddComponent<PopupMessage>();
        }

        public void LogError(string message, int times = 1)
        {
            for (int i = 0; i < times; i++) {
                Logger.LogError(message);
            }
        }

        private void Update()
        {          
            if (_interaction == null) return;

            if (Input.GetKeyDown(scanKey.Value))
            {

                GameObject hitObject = GetRaycastObject();

                Label label = null;
                RackSlot slot = null;
                Box box = null;

                int amount = 1;

                if(hitObject != null)
                {
                    if(hitObject.TryGetComponent<Label>(out label)) {
                        //label is already set
                        slot = label.GetComponentInParent<RackSlot>();
                    } else if (hitObject.TryGetComponent<RackSlot>(out slot)) {
                        label = slot.m_Label;
                    } else if (hitObject.TryGetComponent<Box>(out box)) {
                        if(box.Racked) {
                            slot = box.GetComponentInParent<RackSlot>();
                            if (slot == null) return;
                            label = slot.m_Label;
                        }
                    }

                    if(box == null && slot != null)
                    {
                        ProductSO product = Singleton<IDManager>.Instance.ProductSO(slot.Data.ProductID);
                        box = Singleton<IDManager>.Instance.Boxes.FirstOrDefault((BoxSO i) => i.BoxSize == product.GridLayoutInBox.boxSize).BoxPrefab;
                    }

                    if(label != null)
                    {
                        if (Input.GetKey(KeyCode.LeftControl) && box != null && slot != null)
                        {
                            amount = Singleton<IDManager>.Instance.BoxSO(box.BoxID).GridLayout.boxCount - slot.m_Data.BoxCount;
                        }

                        if(Input.GetKey(KeyCode.LeftAlt))
                        {
                            amount *= -1;
                        }

                        ModifyCart(label, amount);
                    }
                }
            }
        }

        public int RemoveProductFromCart(ItemQuantity productData)
        {
            foreach (ItemQuantity itemQuantity in cart.m_CartData.ProductInCarts)
            {
                if(itemQuantity.FirstItemID == productData.FirstItemID)
                {
                    if(itemQuantity.FirstItemCount < productData.FirstItemCount)
                    {
                        productData.FirstItemCount = itemQuantity.FirstItemCount;
                    }

                    itemQuantity.FirstItemCount -= productData.FirstItemCount;

                    if(itemQuantity.FirstItemCount == 0)
                    {
                        cart.RemoveProduct(productData, SalesType.PRODUCT);
                    }

                    cart.UpdateTotalPrice();

                    using (List<CartItem>.Enumerator enumerator2 = cart.m_CartItems.GetEnumerator())
                    {
                        while (enumerator2.MoveNext())
                        {
                            CartItem cartItem = enumerator2.Current;
                            if (cartItem.SalesItem.FirstItemID == productData.FirstItemID)
                            {
                                cartItem.UpdateTotalPrice();
                                break;
                            }
                        }
                        return productData.FirstItemCount;
                    }
                }
            }
            return 0;
        }

        private void ModifyCart(Label label, int quantity = 1)
        {
            if(cart.CartMaxed())
            {
                popupMessage.ShowToast($"Scan failed, cart is full!", 3f);
                return;
            }
            if (quantity == 0) return;

            int product_id = label.DisplaySlot != null ? label.DisplaySlot.ProductID : label.m_RackSlot.Data.ProductID;
            ProductSO product = Singleton<IDManager>.Instance.ProductSO(product_id);

            ItemQuantity salesItem = new ItemQuantity
            {
                Products = new Dictionary<int, int>()
                {
                    { product_id, Math.Abs(quantity) }
                }
            };

            double price = Math.Round(Singleton<PriceManager>.Instance.CurrentCost(product_id) * quantity, 2);

            string action_word = "Scanned";

            if (quantity < 0) {
                quantity = RemoveProductFromCart(salesItem);

                if (quantity < 1)
                {
                    popupMessage.ShowToast($"Scan failed, cart does not contain the scanned item!", 3f);
                    return;
                }

                //cart.RemoveProduct(salesItem, SalesType.PRODUCT);
                action_word = "Removed";
            } else if (cart.TryAddProduct(salesItem, SalesType.PRODUCT)) {
                //do nothing
            }

            double cart_total = Math.Round(cart.m_OrderTotalPrice, 2);

            //\n(Cart Balance: ${cart.m_OrderTotalPrice})
            popupMessage.ShowToast($"{action_word} {Math.Abs(quantity)} {product.ProductName} @ ${price} (${price*quantity})", 3f);

            Singleton<SFXManager>.Instance.PlayScanningProductSFX();
        }

        private GameObject GetRaycastObject()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;

            // Check if the ray hits any object
            if (Physics.Raycast(ray, out hitInfo, _interaction.m_InteractionDistance))
            {
                return hitInfo.collider.gameObject;
            }
            return null;
        }
    }
}
