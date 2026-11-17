# V1 yayın öncesi kod incelemesi

Tarih: 2026-11-17. İncelenen kaynak: `c9b006c2bf53122dd8bda2bcdade5761fb6e2d81`.
Öncelikli fark: Alpha 6.5 (`dc3eee2`) → mevcut HEAD. Bu rapor uygulama davranışını değiştirmez.

Bu çalışma statik kod incelemesi ve sınırlı yerel doğrulamadır; penetrasyon testi,
tam platform kabulü veya hatasızlık garantisi değildir. GitHub CI ve canlı site
erişimi doğrulanamadı. Fiziksel Android, standart kullanıcı + ayrı yönetici,
Safe Mode ve installer yaşam döngüsü bu ortamda çalıştırılmadı.

## Düzeltme ve doğrulama durumu — 17 Kasım 2026

Aşağıdaki ilk inceleme bulguları tarihsel kayıttır. Düzeltmeler çalışma ağacına uygulanmıştır; commit/push/release yapılmamıştır.

| Bulgu | Uygulanan değişiklik | Kalan kabul |
|---|---|---|
| R01 / R02 | Sahiplik günlüğü, kesilen yazıdan toparlanma, başka politikaları koruma; zorunlu eklenti kurulumu kaldırıldı | Gerçek tarayıcı/registry/legacy kurulum matrisi |
| R03 / R04 / R05 | Family reboot/gece kilidi korunuyor; web gevşetmeleri gecikmeli; ana düğme etkili politikayı kapatıyor | Gerçek cihaz yaşam döngüsü |
| R06 | Kalıcı komut makbuzu, monoton sequence, retry/expiry, kimlik bazlı uygulama | Fiziksel Android offline/crash akışı |
| R07 | Korunan dosya ACL'i gevşetilmeden Guardian üzerinden dar değişiklikler; Family için PIN'den üretilen komuta bağlı kanıt | Android istemcisinin mobile-parent-policy-v1 protokolünü desteklemesi ve ayrı yönetici hesabında kabul |
| R08 | Dört bağlantıyla sınırlı işleme; istemci timeout'u sunucuyu durdurmuyor | Otomatik gerçek soket testi geçti |
| R09 / R10 | Host/path eşleşmesi; global tuş ve pano yazımı kaldırıldı; hedefli UIA/pencere mesajları ve hata geri bildirimi | Gerçek tarayıcılarda yönlendirme, odak ve erişilebilirlik |
| R11 | Sahip olunan Task Manager değerini hedef SID'de geri alma; Web Guard hatası kaldırma sonucuna taşınıyor | Ayrı yöneticiyle kaldırma/onarım |
| R12 | Legacy Alpha→stable sıralaması; community manuel resmi indirme; imzalı akışta yayıncı pin kontrolü | Son paketle uçtan uca güncelleme |
| R13 | Görünen v1.0.0 korunarak dahili MSI/FileVersion 7.0.0; build/CI/metadata belgeleri uyumlu | Dağıtılmış MSI'larla gerçek yükseltme |

Ek düzeltmeler: İngilizce bariyer metinleri, ana düğmenin yüklemede korunması,
PIN hash'inin telefon özetinden kaldırılması, test deposunun gerçek masaüstünü
kilitlememesi, güncel yol haritası ve geri getirilen V1 test matrisi.
Salt/iteration bilgisi yalnız PIN kanıtı için kamusal challenge parametresidir;
PIN veya hash gönderilmez. Eski Android istemcilerinin korunan ayar değiştirmesi
kanıtsız kabul edilmez; istemci uyumluluğu tamamlanmadan bu yetki ilan edilmemeli.

Son kaynak üzerinde geçenler:

- Debug ve Release: 0 hata / 0 uyarı.
- İki yapılandırmada tam smoke paketleri; her birinde 44 yeni V1 regresyon kontrolü.
- Dashboard TLS, eşleştirme, replay, kalıcılık ve iptal kontrolleri.
- Gerçek soket üzerinde yavaş istemci, eşzamanlı istek ve timeout sonrası sunucu devamlılığı.
- Tam dotnet format verify, belge tutarlılığı ve public-build kontrolü.

