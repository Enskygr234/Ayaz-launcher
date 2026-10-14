using System.Windows;
using System.Windows.Input;

namespace AyazLauncher
{
    public partial class KisayolPenceresi : Window
    {
        public uint Degistiriciler { get; private set; }
        public int Tus { get; private set; }

        public KisayolPenceresi(uint degistiriciler, int tus)
        {
            InitializeComponent();
            Degistiriciler = degistiriciler;
            Tus = tus;
            Gosterge.Text = KisayolTusu.Metin(degistiriciler, tus);

            PreviewKeyDown += TusaBasildi;
            MouseLeftButtonDown += (_, _) => { try { DragMove(); } catch { } };
            Loaded += (_, _) => { Activate(); Focus(); };
        }

        void TusaBasildi(object sender, KeyEventArgs e)
        {
            var tus = e.Key == Key.System ? e.SystemKey : e.Key;

            // Esc tek başına: pencereyi kapat.
            if (tus == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
            {
                DialogResult = false;
                return;
            }

            e.Handled = true;

            if (tus is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.Tab)
                return;

            uint deg = (uint)Keyboard.Modifiers; // Alt=1, Ctrl=2, Shift=4, Win=8 — RegisterHotKey ile aynı
            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)) deg |= 0x8;

            int vk = KeyInterop.VirtualKeyFromKey(tus);
            Gosterge.Text = KisayolTusu.Metin(deg, vk);

            if (deg == 0)
            {
                Uyari.Text = "Bir Ctrl, Alt, Shift ya da Win tuşuyla birlikte bas.";
                Tamam.IsEnabled = false;
                return;
            }

            Uyari.Text = " ";
            Degistiriciler = deg;
            Tus = vk;
            Tamam.IsEnabled = true;
        }

        void Tamam_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        void Vazgec_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
