# V1 öncesi arayüz yenileme

## Amaç

Kullanıcı uygulamayı açınca mevcut durumu, sıradaki planı ve yapabileceği eylemi anlayabilmeli. Sitedeki sıcak Kvieta karakteri masaüstüne taşınırken okunabilirlik, klavye kullanımı ve koruma durumlarının açıklığı korunmalı.

## İlk inceleme — 13 Eylül 2026

- Uygulama WPF kullanıyor. Ana sayfalar `src/Kvieta.App/MainWindow.xaml` içindeki gizli sekmelerle yönetiliyor: Bugün, haftalık plan, uygulamalar, geçmiş ve ayarlar.
- Açık/koyu temanın ortak fırçaları ve Türkçe/İngilizce kaynak sözlükleri mevcut. Yenileme bunları kullanmalı.
- Bugün ekranında mevcut durum, açıklaması, sıradaki plan, kalan/kullanılan süre ve günlük karşılaştırma zaten bağlı verilerden geliyor. Yeni hesaplama veya uydurma veri gerekmiyor.
- Hızlı Odak Personal modunda gösteriliyor; hazır süreler, özel süre ve son odağı tekrarlama eylemleri mevcut. Görsel değişiklik bu eylemlerin yetkilendirme veya oturum mantığını değiştirmemeli.
- `ResponsiveColumns` dar alanda tek sütuna geçiyor. Yeni düzen bu mevcut mekanizmayı kullanıyor.
- Bu inceleme kaynak kodu üzerinden yapılmıştır; bütün ekranların etkileşimli görsel denetimi henüz yapılmadı.

## Aşama 1 — Bugün ekranının ilk uygulaması

Durum ve sıradaki plan ilk sütuna alındı. Süre bilgileri farklı bir yüzeyde gruplandı. Hızlı Odak başlığı ve yüzeyi belirginleştirildi; bu ekranın dış çerçeve yoğunluğu azaltıldı. Mevcut bağlamalar, yerelleştirme anahtarları, mod görünürlükleri ve tıklama işleyicileri korundu. Ortak stiller henüz diğer sayfalara yayılmadı.

Doğrulama: Debug derlemesi sıfır hata ve sıfır uyarıyla geçti; diff boşluk kontrolü geçti. Canlı görsel ve etkileşim kontrolü bekliyor. Bu aşama tamamlanmış V1 tasarımı değildir.

## Aşama 2 — Odak yüzeyi ve küçük sayaç

Tam ekran oturum yüzeyi tek bir merkezi odak alanında toplandı. Hafif arka plan şekilleri ve Kvieta harfi, zamanlayıcının önüne geçmeden ekrana derinlik veriyor. Sayaç ayrı yükseltilmiş yüzeye alındı; durum, açıklama, uyarı, kapanış ve mevcut eylem bağlamaları korunuyor.

Küçük oturum sayacı daha yumuşak köşeler, vurgu çizgisi ve belirgin kalan süreyle güncellendi. Mola düğmesi, odak niyeti alanı ve ilerleme çubuğunun davranışı değişmedi.

Doğrulama: Uygulama derlemesi sıfır hata ve sıfır uyarıyla, çekirdek duman testi ve diff boşluk kontrolü başarıyla tamamlandı. Bu oturumda yerel masaüstü pencere yakalama bağlantısı bulunmadığı için gerçek ekran görüntüsü, tema ve DPI kontrolleri hâlâ bekliyor.

## Aşama 3 — Haftalık plan ve uygulama kuralları

Haftalık planın tek dış çerçevesi kaldırıldı; her gün daha geniş boşluklu bağımsız bir plan kartına dönüştürüldü. Saat ve günlük limit bağlamaları korunuyor. Geçici izinler ana haftalık plandan ayrılan, daha belirgin bir yardımcı yüzey olarak düzenlendi.

Uygulama kullanım kartları yumuşatıldı. Kural listesi düz, çizgili satırlar yerine iki sütuna akabilen bağımsız kartlar kullanıyor. Uygulama simgesi, yol, kural açıklaması, mod seçimi, günlük limit ve kaldırma eylemi aynı kartta kalıyor. Boş kural durumu da kendine ait bir yüzeye alındı.

