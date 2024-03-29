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

            if(Input.GetKeyDown(scanKey.Value))
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

                   /* if (slot == null && label != null)
                    {
                        slot = label.m_RackSlot;
                    }*/

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

        private void ModifyCart(Label label, int quantity = 1)
        {
            if (quantity == 0) return;

            int product_id = label.DisplaySlot != null ? label.DisplaySlot.ProductID : label.m_RackSlot.Data.ProductID;

            ItemQuantity salesItem = new ItemQuantity
            {
                Products = new Dictionary<int, int>()
                {
                    { product_id, Math.Abs(quantity) }
                }
            };

            if(quantity < 0) {
                cart.RemoveProduct(salesItem, SalesType.PRODUCT);
            } else if (cart.TryAddProduct(salesItem, SalesType.PRODUCT)) {
                //do nothing
            }

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
