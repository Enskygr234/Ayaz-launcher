using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using WF = System.Windows.Forms;

namespace AyazLauncher
{
    public partial class LauncherWindow : Window
    {
        readonly Ayarlar ayarlar;
        readonly Border[] yuvalar = new Border[Ayarlar.YuvaSayisi];
        readonly Random rastgele = new();
        readonly DispatcherTimer karZamanlayici;

        bool duzenlemeModu;
        bool pencereAcik; // dosya seçici, mesaj kutusu vb. açıkken gizlenme
        int secili = -1;

        static readonly Brush YuvaArka = new SolidColorBrush(Color.FromArgb(0x16, 0xFF, 0xFF, 0xFF));
        static readonly Brush YuvaArkaUzerinde = new SolidColorBrush(Color.FromArgb(0x30, 0xCF, 0xE8, 0xFF));
        static readonly Brush YuvaKenar = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));
        static readonly Brush YuvaKenarSecili = new SolidColorBrush(Color.FromArgb(0xCC, 0xBF, 0xE6, 0xFF));
        static readonly Brush YaziRengi = new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xFF));
        static readonly Brush SoluRenk = new SolidColorBrush(Color.FromRgb(0x7F, 0xA6, 0xCF));

        public string KisayolMetni { get; set; } = "Ctrl + Space";

        public LauncherWindow(Ayarlar ayarlar)
        {
            InitializeComponent();
            this.ayarlar = ayarlar;

            for (int i = 0; i < yuvalar.Length; i++)
            {
                yuvalar[i] = YuvaOlustur(i);
                Yuvalar.Children.Add(yuvalar[i]);
                YuvayiDoldur(i);
            }

            karZamanlayici = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
            karZamanlayici.Tick += (_, _) => KarTanesiEkle();

            Deactivated += (_, _) =>
            {
                if (!duzenlemeModu && !pencereAcik) Gizle();
            };
            PreviewKeyDown += TusaBasildi;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Alt+F4 vb. ile kapatılırsa uygulama kapanmasın, sadece gizlensin.
            e.Cancel = true;
            Gizle();
        }

        // ================= Göster / gizle =================

        public void AcKapa()
        {
            if (IsVisible && IsActive) Gizle();
            else Goster();
        }

        public void Goster(bool duzenleme = false)
        {
            duzenlemeModu = duzenleme;
            secili = -1;
            SeciliyiCiz();
            IpucunuYaz();

            if (!IsVisible)
            {
                Opacity = 0;
                Show();
            }
            UpdateLayout();
            Konumla();

            Activate();
            Focus();
            Keyboard.Focus(this);

            AcilisAnimasyonu();
            KariBaslat();
        }

        public void Gizle()
        {
            if (!IsVisible) return;
            karZamanlayici.Stop();
            Kar.Children.Clear();
            duzenlemeModu = false;
            Hide();
        }

        void Konumla()
        {
            var ekran = WF.Screen.FromPoint(WF.Cursor.Position).WorkingArea;
            var dpi = VisualTreeHelper.GetDpi(this);
            double sol = ekran.Left / dpi.DpiScaleX;
            double alt = ekran.Bottom / dpi.DpiScaleY;
            double genislik = ekran.Width / dpi.DpiScaleX;

            Left = sol + (genislik - ActualWidth) / 2;
            Top = alt - ActualHeight - 12;
        }

        void AcilisAnimasyonu()
        {
            var sure = TimeSpan.FromMilliseconds(220);
            var yumusak = new CubicEase { EasingMode = EasingMode.EaseOut };

            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, sure));
            Kaydirma.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(28, 0, sure) { EasingFunction = yumusak });
            Olcek.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(0.94, 1, sure) { EasingFunction = yumusak });
            Olcek.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(0.94, 1, sure) { EasingFunction = yumusak });
        }

        void IpucunuYaz()
        {
            Ipucu.Text = duzenlemeModu
                ? "✎ Düzenleme modu · yuvaya tıkla ya da program sürükle · sağ tık: seçenekler · bitince Esc"
                : $"1–5 ile aç · ← → ve Enter · sağ tık: düzenle · Esc kapat · {KisayolMetni}";
        }

        // ================= Yuvalar =================

        Border YuvaOlustur(int i)
        {
            var numara = new TextBlock
            {
                Text = (i + 1).ToString(),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = SoluRenk,
                Margin = new Thickness(9, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            var simge = new Image { Width = 44, Height = 44, Stretch = Stretch.Uniform };
            RenderOptions.SetBitmapScalingMode(simge, BitmapScalingMode.HighQuality);

            var arti = new TextBlock
            {
                Text = "+",
                FontSize = 30,
                FontWeight = FontWeights.Light,
                Foreground = SoluRenk,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 44,
                Margin = new Thickness(0, -2, 0, 0)
            };

            var ad = new TextBlock
            {
                FontSize = 11.5,
                Foreground = YaziRengi,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextAlignment = TextAlignment.Center,
                MaxWidth = 78,
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var orta = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) };
            orta.Children.Add(simge);
            orta.Children.Add(arti);
            orta.Children.Add(ad);

            var izgara = new Grid();
            izgara.Children.Add(orta);
            izgara.Children.Add(numara);

            var kutu = new Border
            {
                Width = 92,
                Height = 104,
                Margin = new Thickness(5),
                CornerRadius = new CornerRadius(18),
                Background = YuvaArka,
                BorderBrush = YuvaKenar,
                BorderThickness = new Thickness(1.2),
                Child = izgara,
                Cursor = Cursors.Hand,
                AllowDrop = true,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1, 1),
                Tag = i
            };

            kutu.MouseEnter += (_, _) => { kutu.Background = YuvaArkaUzerinde; Buyut(kutu, 1.07); };
            kutu.MouseLeave += (_, _) => { kutu.Background = YuvaArka; Buyut(kutu, 1.0); };
            kutu.MouseLeftButtonUp += (_, _) =>
            {
                if (duzenlemeModu) ProgramSec(i);
                else Ac(i);
            };
            kutu.MouseRightButtonUp += (_, e) => { MenuGoster(i, kutu); e.Handled = true; };

            kutu.DragOver += (_, e) =>
            {
                e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None;
                e.Handled = true;
            };
            kutu.DragEnter += (_, _) => kutu.BorderBrush = YuvaKenarSecili;
            kutu.DragLeave += (_, _) => SeciliyiCiz();
            kutu.Drop += (_, e) =>
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] dosyalar && dosyalar.Length > 0)
                    YuvayaKoy(i, dosyalar[0]);
                SeciliyiCiz();
            };

            return kutu;
        }

        static void Buyut(Border b, double hedef)
        {
            if (b.RenderTransform is not ScaleTransform s) return;
            var anim = new DoubleAnimation(hedef, TimeSpan.FromMilliseconds(120))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            s.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            s.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        void YuvayiDoldur(int i)
        {
            var kutu = yuvalar[i];
            var orta = (StackPanel)((Grid)kutu.Child).Children[0];
            var simge = (Image)orta.Children[0];
            var arti = (TextBlock)orta.Children[1];
            var ad = (TextBlock)orta.Children[2];

            var y = ayarlar.Yuvalar[i];
            if (y == null || string.IsNullOrWhiteSpace(y.Yol))
            {
                simge.Source = null;
                simge.Visibility = Visibility.Collapsed;
                arti.Visibility = Visibility.Visible;
                ad.Text = "Ekle";
                ad.Foreground = SoluRenk;
                kutu.ToolTip = "Program eklemek için tıkla";
                return;
            }

            var kaynak = Simge.Al(y.Yol);
            simge.Source = kaynak;
            simge.Visibility = kaynak != null ? Visibility.Visible : Visibility.Collapsed;
            arti.Visibility = Visibility.Collapsed;
            if (kaynak == null)
            {
                arti.Text = "?";
                arti.Visibility = Visibility.Visible;
            }
            else arti.Text = "+";

            ad.Text = y.Ad;
            ad.Foreground = YaziRengi;
            kutu.ToolTip = $"{y.Ad}\n{y.Yol}";
        }

        void SeciliyiCiz()
        {
            for (int i = 0; i < yuvalar.Length; i++)
            {
                yuvalar[i].BorderBrush = i == secili ? YuvaKenarSecili : YuvaKenar;
                yuvalar[i].BorderThickness = new Thickness(i == secili ? 1.8 : 1.2);
            }
        }

        // ================= Programlar =================

        void Ac(int i)
        {
            var y = ayarlar.Yuvalar[i];
            if (y == null || string.IsNullOrWhiteSpace(y.Yol))
            {
                ProgramSec(i);
                return;
            }

            if (!File.Exists(y.Yol) && !Directory.Exists(y.Yol))
            {
                pencereAcik = true;
                var cevap = MessageBox.Show(this,
                    $"\"{y.Ad}\" bulunamadı:\n{y.Yol}\n\nYerine başka bir program seçmek ister misin?",
                    "Ayaz Launcher", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                pencereAcik = false;
                if (cevap == MessageBoxResult.Yes) ProgramSec(i);
                else Activate();
                return;
            }

            try
            {
                var bilgi = new ProcessStartInfo(y.Yol) { UseShellExecute = true };
                var klasor = Path.GetDirectoryName(y.Yol);
                if (!string.IsNullOrEmpty(klasor) && Directory.Exists(klasor) &&
                    y.Yol.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    bilgi.WorkingDirectory = klasor;

                Gizle();
                Process.Start(bilgi);
            }
            catch (Exception ex)
            {
                // Kullanıcı UAC'yi reddederse (1223) sessiz geç.
                if (ex is System.ComponentModel.Win32Exception w && w.NativeErrorCode == 1223) return;
                MessageBox.Show($"\"{y.Ad}\" açılamadı.\n\n{ex.Message}",
                    "Ayaz Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void ProgramSec(int i)
        {
            var dlg = new OpenFileDialog
            {
                Title = $"{i + 1}. yuva için program seç",
                Filter = "Programlar ve kısayollar (*.exe;*.lnk;*.url;*.bat;*.cmd)|*.exe;*.lnk;*.url;*.bat;*.cmd|Tüm dosyalar (*.*)|*.*",
                DereferenceLinks = false,
                CheckFileExists = true
            };

            var mevcut = ayarlar.Yuvalar[i]?.Yol;
            if (!string.IsNullOrEmpty(mevcut) && Directory.Exists(Path.GetDirectoryName(mevcut)))
                dlg.InitialDirectory = Path.GetDirectoryName(mevcut);
            else
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs";

            pencereAcik = true;
            bool? sonuc = dlg.ShowDialog(this);
            pencereAcik = false;

            if (sonuc == true) YuvayaKoy(i, dlg.FileName);

            Activate();
            Focus();
        }

        void YuvayaKoy(int i, string yol)
        {
            ayarlar.Yuvalar[i] = new Yuva { Yol = yol, Ad = Simge.Ad(yol) };
            ayarlar.Kaydet();
            YuvayiDoldur(i);
        }

        void Kaldir(int i)
        {
            ayarlar.Yuvalar[i] = null;
            ayarlar.Kaydet();
            YuvayiDoldur(i);
        }

        void Tasi(int i, int j)
        {
            if (j < 0 || j >= yuvalar.Length) return;
            (ayarlar.Yuvalar[i], ayarlar.Yuvalar[j]) = (ayarlar.Yuvalar[j], ayarlar.Yuvalar[i]);
            ayarlar.Kaydet();
            YuvayiDoldur(i);
            YuvayiDoldur(j);
        }

        void MenuGoster(int i, Border kutu)
        {
            bool dolu = ayarlar.Yuvalar[i] != null;
            var menu = new ContextMenu { PlacementTarget = kutu };

            MenuItem Oge(string baslik, Action is_, bool etkin = true)
            {
                var m = new MenuItem { Header = baslik, IsEnabled = etkin };
                m.Click += (_, _) => is_();
                return m;
            }

            if (dolu) menu.Items.Add(Oge("Aç", () => Ac(i)));
            menu.Items.Add(Oge(dolu ? "Değiştir…" : "Program seç…", () => ProgramSec(i)));
            menu.Items.Add(Oge("◀ Sola taşı", () => Tasi(i, i - 1), i > 0));
            menu.Items.Add(Oge("Sağa taşı ▶", () => Tasi(i, i + 1), i < yuvalar.Length - 1));
            if (dolu)
            {
                menu.Items.Add(new Separator());
                menu.Items.Add(Oge("Dosya konumunu aç", () => KonumuAc(i)));
                menu.Items.Add(Oge("Kaldır", () => Kaldir(i)));
            }

            pencereAcik = true;
            menu.Closed += (_, _) =>
            {
                pencereAcik = false;
                Dispatcher.InvokeAsync(() => { if (IsVisible) Activate(); }, DispatcherPriority.Background);
            };
            menu.IsOpen = true;
        }

        void KonumuAc(int i)
        {
            var yol = ayarlar.Yuvalar[i]?.Yol;
            if (string.IsNullOrEmpty(yol)) return;
            try
            {
                Gizle();
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{yol}\"") { UseShellExecute = true });
            }
            catch { }
        }

        // ================= Klavye =================

        void TusaBasildi(object sender, KeyEventArgs e)
        {
            var tus = e.Key == Key.System ? e.SystemKey : e.Key;
            int no = tus switch
            {
                >= Key.D1 and <= Key.D5 => tus - Key.D1,
                >= Key.NumPad1 and <= Key.NumPad5 => tus - Key.NumPad1,
                _ => -1
            };

            if (no >= 0)
            {
                e.Handled = true;
                if (duzenlemeModu) ProgramSec(no);
                else Ac(no);
                return;
            }

            switch (tus)
            {
                case Key.Escape:
                    e.Handled = true;
                    Gizle();
                    break;
                case Key.Left:
                    e.Handled = true;
                    secili = secili <= 0 ? yuvalar.Length - 1 : secili - 1;
                    SeciliyiCiz();
                    break;
                case Key.Right:
                case Key.Tab:
                    e.Handled = true;
                    secili = (secili + 1) % yuvalar.Length;
                    SeciliyiCiz();
                    break;
                case Key.Enter:
                case Key.Space:
                    e.Handled = true;
                    if (secili >= 0)
                    {
                        if (duzenlemeModu) ProgramSec(secili);
                        else Ac(secili);
                    }
                    break;
                case Key.Delete:
                    if (duzenlemeModu && secili >= 0) { Kaldir(secili); e.Handled = true; }
                    break;
            }
        }

        // ================= Kar ❄ =================

        void KariBaslat()
        {
            Kar.Children.Clear();
            // Açılışta küçük bir kar serpintisi, sonra hafif ve sürekli yağış.
            for (int i = 0; i < 14; i++) KarTanesiEkle(rastgeleBaslangic: true);
            karZamanlayici.Start();
        }

        void KarTanesiEkle(bool rastgeleBaslangic = false)
        {
            double g = Cam.ActualWidth, y = Cam.ActualHeight;
            if (g <= 0 || y <= 0 || Kar.Children.Count > 40) return;

            double boyut = 6 + rastgele.NextDouble() * 8;
            var tane = new TextBlock
            {
                Text = "❄",
                FontSize = boyut,
                Foreground = Brushes.White,
                Opacity = 0.18 + rastgele.NextDouble() * 0.45
            };

            double x = rastgele.NextDouble() * g;
            double basY = rastgeleBaslangic ? rastgele.NextDouble() * y * 0.6 - 10 : -boyut - 4;
            double sure = 2.6 + rastgele.NextDouble() * 2.8;
            double kayma = (rastgele.NextDouble() - 0.5) * 40;

            Canvas.SetLeft(tane, x);
            Canvas.SetTop(tane, basY);
            Kar.Children.Add(tane);

            var dus = new DoubleAnimation(basY, y + 4, TimeSpan.FromSeconds(sure));
            dus.Completed += (_, _) => Kar.Children.Remove(tane);
            var yan = new DoubleAnimation(x, x + kayma, TimeSpan.FromSeconds(sure))
            {
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            tane.BeginAnimation(Canvas.TopProperty, dus);
            tane.BeginAnimation(Canvas.LeftProperty, yan);
        }
    }
}
