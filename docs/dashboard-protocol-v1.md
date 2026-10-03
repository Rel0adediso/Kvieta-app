# Android eşlikçi protokolü — V1 RC

21 Eylül 2026. Yalnız kullanım okumak için ayrı yetki. Eski `ManagerDevice*` PIN kurtarma/yönetim yetkileri kullanılmaz. Android kaynakları komşu `kveita-android` deposundadır.

## Çalışma ve sahiplik

Windows: Ayarlar → Gizlilik ve veri → Telefonum. PIN varsa doğrulanır, PIN'siz Aile akışı kapalıdır. Aynı ekran Android QR'ını, kullanım yetkisini ve açılır PIN kurtarma bölümündeki mevcut güvenilir telefon kaydını gösterir. Eski kurtarma yönetimi buradan açılır; kayıtlar/anahtarlar birleştirilmez, Android eşleşmesi kurtarma yetkisi vermez. Kurtarma yetkisi ekleme hâlâ ayrı tarayıcı QR'ı gerektirir. QR davet metni varsayılan kapalıdır. Ağ sunucusu açılamasa da kurtarma ayarları kullanılabilir.

Paylaşım penceresi açıkken ilk eşleştirme için özel IPv4 adresinde TCP 24882 / HTTPS sunucusu çalışır; pencere kapanınca listener kapanır. Onaylı uzaktan paylaşım ise Kvieta çalıştığı sürece ayrı relay yayıncısıyla devam eder. Aynı anda bir Android telefon eşleştirilir.

Windows RSA TLS sertifikası ve telefonun P-256 açık anahtarı `%LOCALAPPDATA%/Kvieta/dashboard-pairing.bin` içinde CurrentUser DPAPI ile korunur. Sertifika pininin sunucu yeniden açılınca aynı kalması gerekir. Özel anahtar Android Keystore'dadır; bağlantı metadatası telefon yedeklerinden dışlanır. Bu Windows kaydı, aynı Windows kullanıcısına karşı bir ayrıcalık sınırı değildir; mevcut koruma/Guardian politikası değiştirilmez.

## Davet ve onay

QR biçimi: `kvieta-companion://pair?v=1&origin=<urlencoded-https-origin>&pin=<certificate-sha256-hex>&token=<256-bit-hex>&expires=<unix-seconds>`.

- Origin yalnız 10/8, 172.16/12 veya 192.168/16 içindeki açık IPv4 ve 24873–24882 port aralığıdır. Kullanıcı adı, yol, fragment, yinelenen alanlar, DNS ve HTTP reddedilir. Bu sürüm sunucusu 24882 kullanır.
- Davet iki dakika geçerlidir; Android beş dakikadan uzun ömrü de reddeder. Telefon/bilgisayar saatleri doğru olmalıdır.
- Android HTTPS sunucusunun DER sertifikası SHA-256 değerini QR'daki pinle ve sertifika tarihlerini denetler. Hostname CA denetimi yerine tam sertifika pinlemesi vardır; otomatik yönlendirme ve proxy kullanılmaz.
- `POST /v1/pair`: JSON `token`, `deviceId` (UUID), `name` (en çok 80 karakter), `publicKey` (P-256 SPKI DER base64), `signature` (ECDSA SHA-256 DER base64).
- İmzalanan UTF-8 metin, son newline olmadan: `kvieta-dashboard-pair-v1\n{token}\n{deviceId}\n{name}\n{publicKey}`.
- Geçerli öneri daveti tüketir. Sunucu `state=pending` ve SPKI SHA-256 özetinin ilk 12 hex karakterinden oluşan `code` döndürür. İki cihazda kod karşılaştırılıp Windows penceresinde onaylanır. Ağdan onay endpoint'i yoktur. Her davette en çok 10 öneri denenebilir.
- Öneri henüz yetki değildir. İmzalı okuma `pending` döner; kullanım verisi içermez. Telefon pinli eş bağlantısını onay beklerken saklar, işlem yeniden açıldığında onay durumunu tekrar sorgulayabilir.

## Kullanım özeti ve uzaktan bağlantı

`POST /v1/snapshot`: JSON `deviceId`, `time` (Unix saniye), `nonce` (16 rastgele baytın 32 hex karakteri), `signature`.

İmza metni: `kvieta-dashboard-read-v1\n{deviceId}\n{time}\n{nonce}`. Zaman toleransı 90 saniye; kullanılan nonce'lar 180 saniye tutulur, kapasite 512. Yeniden başlatmada bu geçici tekrar önleme önbelleği sıfırlanır. İşlem hiçbir policy mutasyonu içermez; ileride mutasyon eklenecekse kalıcı işlem kimliği ve sonuç makbuzu ayrıca gerekir.

