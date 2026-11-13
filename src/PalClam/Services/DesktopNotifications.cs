// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace PalClam.Services;

/// <summary>Aviso de Windows desde la bandeja; no requiere servicios ni paquetes externos.</summary>
internal sealed class DesktopNotifications : IDisposable
{
    private readonly Window _window;
    private DispatcherTimer? _cleanup;
    private bool _active;
    private IntPtr _icon;
    private bool _ownsIcon;
    private const uint NIM_ADD = 0, NIM_MODIFY = 1, NIM_DELETE = 2;
    private const uint NIF_MESSAGE = 1, NIF_ICON = 2, NIF_TIP = 4, NIF_INFO = 16;

    public DesktopNotifications(Window window) { _window = window; }

    public void Show(string title, string message, bool warning = false)
    {
        if (!_window.IsLoaded) return;
        title = PalClam.Localization.DisplayText.Translate(title);
        message = PalClam.Localization.DisplayText.Translate(message);
        try
        {
            var handle = new WindowInteropHelper(_window).Handle;
            if (handle == IntPtr.Zero) return;
            if (_icon == IntPtr.Zero)
            {
                _icon = ExtractIcon(IntPtr.Zero, Environment.ProcessPath ?? "", 0);
                _ownsIcon = _icon.ToInt64() > 1;
                if (!_ownsIcon) _icon = LoadIcon(IntPtr.Zero, new IntPtr(32512));
            }
            var data = Data(handle);
            data.uFlags = NIF_ICON | NIF_TIP;
            data.hIcon = _icon;
            data.szTip = "PalClam";
            if (!_active)
            {
                if (!Shell_NotifyIcon(NIM_ADD, ref data)) return;
                _active = true;
            }
            data.uFlags = NIF_INFO;
            data.szInfoTitle = title.Length <= 63 ? title : title[..63];
            data.szInfo = message.Length <= 255 ? message : message[..255];
            data.dwInfoFlags = warning ? 2u : 1u;
            Shell_NotifyIcon(NIM_MODIFY, ref data);
            _cleanup?.Stop();
            _cleanup = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            _cleanup.Tick += (_, _) => { _cleanup?.Stop(); Remove(); };
            _cleanup.Start();
        }
        catch { /* No afectar un análisis por fallos del subsistema de notificaciones. */ }
    }

    private NOTIFYICONDATA Data(IntPtr handle) => new()
    {
        cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(), hWnd = handle, uID = 0x5043,
        szTip = "PalClam", szInfo = "", szInfoTitle = ""
    };

    private void Remove()
    {
        if (!_active) return;
        var handle = new WindowInteropHelper(_window).Handle;
        if (handle != IntPtr.Zero)
        {
            var data = Data(handle);
            Shell_NotifyIcon(NIM_DELETE, ref data);
        }
        _active = false;
    }

    public void Dispose()
    {
        _cleanup?.Stop();
        Remove();
        if (_ownsIcon && _icon != IntPtr.Zero) DestroyIcon(_icon);
        _icon = IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
        public uint dwTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(uint message, ref NOTIFYICONDATA data);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "ExtractIconW")]
    private static extern IntPtr ExtractIcon(IntPtr instance, string filename, uint iconIndex);
    [DllImport("user32.dll", EntryPoint = "LoadIconW")]
    private static extern IntPtr LoadIcon(IntPtr instance, IntPtr iconName);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);
}
