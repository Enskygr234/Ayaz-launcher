using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace AyazLauncher
{
    /// <summary>Sistem genelinde çalışan kısayol tuşu (RegisterHotKey).</summary>
    public sealed class KisayolTusu : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        const int WM_HOTKEY = 0x0312;
        const int Kimlik = 0xA7A2;
        const uint MOD_NOREPEAT = 0x4000;

        readonly HwndSource kaynak;
        bool kayitli;

        public event Action? Basildi;

        public KisayolTusu()
        {
            // Görünmez, yalnızca mesaj alan pencere.
            var p = new HwndSourceParameters("AyazLauncherKisayol")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0,
                ParentWindow = new IntPtr(-3) // HWND_MESSAGE
            };
            kaynak = new HwndSource(p);
            kaynak.AddHook(Kanca);
        }

        public bool Kaydet(uint degistiriciler, int tus)
        {
            Birak();
            kayitli = RegisterHotKey(kaynak.Handle, Kimlik, degistiriciler | MOD_NOREPEAT, (uint)tus);
            return kayitli;
        }

        void Birak()
        {
            if (!kayitli) return;
            UnregisterHotKey(kaynak.Handle, Kimlik);
            kayitli = false;
        }

        IntPtr Kanca(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == Kimlik)
            {
                Basildi?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Birak();
            kaynak.Dispose();
        }

        /// <summary>"Ctrl + Space" gibi okunabilir metin.</summary>
        public static string Metin(uint degistiriciler, int tus)
        {
            var p = new List<string>();
            if ((degistiriciler & 0x2) != 0) p.Add("Ctrl");
            if ((degistiriciler & 0x1) != 0) p.Add("Alt");
            if ((degistiriciler & 0x4) != 0) p.Add("Shift");
            if ((degistiriciler & 0x8) != 0) p.Add("Win");
            p.Add(TusAdi(KeyInterop.KeyFromVirtualKey(tus)));
            return string.Join(" + ", p);
        }

        public static string TusAdi(Key k) => k switch
        {
            Key.Space => "Space",
            Key.Return => "Enter",
            Key.Escape => "Esc",
            Key.Back => "Backspace",
            Key.OemTilde => "\"",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemMinus => "-",
            Key.OemPlus => "+",
            >= Key.D0 and <= Key.D9 => ((int)(k - Key.D0)).ToString(),
            >= Key.NumPad0 and <= Key.NumPad9 => "Num " + (int)(k - Key.NumPad0),
            _ => k.ToString()
        };
    }
}
