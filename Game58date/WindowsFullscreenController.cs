#nullable enable
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Stride.Core.Mathematics;

namespace Game58date;

public sealed class WindowsFullscreenController
{
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const int WsVisible = 0x10000000;
    private const int WsPopup = unchecked((int)0x80000000);
    private const int WsCaption = 0x00C00000;
    private const int WsThickFrame = 0x00040000;
    private const int WsMinimize = 0x20000000;
    private const int WsMaximize = 0x01000000;
    private const int WsSysMenu = 0x00080000;
    private const int WsMinimizeBox = 0x00020000;
    private const int WsMaximizeBox = 0x00010000;
    private const int WsExDlgModalFrame = 0x00000001;
    private const int WsExClientEdge = 0x00000200;
    private const int WsExStaticEdge = 0x00020000;
    private const int WsExWindowEdge = 0x00000100;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const uint SwpShowWindow = 0x0040;
    private const uint MonitorDefaultToNearest = 0x00000002;

    private IntPtr windowHandle;

    public bool TryApplyBorderlessFullscreen(object? strideWindow, out Int2 clientSize)
    {
        clientSize = default;
        if (!TryResolveWindowHandle(strideWindow))
        {
            return false;
        }

        IntPtr monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var monitorInfo = new MonitorInfoEx();
        monitorInfo.cbSize = Marshal.SizeOf<MonitorInfoEx>();
        if (!GetMonitorInfoW(monitor, ref monitorInfo))
        {
            return false;
        }

        nint style = GetWindowLongPtr(windowHandle, GwlStyle);
        nint desiredStyle = BuildDesiredWindowStyle(style);
        bool styleChanged = false;
        if (style != desiredStyle)
        {
            SetWindowLongPtr(windowHandle, GwlStyle, desiredStyle);
            styleChanged = true;
        }

        nint exStyle = GetWindowLongPtr(windowHandle, GwlExStyle);
        nint desiredExStyle = BuildDesiredExtendedStyle(exStyle);
        bool exStyleChanged = false;
        if (exStyle != desiredExStyle)
        {
            SetWindowLongPtr(windowHandle, GwlExStyle, desiredExStyle);
            exStyleChanged = true;
        }

        Rect bounds = monitorInfo.rcMonitor;
        int width = Math.Max(1, bounds.Right - bounds.Left);
        int height = Math.Max(1, bounds.Bottom - bounds.Top);

        bool positionChanged = true;
        if (GetWindowRect(windowHandle, out Rect currentWindowRect))
        {
            positionChanged =
                currentWindowRect.Left != bounds.Left ||
                currentWindowRect.Top != bounds.Top ||
                (currentWindowRect.Right - currentWindowRect.Left) != width ||
                (currentWindowRect.Bottom - currentWindowRect.Top) != height;
        }

        if (styleChanged || exStyleChanged || positionChanged)
        {
            uint flags = SwpNoZOrder | SwpNoActivate | SwpShowWindow;
            if (styleChanged || exStyleChanged)
            {
                flags |= SwpFrameChanged;
            }

            SetWindowPos(
                windowHandle,
                IntPtr.Zero,
                bounds.Left,
                bounds.Top,
                width,
                height,
                flags);
        }

        return TryGetClientSize(out clientSize);
    }

    public bool IsCurrentlyBorderlessFullscreen(object? strideWindow, out Int2 clientSize)
    {
        clientSize = default;
        if (!TryResolveWindowHandle(strideWindow))
        {
            return false;
        }

        IntPtr monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var monitorInfo = new MonitorInfoEx();
        monitorInfo.cbSize = Marshal.SizeOf<MonitorInfoEx>();
        if (!GetMonitorInfoW(monitor, ref monitorInfo))
        {
            return false;
        }

        if (!GetWindowRect(windowHandle, out Rect windowRect))
        {
            return false;
        }

        nint style = GetWindowLongPtr(windowHandle, GwlStyle);
        nint exStyle = GetWindowLongPtr(windowHandle, GwlExStyle);
        bool styleMatches = style == BuildDesiredWindowStyle(style);
        bool exStyleMatches = exStyle == BuildDesiredExtendedStyle(exStyle);

        Rect bounds = monitorInfo.rcMonitor;
        bool rectMatches =
            windowRect.Left == bounds.Left &&
            windowRect.Top == bounds.Top &&
            windowRect.Right == bounds.Right &&
            windowRect.Bottom == bounds.Bottom;

        bool hasClient = TryGetClientSize(out clientSize);
        return styleMatches && exStyleMatches && rectMatches && hasClient;
    }

