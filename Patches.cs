﻿using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace PortableScanner
{
    class Patches
    {
        [HarmonyPatch(typeof(Computer), nameof(Computer.Start))]
        [HarmonyPostfix]
        static void ComputerStartPrefix(Computer __instance)
        {
            ComputerOperatingSystem os;
            __instance.TryGetComponent<ComputerOperatingSystem>(out os);

            BaseWindow cart_window = os.GetAppWindowByName("Market");

            cart_window.Open();

            Plugin.Instance.cart = cart_window.GetComponentInChildren<MarketShoppingCart>();
        }
    }
}