Doğrulama: Uygulama XAML derlemesi tamamlandıktan sonra çekirdek duman testleri ve diff boşluk kontrolü çalıştırıldı. Canlı tema, DPI ve klavye sırası kontrolü hâlâ gerçek pencere incelemesini bekliyor.

## Aşama 4 — Geçmiş ve ritim özeti

Haftalık toplam ana metrik olarak belirginleştirildi ve haftalık değişim doğrudan bu toplamın altında gösterildi. Günlük ortalama ile en çok kullanılan uygulama kendi ikincil kartlarına ayrıldı. Üç metrik sabit sütunlar yerine pencere daraldığında yeniden akabilen bir yerleşim kullanıyor.

Son yedi gün, uygulamalar ve son olaylar bölümleri çerçeve yoğunluğu azaltılmış yüzeylere taşındı. Ritim ayrıntıları isteğe bağlı açılır bölümde kalıyor; seri, günlük desen, öneri ve ilk adım alanlarının köşeleri ve boşlukları yeni tasarım diliyle uyumlu hâle getirildi. Mevcut hesaplama, gün seçimi, öneri uygulama, erteleme ve paylaşma eylemleri değiştirilmedi.

Doğrulama: Uygulama derlemesi, çekirdek duman testleri ve diff boşluk kontrolü çalıştırıldı. Gerçek veri yoğunluğu, uzun uygulama adları, TR/EN metin taşmaları ve DPI kontrolü gerçek pencere incelemesinde tamamlanacak.

## Aşama 5 — Ayarlar ve ilk kurulum

Ayarların görünüm, koruma, gizlilik ve bakım bölümleri korunarak ortak açılır bölüm stili yenilendi. İnce dış çizgiler kaldırıldı; başlık, boşluk ve köşe ölçekleri artırıldı. Personal değişiklik gecikmesi, Guardian durumu, veri yönetimi, onarım ve kaldırma gibi önemli alt bölümler ayrı yükseltilmiş yüzeylerde kalıyor. Güvenlik seçeneklerinin sırası ve görünürlük bağlamaları değişmedi.

Kurulum uygulamasındaki dil, mod ve özet kartları aynı yumuşak görsel dile geçirildi. Düğmeler, seçim kartları ve bilgi kutuları daha geniş köşe ve boşluk kullanıyor. Ana içerik alanına mevcut `KvietaAppIconTemplate` üzerinden çok hafif bir logo izi eklendi; yeni veya kopya bir logo çizilmedi. Kurulum adımları, seçenekler ve tıklama işleyicileri değişmedi.

Doğrulama: Ana uygulama ile kurulum uygulaması sıfır hata ve uyarıyla derlendi. Kurulumun gerçek yönetici izni, yükseltilmiş paket kurulumu ve farklı ekran ölçekleri manuel test matrisinde doğrulanacak.

## Aşama 6 — Gezinme, pencere ve boş durumlar

Sol gezinme satırları büyütüldü; seçili sayfaya Kvieta vurgu rengiyle ince bir işaret eklendi. Başlık çubuğu, yan menü ve içerik başlığı arasındaki sert ayırıcı çizgiler kaldırıldı. Daraltılmış ve genişletilmiş menünün mevcut davranışı ile genişlikleri korunuyor. Yalnızca bu cihaz durumuna daha okunaklı, yumuşak bir durum yüzeyi verildi.

Bugün ekranındaki uygulama yok durumu ile geçmişteki uygulama ve olay yok durumları küçük metinlerden gerçek boş durum yüzeylerine dönüştürüldü. Mevcut görünürlük bağlamaları, metin kaynakları ve uygulama davranışı değiştirilmedi.

Doğrulama: Uygulama sıfır hata ve uyarıyla derlendi; çekirdek duman testi ile diff boşluk kontrolü başarıyla tamamlandı. Gerçek pencere yakalama bağlantısı bulunmadığı için açık/koyu tema, ölçeklendirme ve klavye sırası kontrolleri hâlâ manuel incelemeyi bekliyor.

## Aşama 7 — Klavye erişimi ve hareket tutarlılığı

