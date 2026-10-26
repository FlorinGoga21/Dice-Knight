using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

/// <summary>Formats the actual action bindings with the imported TMP input sprites.</summary>
public static class ControlGlyphs
{
    public static string SchemeName(Gamepad gamepad)
    {
        if (gamepad == null) return "Keyboard & Mouse";
        if (gamepad is DualShockGamepad) return "PlayStation Controller";
        return IsNintendo(gamepad) ? "Nintendo Controller" : "Xbox / Gamepad";
    }

    public static string Bindings(InputAction action, Gamepad gamepad, TMP_SpriteAsset sprites)
    {
        var parts = new List<string>();
        if (action == null) return "Unbound";
        if (gamepad == null && action.name == "Navigate")
        {
            string clusters = KeyboardNavigation(action, sprites);
            if (clusters != null) return clusters;
        }
        foreach (var binding in action.bindings)
        {
            if (binding.isComposite || string.IsNullOrEmpty(binding.effectivePath)) continue;
            string path = binding.effectivePath;
            if (path == "*/{Submit}") path = gamepad == null ? "<Keyboard>/enter" : "<Gamepad>/buttonSouth";
            if (path == "*/{Cancel}") path = gamepad == null ? "<Keyboard>/escape" : "<Gamepad>/buttonEast";
            bool padBinding = path.StartsWith("<Gamepad>", StringComparison.OrdinalIgnoreCase);
            bool desktopBinding = path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase);
            if (gamepad == null ? !desktopBinding : !padBinding) continue;
            string name = SpriteName(path, gamepad);
            string text = name != null && sprites != null && sprites.GetSpriteIndexFromName(name) >= 0
                ? "<sprite name=\"" + name + "\" tint=0>"
                : InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
            if (!parts.Contains(text)) parts.Add(text);
        }
        return parts.Count == 0 ? "Unbound" : string.Join("  ", parts);
    }

    private static string KeyboardNavigation(InputAction action, TMP_SpriteAsset sprites)
    {
        var keys = new string[4];
        var arrows = new string[4];
        foreach (var binding in action.bindings)
        {
            string path = binding.effectivePath;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase)) continue;
            int direction = Array.IndexOf(new[] { "up", "left", "down", "right" }, binding.name);
            if (direction < 0) continue;
            string name = SpriteName(path, null);
            if (sprites == null || sprites.GetSpriteIndexFromName(name) < 0) return null;
            var cluster = path.EndsWith("Arrow", StringComparison.OrdinalIgnoreCase) ? arrows : keys;
            cluster[direction] = "<sprite name=\"" + name + "\" tint=0>";
        }
        for (int i = 0; i < 4; i++) if (keys[i] == null || arrows[i] == null) return null;
        // Two equal key clusters: up centered above left/down/right in each half of the binding column.
        return "<align=left><line-height=16><pos=18>" + keys[0] + "<pos=76>" + arrows[0]
            + "\n<pos=2>" + keys[1] + "<pos=18>" + keys[2] + "<pos=34>" + keys[3]
            + "<pos=60>" + arrows[1] + "<pos=76>" + arrows[2] + "<pos=92>" + arrows[3]
            + "</line-height></align>";
    }

    private static string SpriteName(string path, Gamepad gamepad)
    {
        string control = path.Substring(path.IndexOf('>') + 2).ToLowerInvariant();
        if (path.StartsWith("<Keyboard>", StringComparison.OrdinalIgnoreCase))
        {
            switch (control)
            {
                case "escape": return "esc";
                case "uparrow": return "ku";
                case "downarrow": return "kd";
                case "leftarrow": return "kl";
                case "rightarrow": return "kr";
                default: return control;
            }
        }
        if (path.StartsWith("<Mouse>", StringComparison.OrdinalIgnoreCase))
        {
            switch (control)
            {
                case "leftbutton": return "ml";
                case "rightbutton": return "mr";
                case "middlebutton": return "mm";
                default: return "mouse";
            }
        }
        if (control.StartsWith("leftstick")) return "jl";
        if (control.StartsWith("rightstick")) return "jr";
        if (control.StartsWith("dpad")) return "dpad";
        bool playstation = gamepad is DualShockGamepad;
        bool nintendo = gamepad != null && IsNintendo(gamepad);
        switch (control)
        {
            case "buttonsouth": return playstation ? "px" : nintendo ? "nb" : "xa";
            case "buttoneast": return playstation ? "po" : nintendo ? "na" : "xb";
            case "buttonwest": return playstation ? "ps" : nintendo ? "ny" : "xx";
            case "buttonnorth": return playstation ? "pt" : nintendo ? "nx" : "xy";
            case "start": return playstation ? "pmenu" : nintendo ? "n+" : "xmenu";
            case "select": return playstation ? "pshare" : nintendo ? "n-" : "xwin";
            case "leftshoulder": return "lb";
            case "rightshoulder": return "rb";
            case "lefttrigger": return "lt";
            case "righttrigger": return "rt";
            default: return null;
        }
    }

    private static bool IsNintendo(Gamepad gamepad)
    {
        return gamepad.layout.IndexOf("Switch", StringComparison.OrdinalIgnoreCase) >= 0
            || gamepad.description.manufacturer?.IndexOf("Nintendo", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
