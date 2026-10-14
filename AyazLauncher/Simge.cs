using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AyazLauncher
{
    /// <summary>Programların simgesini ve görünen adını bulur.</summary>
    public static class Simge
    {
        public static ImageSource? Al(string yol)
        {
            try
            {
                if (!File.Exists(yol)) return null;

                System.Drawing.Icon? ikon = null;

                // .exe için büyük (64 px) simgeyi dene; olmazsa sistemin verdiği simge.
                if (yol.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    try { ikon = System.Drawing.Icon.ExtractIcon(yol, 0, 64); } catch { ikon = null; }
                }
                ikon ??= System.Drawing.Icon.ExtractAssociatedIcon(yol);
                if (ikon == null) return null;

                using (ikon)
                {
                    var kaynak = Imaging.CreateBitmapSourceFromHIcon(
                        ikon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    kaynak.Freeze();
                    return kaynak;
                }
            }
            catch
            {
                return null;
            }
        }

        public static string Ad(string yol)
        {
            try
            {
                if (yol.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(yol))
                {
                    var bilgi = FileVersionInfo.GetVersionInfo(yol);
                    var aciklama = bilgi.FileDescription?.Trim();
                    if (!string.IsNullOrEmpty(aciklama) && aciklama.Length <= 24) return aciklama;
                    var urun = bilgi.ProductName?.Trim();
                    if (!string.IsNullOrEmpty(urun) && urun.Length <= 24) return urun;
                }
            }
            catch { }
            return Path.GetFileNameWithoutExtension(yol);
        }
    }
}