Daraltılmış sol menüde görsel metinler gizlense bile Bugün, Plan, Uygulamalar, Geçmiş ve Ayarlar seçeneklerinin erişilebilir adları korunuyor. Menü daraltma düğmesinin erişilebilir adı, açık ve kapalı durumla birlikte ve dil değişince güncelleniyor. Başlıkları gizli olan içerik sekmesi klavye sırasından çıkarıldı; böylece Tab tuşu görünmeyen bir denetime uğramıyor. Mevcut ortak odak halkaları ve uygulama ayrıntısı panelinin odak döngüsü korunuyor.

Kurulumdaki dil ve mod kartlarının sistem hareket tercihinden bağımsız çalışan yükselme animasyonu kaldırıldı. Açılır listeler ile saat seçicideki zorunlu solma animasyonları da kaldırıldı. Ana uygulamadaki sayfa, sayaç, ilerleme, kaydırma ve geçiş hareketleri mevcut `MotionService` üzerinden kullanıcının ayarıyla Windows'un hareket azaltma tercihini izlemeye devam ediyor.

Doğrulama: Ana uygulama ve kurulum uygulaması sıfır hata ve uyarıyla derlendi; çekirdek duman testi ile diff boşluk kontrolü geçti. Gerçek Tab/Shift+Tab turu, ekran okuyucu ve görünür odak incelemesi masaüstü pencere bağlantısıyla ayrıca yapılmalı.

## Aşama 8 — Alpha 4.3 VM doğrulama paketi

Uygulama ve kurulum sürümleri `4.3.0`, görünen paket etiketi `Alpha-4.3` olarak güncellendi. Alpha 4, yerel Alpha 4.1 ve Alpha 4.2 sürümlerinin 4.3'e güncelleme olarak tanınması duman testine eklendi. Sürüm notları; arayüz yenilemesini, erişilebilirlik/hareket değişikliklerini ve kalan saha sınırlamalarını belgeliyor.

Biçim doğrulaması, Debug/Release çözüm derlemeleri, iki yapıdaki çekirdek duman testleri, dokümantasyon denetimi ve public-build geliştirme bypass taraması geçti. İmzasız Release community paketi üretildi; MSI/Setup metadata, gömülü MSI, SHA-256, tek dosya publish ve release manifest kontrolleri tamamlandı. Hyper-V turu için Setup, MSI, checksum dosyaları, manifest ve kısa kontrol kartı tek ZIP içinde toplandı.

Bu çalışma ağda yayımlanmadı, Git etiketi oluşturulmadı ve Authenticode imzası uygulanmadı. Kaynak çalışma ağacı commitlenmemiş değişiklikler içerdiğinden paket yalnızca yerel/VM doğrulaması içindir. Temiz release commit'i, etiket ve public yükleme ancak Hyper-V matrisindeki cihaz testleri tamamlandıktan sonra yapılmalı.

## Aşama 8.1 — Hyper-V görsel düzeltmesi

Hyper-V incelemesinde kategori başlıklarının üç sütunlu düzene yerleştirildiği, uygulama kartlarının ise kendi dar grubunda alt alta kaldığı görüldü. Kategori artık tam satır genişliğinde; uygulama kartları bu satırda üç sütuna akıyor ve dar alanda tek sütuna dönüyor. Bugün ekranındaki süre özeti daha güçlü bir vurgu yüzeyine alındı; açık tema zemini ile kartları daha aydınlık ve kontrastlı hâle getirildi.

Bu düzeltme `4.3.1 / Alpha-4.3.1` olarak paketlendi. WPF yerleşim duman testi geniş ve dar kategorili uygulama listesini doğruluyor; Release duman testi, yayın çıktısı, MSI/Setup metadata ve release manifest denetimleri geçti.

## Aşama 8.2 — Bugün panosu ve izin penceresi

Bugün ekranındaki kullanım bölümü dijital sağlık panosu mantığıyla yeniden kuruldu: ölçülen toplam süre ana sayı, uygulama dağılımı gerçek halka grafik, en çok kullanılan üç uygulama süreleriyle birlikte görünür. Kalan süre, günlük limit ve ilerleme de aynı yüzeyde korunuyor. Mevcut durum ve sıradaki plan kendi kartında kalıyor.

