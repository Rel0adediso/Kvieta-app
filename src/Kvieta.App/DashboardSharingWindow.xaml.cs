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
        Heading.Text = T("Telefonum", "My phone");
        Explanation.Text = T("Bilgisayarındaki günü telefonundan takip et. İki cihazı aynı Wi-Fi'a bağla, Android'de Bilgisayar bağla → QR kodunu okut'a dokun.",
            "Follow your day from your phone. Connect both devices to the same Wi-Fi, then choose Connect computer → Scan computer QR on Android.");
        UsageTitle.Text = T("Android eşlikçi", "Android companion");
        UsagePermission.Text = T("Yetki: kullanım özetini görme ve yalnızca bilgisayardan gelen ek süre taleplerini yanıtlama. Planı ve PIN'i değiştiremez.", "Permission: view usage and answer only extra-time requests created by this computer. Cannot change the plan or PIN.");
        ManualInvite.Header = T("QR okutamıyorum · Daveti elle gir", "Cannot scan? Enter invitation manually");
        RecoverySection.Header = T("PIN kurtarma yetkisi", "PIN recovery permission");
        RecoveryHint.Text = T("Mevcut güvenilir telefonun burada korunur. Kurtarma, telefon tarayıcısına ayrıca verilen bir yetkidir; Android bağlantısı bu yetkiyi otomatik almaz.",
            "Your existing trusted phone is preserved here. Recovery is a separate permission held by the phone browser; pairing Android does not grant it automatically.");
        RecoveryButton.Content = T("Kurtarma telefonunu yönet", "Manage recovery phone");
        SharingHint.Text = T("Paylaşılanlar: bilgisayar adı, kullanım süreleri, en çok kullanılan uygulamalar ve varsa ek süre talebi. Telefon planı veya PIN'i değiştiremez.",
            "Shared: computer name, usage totals, most-used applications and an active extra-time request. The phone cannot change your plan or PIN.");
        ApproveButton.Content = T("Kodlar aynı · Onayla", "Codes match · Approve");
        RenewButton.Content = T("Yeni bağlantı oluştur", "Create a new invitation");
        RevokeButton.Content = T("Telefon erişimini kaldır", "Revoke phone access");
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
        RenewButton.Visibility = paired || !_endpoint.CanPair ? Visibility.Collapsed : Visibility.Visible;
        ApproveButton.Visibility = pending ? Visibility.Visible : Visibility.Collapsed;
        CodeText.Text = _endpoint.VerificationCode ?? "";
        StatusText.Text = paired ? T($"Bağlı telefon: {_endpoint.PhoneName}", $"Paired phone: {_endpoint.PhoneName}")
            : pending ? T($"{_endpoint.PendingName} bağlanmak istiyor. Kodu karşılaştır.", $"{_endpoint.PendingName} wants to connect. Compare the code.")
            : T("Bağlantı 2 dakika geçerli. Süresi geçerse yeni bağlantı oluştur.", "Invitation valid for 2 minutes. Create a new one if it expires.");
        if (!paired && !_endpoint.CanPair) StatusText.Text = T("QR eşleştirmesi için özel bir yerel ağa bağlanıp bu ekranı yeniden aç.", "Connect to a private local network and reopen this screen to pair with QR.");
    }
    private void Renew()
    {
        if (_endpoint is not { CanPair: true }) { Update(); return; }
        try { InvitationBox.Text = _endpoint.CreateInvitation(); QrImage.Source = QrCodeImageService.Create(InvitationBox.Text); Update(); }
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
        if (System.Windows.MessageBox.Show(this, T("Telefonun kullanım özetine erişimi kaldırılacak. PIN kurtarma yetkisi etkilenmez.",
            "Remove this phone's usage access? PIN recovery is unaffected."), T("Telefon erişimini kaldır", "Revoke phone access"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
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
