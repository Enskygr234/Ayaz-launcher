using System;
using System.Threading;
using System.Windows;
using WF = System.Windows.Forms;

namespace AyazLauncher
{
    public partial class App : Application
    {
        const string KopyaAdi = "AyazLauncher_TekKopya";
        const string GosterAdi = "AyazLauncher_Goster";

        Mutex? tekKopya;
        EventWaitHandle? gosterSinyali;
        WF.NotifyIcon? tepsi;
        WF.ToolStripMenuItem? kisayolMenusu;
        KisayolTusu? kisayol;
        LauncherWindow? pencere;
        Ayarlar ayarlar = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            tekKopya = new Mutex(true, KopyaAdi, out bool yeni);
            if (!yeni)
            {
                // Zaten açık: var olan kopyaya "çubuğu göster" de ve çık.
                try { EventWaitHandle.OpenExisting(GosterAdi).Set(); } catch { }
                Shutdown();
                return;
            }

            base.OnStartup(e);

            bool ilk = Ayarlar.IlkCalisma;
            ayarlar = Ayarlar.Yukle();

            pencere = new LauncherWindow(ayarlar);
            pencere.KisayolMetni = KisayolTusu.Metin(ayarlar.Degistiriciler, ayarlar.Tus);

            kisayol = new KisayolTusu();
            kisayol.Basildi += () => pencere.AcKapa();

            TepsiKur();
            bool tamam = KisayoluKaydet();

            GosterSinyaliniDinle();

            if (ilk)
            {
                ayarlar.Kaydet();
                Bildir("Ayaz Launcher çalışıyor ❄",
                    $"Çubuğu açmak için {pencere.KisayolMetni} tuşlarına bas. İlk açılışta yuvalara program ekle.");
                pencere.Goster();
            }
            else if (!tamam)
            {
                Bildir("Kısayol kullanılamadı",
                    "Seçili kısayolu başka bir program kullanıyor. Tepsi simgesinden yenisini seçebilirsin.");
            }
        }

        bool KisayoluKaydet()
        {
            if (kisayol == null || pencere == null) return false;

            bool tamam = kisayol.Kaydet(ayarlar.Degistiriciler, ayarlar.Tus);

            // Varsayılan Ctrl+Space doluysa Ctrl+Alt+Space'e geç.
            if (!tamam && ayarlar.Degistiriciler == 0x2 && ayarlar.Tus == 0x20)
            {
                if (kisayol.Kaydet(0x3, 0x20))
                {
                    ayarlar.Degistiriciler = 0x3;
                    ayarlar.Kaydet();
                    tamam = true;
                }
            }

            pencere.KisayolMetni = KisayolTusu.Metin(ayarlar.Degistiriciler, ayarlar.Tus);
            if (kisayolMenusu != null) kisayolMenusu.Text = $"Kısayol tuşu: {pencere.KisayolMetni}…";
            if (tepsi != null) tepsi.Text = $"Ayaz Launcher ({pencere.KisayolMetni})";
            return tamam;
        }

        void TepsiKur()
        {
            System.Drawing.Icon ikon;
            try
            {
                var akis = GetResourceStream(new Uri("pack://application:,,,/app.ico"))!.Stream;
                ikon = new System.Drawing.Icon(akis, WF.SystemInformation.SmallIconSize);
            }
            catch
            {
                ikon = System.Drawing.SystemIcons.Application;
            }

            var menu = new WF.ContextMenuStrip();

            var ac = new WF.ToolStripMenuItem("Çubuğu aç") { Font = new System.Drawing.Font(menu.Font, System.Drawing.FontStyle.Bold) };
            ac.Click += (_, _) => pencere?.Goster();

            var duzenle = new WF.ToolStripMenuItem("Yuvaları düzenle");
            duzenle.Click += (_, _) => pencere?.Goster(duzenleme: true);

            kisayolMenusu = new WF.ToolStripMenuItem("Kısayol tuşu…");
            kisayolMenusu.Click += (_, _) => KisayolDegistir();

            var baslangic = new WF.ToolStripMenuItem("Windows ile başlat") { CheckOnClick = true };
            try { baslangic.Checked = Ayarlar.WindowsIleBasliyor; } catch { }
            baslangic.CheckedChanged += (_, _) =>
            {
                try { Ayarlar.WindowsIleBasliyor = baslangic.Checked; }
                catch { Bildir("Ayar kaydedilemedi", "Windows ile başlatma ayarı değiştirilemedi."); }
            };

            var cikis = new WF.ToolStripMenuItem("Çıkış");
            cikis.Click += (_, _) => Kapat();

            menu.Items.AddRange(new WF.ToolStripItem[]
            {
                ac, duzenle, new WF.ToolStripSeparator(), kisayolMenusu, baslangic, new WF.ToolStripSeparator(), cikis
            });

            tepsi = new WF.NotifyIcon
            {
                Icon = ikon,
                Text = "Ayaz Launcher",
                ContextMenuStrip = menu,
                Visible = true
            };
            tepsi.MouseClick += (_, a) =>
            {
                if (a.Button == WF.MouseButtons.Left) pencere?.AcKapa();
            };
        }

        void KisayolDegistir()
        {
            if (pencere == null || kisayol == null) return;
            pencere.Gizle();

            var eskiDeg = ayarlar.Degistiriciler;
            var eskiTus = ayarlar.Tus;

            var p = new KisayolPenceresi(eskiDeg, eskiTus);
            if (p.ShowDialog() != true) return;

            ayarlar.Degistiriciler = p.Degistiriciler;
            ayarlar.Tus = p.Tus;

            if (kisayol.Kaydet(ayarlar.Degistiriciler, ayarlar.Tus))
            {
                ayarlar.Kaydet();
                KisayoluKaydet();
                Bildir("Kısayol değişti", $"Artık {pencere.KisayolMetni} ile açılıyor.");
            }
            else
            {
                MessageBox.Show(
                    $"{KisayolTusu.Metin(ayarlar.Degistiriciler, ayarlar.Tus)} kısayolunu başka bir program kullanıyor. Eski kısayol korunuyor.",
                    "Ayaz Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                ayarlar.Degistiriciler = eskiDeg;
                ayarlar.Tus = eskiTus;
                KisayoluKaydet();
            }
        }

        void GosterSinyaliniDinle()
        {
            gosterSinyali = new EventWaitHandle(false, EventResetMode.AutoReset, GosterAdi);
            var izleyici = new Thread(() =>
            {
                while (true)
                {
                    try { gosterSinyali.WaitOne(); }
                    catch { return; }
                    Dispatcher.InvokeAsync(() => pencere?.Goster());
                }
            })
            { IsBackground = true, Name = "GosterSinyali" };
            izleyici.Start();
        }

        void Bildir(string baslik, string metin)
        {
            if (tepsi == null) return;
            tepsi.BalloonTipTitle = baslik;
            tepsi.BalloonTipText = metin;
            tepsi.ShowBalloonTip(4000);
        }

        void Kapat()
        {
            if (tepsi != null)
            {
                tepsi.Visible = false;
                tepsi.Dispose();
            }
            kisayol?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (tepsi != null) tepsi.Visible = false;
            try { tekKopya?.ReleaseMutex(); } catch { }
            base.OnExit(e);
        }
    }
}