    public bool TryGetClientSize(out Int2 size)
    {
        size = default;
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        if (!GetClientRect(windowHandle, out Rect rect))
        {
            return false;
        }

        size = new Int2(
            Math.Max(1, rect.Right - rect.Left),
            Math.Max(1, rect.Bottom - rect.Top));
        return true;
    }

    private bool TryResolveWindowHandle(object? strideWindow)
    {
        if (windowHandle != IntPtr.Zero && IsWindow(windowHandle))
        {
            return true;
        }

        Process currentProcess = Process.GetCurrentProcess();
        currentProcess.Refresh();
        if (currentProcess.MainWindowHandle != IntPtr.Zero)
        {
            windowHandle = currentProcess.MainWindowHandle;
            return true;
        }

        windowHandle = TryExtractHandle(strideWindow);
        return windowHandle != IntPtr.Zero;
    }

    private static IntPtr TryExtractHandle(object? value)
    {
        if (value is null)
        {
            return IntPtr.Zero;
        }

        if (value is IntPtr handle && handle != IntPtr.Zero)
        {
            return handle;
        }

        Type type = value.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        string[] candidates =
        {
            "Handle",
            "WindowHandle",
            "NativeHandle",
            "Hwnd",
            "SDLWindow",
            "NativeWindow",
            "Window",
            "Form",
            "Control",
        };

        foreach (string candidate in candidates)
        {
            PropertyInfo? property = type.GetProperty(candidate, flags);
            if (property is not null)
            {
                object? nested = property.GetValue(value);
                IntPtr nestedHandle = TryExtractHandleDirect(nested);
                if (nestedHandle != IntPtr.Zero)
                {
                    return nestedHandle;
                }
            }

            FieldInfo? field = type.GetField(candidate, flags);
            if (field is not null)
            {
                object? nested = field.GetValue(value);
                IntPtr nestedHandle = TryExtractHandleDirect(nested);
                if (nestedHandle != IntPtr.Zero)
                {
                    return nestedHandle;
                }
            }
        }

        return IntPtr.Zero;
    }

    private static IntPtr TryExtractHandleDirect(object? value)
    {
        if (value is null)
        {
            return IntPtr.Zero;
        }

        if (value is IntPtr handle)
        {
            return handle;
        }

        Type nestedType = value.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (string name in new[] { "Handle", "WindowHandle", "NativeHandle", "Hwnd" })
        {
            PropertyInfo? property = nestedType.GetProperty(name, flags);
            if (property?.PropertyType == typeof(IntPtr))
            {
                return (IntPtr)(property.GetValue(value) ?? IntPtr.Zero);
            }

            FieldInfo? field = nestedType.GetField(name, flags);
            if (field?.FieldType == typeof(IntPtr))
            {
                return (IntPtr)(field.GetValue(value) ?? IntPtr.Zero);
            }
        }

        return IntPtr.Zero;
    }

    private static nint BuildDesiredWindowStyle(nint currentStyle)
    {
        nint style = currentStyle;
        style &= ~(WsCaption | WsThickFrame | WsMinimize | WsMaximize | WsSysMenu | WsMinimizeBox | WsMaximizeBox);
        style |= WsPopup | WsVisible;
        return style;
    }

    private static nint BuildDesiredExtendedStyle(nint currentExStyle)
    {
        nint exStyle = currentExStyle;
        exStyle &= ~(WsExDlgModalFrame | WsExClientEdge | WsExStaticEdge | WsExWindowEdge);
        return exStyle;
    }

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfoW(IntPtr hMonitor, ref MonitorInfoEx lpmi);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }
}