Paketleme güncellemesi: seri MSBuild kullanımıyla restore/publish engeli giderildi.
Yeni self-contained win-x64 uygulama üretildi ve publish kontrolünden geçti
(82.137.658 byte). MSI ICE doğrulaması bu oturumun Windows Installer Service'e
erişememesi nedeniyle WIX0217 ile durdu; doğrulama kapatılmadı. Yeni installer,
embedded payload, manifest ve hash doğrulaması tamamlandı sayılmaz. Önceki
1.0.0 artifactleri bu düzeltmeleri içermez ve final aday olarak kullanılmamalı.
Android geliştirmesi kullanıcı kararıyla sonraya bırakıldı: `POST-V1.md`.
Commit/push istendi; `.git/index.lock` yazımı izin nedeniyle, GitHub erişimi DNS
nedeniyle engellendi. Kaynak dosyaları korunuyor; commit/push başarılı sayılmıyor.
Eski bir sürümün daha önce sildiği politika özgün değerleri, eski yedek yoksa
yeniden oluşturulamaz; yeni kod bunları tahmin edip başka ayarları silmez.


## İlk inceleme: yayından önce giderilecek bulgular

### R01 — Tarayıcı politikaları başka uygulamalara ait kayıtlarla birlikte siliniyor (yüksek)

- Kaynak: `src/Kvieta.App/Services/WebGuardService.cs:491-625`.
- URLBlocklist içindeki bütün değerler silinip yeniden yazılıyor. Kapatırken
  URLBlocklist ve ExtensionInstallForcelist ağaçları bütünüyle siliniyor.
  Firefox WebsiteFilter/DNSOverHTTPS ve DoH/SafeSearch değerlerinde de önceki
  ayarları geri getiren bir sahiplik/yedek mekanizması yok.
- Sonuç: mevcut aile/kurum güvenlik politikaları veya zorunlu eklentiler
  kaybolabilir. Uninstaller aynı temizleme yolunu çağırıyor. NeedsApply başka
  bir uygulamanın politika anahtarını da Kvieta temizliği için yeterli sayıyor.
- Çözüm: yalnız Kvieta'nın eklediği kayıtları yönetmek; önceki değerleri türüyle
  saklamak; dışarıdan değişen ayarı körlemesine geri yazmamak. Önceden var olan
  politika ve eklentilerle aç/kapat/kaldır testi zorunlu.
- Kanıt: kaynakta doğrudan silme; gerçek registry üzerinde test yapılmadı.

### R02 — Kısa video düğmesi üçüncü taraf eklentiyi zorunlu kurduruyor (yüksek)

- Kaynak: `WebGuardService.cs:542-549`.
- BlockShortFormVideo, sabit bir Chrome Web Store eklenti kimliğini bütün
  Chromium politika hivelarında ExtensionInstallForcelist/1'e yazıyor.
- Sonuç: kullanıcı yalnız kısa video engelleme seçmişken ek yazılım bağımlılığı
  oluşuyor ve mevcut 1 numaralı kayıt üzerine yazılabiliyor. Eklentinin kimliği,
  izinleri ve güncelleme zinciri bu incelemede doğrulanmadı; zararlı olduğu iddia edilmiyor.
- Çözüm: v1'de bu bağımlılığı kaldırmak veya ayrı, açık bir kullanıcı seçimi ve
  doğrulanmış izin/tedarik değerlendirmesiyle sunmak. Diğer eklentilere dokunmamak.

### R03 — Aile uzaktan kilidi reboot ile kalkıyor (yüksek)

- Kaynak: `src/Kvieta.Core/Services/SessionEngine.cs:33-46`.
- ObserveClock, Reboot durumunda moddan bağımsız RemoteLockActive=false yapıyor.
- İzole gerçek Core DLL kontrolü: Family + RemoteLockActive=true için reboot
  öncesi `True / TimeExpired`, reboot sonrası `False / Ready`.
- Sonuç: yönetici tekrar izin vermeden, normal izinli saatlerde uzaktan kilit
  yeniden başlatılarak kaldırılabilir. Gece değişiminde de kilit sıfırlanıyor;
  kilidin süreli mi, yönetici kaldırana kadar mı olduğu ürün sözleşmesinde açık olmalı.
- Çözüm: Personal esneklik kuralını Family'den ayırmak; Family kilidini açık
  yönetici kararı veya belgelenmiş son kullanma zamanına bağlamak.

### R04 — Web kısıtları değişiklik bekleme süresini atlıyor (yüksek)

