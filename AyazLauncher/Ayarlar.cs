using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace AyazLauncher
{
    /// <summary>Çubuktaki bir yuvaya yerleştirilmiş program.</summary>
    public class Yuva
    {
        public string Yol { get; set; } = "";
        public string Ad { get; set; } = "";
    }

    /// <summary>%AppData%\AyazLauncher\ayarlar.json içinde saklanan ayarlar.</summary>
    public class Ayarlar
    {
        public const int YuvaSayisi = 5;

        public List<Yuva?> Yuvalar { get; set; } = new();

        /// <summary>RegisterHotKey değiştiricileri: 1 Alt, 2 Ctrl, 4 Shift, 8 Win.</summary>
        public uint Degistiriciler { get; set; } = 0x2;

        /// <summary>Sanal tuş kodu (varsayılan: Space).</summary>
        public int Tus { get; set; } = 0x20;

        static string Klasor => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AyazLauncher");

        static string Dosya => Path.Combine(Klasor, "ayarlar.json");

        public static bool IlkCalisma => !File.Exists(Dosya);

        static readonly JsonSerializerOptions Secenekler = new() { WriteIndented = true };

        public static Ayarlar Yukle()
        {
            Ayarlar a = new();
            try
            {
                if (File.Exists(Dosya))
                    a = JsonSerializer.Deserialize<Ayarlar>(File.ReadAllText(Dosya)) ?? new Ayarlar();
            }
            catch
            {
                a = new Ayarlar();
            }

            a.Yuvalar ??= new();
            while (a.Yuvalar.Count < YuvaSayisi) a.Yuvalar.Add(null);
            if (a.Yuvalar.Count > YuvaSayisi) a.Yuvalar = a.Yuvalar.GetRange(0, YuvaSayisi);
            return a;
        }

        public void Kaydet()
        {
            try
            {
                Directory.CreateDirectory(Klasor);
                File.WriteAllText(Dosya, JsonSerializer.Serialize(this, Secenekler));
            }
            catch
            {
                // Ayar kaydedilemezse uygulama yine de çalışmaya devam etsin.
            }
        }

        // ---- Windows ile başlat ----

        const string CalistirAnahtari = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string DegerAdi = "AyazLauncher";

        static string ExeYolu => Environment.ProcessPath ?? "";

        public static bool WindowsIleBasliyor
        {
            get
            {
                using var k = Registry.CurrentUser.OpenSubKey(CalistirAnahtari);
                return k?.GetValue(DegerAdi) is string s
                       && s.Trim('"').Equals(ExeYolu, StringComparison.OrdinalIgnoreCase);
            }
            set
            {
                using var k = Registry.CurrentUser.CreateSubKey(CalistirAnahtari);
                if (value) k.SetValue(DegerAdi, $"\"{ExeYolu}\"");
                else k.DeleteValue(DegerAdi, false);
            }
        }
    }
}
