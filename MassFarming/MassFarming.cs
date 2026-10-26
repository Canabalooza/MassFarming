using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MassFarming
{
    [BepInPlugin("xeio.MassFarming", "MassFarming", "1.13")]
    public class MassFarming : BaseUnityPlugin
    {
        public enum GamepadButton
        {
            None,
            AlternateFunction,
            LeftTrigger,
            RightTrigger,
            LeftBumper,
            RightBumper,
            DPadLeft,
            DPadRight,
            DPadUp,
            DPadDown,
            LeftStick,
            RightStick,
        }

        public static ConfigEntry<KeyboardShortcut> MassActionHotkey { get; private set; }
        public static ConfigEntry<GamepadButton> ControllerPickupHotkey { get; private set; }
        public static ConfigEntry<GamepadButton> ControllerPlantHotkey { get; private set; }
        public static ConfigEntry<GamepadButton> ControllerGridGrowHotkey { get; private set; }
        public static ConfigEntry<GamepadButton> ControllerGridShrinkHotkey { get; private set; }
        public static ConfigEntry<float> MassInteractRange { get; private set; }
        public static ConfigEntry<int> PlantGridWidth { get; private set; }
        public static ConfigEntry<int> PlantGridLength { get; private set; }
        public const int PlantGridMinSize = 3;
        public const int PlantGridMaxSize = 9;
        public static ConfigEntry<bool> IgnoreStamina { get; private set; }
        public static ConfigEntry<bool> IgnoreDurability { get; private set; }
        public static ConfigEntry<bool> GridAnchorWidth { get; private set; }
        public static ConfigEntry<bool> GridAnchorLength { get; private set; }

        public static bool IsPickupHotKeyPressed => IsMassActionHeld || ControllerPickupHotkey.Value.IsButtonHeld();

        public static bool IsPlantHotKeyPressed => IsMassActionHeld || ControllerPlantHotkey.Value.IsButtonHeld();

        private static bool IsMassActionHeld => MassActionHotkey.Value.MainKey != KeyCode.None && Input.GetKey(MassActionHotkey.Value.MainKey);

        // Stick clicks while the plant modifier is held. Steps stay odd so the grid stays centered.
        public static bool TryAdjustPlantGridSize()
        {
            int delta = 0;
            if (ControllerGridGrowHotkey.Value.IsButtonDown())
            {
                delta = 2;
            }
            else if (ControllerGridShrinkHotkey.Value.IsButtonDown())
            {
                delta = -2;
            }

            if (delta == 0)
            {
                return false;
            }

            int size = Mathf.Max(PlantGridWidth.Value, PlantGridLength.Value);
            if ((size & 1) == 0)
            {
                size -= 1;
            }

            size = Mathf.Clamp(size + delta, PlantGridMinSize, PlantGridMaxSize);
            if (PlantGridWidth.Value == size && PlantGridLength.Value == size)
            {
                return false;
            }

            PlantGridWidth.Value = size;
            PlantGridLength.Value = size;
            return true;
        }

        public void Awake()
        {
            MassActionHotkey = Config.Bind("Hotkeys", nameof(MassActionHotkey), new KeyboardShortcut(KeyCode.LeftShift), "Mass activation hotkey for multi-pickup/multi-plant.");
            // The bumper opens the radial menu unless a build piece is up, so harvesting uses the alternate function instead
            ControllerPickupHotkey = Config.Bind("Hotkeys", nameof(ControllerPickupHotkey), GamepadButton.AlternateFunction, "Controller modifier for multi-pickup. AlternateFunction follows whatever button the game has bound to its alternate function (Left Trigger by default). Set to None to disable.");
            ControllerPlantHotkey = Config.Bind("Hotkeys", nameof(ControllerPlantHotkey), GamepadButton.LeftBumper, "Controller modifier for multi-plant. The alternate function rotates the placement ghost, so the bumper is the default here. Set to None to disable.");

            // The stick clicks only cycle snap points on pieces that have them, so plants leave them free
            ControllerGridGrowHotkey = Config.Bind("Hotkeys", nameof(ControllerGridGrowHotkey), GamepadButton.RightStick, "Controller button that grows the planting grid while the multi-plant modifier is held. Set to None to disable.");
            ControllerGridShrinkHotkey = Config.Bind("Hotkeys", nameof(ControllerGridShrinkHotkey), GamepadButton.LeftStick, "Controller button that shrinks the planting grid while the multi-plant modifier is held. Set to None to disable.");

            MassInteractRange = Config.Bind("Pickup", nameof(MassInteractRange), 5f, "Range of auto-pickup.");

            PlantGridWidth = Config.Bind("PlantGrid", nameof(PlantGridWidth), 5, "Grid width of auto-plant. Recommend odd-number, default is '5' so (5x5).");
            PlantGridLength = Config.Bind("PlantGrid", nameof(PlantGridLength), 5, "Grid length of auto-plant. Recommend odd-number, default is '5' so (5x5).");
            IgnoreStamina = Config.Bind("Plant", nameof(IgnoreStamina), false, "Ignore stamina requirements when planting extra rows.");
            IgnoreDurability = Config.Bind("Plant", nameof(IgnoreDurability), false, "Ignore durability when planting extra rows.");
            GridAnchorWidth = Config.Bind("PlantGrid", nameof(GridAnchorWidth), true, "Planting grid anchor point (width). Default is 'enabled' so the grid will extend left and right from the crosshairs. Disable to set the anchor to the side of the grid. Disable both anchors to set to corner.");
            GridAnchorLength = Config.Bind("PlantGrid", nameof(GridAnchorLength), true, "Planting grid anchor point (length). Default is 'enabled' so the grid will extend forward and backward from the crosshairs. Disable to set the anchor to the side of the grid. Disable both anchors to set to corner.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
    }

    public static class GamepadExtensions
    {
        public static bool IsBound(this MassFarming.GamepadButton button)
        {
            return button != MassFarming.GamepadButton.None;
        }

        public static bool IsButtonHeld(this MassFarming.GamepadButton button)
        {
            string buttonName = button.ZInputName();
            return buttonName.Length > 0 && ZInput.instance != null && ZInput.GetButton(buttonName);
        }

        public static bool IsButtonDown(this MassFarming.GamepadButton button)
        {
            string buttonName = button.ZInputName();
            return buttonName.Length > 0 && ZInput.instance != null && ZInput.GetButtonDown(buttonName);
        }

        public static string DisplayName(this MassFarming.GamepadButton button)
        {
            string buttonName = button.ZInputName();
            if (buttonName.Length > 0 && ZInput.instance != null)
            {
                try
                {
                    // Returns the glyph the game shows for the bound control
                    string boundKey = ZInput.instance.GetBoundKeyString(buttonName, true);
                    if (!string.IsNullOrEmpty(boundKey))
                    {
                        return boundKey;
                    }
                }
                catch (KeyNotFoundException)
                {
                    // No glyph is registered for the bound control, fall back to the config value's name
                }
            }

            return button.ToString();
        }

        private static string ZInputName(this MassFarming.GamepadButton button)
        {
            switch (button)
            {
                case MassFarming.GamepadButton.AlternateFunction:
                    return "JoyAltKeys";
                case MassFarming.GamepadButton.LeftTrigger:
                    return "JoyLTrigger";
                case MassFarming.GamepadButton.RightTrigger:
                    return "JoyRTrigger";
                case MassFarming.GamepadButton.LeftBumper:
                    return "JoyLBumper";
                case MassFarming.GamepadButton.RightBumper:
                    return "JoyRBumper";
                case MassFarming.GamepadButton.DPadLeft:
                    return "JoyDPadLeft";
                case MassFarming.GamepadButton.DPadRight:
                    return "JoyDPadRight";
                case MassFarming.GamepadButton.DPadUp:
                    return "JoyDPadUp";
                case MassFarming.GamepadButton.DPadDown:
                    return "JoyDPadDown";
                case MassFarming.GamepadButton.LeftStick:
                    return "JoyLStick";
                case MassFarming.GamepadButton.RightStick:
                    return "JoyRStick";
                default:
                    return string.Empty;
            }
        }
    }
}
