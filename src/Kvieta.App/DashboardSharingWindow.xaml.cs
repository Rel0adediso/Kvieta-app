using System.Windows;
using Kvieta.App.Services;
using Kvieta.Core.Models;

namespace Kvieta.App;

public partial class DashboardSharingWindow : Window
{
    private readonly DashboardEndpoint? _endpoint;
    private readonly Func<Task> _manageRecovery;
    private static string T(string tr, string en) => LocalizationService.CurrentLanguage == LanguagePreference.English ? en : tr;

    public DashboardSharingWindow(DashboardEndpoint? endpoint, Func<Task> manageRecovery)
    {
        InitializeComponent();
        _endpoint = endpoint;
        _manageRecovery = manageRecovery;
        Heading.Text = T("Kvieta Mobil", "Kvieta Mobile");
        Explanation.Text = T("Bilgisayarındaki günü telefonundan takip et. Kvieta Mobil uygulamasında Bilgisayar bağla → QR kodunu okut'a dokun.",
            "Follow your day from your phone. In Kvieta Mobile, tap Connect computer → Scan computer QR.");
        UsageTitle.Text = T("Kvieta Mobil Bağlantısı", "Kvieta Mobile Connection");
        UsagePermission.Text = T("Yetki: kullanım özetini görme, uzaktan kilitleme ve ek süre taleplerini yanıtlama. Planı ve PIN'i değiştiremez.", "Permission: view usage summaries, remote lock, and answer extra-time requests. Cannot change the plan or PIN.");
        ManualInvite.Header = T("QR okutamıyorum · Daveti elle gir", "Cannot scan? Enter invitation manually");
        RecoverySection.Header = T("PIN kurtarma yetkisi", "PIN recovery permission");
        RecoveryHint.Text = T("Mevcut güvenilir telefonun burada korunur. Kurtarma, telefon tarayıcısına ayrıca verilen bir yetkidir; Kvieta Mobil bu yetkiyi otomatik almaz.",
            "Your existing trusted phone is preserved here. Recovery is a separate permission held by the phone browser; Kvieta Mobile does not grant it automatically.");
        RecoveryButton.Content = T("Kurtarma telefonunu yönet", "Manage recovery phone");
        SharingHint.Text = T("Paylaşılanlar: bilgisayar adı, kullanım süreleri, en çok kullanılan uygulamalar, uzaktan kilit durumu ve ek süre talepleri. Kvieta Mobil planı veya PIN'i değiştiremez.",
            "Shared: computer name, usage totals, most-used applications, remote lock status, and extra-time requests. Kvieta Mobile cannot change your plan or PIN.");
        ApproveButton.Content = T("Kodlar aynı · Onayla", "Codes match · Approve");
        RenewButton.Content = T("Yeni bağlantı oluştur", "Create a new invitation");
        RevokeButton.Content = T("Kvieta Mobil erişimini kaldır", "Revoke Kvieta Mobile access");
        RemoteButton.Content = T("Dışarıdan erişimi aç", "Enable remote access");
        if (_endpoint is not null) _endpoint.Changed += EndpointChanged;
        UpdateRecovery();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_endpoint is { CanPair: true } && _endpoint.PhoneName is null)
        {
            try { _endpoint.EnableRemote(); } catch { }
            Renew();
        }
        else Update();
    }
    private void EndpointChanged() => Dispatcher.BeginInvoke(new Action(Update));
    private void Update()
    {
        if (_endpoint is null)
        {
            QrBorder.Visibility = ManualInvite.Visibility = RenewButton.Visibility = RevokeButton.Visibility = RemoteButton.Visibility = Visibility.Collapsed;
            StatusText.Text = T("Bağlantı başlatılamadı. Özel bir Wi-Fi ağına bağlanıp bu ekranı yeniden aç. Kurtarma ayarlarına aşağıdan ulaşabilirsin.",
                "Sharing could not start. Connect to a private Wi-Fi network and reopen this screen. Recovery settings remain available below.");
            return;
        }
        bool paired = _endpoint.PhoneName is not null;
        bool pending = _endpoint.PendingName is not null;
        if (_endpoint.RemoteEnabled)
            SharingHint.Text = T("Özet yaklaşık her dakika güncellenir; bekleyen süre talepleri daha sık kontrol edilir. Bilgisayar kapalıysa son kayıt gösterilir. Sunucu bir günden eski özeti iletmez.",
                "Summaries update about once a minute; pending time requests are checked more often. When the computer is off, the last record is shown. The server does not deliver summaries older than a day.");
        RemoteButton.Visibility = paired && !_endpoint.RemoteEnabled ? Visibility.Visible : Visibility.Collapsed;
        RemoteHint.Text = !paired ? "" : _endpoint.RemoteEnabled
            ? T("Uzaktan erişim açık. Telefonda aynı Wi-Fi'dayken Özeti yenile'ye bir kez bas. Sonra mobil veriden de bağlanabilirsin. Kvieta çalışırken bu pencere kapalı olsa da özet paylaşılır.",
                "Remote access enabled. Refresh once on your phone while on the same Wi-Fi. Then mobile data works too. Summaries continue while Kvieta runs, even with this window closed.")
            : T("Dışarıdan erişimi açarsan kullanım özetin uçtan uca şifrelenerek Cloudflare üzerinden telefonuna iletilir. Sunucu yalnızca son şifreli özeti tutar.",
                "Enable remote access to send an end-to-end encrypted usage summary to your phone through Cloudflare. The server keeps only the latest encrypted summary.");
        QrBorder.Visibility = paired || pending || !_endpoint.CanPair ? Visibility.Collapsed : Visibility.Visible;
        ManualInvite.Visibility = paired || pending || !_endpoint.CanPair ? Visibility.Collapsed : Visibility.Visible;
        RevokeButton.Visibility = paired ? Visibility.Visible : Visibility.Collapsed;
        RenewButton.Visibility = !_endpoint.CanPair ? Visibility.Collapsed : Visibility.Visible;
        RenewButton.Content = paired ? T("Başka cihaz bağla (Yeni QR)", "Connect another device (New QR)") : T("Yeni bağlantı oluştur", "Create a new invitation");
        ApproveButton.Visibility = pending ? Visibility.Visible : Visibility.Collapsed;
        CodeText.Text = _endpoint.VerificationCode ?? "";
        StatusText.Text = paired ? T($"Bağlı Kvieta Mobil: {_endpoint.PhoneName} (Etkin ✓)", $"Paired Kvieta Mobile: {_endpoint.PhoneName} (Active ✓)")
            : pending ? T($"{_endpoint.PendingName} bağlanmak istiyor. Kodu karşılaştır.", $"{_endpoint.PendingName} wants to connect. Compare the code.")
            : T("Bağlantı geçerli. QR kodunu Kvieta Mobil uygulamasından okut.", "Invitation valid. Scan this QR code using the Kvieta Mobile app.");
        if (!paired && !_endpoint.CanPair) StatusText.Text = T("QR eşleştirmesi için özel bir yerel ağa bağlanıp bu ekranı yeniden aç.", "Connect to a private local network and reopen this screen to pair with QR.");
    }
    private void Renew()
    {
        if (_endpoint is not { CanPair: true }) { Update(); return; }
        try
        {
            if (!_endpoint.RemoteEnabled)
            {
                try { _endpoint.EnableRemote(); } catch { }
            }
            InvitationBox.Text = _endpoint.CreateInvitation();
            QrImage.Source = QrCodeImageService.Create(InvitationBox.Text);
            Update();

            _ = Task.Run(async () =>
            {
                try
                {
                    var store = DashboardRelay.Load(DashboardRelay.StorePath);
                    if (store?.Relay is not null)
                    {
                        var snap = await MainWindow.ReadDashboardSnapshotAsync();
                        await DashboardRelay.PublishAsync(store.Relay.Upgrade(), snap, CancellationToken.None);
                    }
                }
                catch { }
            });
        }
        catch (Exception) { StatusText.Text = T("Davet oluşturulamadı. Tekrar dene.", "Could not create invitation. Try again."); }
    }
    private void Renew_Click(object sender, RoutedEventArgs e) => Renew();
    private void Remote_Click(object sender, RoutedEventArgs e)
    {
        try { _endpoint?.EnableRemote(); Update(); }
        catch (Exception) { StatusText.Text = T("Uzaktan erişim açılamadı.", "Could not enable remote access."); }
    }
    private void Approve_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_endpoint?.Approve() != true) StatusText.Text = T("Davetin süresi doldu; yeni bağlantı oluştur.", "Invitation expired; create a new one.");
        }
        catch (Exception) { StatusText.Text = T("Eşleşme kaydedilemedi. Tekrar dene.", "Could not save pairing. Try again."); }
    }
    private async void Revoke_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show(this, T("Kvieta Mobil uygulamasının bu bilgisayara erişimi kaldırılacak. PIN kurtarma yetkisi etkilenmez.",
            "Remove Kvieta Mobile access for this computer? PIN recovery is unaffected."), T("Kvieta Mobil erişimini kaldır", "Revoke Kvieta Mobile access"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { _endpoint?.Revoke(); Renew(); }
        catch (Exception)
        {
            StatusText.Text = T("Erişim kaydı güncellenemedi. Paylaşımı durdurmak için Kvieta'yı kapat.", "Could not update access. Close Kvieta to stop sharing.");
            return;
        }
        try { if (_endpoint is not null) await _endpoint.FlushRevocationsAsync(); }
        catch (Exception)
        {
            StatusText.Text = T("Yeni paylaşım durdu. Sunucudaki son özete erişimi kaldırmak için internet bekleniyor; Kvieta yeniden deneyecek.",
                "New sharing stopped. Internet is needed to revoke access to the last server snapshot; Kvieta will retry.");
        }
    }
    private void UpdateRecovery()
    {
        try
        {
            var current = ManagerDeviceEnrollmentStore.Load();
            RecoveryStatus.Text = current?.IsActive == true
                ? T("Kurtarma telefonu: ", "Recovery phone: ") + ManagerDeviceWindow.GetFriendlyDeviceName(current.DeviceName, LocalizationService.CurrentLanguage == LanguagePreference.English)
                : T("Kurtarma telefonu eklenmemiş", "No recovery phone configured");
        }
        catch (Exception) { RecoveryStatus.Text = T("Kurtarma kaydı okunamadı", "Recovery record unavailable"); }
    }
    private async void Recovery_Click(object sender, RoutedEventArgs e)
    {
        RecoveryButton.IsEnabled = false;
        try { await _manageRecovery(); UpdateRecovery(); }
        catch (Exception) { RecoveryStatus.Text = T("Kurtarma ayarları açılamadı", "Could not open recovery settings"); }
        finally { RecoveryButton.IsEnabled = true; }
    }
    private void Window_Closed(object? sender, EventArgs e)
    {
        if (_endpoint is not null) _endpoint.Changed -= EndpointChanged;
    }
}