Yanıt: `state=paired`, `snapshot={deviceName,mode,sessionState,localDay,observedAtUtc,servedAtUtc,stale,usedSeconds,remainingSeconds,applications,timeRequest}`. Liste en çok üç öğedir; tam uygulama yolları dışarı gönderilmez. `timeRequest` yalnız bilgisayarın oluşturduğu tek etkin talebi ve durum makbuzunu içerir.

Telefonum ekranından uzaktan erişim ayrıca etkinleştirilir. Bilgisayar her oda için ayrı yazma, okuma ve karar token'ı ile 256 bit içerik anahtarı üretir. Cloudflare Worker yalnız son AES-256-GCM şifreli özeti ve son şifreli kararı saklar; içerik anahtarı sunucuya gitmez. Özet bir günden, karar bir saatten sonra iletilmez. İptal, odada tombstone bırakır. Android anahtarları Android Keystore ile sarar.

## Ek süre talebi

- Yalnız Aile modundaki bilgisayar 1–180 dakika ve isteğe bağlı en çok 120 karakter not içeren bir talep oluşturur. Talep 30 dakika geçerlidir ve bilgisayarın yerel gününe bağlıdır.
- Android yalnız bekleyen talebi onaylar veya reddeder. Onayda 1–180 dakika seçilir. Karar `kvieta-relay-decision-v1\n{room}` AAD değeriyle AES-256-GCM şifrelenir.
- Worker karar içeriğini okuyamaz; karar token'ı yalnız karar posta kutusuna yazabilir. Plan, PIN veya başka politika endpoint'i yoktur.
- Bilgisayar request ID, gün, sona erme, karar zamanı ve dakika sınırını yeniden doğrular. Karar önce `ApprovedAwaitingDevice`, sonra oturum işlemi tarafından atomik claim edilerek `Applied` olur. Aynı karar ikinci kez süre ekleyemez.
- Android uygulama açıkken yaklaşık 30 saniyede yeniler. WorkManager bekleyen talepleri Android'in izin verdiği periyotta denetler ve kullanıcı izin verdiyse yerel bildirim üretir.

Sertifika hatası, 403 veya geçersiz yanıt başarılı bağlantı sayılmaz. İptal edilen telefonun yeni istekleri reddedilir. Telefon önceden aldığı veriyi geri alınmış saymaz; 403 sonrasında görünüm temizlenir. Geçici ağ hatasında eldeki kayıt eski olarak kalır. Sunucu yanıtlarında `Cache-Control: no-store`; istek logging kapalıdır. Kestrel gövde sınırı 8 KiB, bağlantı sınırı 8; Android yanıt sınırı 64 KiB ve timeout 6 saniyedir.

## Doğrulama ve sınırlar

Telefonum açılışındaki `SectionExpanderStyle` kaynak hatası bağımsız WPF pencere oluşturma testiyle yeniden üretildi ve stil uygulama kaynaklarına taşındı. `--phone-window-checks` ana pencere olmadan oluşturma, çevrimdışı kurtarma erişimi ve çizim kontrolünü çalıştırır. Android tarayıcısı uygulama temasındaki kamera çerçevesi, izin/hata ekranı, fener ve yanlış davette yeniden tarama akışını kullanır; kamera arka plana geçince durur. Gerçek telefon kamera testi bekleniyor.

`dotnet run --project tests/Kvieta.Core.SmokeTests -c Release -- --dashboard-checks`: gerçek TLS üzerinden hatalı pin, onay öncesi erişim, imza, tekrar, eski istek, diskten yeniden bağlanma ve iptal kontrolleri.

Kotlin/.NET birlikte test: smoke uygulamasını `--dashboard-interop <geçici-davet-dosyası>` ile başlat; Android Gradle test sürecine `KVIETA_TEST_INVITE_FILE` vererek `DesktopInteropTest` çalıştır. Bu seçenek yalnız smoke test programındadır; izole örnek veri ve geçici store kullanır, otomatik onay yalnız bu fixture'dadır, iki dakikada kapanır. Test fiziksel Android Keystore'u değil gerçek Kotlin ağ/kriptografi istemcisini JDK anahtarıyla çalıştırır.

Fiziksel telefon kamera, OEM arka plan zamanlaması ve gerçek Guardian oturumuna süre uygulanması sahada ayrıca doğrulanmalıdır. Hesap sistemi, iOS, çoklu telefon ve telefondan plan/PIN yönetimi bilinçli olarak kapsam dışıdır.

Temel API kaynakları: [Android Keystore](https://developer.android.com/privacy-and-security/keystore), [Kestrel HTTPS yapılandırması](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/endpoints).