- Kaynak: `src/Kvieta.Core/Services/SettingsPolicyComparer.cs:7-112` ve
  `src/Kvieta.App/ViewModels/MainViewModel.cs:1112-1154`.
- HasRelaxation web koruması, SafeSearch, kısa video ve domain listesine bakmıyor.
- İzole Core kontrolünde dört işlem de False döndü: web'i kapat, SafeSearch'ü
  kapat, kısa videoyu kapat, engelli domaini kaldır.
- Ek sorun: başka bir gevşetme gecikmeli dalı açsa bile `immediate`, desired'dan
  kopyalanıyor ve eski web değerleri geri konmuyor; web gevşetmesi yine hemen uygulanıyor.
- Çözüm: web gevşetmelerini karşılaştırmaya ve gecikmeli snapshot'a dahil etmek.
  Sıkılaştırmanın hemen, gevşetmenin süre sonunda uygulandığını test etmek.

### R05 — Web koruması ana düğmesi ve fiili koruma farklı davranıyor (orta)

- Kaynak: `MainViewModel.cs:698-755`, `WebGuardService.cs:228,498,584`,
  `DopamineTrapWatcher.cs:53,95-96`.
- Ana düğmeyi kapatmak BlockShortFormVideo'yu temizlemiyor. Servis ve watcher
  WebGuardEnabled OR BlockShortFormVideo kullanırken HasWebRestrictions AND kullanıyor.
- Sonuç: arayüz web korumasını kapalı gösterebilirken kısa video engeli/eklenti
  zorlaması devam eder. MobileWebGuardStore da aynı tutarsız kombinasyonu kaydedebilir.
- Çözüm: bağımsız özellikler mi ana aç/kapat mı olduğuna karar verip UI, payload,
  persistence ve enforcement için tek etkili-politika hesabı kullanmak.

### R06 — Telefon komutları kaybolabilir veya tekrar çalışabilir (yüksek)

- Kaynak: `src/Kvieta.App/Services/DashboardRelay.cs:145-146,187-225` ve
  `companion-relay/src/worker.mjs:167-181`.
- Son işlenen ID yalnız bellekte; işlem denenmeden önce atanıyor. ApplyAsync
  bool sonuçları kontrol edilmiyor. Worker son komutu bir saate kadar döndürüyor.
- Sonuç: disk/ACL hatası veya Gate çakışmasında komut tekrar denenmiyor. Süreç
  yeniden başladığında aynı son komut yeniden uygulanabiliyor; örneğin telefondan
  kilitle, PC'de aç, uygulamayı yeniden başlat akışında eski kilit geri gelebilir.
- Genel plan/web/app komutları, süre isteği kararlarındaki tarih ve request
  eşleştirme kontrollerinden geçmiyor. Bu ayrı bir güvenilirlik/replay sınırı.
- Çözüm: kalıcı room+sequence/decision ID kaydı, komut türüne göre expiry,
  başarısızlıkta retry, yalnız kalıcı uygulamadan sonra Applied makbuzu.
- Kanıt: statik akış; canlı relay üzerinde komut gönderilmedi.

### R07 — Standart kullanıcıdaki telefon ayarları Guardian yazma yolunu kullanmıyor (yüksek)

- Kaynak: `MobilePlanChangeStore.cs:28-39`, `MobileAppRuleStore.cs:31-69`,
  `MobileWebGuardStore.cs:33-96`; `ProtectionServiceManager.cs:1102` civarı
  HardenProtectedPolicyAcl; `KvietaGuardianService.cs:360` civarı LaunchProtectedSession.
- Protected policy standart kullanıcıya yalnız R izniyle sertleştiriliyor.
  Guardian oturumu kullanıcı token'ıyla açılıyor; mobil store'lar bu dosyaya
  doğrudan JsonSettingsStore.SaveAsync yapıyor, Guardian IPC kullanmıyor.
- Sonuç: önerilen standart Windows hesabında plan/web/app düzenlemelerinin
  kaydedilememesi beklenir. R06 bu başarısızlıkları da görünmez hale getiriyor.
- Plan store'u ayrıca korumasız Personal için yerel settings fallback'i kullanmıyor.
- Çözüm: doğrulanmış eşleşmiş cihaz yetkisiyle dar kapsamlı Guardian komutları;
  normal kullanıcı profilinde Personal fallback; telefona gerçek sonuç bildirimi.
- ACL altında fiziksel/VM yeniden üretim gerekli; bu ortamda yapılmadı.