Kurulum güncelleme ekranındaki İptal düğmesi doğru alt satıra ve sağ köşeye taşındı. Geçici izin penceresinin ana pencereye özel `CaptionStyle` kaynağına bağlı olması nedeniyle oluşan açılış çökmesi giderildi; pencere artık kendi etiket stilini taşıyor. Bu değişiklikler `4.3.2 / Alpha-4.3.2` VM paketi olarak hazırlandı; paket metadata, gömülü MSI ve release manifest denetimleri geçti.

## Aşama 8.4 — Bugün ekranının görsel kimliği

Bugün ekranı büyük karşılama başlığı, gerçek Kvieta K filigranı, tek kullanım özeti ve belirgin ana eylemle yeniden kuruldu. Halka ve büyük süre aynı uygulama kayıtlarını kullanıyor; oturum limiti ayrıca etiketleniyor. Saatlik grafik mevcut yerel ölçümleri 0–60 dakika ölçeğinde gösteriyor; yoğun saat yorumu gerçek veriden üretiliyor. Ölçüm bulunmayan saatlere yapay çubuk eklenmiyor. Açık/koyu temada geniş ve dar WPF önizlemeleri kontrol edildi. Diğer odak süreleri açılır bölümde korundu. Sürüm kullanıcının isteğiyle 4.3.3 kaldı; aynı sürüm kuruluysa Setup onarım olarak açılır. Kaynak değişiklikleri commit edilerek yerel paket o commit üzerinden üretilir.

## Aşama 8.3 — Uygulama içgörüleri ve sadeleştirme

Kompakt oturum sayacı ile haftalık Plan ekranı önceki daha sade görsel yapısına döndürüldü. Uygulamalar ekranında yinelenen büyük kartlar kaldırıldı; son 7 günlük gerçek ölçümden en yoğun kategori, en çok kullanılan uygulama ve düne göre en hızlı yükselen uygulama hesaplanıyor. Alt bölümde kategori toplamı seçildiğinde sağdaki kompakt uygulama listesi filtreleniyor; her satır süre, kategori, 7 günlük eğilim ve doğrudan zamanlayıcı/kural erişimi sunuyor. Değişiklikler `4.3.3 / Alpha-4.3.3` yerel Hyper-V doğrulama paketine alındı.

## Devam sırası

1. İlk ekranı gerçek WPF görünümünde incele: açık/koyu tema, TR/EN, dar/geniş pencere ve Windows ölçeklendirmesi. Uzun durum metinleri ve klavye sırası kontrol edilecek.
2. Insights, Personal ve Family için veri yok, aktif oturum, mola, limit dolması ve bekleyen değişiklik durumlarını doğrula. Guardian uyarıları dekorasyon içinde kaybolmamalı.
3. Odak akışını başlatma, özel süre, aktif oturum ve bitiş boyunca gerçek pencerede incele. Günlük kullanım için tek belirgin ana eylemi bu incelemeyle netleştir.
4. Haftalık plan ve uygulama kurallarını gerçek pencerede doğrula; onaylanan görsel kuralları ortak bileşenlere çıkar. Mevcut gezinme yollarını gereksiz yere değiştirme.
5. Geçmiş ve istatistik ekranını gerçek veriyle doğrula; veri eksikliği ile ölçülen sıfırı ayırmaya devam et.
6. İlk kurulum, boş ekranlar ve ayar gruplarını gerçek pencerede doğrula.
7. Gerçek pencerede Tab/Shift+Tab, Escape, ekran okuyucu adları ve görünür odak durumları için erişilebilirlik turu yap.
8. Alpha 4.3 paketini Hyper-V'de mevcut V1 matrisiyle doğrula: temiz kurulum/güncelleme, Guardian, uyku/yeniden başlatma, DPI, klavye ve çoklu monitör.
9. VM sonuçları temizse release değişikliklerini commit/tag ile sabitle, imza durumunu açıkça koruyarak public önizlemeyi yükle.

## Sınırlar

Yeni AI özelliği bu tasarım çalışmasının hedefi değildir. Koruma ve süre hesaplama kuralları görsel gerekçelerle değiştirilmeyecek. Her aşamanın doğrulaması ayrıca kaydedilecek; derlemenin geçmesi görsel kontrollerin geçtiği anlamına gelmez.
