using QuickHotkeyLauncher.Models;
using QuickHotkeyLauncher.Localization;

namespace QuickHotkeyLauncher.Services;

public sealed class HotkeyService : IDisposable
{
    private readonly IntPtr _windowHandle;
    private readonly Dictionary<Guid, int> _bindingToHotkeyId = new();
    private readonly Dictionary<int, Guid> _hotkeyIdToBinding = new();
    private int _nextId = 1000;

    public HotkeyService(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public IReadOnlyDictionary<int, Guid> HotkeyMap => _hotkeyIdToBinding;

    public bool TryRegister(Guid bindingId, HotkeyDefinition hotkey, out string error)
    {
        error = string.Empty;
        if (_bindingToHotkeyId.ContainsKey(bindingId))
        {
            Unregister(bindingId);
        }

        var modifiers = ToModifiers(hotkey);
        var vk = (uint)hotkey.Key;
        var id = _nextId++;

        if (!NativeMethods.RegisterHotKey(_windowHandle, id, modifiers, vk))
        {
            error = L.T(
                "Registration failed: hotkey may be occupied by system or another app.",
                "注册失败：快捷键可能被系统或其他应用占用。");
            return false;
        }

        _bindingToHotkeyId[bindingId] = id;
        _hotkeyIdToBinding[id] = bindingId;
        return true;
    }

    public void Unregister(Guid bindingId)
    {
        if (!_bindingToHotkeyId.TryGetValue(bindingId, out var id))
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_windowHandle, id);
        _bindingToHotkeyId.Remove(bindingId);
        _hotkeyIdToBinding.Remove(id);
    }

    public bool TryGetBindingId(int hotkeyId, out Guid bindingId)
    {
        return _hotkeyIdToBinding.TryGetValue(hotkeyId, out bindingId);
    }

    public void Dispose()
    {
        foreach (var item in _bindingToHotkeyId.ToArray())
        {
            NativeMethods.UnregisterHotKey(_windowHandle, item.Value);
        }

        _bindingToHotkeyId.Clear();
        _hotkeyIdToBinding.Clear();
    }

    private static uint ToModifiers(HotkeyDefinition hotkey)
    {
        uint modifiers = NativeMethods.ModNoRepeat;
        if (hotkey.Ctrl) modifiers |= NativeMethods.ModControl;
        if (hotkey.Alt) modifiers |= NativeMethods.ModAlt;
        if (hotkey.Shift) modifiers |= NativeMethods.ModShift;
        if (hotkey.Win) modifiers |= NativeMethods.ModWin;
        return modifiers;
    }
}