### R08 — Tek yavaş bağlantı yerel eşleşme/kurtarma sunucusunu durdurabilir (orta)

- Kaynak: `src/Kvieta.App/Services/LocalNetworkHttpServer.cs:79-103`.
- Tüm bağlantılar sırayla işleniyor. İstemciye verilen 12 saniyelik timeout
  OperationCanceledException üretince dış catch, sunucu iptal edilmemiş olsa da return ediyor.
- Sonuç: aynı ağdaki eksik/yavaş bir HTTP isteği eşleşme/kurtarma serve task'ını
  bitirebilir; listener açık kalsa bile yeni istekler işlenmez.
- Çözüm: global iptal ile istemci timeout'unu ayırmak; timeout sonrası accept'e
  devam etmek; eşzamanlılığı sınırlı işleme almak.
- Kanıt: statik kontrol akışı; ortam socket engeli nedeniyle ağda yeniden üretilmedi.

### R09 — URL yerine alt metin eşleştirmesi hatalı engelliyor (orta)

- Kaynak: `src/Kvieta.App/Services/DopamineTrapWatcher.cs:161-183`.
- Contains(domain/pattern), host/path sınırı tanımıyor. Örneğin
  `https://example.org/search?q=tiktok.com` kısa video olarak; engelli
  example.com için `https://notexample.com` engelli site olarak algılanır.
- Çözüm: Uri parse, host'un eşitliği veya noktalı alt domain sınırı, yalnız ilgili
  host'ta path kontrolü; query/fragment ve yazılmakta olan adresi ayırmak.

### R10 — Bariyer yanlış pencereye tuş gönderebilir, panoyu bozabilir (orta)

- Kaynak: `src/Kvieta.App/DopamineBarrierWindow.xaml.cs:149-194,224-241`.
- SetForegroundWindow dönüşü kontrol edilmiyor; ardından global keybd_event ile
  Ctrl+L/V/Enter gönderiliyor. Açık bariyer yeni browser için yeniden hedeflenmiyor.
- Clipboard yalnız text olarak yedekleniyor; görsel/dosya/zengin içerik kaybolabilir.
  Aradaki kullanıcı clipboard değişikliği de eski metinle ezilebilir.
- Çözüm: hedef pencereyi işlem anında doğrulamak; başarısız odaklamada durmak;
  pencere kapanması/sekme değişimi için davranış belirlemek; panosuz yöntem tercih etmek.
- UI üzerinde otomasyonla test yapılmadı.

### R11 — Kaldırma geride kalan korumaya rağmen başarılı sayılabiliyor (orta)

- Kaynak: `src/Kvieta.App/Services/UninstallDataCleaner.cs:12-43`.
- WebGuardService.ApplyDirect sonucu yok sayılıyor, hatalar FailedPaths'e
  eklenmiyor. Sonuç yalnız dosya/startup temizleme hatalarına göre başarılı oluyor.
- UAC'de ayrı yönetici kullanılmışsa Registry.CurrentUser hedef çocuğun değil,
  yöneticinin hive'ıdır; web/TaskManager temizleme userSid'i kullanmıyor.
- Çözüm: kullanıcıya ait hive'ı SID ile hedeflemek, temizleme sonucunu doğrulamak,
  başarısız koruma geri alımını açık hata ve onarım yolu olarak sunmak.

### R12 — Mevcut güncelleyici community v1 stratejisiyle uyumsuz (yüksek, önceki inceleme)

- Kaynak: `UpdateCheckService.cs:174-184,229-272`, `AuthenticodeTrustVerifier.cs`,
  `artifacts/installer-community/1.0.0/release-manifest.json`.
- Community paket signed=false, public güncelleyici Authenticode zorunlu tutuyor.
  Alpha 6.5 sayısal olarak 6.5'e temizleniyor ve v1.0.0'dan büyük görülüyor.
- İmza doğrulayıcı da yalnız geçerli Windows güven zincirine bakıyor; beklenen
  Kvieta yayıncısına pin yapmıyor. İmzalı dağıtım seçilirse yayıncı doğrulaması gerekir.
- Çözüm: public/community dağıtımı için açık güven modeli, doğru release-channel
  sıralaması; imzasız binary çalıştırmayı sırf kontrolü kaldırarak çözmemek.

### R13 — MSI sürüm numarası eski Alpha'ların altına düşüyor (yüksek risk, önceki inceleme)

