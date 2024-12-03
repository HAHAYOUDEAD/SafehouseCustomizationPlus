global using CC = System.ConsoleColor;
global using System;
global using MelonLoader;
global using HarmonyLib;
global using UnityEngine;
global using System.Reflection;
global using System.Collections;
global using System.Collections.Generic;
global using Il2Cpp;
global using Il2CppTLD;

namespace BCP
{
    internal class Utility
    {
        public const string modVersion = "1.0";
        public const string modName = "SafehouseCustomizationPlus";
        public const string modAuthor = "Waltz";
        

        public static bool IsScenePlayable()
        {
            return !(string.IsNullOrEmpty(GameManager.m_ActiveScene) || GameManager.m_ActiveScene.Contains("MainMenu") || GameManager.m_ActiveScene == "Boot" || GameManager.m_ActiveScene == "Empty");
        }

        public static bool IsScenePlayable(string scene)
        {
            return !(string.IsNullOrEmpty(scene) || scene.Contains("MainMenu") || scene == "Boot" || scene == "Empty");
        }

        public static bool IsMainMenu(string scene)
        {
            return !string.IsNullOrEmpty(scene) && scene.Contains("MainMenu");
        }

    }
}
