# Kvieta v1 Windows test matrisi

Her koşuda sürüm, commit, Setup SHA-256, Windows build'i, gerçek sonuç ve kanıt
kaydedilir. PIN, kurtarma kodu, kullanıcı adı veya özel ekran içeriği eklenmez.

Sonuçlar: `Geçti`, `Kaldı`, `Engelli`, `Tekrar gerekli`.

## Koşu bilgileri

| Sürüm | Commit | Setup SHA-256 | Windows | Hesap | Ekran/DPI | Tarih |
|---|---|---|---|---|---|---|
| | | | | | | |

## Temel kullanım

| Kimlik | Senaryo | Beklenen sonuç | Sonuç/not |
|---|---|---|---|
| BASE-01 | Türkçe ve English temiz kurulum | Seçilen dil korunur, doğru yüzey açılır | |
| BASE-02 | Açık/koyu/sistem teması | Kontroller okunur, native beyaz alan oluşmaz | |
| BASE-03 | Awareness | Ölçüm çalışır, kısıtlama uygulanmaz | |
| BASE-04 | Personal Flexible | Manuel oturum kullanıcı tarafından kapatılabilir | |
| BASE-05 | Personal Balanced | Plan/bekleme uygulanır, güvenli yönetim yolu erişilir | |
| BASE-06 | Guarded ve Protected | Guardian/PIN koruması doğru uygulanır | |

## Oturum ve Windows yaşam döngüsü

Bu bölüm yalnız doğrulanmış yönetici çıkış yolu hazırken çalıştırılır. Gerçek kilit
yüzeyinde Debug kısayoluna güvenilmez.

| Kimlik | Senaryo | Beklenen sonuç | Sonuç/not |
|---|---|---|---|
| LIFE-01 | `Alt+Tab`, `Win+D`, görev çubuğu | İzin verilmeyen masaüstü erişimi oluşmaz | |
| LIFE-02 | Kontrol Merkezi isteği | Tek yönetim penceresi öne gelir | |
| LIFE-03 | Kvieta sürecini kapatma | Guardian gerekli modda yüzeyi geri getirir | |
| LIFE-04 | Explorer restart | Korumasız boşluk kalmaz | |
| LIFE-05 | `Win+L`, uyku ve yeniden başlatma | Sayaç ve yüzey doğru geri gelir | Fiziksel test kullanıcı isteğiyle sonraki doğrulama turuna ertelendi; sıralı lifecycle policy ve otomatik regresyonları hazır. |
| SUPPORT-01 | Sistem Sağlığı ve Tanılama Merkezi | Uygulama, installer, Guardian ve yerel veri durumları doğru gösterilir; rapor dışa aktarılır | Kullanıcı isteğiyle final doğrulama turuna ertelendi. |
| LIFE-06 | Saat ileri/geri alma | Güvenli saat koruması uygulanır | |

## Ekran ve erişilebilirlik

| Kimlik | Senaryo | Beklenen sonuç | Sonuç/not |
|---|---|---|---|
| DISP-01 | İki fiziksel ekran ve takma/çıkarma | Bütün ekranlar boşluk bırakmadan korunur | |
| DISP-02 | %100–200 DPI, dikey/negatif yerleşim | Metin ve düğmeler kırpılmaz | |
| DISP-03 | Reduce Motion | Gereksiz animasyonlar kapanır | |

## Installer yaşam döngüsü

| Kimlik | Senaryo | Beklenen sonuç | Sonuç/not |
|---|---|---|---|
| INST-01 | Temiz kurulum | Dosya, servis ve kısayollar doğru oluşur | |
| INST-02 | Reinstall/upgrade/repair | Kullanıcı verisi korunur | |
| INST-03 | Downgrade | Eski paket reddedilir | |
| INST-04 | İptal/rollback | Ayarlar yarım yazılmaz | |
| INST-05 | Uygulama içinden kaldırma | Doğrulama sonrası Windows kaldırıcı açılır | |

## V1 ürün paketleri

`Otomatik geçti` kayıtları smoke testteki deterministik kabul örnekleridir; gerçek
cihaz gerektiren satırların yerine geçmez. Son fiziksel koşuda üstteki build/SHA
alanları ve kanıt bağlantısı doldurulur.

| Kimlik | Otomatik kabul | Fiziksel/görsel kabul | Sonuç/not |
|---|---|---|---|
| V1-01 | Günlük snapshot, Pending, checkpoint, migration ve ayrı silme kapsamları | Saat dilimi ve çok günlük gerçek kullanım | Otomatik geçti; fiziksel açık |
| V1-02 | Bağımsız odak, gece yarısı, restart ve çift bitiş | Uyku/kilit sırasında gerçek aktif süre | Otomatik geçti; fiziksel açık |
| V1-03 | Tek günlük hedef ve ertesi gün yürürlük | TR/EN ilk kullanım anlaşılabilirliği | Otomatik geçti; görsel açık |
| V1-04 | Dar kayıt, hata, ertele/gizle ve policy gevşetme | Disk dolu ve UAC iptali | Otomatik geçti; cihaz açık |
| V1-05 | Yedi gün, Protector, milestone ve paylaşım modeli | İki dil/DPI/ekran okuyucu | Otomatik geçti; görsel açık |
| V1-06 | Niyet gizliliği, erken/tam bitiş ve devam policy'si | Klavye ve gerçek oturum yüzeyi | Otomatik geçti; görsel açık |
| V1-07 | Ortak açıklama ve gerçek kural predicate'i | Guardian kaybı/çakışan kural | Otomatik geçti; cihaz açık |
| V1-08 | Kapalı/yok/sıfır/bozuk ölçüm ve dönem karşılaştırması | İlk iki dakikalık kullanım | Otomatik geçti; gözlem açık |
| V1-10 | Policy sonucu, recovery hazırlığı ve atomik ret | UAC/Guardian iptali | Otomatik geçti; cihaz açık |
| V1-11 | Sentetik yan etkisiz önizleme ve pencere yükleme | TR/EN klavye kapatma | Otomatik geçti; görsel açık |
| V1-12 | Ölçüm/yerel kayıt hata durumları | Guardian servis kaybı/iyileşme | Otomatik geçti; cihaz açık |
| V1-13 | Odak+günlük/plan/uygulama sınırı ve olay kimliği | Eşzamanlı gerçek yüzey | Otomatik geçti; cihaz açık |
| V1-14 | Öncelik, erteleme, süre sonu ve tekrar bastırma | Uyku/modal/ekran okuyucu | Otomatik geçti; cihaz açık |
| V1-15 | Envanter, kapsamlı silme ve güvenlik alanı koruma | Dosya seçme/üzerine yazma/disk hatası | Otomatik geçti; cihaz açık |

Kalan her vaka için GitHub hata şablonuyla issue açılır ve bağlantısı buraya yazılır.