- Kaynak: `Directory.Build.props`, `installer/Kvieta.Setup/Package.wxs:14-16`,
  `docs/RELEASE_PROCESS.md:21-35`.
- Eski paketler belgede 4.2.0/4.3.0, v1 paket 1.0.0; MajorUpgrade downgrade engelliyor.
- Çözüm: dağıtılmış paketlerin gerçek MSI metadata'sını envanterlemek; artan
  installer sürümü veya açık migration. Temiz kurulumun geçmesi upgrade'i kanıtlamaz.

## V1 için ürün ve doğrulama eksikleri

1. **Komut durumu:** telefon için Gönderildi / PC'ye ulaştı / Uygulandı / Hata
   durumları ayrı olmalı. Bir bool'un yok sayılması yerine hata nedeni ve retry gösterilmeli.
2. **Tek politika hesabı:** yerel UI, Guardian, session, mobil ve web worker aynı
   normalized effective policy'yi kullanmalı. Her platformun ayrı istisna eklemesi Android'de sorunları büyütür.
3. **Yerelleştirme:** yeni DopamineBarrierWindow XAML/code-behind metinleri Türkçe
   sabit; English seçimiyle uyumlu değil. DynamicResource/localization'a taşınmalı.
4. **Kapsam sözü:** DACL'ı “kernel-level”, kurtarmayı “bulletproof”, opsiyonel
   Cloudflare relay/push varken bütün companion kullanımını “zero cloud” diye sunmamak.
5. **Test matrisi:** Alpha→v1 diff'inde docs/V1-TEST-MATRIX.md silinmiş. Yeni
   WebGuard/watcher/bariyer/remote-lock davranışlarına özel regresyonlar görünmüyor.
   Deleted test matrix maddelerini güncel aday için geri kurup kanıtla kapatmak.
6. **Doküman doğruluğu:** ROADMAP/RELEASE_PROCESS eski Alpha'yı güncel gösterirken
   verify-documentation geçiyor. Belge kontrolünün geçmesi yayına hazırlığı kanıtlamaz.
7. **Paket boyutu:** manifest Setup 141,910,319 byte, MSI 76,742,656 byte.
   78 MB ifadesinin hangi çıktıyı anlattığını açık yazmak; Setup boyutuyla karıştırmamak.

## Doğrulama kaydı

- Aynı HEAD için Release derlemesi: 0 hata / 0 uyarı. Compiler server bağlantı
  timeout'u nedeniyle `-m:1 -p:UseSharedCompilation=false` ile tamamlandı.
- Release manifest doğrulaması: commit, boyut, SHA-256 ve community kimliği geçti.
- Documentation verification geçti; içerik güncelliği boşluğu yukarıda kayıtlı.
- Tam Release smoke: mobil request kontrolleri geçti; DashboardEndpointChecks
  localhost bağlantısında SocketException 10013 ile durdu. Tam testler geçti denemez.
- İzole Core DLL kontrolleri (`artifacts/review-checks`): dört web gevşetmesi
  algılanmadı; Family reboot kilit kaldırma davranışı yeniden üretildi.
- Kaynak davranışı değiştirilmedi. Registry, hosts, servis veya gerçek telefon
  komutlarıyla deney yapılmadı. Canlı site/GitHub kontrolü tamamlanmadı.

## Önerilen kapanış sırası

1. R01/R02: sistem ayarlarına sahiplik, geri alma ve eklenti bağımlılığı.
2. R03/R04/R05: kilit ve etkili politika kuralları; küçük Core regresyonları.
3. R06/R07/R08: telefon komutlarının kalıcılığı, Guardian yetkisi ve timeout toparlanması.
4. R12/R13: dağıtım/güncelleme stratejisi; son aday installer'ı yeniden üretmek.
5. R09/R10/R11 ve yerelleştirme: yanlış engel, kullanıcı girdisi ve kaldırma güvenilirliği.
6. Son adayda standart kullanıcı + ayrı yönetici VM: temiz kurulum, Alpha upgrade,
   reboot/crash/sleep, PIN/recovery, korumalı kaldırma, mevcut browser politikaları,
   offline/relay retry, iki monitör ve %100/%150/%200 DPI. Aday commit ve hash'leri kaydetmek.
7. Ardından GitHub CI, gerçek download bağlantısı ve site mobil/masaüstü kabulü.
