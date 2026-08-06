using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VPet.Cognition.Wpf.Helpers;

/// <summary>
/// Low-level Windows keyboard hook that detects Alt+Q press and release
/// for push-to-talk functionality.
/// </summary>
public sealed class LowLevelKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int VK_Q = 0x51;

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookProc;
    private bool _altQIsDown;

    /// <summary>Fired when Alt+Q is pressed (key down).</summary>
    public event Action? AltQPressed;

    /// <summary>Fired when Alt+Q is released (key up).</summary>
    public event Action? AltQReleased;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    /// <summary>Install the keyboard hook. Must be called from a thread with a message loop.</summary>
    public void Subscribe()
    {
        if (_hookId != IntPtr.Zero) return;

        _hookProc = HookCallback;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(curModule.ModuleName), 0);
    }

    /// <summary>Remove the keyboard hook.</summary>
    public void Unsubscribe()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            int msg = wParam.ToInt32();

            // Check if Alt is held (for both WM_KEYDOWN and WM_SYSKEYDOWN)
            bool altDown = (msg == WM_SYSKEYDOWN || msg == WM_SYSKEYUP);

            if (vkCode == VK_Q && altDown)
            {
                if (msg == WM_SYSKEYDOWN && !_altQIsDown)
                {
                    _altQIsDown = true;
                    AltQPressed?.Invoke();
                }
                else if (msg == WM_SYSKEYUP && _altQIsDown)
                {
                    _altQIsDown = false;
                    AltQReleased?.Invoke();
                }
                // Suppress auto-repeat: don't fire again while key is held
                if (msg == WM_SYSKEYDOWN && _altQIsDown)
                    return (IntPtr)1; // Suppress the key event
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Unsubscribe();
    }
}