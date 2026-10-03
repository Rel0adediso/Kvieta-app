# Mobil V1 RC2 — 22 Eylül 2026

Bu kayıt yalnız bu turda doğrulanan kapsamı belirtir; önceki V1 planındaki bütün saha kabul maddelerinin tamamlandığı anlamına gelmez.

## Değişiklikler

- Android ekranı Kvieta paletinde sadeleştirildi: belirgin özet, aile talebi önce, kullanım ayrıntıları kapalı, bağlantı/bildirim/unutma ayrı panelde.
- Yenileme ve karar işlemleri seri hale getirildi; yenileme artık gönderimi iptal etmiyor.
- Karar şifreli olarak diskte hazırlanıyor. Belirsiz ağ sonucu aynı kimlik/sequence/ciphertext ile yeniden deneniyor. Relay yalnız birebir aynı tekrar için başarı dönüyor; farklı içerikli tekrar reddediliyor.
- Son özet Keystore ile şifreli saklanıyor, çevrimdışı/eski olarak gösteriliyor. Taşıma tokenları görüntü önbelleğine alınmıyor.
- Windows talebi eşleşme odasına ve yerel güne bağlı; iptal edilmiş, dünün veya süresi dolmuş talepler uygulanmıyor.
- PC yalnız ledger diske başarıyla yazıldıktan sonra Applied makbuzu veriyor. Tekrar/crash sonrası hedef bonus korunuyor; çift ekleme önleniyor.
- Relay yayıncı kilidi süreç ölünce işletim sisteminin bıraktığı dosya kilidine taşındı; salt okuma anahtar yükseltmesi eşleşme kaydını yeniden yazmıyor. Kota yanıtı karar okumayı engellemiyor.
- Ön plan uygulama kullanımı ile aile oturum sayacı ayrı alanlar; arayüz bunları karıştırmıyor.

## Doğrulananlar

- Windows Release derlemesi: 0 hata, 0 uyarı; tam çekirdek smoke testleri başarılı.
- Telefon penceresi başlatma, offline toparlanma ve render kontrolleri başarılı.
- Talep deposu: tekrar karar, tek claim, crash toparlanma, kayıt hedefi, iptal, son tarih, gece yarısı ve 1440 dakika tavanı kontrolleri başarılı.
- Android: 22 test, 0 başarısız, 0 atlanan; assembleDebug ve lintDebug başarılı. Lint 69 uyarı, 0 hata.
- Gerçek izole .NET HTTPS endpoint: Kotlin istemcisi doğru pini kabul etti, yanlış pini reddetti, onay sonrası özet okudu.
- Canlı Cloudflare: şifreli .NET özeti Android istemcisinde açıldı, Android kararı .NET tarafından okundu. Birebir karar tekrarı kabul, farklı tekrar ret.
- Relay Node testleri yerelde ve canlıda 2/2 geçti; sentetik bağımsız odalar kullanıldı, gerçek kullanıcı verisi gönderilmedi.
- Robolectric API 35 yerel render: açık aile, koyu kişisel, 320 dp / %150 yazı. Dakika seçimi göndermez, Onayla gönderir; ayrıntı açılır; süresi dolmuş talep onaylanamaz.
- Workers deploy: 065854f1-4361-42ad-964f-7dc0a4546ba7.

## Paketler

- Android: artifacts/android/v1-rc2/Kvieta-Companion-V1-RC2-debug.apk
  - 1.0.0-rc2-dev, versionCode 4, com.kvieta.companion.dev, API 26+.
  - SHA256 8C9F99098F1ABCACFB4F99069A18BDB28340594B4EE2ADD8FB721BCEF69D2C18
  - APK v2 debug imzası doğrulandı.
- Windows: artifacts/installer-community/5.1.1/Kvieta-Setup-V1-RC2.exe
  - SHA256 856D19727BB3CFCBD9CCAA00782255AEFD24142B4BEDDF538F1906BBE3F3FE64D
  - MSI/setup sürüm ve manifest kontrolleri geçti. Community paketi kod imzasızdır.

## Sınırlar ve deneme

1. Önce uyumlu Windows V1-RC2, ardından Android RC2 kurulur. Eski RC1-dev üstüne APK güncellemesi yapılabilir; veri silme zorunlu değildir.
2. İlk eşleşme aynı Wi-Fi'da, iki ekrandaki kod karşılaştırılıp PC'de onaylanarak yapılır. PC'de dışarıdan erişim açıldıktan sonra telefonda bir kez yenilemek gerekir.
3. Gerçek telefon bağlı olmadığı için kamera, Android Keystore, Windows güvenlik duvarı, gerçek mobil veri geçişi ve OEM pil kısıtları fiziksel cihazda henüz doğrulanmadı.
4. Bildirim anlık push değildir: WorkManager periyodik kontrolü 15 dakika, işletim sistemi geciktirebilir. Ön planda 30 saniyede yenileme ve elle yenileme vardır.
5. Üretim/mağaza imzalaması ve geniş cihaz matrisi bu RC kapsamının dışındadır. Git commit/push bu turda yapılmadı.
