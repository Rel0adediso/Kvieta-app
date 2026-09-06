# Kullanıcı notlarına göre arayüz düzeltmeleri

Bu değişiklikler Alpha 4.2 paketinde birleştirildi; önceki Alpha 4 yayını tarihsel olarak korunur.

| Not | Yapılan değişiklik |
|---|---|
| Dil seçimi | Bayrak ve konuşma simgeleri kaldırıldı; TR / EN dil etiketleri kullanılıyor. |
| Kurulum hareket etmeli | Boş başlık alanına tıklama yüzeyi eklendi; pencere çalışma alanına sınırlandı. |
| Kurulum çok büyük | Başlangıç 960×620, minimum 800×500. Küçük çalışma alanına göre başlangıç boyutu ayrıca azalır; yerel Windows kenarlarıyla yeniden boyutlandırma etkin. Sol panel ve boşluklar daraltıldı; uzun adımlar dikey kaydırılır. |
| Kullanımımı gör | Başlangıç şablonu “Kullanımı takip et” olarak adlandırıldı; seçim ve sonraki adım açıkça gösteriliyor. |
| Nasıl kullanacaksınız ekranı | Kart metinleri sarılıyor; küçük ekranlarda içerik kaydırılabiliyor. |
| Uygulama içi öğretici | İlk açılışta ekran üzerinde alanları işaretleyip sayfalar arasında ilerler. Esc veya sağ üstteki atla düğmesiyle kapanır; ? ve F1 yeniden açar. Telefon teklifi tur kapanana kadar bekler. |
| Kurulumdaki plan | Ana uygulamayla aynı saat seçicisini kullanır; saat/dakika seçimi ve klavyeyle giriş desteklenir. |
| Telefon onayında yanlış uyarı | Kod yenileme uyarısı telefon eşleştirmesinden kaldırıldı; yalnız kurtarma kodları yenilenirken gösterilir. PIN açıklaması kısaltıldı. |
| Tekrarlanan telefon tanımlama | Kurulum sonrası otomatik teklif, kullanıcı iptal etse de gösterilmiş olarak kaydedilir. Sonraki açılışlarda tekrarlanmaz; Ayarlar'dan elle erişim sürer. |
| Punto / arayüz büyütme | %100–150 ölçek sol menü ve üst çubuğu da kapsar. Kartlar kullanılabilir genişliğe göre sütun değiştirir; yatay sınırsız ölçüm kaldırıldı. |
| Bugün | Kalan süre ve kullanım büyütüldü; kural sayısı ve tamamen kapalı metrikleri kaldırıldı. |
| Uygulamalar | En çok kullanılanlar ve uygulama adına göre otomatik kategoriler bu sayfaya taşındı. Farkındalık modunda salt kullanım görünümü açılır. |
| Ayarlar | Görünüm/genel, koruma, gizlilik ve bakım olarak açılır bölümlere ayrıldı. |
| Kurtarma kodları | Boş metin kutusu yerine okunaklı, satıra sığmadığında alta geçen kod kartları kullanılıyor. Kopyalama, kaydetme ve onay koşulları korundu. |
| Korumayı başlat ne işe yarıyor | Düğme ve açıklaması Windows koruma hizmetini başlattığını belirtiyor. |
| Uygulama ekle | Çoklu EXE seçimi, arayüzü kilitlemeden kimlik okuma, görünür hata bildirimi ve Kaydet açıklaması eklendi. |
| Geçmiş çok büyük | Ritim ayrıntıları açılır bölüme taşındı; gün satırları sıkıştırıldı. |
| Kişisel esnekte gereksiz davranış | Değişiklik beklemesi ve Ek süre iste düğmesi kaldırıldı; üst koruma düzeyinden esneğe geçişin beklemesi korunuyor. |
| Cihaz simgesi yok | Temada bulunmayan renk kaynağı yerine düğmenin rengi kullanılıyor. |
| Plan alanlarına giriş | Saatler klavyeyle yazılabiliyor; seçici de çalışıyor. Geçersiz saatler kaydedilmiyor. Kaydet öncesi alan odağı bırakılıyor. |
| Gereksiz teknik metin | Ayarlar altındaki ham yerel dosya yolu gizlendi; durum mesajı için açıklama balonu eklendi. |
| Sözler otomatik değişsin | Oturum ekranındaki dört kısa mesaj 30 saniyede bir dönüyor; başkasına atfedilen alıntı kullanılmadı. |

## Doğrulama sınırı

Saat seçimi, doğrudan saat girişi, geçersiz saat, esnek modun bekleme ve ek süre
davranışı ile ölçek kaydı otomatik regresyon kontrollerine eklendi. WPF ekran
renderlarıyla ana sayfalar, kurulum kartları, tema simgeleri ve plan incelendi.
İlk açılış kaydının yeniden yüklenmesi, ölçek değişikliğiyle korunması ve eşzamanlı
pencerelerde yalnız bir rehber açılması da regresyon testleriyle denetlendi.
Turun sayfa geçişleri, tamamlanması ve atlama düğmesi; kartların daralıp genişlemesi
ve gizlenen kartların boşluk bırakmaması test edildi. Sentetik verilerle 1160×730 ve
840×540 / %150 dahil WPF renderları incelendi. Canlı telefon eşleştirmesi ve VM üzerinde
kurulum/güncelleme bu kaynak kontrollerinin yerine geçmez; ayrıca kullanıcı testi gerekir.
Canlı masaüstü aracı Access is denied döndürdüğü için gerçek fareyle tüm düğmelere
basma, dosya seçimi ve pencere sürükleme uçtan uca doğrulanamadı.
