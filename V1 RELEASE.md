# BIG V1 UPDATE — Kvieta ürün ve geliştirme planı

> Oluşturulma: 20 Eylül 2026  
> Durum: V1 RC uygulandı. Windows sadeleştirmesi, Android dashboard, onaylı QR eşleştirmesi, şifreli uzaktan özet, aile ek-süre kararı ve bildirim altyapısı çalışır paketlere alındı; fiziksel cihaz saha doğrulaması ve üretim imzaları bekliyor.  
> Amaç: Masaüstü deneyimini sadeleştirmek ve kişisel/aile kullanımına uygun bir mobil eşlikçi sunmak.

## 1. Bu belgenin amacı ve yeni sohbetlerde kullanımı

Bu dosya, kullanıcıyla yapılan BIG V1 UPDATE görüşmesinin kalıcı kaydıdır. Yeni bir sohbet veya geliştirme oturumu başladığında ürün niyetinin kaybolmaması için kararları, önerilen ekranları, açık soruları, iş sırasını ve kabul ölçütlerini bir arada tutar.

Bu belge bir tamamlanmış işler listesi veya yayın duyurusu değildir. Burada tarif edilen işlevler, yalnızca bu dosyada yer aldıkları için uygulanmış kabul edilmemelidir. Önce mevcut kod ve davranış doğrulanmalıdır. Sohbetteki Alpha 5 çalışmaları tarihsel başlangıç bağlamıdır; sonraki oturumlarda güncel sürüm depodan kontrol edilir.

Yeni oturumda:

1. Önce bu dosyayı ve geçerli `AGENTS.md` talimatlarını oku.
2. Aşağıdaki kesinleşmiş yönü, önerileri ve açık kararları birbirinden ayır.
3. Mevcut kodu inceleyerek hangi parçaların zaten bulunduğunu doğrula.
4. İşe ilgili aşamadan devam et; aynı özellikleri farklı adlarla tekrar ekleme.
5. Yeni ürün kararlarını ve doğrulanmış ilerlemeyi bu dosyaya işle.
6. Kullanıcının yeni talimatı önceki öneriyle çelişiyorsa yeni talimatı esas al ve karar kaydını güncelle.

Bu dosya başlangıçta plan olarak hazırlanmıştır. Kullanıcı daha sonra plana uygun geliştirmeye başlanmasını istemiştir. Dağıtım, GitHub yayını ve mobil mağaza yayını bu geliştirme talimatıyla yapılmış sayılmaz.

İlgili mevcut belgeler:

- [Önceki masaüstü arayüz çalışması](docs/V1-UI-RENEWAL.tr.md)
- [V1 test matrisi](docs/V1-TEST-MATRIX.md)
- [Yol haritası](docs/ROADMAP.md)
- [Sürüm geçmişi](docs/RELEASE_NOTES.md)

Önceki belgelerdeki tamamlanma iddiaları bu yeni kapsamın tamamlandığı anlamına gelmez. Yeni sadeleştirme ve mobil eşlikçi yönü için başlangıç referansı bu dosyadır.

## 2. Neden BIG V1 UPDATE?

Kullanıcının temel geri bildirimi: Bir menüye girildiğinde çok fazla seçenek, kart, açıklama ve kontrol aynı anda görünmektedir. Kullanıcı neye bakacağını ve hangi işlemi yapacağını seçmekte zorlanmakta; bu da uygulamayı açma isteğini azaltmaktadır.

Sorun yalnızca görsel sadelik veya renk seçimi değildir. Özellikler arttıkça günlük kullanım, ayar yapma, gelişmiş yönetim ve ayrıntılı analiz aynı ekranlarda birikmiştir. Yeni tasarım bu işlerin önceliğini yeniden kurmalıdır.

Ana hedef:

**Kullanıcı durumunu hemen anlasın, gerekli işlemi kolayca bulsun, ayrıntıya yalnızca ihtiyaç duyduğunda girsin.**

BIG V1 UPDATE iki ana ürünü birlikte ele alır:

1. Daha anlaşılır, çekici ve günlük kullanımı kolay bir Windows masaüstü uygulaması.
2. Kişinin kendi bilgisayarının durumunu veya ebeveynin çocuğunun durumunu gösteren mobil eşlikçi dashboard.

Başarı, eklenen özellik sayısıyla ölçülmez. Aynı işi yapmak için gereken kararların, aramanın ve gereksiz ekran kalabalığının azalmasıyla ölçülür.

## 3. Kesinleşen yön ve henüz açık olanlar

### 3.1. Kullanıcıyla kararlaştırılan ürün yönü

- Masaüstü arayüzü sadeleşecek; önemli bilgiler ve gerekli işlemler daha belirgin olacak.
- Mobil uygulama bir eşlikçi dashboard olacak.
- Mobil yalnızca Android için hazırlanacak. iOS geliştirmesi ve iOS uyumluluğu bu güncellemenin kapsamında değildir.
- Kvieta kullanıcı hesabı olmayacak: e-posta/parola ile kayıt, bulut hesabına giriş veya sosyal giriş akışı tasarlanmayacak. Bağlantı cihaz eşleştirmesiyle kurulacak.
- Kişisel kullanımda telefon, kişinin kendi bilgisayarındaki durumunu gösterecek.
- Aile kullanımında çocuk bilgisayardan talep gönderebilecek; ebeveyn telefondan talebi değerlendirip yanıtlayabilecek.
- Mobil uygulama, telefonun kendi uygulamalarını engelleyen veya telefon ekran süresini yöneten başka bir ürün olarak tasarlanmayacak.
- Plan, yeni sohbetlerde kullanılabilecek ayrıntılı bir dosyada tutulacak.

### 3.2. Başlangıç tasarım önerileri

Bunlar planın önerilen başlangıç noktalarıdır; prototip ve kullanıcı değerlendirmesiyle değişebilir:

- Masaüstünde dört ana bölüm: Bugün, Uygulamalar, Planım, Ayarlar.
- Önce özet, sonra ayrıntı, ihtiyaç olduğunda düzenleme.
- İlk görünümde yaklaşık üç-dört belirgin bilgi grubu ve tek baskın işlem.
- Geçmişin Bugün üzerinden açılması.
- Kişisel mobil ilk sürümün ağırlıklı olarak salt görüntüleme sunması.
- Aile mobil ilk sürümünün ana işleminin ek süre talebi olması.
- Ebeveynin ev dışındayken de taleplere yanıt verebilmesi.

Üç-dört bilgi grubu bilimsel bir mutlak sınır veya her sayfada uygulanacak öğe sayacı değildir. Aynı karta çok sayıda bağımsız kontrol koymak da sadeleştirme sayılmaz.

### 3.3. Henüz seçilmemiş teknik ve ürün kararları

21 Eylül 2026 RC kararları: Android 8/API 26 alt sınırı; Kotlin + Jetpack Compose; tek eşleşmiş telefon; hesapsız P-256 cihaz kimliği; yerelde sertifika pinli HTTPS; uzakta Cloudflare Worker/Durable Object üzerinde AES-256-GCM şifreli son özet ve karar; uygulama içi 30 saniye yenileme ve WorkManager bildirimi. Telefon plan/PIN yönetemez. Aşağıdakiler RC sonrası genişleme konularıdır:

- Birden fazla ebeveyn, çocuk ve bilgisayar için ilk sürüm sınırları.
- Üretim Android imzası, mağaza yayın planı ve anlık bildirim altyapısı.
- Mevcut telefon kurtarma bağlantısının yeni mobil bağlantıyla nasıl birleşeceği.
- Birden fazla ebeveyn/telefon için sahiplik aktarımı ve kurtarma ayrıntıları.

Bu konular uygulama başlamadan ilgili aşamada karara bağlanmalı; bu dosya bunlardan birini sessizce seçilmiş kabul etmez.

## 4. Deneyim ilkeleri

### Önce özet, sonra ayrıntı

Ekran açıldığında mevcut durum görünür. Grafikler, geçmiş karşılaştırmaları ve gelişmiş seçenekler anlamlı bir başlık veya işlem üzerinden açılır. Kullanıcı ayrıntıya girmeden temel işi yapabilmelidir.

### Kullanım ile yönetimi ayır

Her gün yapılan işlemler ile nadiren değişen kurulum/koruma kararları aynı ağırlıkta sunulmaz. Mod, Guardian, kurtarma ve ayrıntılı politika yönetimi günlük ekranı doldurmaz. Müdahale gerektiren gerçek bir problem varsa anlaşılır bir uyarı gösterilir.

### Tek baskın işlem

Bir ekranda o anki iş için en önemli düğme belirgindir. Diğer işlemler kaybolmaz ancak aynı görsel ağırlıkta yarışmaz. Her şeyi alt menülere saklamak da çözüm değildir: mola gibi sık işlemler doğrudan erişilebilir kalmalıdır.

### Gerçek durumu göster

Kullanılan süre, kalan süre, plan kotası ve ölçülen uygulama süresi birbirinin yerine kullanılmaz. Yetersiz veriyle karşılaştırma veya yorum üretilmez. Çevrimdışı veri güncelmiş gibi sunulmaz. Kaydedilmemiş seçim etkin kural gibi gösterilmez.

### Daha az zorunlu karar

Kullanıcı tüm özellikleri öğrenmeden başlayabilmelidir. Uygun varsayılanlar kullanılabilir, ancak koruma ve yetkiyle ilgili önemli sonuçlar gizlenmez. İlk açılış uzun bir ayar sınavına dönüşmemelidir.

## 5. Masaüstü bilgi mimarisi

| Bölüm | Temel amaç | Ayrıntıda kalacaklar |
| --- | --- | --- |
| Bugün | Durumunu gör, günlük işlemini yap | Geçmiş, uzun grafikler, ayrıntılı karşılaştırmalar |
| Uygulamalar | Kullanımını anla, uygulama sınırını yönet | Uygulama grafikleri ve kural seçenekleri |
| Planım | Mevcut planı anla ve gerektiğinde değiştir | Gün bazlı saat alanları, geçici izin düzenleme |
| Ayarlar | Tercihleri ve yönetimi düzenle | Koruma ayrıntıları, kurtarma, teknik tanılama |

Bu dört bölüm prototipte doğrulanacaktır. Bölüm adlarının Türkçe ve İngilizce karşılıkları tutarlı olmalıdır.

### 5.1. Bugün ekranı

İlk görünümde önerilen gruplar:

1. Ana durum: açık etiketli kullanım veya kalan süre.
2. Ana işlem: mevcut duruma uygun oturum/odak/mola işlemi.
3. Kısa kullanım özeti: en çok kullanılan birkaç uygulama.
4. Gerekliyse anlamlı bilgi: yaklaşan plan, bekleyen talep veya güvenilir karşılaştırma.

Dördüncü grup her durumda zorunlu değildir; boşluğu doldurmak için öneri üretilmez.

| Kullanıcı | Ana vurgu | Önerilen işlem |
| --- | --- | --- |
| Farkındalık | Bugünkü ölçülen kullanım | Kullanım ayrıntısını gör |
| Kişisel | Kullanım ve etkin odak/plan durumu | Odak başlat veya oturumu yönet |
| Ailede çocuk | Kalan süre ve sonraki izinli zaman | Oturumu yönet veya ek süre iste |

Ekran gereksinimleri:

- Büyük sayının neyi ifade ettiği hemen anlaşılmalı.
- Etkin odak, mola, süre dolması ve plan dışı durum için ayrı, tutarlı görünüm olmalı.
- Hiç veri yok, ölçüm eksik ve gerçekten sıfır kullanım durumları ayrılmalı.
- Ritim ve benzeri yardımcı bilgiler ana işi bastırmamalı; gerekirse ayrıntıya taşınmalı.
- Geçmişe erişim bulunabilir olmalı; dönem seçimi ve uzun grafikler orada gösterilmeli.
- Gerçek koruma/ölçüm arızası sıradan bir ipucu gibi sunulmamalı.

### 5.2. Uygulamalar ekranı

İlk görünüm basit dönem seçimi, kullanım listesi, kategoriye geçiş ve gerektiğinde arama içerir. Bir satırın temel bilgileri simge, uygulama adı, süre ve varsa kısa sınır bilgisidir.

Uygulamaya basıldığında:

- Seçili dönemin kullanım özeti.
- Anlamlı veri varsa kullanım eğilimi.
- Mevcut kural ve kısa açıklaması.
- Belirgin bir “Sınırı düzenle” işlemi.

Sınır düzenleme, uygulama ayrıntısının içinde ayrı bir adım olur. Kullanıcı programı seçmeden önce bütün kısıtlama türleriyle karşılaşmaz. Var olan sınırlama türlerinin davranışı yalnızca görsel sadeleştirme nedeniyle değiştirilmez.

Kategoriye basıldığında o kategorideki uygulamalar gösterilir; geri dönüş kullanıcının dönemini ve konumunu korur. Uzun isimler, simgesi bulunamayan uygulamalar ve büyük listeler tasarımda ele alınır.

Mevcut büyük özet kartlarının her biri yeniden gerekçelendirilir. “Son yükseliş” gibi bilgi yeterli karşılaştırma verisi yoksa gösterilmez; her zaman büyük kart olarak tutulması gerekmez. Grafiklerin amacı aynı sürenin birkaç yerde tekrar edilmesi değildir.

### 5.3. Planım ekranı

Normal görünüm form yerine okunabilir plan özeti sunar. Örnek:

> Hafta içi · 09.00–21.00 · Günlük 3 saat  
> Hafta sonu · 10.00–23.00 · Günlük 4 saat

Bu sadece örnektir. Farklı günlere özel planları olan kullanıcı zorla iki gruba indirgenmez; özet gerçek kuralı yansıtır.

“Planı düzenle” ile düzenleme kontrolleri açılır:

- Gün veya gün grubu seçimi.
- İzin verilen saat aralığı.
- Günlük süre.
- Kaydetmeden önce etkisinin anlaşılması.

Kaydetme/iptal ve değişikliğin uygulanma durumu açık olmalıdır. Yetkilendirme veya gecikmeli değişiklik gerekiyorsa mevcut koruma korunur. Kullanıcı ekranda görünen taslakla etkin planı ayırt edebilmelidir.

Geçici izin bölümü:

- Etkin/yaklaşan izinler kısa bir özetle görünür.
- Yeni izin kısa bir akışla oluşturulur.
- Tarih, süre ve saat aralığının etkisi onay öncesi gösterilir.
- Süresi geçmiş izinler etkinmiş gibi sunulmaz.
- Kalıcı plan değişikliğiyle geçici izin karışmaz.

### 5.4. Ayarlar

Önerilen gruplar:

- Görünüm ve dil.
- Kullanım biçimi ve koruma.
- Bağlı cihazlar ve telefon.
- Veriler, kurtarma ve uygulama bilgileri.

Tanılama ve teknik ayrıntılar ayrı bir ayrıntı alanında bulunur. Arayüz teknik terimi göstermek zorundaysa kullanıcının kararına etkisini kısa ve anlaşılır biçimde açıklar.

Mod değiştirme, güvenlik sınırını gevşetme ve koruma kaldırma gibi işlemler sıradan görsel tercih değişikliği gibi ele alınmaz. Arayüzün sadeleşmesi yetkilendirmeyi kaldırmaz.

## 6. Görsel tasarım ve erişilebilirlik

Yön: Sitenin beğenilen güçlü başlıkları, sıcak yüzeyleri, dengeli boşlukları ve belirgin işlemleri masaüstüne uyarlanır. Masaüstü geniş bir telefon ekranına dönüştürülmez.

- Liste olan içerik liste olarak kalır; her bilgi için yeni kart üretilmez.
- Düğmeler, metin girişleri, açılır pencereler ve hizalamalar ortak kurallara bağlanır.
- Ana işlem, ikincil işlem ve tehlikeli işlem birbirinden anlaşılır biçimde ayrılır.
- Açık/koyu temada okunabilir kontrast sağlanır.
- Durumlar yalnızca renk kullanılarak anlatılmaz.
- Metin büyütme, ekran ölçeklendirmesi ve dar pencere içerikleri kesmemelidir.
- Klavye odağı görünür olur; diyalog açma/kapatma sonrası odak doğru yere döner.
- Ekran okuyucu etiketleri ve bildirimleri temel işlemleri anlaşılır kılar.
- Hareketler kısa ve işlevsel olur; azaltılmış hareket tercihi gözetilir.
- Türkçe ve İngilizce uzun metinler gerçek yerleşimde denenir.

Önceki saha geri bildirimleri özellikle korunacak: kesilen dakika alanları, uygunsuz büyüyen İptal düğmeleri, hizasız saat alanları, ağır hover efektleri ve gereksiz büyük uygulama kartları tekrarlanmamalıdır.

## 7. Mobil eşlikçi: kapsam ve ekranlar

### 7.1. Ürün sınırı

Mobil uygulama bilgisayardaki Kvieta'nın eşlikçisidir. V1'de telefonun kendi kullanımını izleme veya telefon uygulamalarını sınırlama hedeflenmez. Masaüstünün bütün ayarlarını mobilde yeniden üretmek de hedef değildir.

Mobil hedef yalnızca Android'dir. Kullanıcı Kvieta hesabı açmaz; başlangıç ekranı kayıt/giriş yerine “Bilgisayar bağla” işlemine yönlendirir. Android dağıtımının mağaza veya APK üzerinden yapılması ayrıca kararlaştırılacaktır.

### 7.2. Kişisel dashboard

Kişi telefondan kendi bilgisayarının durumunu görür:

- Cihaz adı, bağlantı durumu, son güncelleme zamanı.
- Bugünkü kullanım.
- Varsa kalan süre, etkin odak veya oturum durumu.
- En çok kullanılan uygulamaların kısa özeti.
- Özetten açılan gün/hafta ayrıntısı.

İlk öneri ağırlıklı olarak görüntülemedir. Telefondan plan değiştirme, odak başlatma veya başka uzaktan işlemler ayrıca kararlaştırılmadan eklenmez.

Birden fazla cihaz desteklenirse seçili cihaz açıkça görünür. Farklı bilgisayarların eşzamanlı kullanımını toplamak yanıltıcı olabilir; ilk sürümde cihazlar sessizce birleştirilmez. Birleşik görünüm istenirse ölçüm tanımı ayrıca yapılır.

### 7.3. Ebeveyn dashboard'u

Birden fazla çocuk/cihaz varsa kısa durum listesi; tek eşleşme varsa doğrudan ayrıntı önerilir. Ebeveynin gördüğü temel alanlar:

1. Çocuk/cihaz ve bağlantı bilgisi.
2. Bugünkü kullanılan/kalan süre.
3. Bekleyen talep varsa talep kartı.

Kullanım ayrıntısı özete dokununca açılır. Bekleyen talep en belirgin işlem olur. Talep yokken sırf üçüncü alanı doldurmak için gereksiz uyarılar gösterilmez.

Kişisel dashboard'a aile işlemleri eklenmez. Ebeveynin farklı çocuklara veya cihazlara ait bilgileri karıştırmaması için bağlam işlem ekranında da görünür.

### 7.4. Çocuğun masaüstü deneyimi

Çocuğun ek süre talebi masaüstündeki uygun günlük/oturum yüzeyinden başlar. Talep göndermek koruma ekranını geçmek veya yönetim paneli açmak için yol oluşturmamalıdır.

Çocuk istediği süreyi seçer, isteğe bağlı kısa bir not yazar ve gönderir. Bekleyen talebin durumu görünür. Aynı istek tekrar tekrar gönderilmez. Talep sonucu açık ve sakin bir dille gösterilir.

## 8. Ek süre talebi: uçtan uca davranış

### 8.1. Normal akış

1. Çocuk “Ek süre iste” işlemini açar.
2. Süreyi ve isteğe bağlı notu belirler.
3. Talep gönderilir ve ebeveynin telefonuna bildirim gider.
4. Ebeveyn talebi, ilgili çocuğu, bugünkü kullanımı ve güncel bağlamı görür.
5. “Onayla”, “Farklı süre ver” veya “Reddet” işlemini seçer.
6. Karar yetkili bağlantı üzerinden ilgili bilgisayara ulaştırılır.
7. Bilgisayar geçerliliği denetler ve izin değişikliğini uygular.
8. Uygulama sonucu ebeveyne ve çocuğa gösterilir.

### 8.2. Durumların anlamı

| Durum | Kullanıcıya anlatılacak gerçek |
| --- | --- |
| Gönderiliyor | Talebin alınması henüz doğrulanmadı |
| Yanıt bekleniyor | Talep alındı; ebeveyn kararı yok |
| Reddedildi | Yetkili ebeveyn talebi reddetti |
| Onaylandı / uygulama bekleniyor | Ebeveyn karar verdi; bilgisayar henüz uyguladığını doğrulamadı |
| Uygulandı | Bilgisayar geçerli izni kaydedip uyguladığını doğruladı |
| Süresi doldu | Talep veya karar artık uygulanamaz |
| Başarısız / tekrar bağlantı bekleniyor | İşlem tamamlanmadı; nedeni ve sonraki durum anlaşılır olmalı |

Kesin durum isimleri tasarımda sadeleştirilebilir; farklı gerçekler tek bir yanıltıcı “Başarılı” etiketi altında birleşmemelidir.

### 8.3. Kenar durumlar ve kurallar

- Aynı karar iki kez ulaşsa bile ek süre bir kez uygulanır.
- Gönderim zaman aşımı nedeniyle tekrar denendiğinde mükerrer talep oluşmaz.
- Talebin bir geçerlilik süresi bulunur; kesin süre ürün kararıdır.
- Gün değişimi, saat dilimi ve bilgisayar saatinin değişmesi eski izni yanlış güne taşımamalıdır.
- Plan dışı zamanda dakika eklemenin ne anlama geldiği açık olmalıdır. Günlük kotayı artırmak ve saat aralığına izin vermek aynı işlem değildir.
- Talep sırasında plan değişmişse onay güncel kurala göre yeniden denetlenir.
- Telefon veya bilgisayar çevrimdışıysa eski durum güncel gibi gösterilmez.
- Bildirimin gelmesi işlemin tamamlandığının kanıtı değildir; uygulama içindeki kayıt esas alınır.
- Yetkisi kaldırılan telefon bekleyen veya yeni bir kararı uygulatamaz.
- Birden fazla ebeveyn desteklenirse çelişen kararların nasıl çözüleceği ayrıca tanımlanır.
- Talep notları ve süre girişleri sınırlandırılır; hata halinde anlaşılır geri bildirim verilir.
- Talep geçmişi için gerekli en küçük denetim kaydı ve saklama süresi belirlenir.

## 9. Eşleştirme, sahiplik ve erişim

Önerilen eşleştirme akışı:

1. Bilgisayarda yetkili kişi “Telefon bağla”yı açar.
2. Telefonda QR okutulur.
3. İlgili cihaz ve verilecek izinler iki tarafta anlaşılır biçimde gösterilir.
4. Yetkili kişi bilgisayarda onaylar.
5. Bağlantı hem telefonda hem bilgisayarda listelenir.

QR/eşleştirme isteği tek kullanımlık ve süreli olmalıdır. QR'ı görebilmek tek başına kalıcı yönetici yetkisi vermemelidir.

İzin ayrımı:

- Kişisel erişim: sahibinin belirlenmiş cihaz özetini görmesi.
- Ebeveyn erişimi: yetkilendirildiği çocuk/cihazı görmesi ve izin verilen talepleri yanıtlaması.
- Çocuk: kendi durumunu görmesi ve talep göndermesi; ebeveyn yetkisi oluşturamaması.

Bağlı cihazları adlandırma, erişim iptali, kayıp telefon, telefon değişimi ve yeniden kurulum ele alınır. Sunucu veya bildirim hizmetine sahip olmak tek başına koruma kararlarını yetkilendirmemelidir; teknik güven modeli açıkça tasarlanır.

Mevcut kurtarma telefon bağlantısı incelenir. Yeni dashboard yetkileri eski bağlantıya habersiz eklenmez. Kullanıcıya iki belirsiz telefon kaydı sunmamak için ortak bir bağlantı deneyimi değerlendirilebilir.

### 9.1. Hesapsız bağlantı için önerilen teknik model

Hesap olmaması, kimlik ve yetki denetimi olmaması anlamına gelmez. Kimlik, kişinin e-posta adresi yerine eşleştirilmiş cihazların anahtarlarıyla kurulabilir. Aşağıdaki model mimari çalışma için öneridir; protokol ve teknoloji henüz uygulanmış veya seçilmiş değildir.

1. Bilgisayar ve Android uygulaması cihazlarına özgü anahtarlar oluşturur. Özel anahtarlar uygun işletim sistemi korumasında tutulur; sunucuya gönderilmez.
2. Bilgisayar kısa ömürlü, tek kullanımlık bir eşleştirme daveti gösterir. QR kalıcı parola veya yönetici sırrı içermez.
3. Telefon QR'ı okutur; iki cihazın anahtarları ve istenen yetki güvenli biçimde eşleştirme işlemine bağlanır. Gerekirse iki ekranda karşılaştırılan kısa bir doğrulama kodu gösterilir.
4. Bilgisayardaki yetkili kişi telefonun adını ve erişimini onaylar. Aile bağlantısı mevcut yönetici doğrulamasını gerektirir; çocuk kendi telefonuna ebeveyn yetkisi veremez.
5. Sonraki bağlantılar ve kararlar cihaz kimliğine, hedef cihaza, geçerlilik süresine ve ilgili talebe bağlı olarak doğrulanır. Rastgele bir bağlantı kodunu bilmek kalıcı erişim için yeterli değildir.
6. Kullanıcı sonraki açılışlarda giriş yapmadan eşleştirilmiş dashboard'u görür. Korunan işlemler için telefonun yerel kilit/biyometri doğrulamasının kullanımı ayrıca değerlendirilir.

Ev dışından erişim için cihazlar arasında iletişimi taşıyan bir hizmet yine gereklidir. Bu hizmet kullanıcı hesabı açmadan cihaz eşleşmelerini, yönlendirmeyi ve Android bildirim hedeflerini yönetebilir. Hesapsız olmak sunucusuz veya hiçbir bağlantı verisi tutulmayan bir sistem demek değildir. İçerik gizliliği, sunucunun görebildiği metadata, kötüye kullanım sınırları ve anahtar doğrulaması teknik tasarımda açıkça belirtilir.

### 9.2. Telefon değişimi ve erişim kurtarma

- Yeni telefonda eski eşleşmeler yalnızca uygulamayı yüklemekle geri gelmez.
- Önerilen ilk sürüm yolu: yetkili kişi bilgisayardan eski telefonun erişimini kaldırır ve yeni telefonu tekrar eşleştirir.
- Kaybolan telefona erişim olmadan bilgisayardaki yetkili yönetim akışından iptal yapılabilmelidir.
- Kvieta hesabı olmadığı için e-postayla “şifremi unuttum” veya hesaba girişle otomatik geri yükleme vaat edilmez.
- Bilgisayara yetkili erişimin de kaybedildiği durumda mevcut kurtarma mekanizmaları değerlendirilir; bağlantı hizmeti kendiliğinden yönetici yetkisi vermez.
- Eşleşme anahtarlarının Android yedeklerinden başka cihaza kontrolsüz taşınması önlenir; yeniden kurulum ve cihaz yedeği davranışı test edilir.
- Ayrı bir taşınabilir kurtarma yöntemi gerekirse sonraki bir karar olarak tasarlanır; ilk sürüme sessizce bulut hesabı eklenmez.

## 10. İnternet erişimi, çevrimdışı çalışma ve gizlilik

Hedef kullanımda ebeveyn farklı ağdayken de talep alabilir. Mevcut yerel ağ bağlantısının bunu zaten sağladığı varsayılmaz. Uzak erişim ayrı tasarım ve doğrulama gerektirir.

Temel koşullar:

- Masaüstü mobil bağlantı olmadan kullanılabilir.
- Mobil bağlantı isteğe bağlıdır.
- İnternet yokken mevcut masaüstü planları ve koruma çalışmaya devam eder.
- Telefon, son bilinen veri ile canlı veriyi ayırt eder.
- Son güncelleme zamanı görünür; bağlantı durumunda belirsizlik saklanmaz.
- Ayrıntılı yerel kullanım geçmişinin tamamı varsayılan olarak buluta taşınmaz.
- Aktarılacak veri alanları tek tek belirlenir: cihaz durumu, kullanım özeti, gerekli uygulama özeti, talep ve karar gibi.
- Veri aktarımı ve saklama koruması; anahtar üretimi, yenileme, kurtarma ve iptal davranışıyla birlikte tasarlanır.
- Bağlantı kaldırıldığında sunucu, telefon ve bilgisayardaki ilgili verilerin yaşam döngüsü tanımlanır.
- Çocuklara ilişkin veriler ve uygulama kullanım ayrıntıları gereksiz log/analitik kayıtlarına yazılmaz.
- Destek inbox'u ve web yönetimiyle mobil kullanıcı verisinin sorumluluk ve erişimleri karıştırılmaz.

Mobil eşlikçiyle birlikte ürünün gizlilik açıklaması güncellenmelidir. “Her şey yalnızca cihazda kalır” gibi bir ifade, kullanıcı mobil aktarımı etkinleştirdiğinde gerçekleşen veri akışını saklamamalıdır.

## 11. Bildirimler

İlk önerilen kapsam:

- Ebeveyne yeni ek süre talebi.
- İşlem için gerçekten gerekli bağlantı veya erişim uyarısı.
- Daha sonra kararlaştırılırsa kullanıcının açtığı kısa günlük özet.

Her uygulama açılışı, küçük kullanım değişimi veya mola için bildirim gönderilmez. Günlük özet V1'in zorunlu çekirdek parçası değildir.

Kilit ekranı metni hassas kullanım ayrıntılarını varsayılan olarak göstermemelidir. Bildirime dokununca doğru çocuk, cihaz ve talep açılmalıdır. Eski bildirim güncel olmayan bir işlemi doğrudan uygulamamalıdır. Bildirim izni verilmezse uygulama içinden taleplere erişim çalışmalıdır.

## 12. Mevcut özellikleri sadeleştirme yöntemi

Her ekran ve özellik için şu karar kaydedilir:

| Karar | Gerekçe |
| --- | --- |
| Öne çıkar | Günlük kullanımda gerekli; doğrudan değer sağlıyor |
| Ayrıntıya taşı | Faydalı fakat sürekli görünmesi gerekmiyor |
| Birleştir | Başka bir yerde aynı iş veya bilgi tekrar ediliyor |
| Kaldırmayı değerlendir | Belirgin değer sağlamıyor veya gereksiz karmaşa oluşturuyor |

İncelenecek örnekler: yinelenen kullanım özetleri, büyük uygulama kartları, sürekli açık kural seçenekleri, ayrı ritim alanları, uzun açıklamalar, günlük ekrandaki teknik sağlık bilgileri ve farklı yerlerde aynı işlemi yapan düğmeler.

İşlev kaldırmak ile görünürlük azaltmak ayrı kararlardır. Mevcut veriyi, korumayı veya kullanıcının alıştığı bir yeteneği kaldıracak değişiklik açıkça değerlendirilir. “Daha sade göründü” gerekçesiyle veri veya güvenlik davranışı kaybedilmez.

## 13. V1 dışında tutulacaklar

- Telefonun kendi ekran süresini ölçmek ve telefon uygulamalarını engellemek.
- iOS uygulaması ve iOS'a yönelik uyumluluk çalışmaları.
- Kvieta kullanıcı hesabı, e-posta/parola kaydı ve sosyal giriş sistemi.
- Masaüstünün tüm gelişmiş ayarlarını mobil uygulamaya kopyalamak.
- Tam uzaktan masaüstü, dosya erişimi veya rastgele komut çalıştırma.
- Ayrıntılı kullanım geçmişinin zorunlu bulut senkronizasyonu.
- Sırf ekran doldurmak için yeni kartlar, puanlar ve grafikler.
- Yeni bir sosyal sistem, yarışma veya zorunlu abonelik sistemi.
- Gerçek ihtiyaç doğrulanmadan eklenen kişisel uzaktan kontrol işlemleri.

Bu başlıklar ancak açık bir ürün kararıyla sonraki kapsama alınır. V1'in bitmesini sürekli erteleyen yan projelere dönüşmemelidir.

## 14. Geliştirme aşamaları ve teslimatlar

### Aşama 1 — Mevcut deneyim envanteri

- Ekranları, kullanıcı rollerini ve mevcut işlemleri çıkar.
- Yinelenen bilgi/işlemleri ve sık yapılan işlerdeki gereksiz adımları işaretle.
- Her özellik için koru, taşı, birleştir veya kaldırmayı değerlendir kararı öner.
- Mevcut telefon bağlantısı ve koruma akışını koddan doğrula.

Teslimat: Ekran/özellik haritası, sadeleştirme tablosu ve kısa kullanıcı senaryoları.

### Aşama 2 — Tıklanabilir tasarım ve ürün kararları

İlk tasarım paketi:

1. Masaüstü Bugün.
2. Masaüstü Planım: özet ve düzenleme.
3. Kişisel mobil dashboard.
4. Ebeveyn mobil talep ekranı.

İkinci paket: Uygulama listesi/ayrıntısı, geçmiş, ayarlar, eşleştirme ve çocuk talep ekranı.

Prototipte boş veri, uzun isim, çevrimdışı cihaz, reddedilmiş talep, süre dolması, dar ekran ve iki dil örnekleri bulunur. Kullanıcı temel işi açıklama almadan bulabiliyor mu gözlemlenir.

Teslimat: Değerlendirilmiş ekran akışları ve netleşmiş tasarım kuralları. Arayüz yönü doğrulanmadan tüm uygulama yeniden yazılmaz.

### Aşama 3 — Masaüstü sadeleştirmesi

- Ortak görsel kuralları ve yeni gezinmeyi uygula.
- Bugün, Uygulamalar, Planım ve Ayarlar akışlarını sırayla dönüştür.
- Mevcut veriler, kurallar ve koruma davranışıyla uyumluluğu koru.
- Geçmiş/ayrıntı ekranlarında seçim ve geri dönüş davranışlarını doğrula.

Teslimat: Mobil bağlantı olmadan da kullanılabilir sade masaüstü sürümü.

### Aşama 4 — Mobil bağlantı ve güven modeli

- Android teknik yaklaşımı, hesapsız cihaz sahipliği ve iletişim mimarisi kararlarını kaydet.
- Eşleştirme, izinler, erişim iptali ve veri yaşam döngüsünü uygula.
- Önce salt görüntülenen cihaz durumu ve özet aktarımını doğrula.
- Çevrimdışı ve eski veri davranışını test et.

Teslimat: Yetkili telefonda doğru cihazın durumunu gösteren bağlantı temeli.

### Aşama 5 — Mobil dashboard ve aile talepleri

- Kişisel ve ebeveyn görünümlerini tamamla.
- Çocuk talebi, ebeveyn kararı ve bilgisayarda uygulama zincirini kur.
- Bildirimleri, mükerrer işlem önlemlerini ve talep geçerliliğini doğrula.
- Yetki iptali, yeniden bağlanma ve gün değişimi senaryolarını tamamla.

Teslimat: Gerçek telefon ve bilgisayarda, farklı ağlar arasında çalışan uçtan uca deneyim.

### Aşama 6 — Saha testi ve V1 yayın adayı

- Gerçek Windows cihazları ve desteklenen telefonlarda temel akışları dene.
- Alpha'dan güncelleme, onarım, yeniden başlatma ve çevrimdışı davranışı doğrula.
- Gizlilik metinlerini, rehberi, web içeriğini ve sürüm notlarını son davranışla eşleştir.
- Paketleri kaynak commit'i ve doğrulama sonuçlarıyla ilişkilendir.
- Kritik açıklar kapandıktan sonra yayın adayı hazırla.

Mobilin hazır olmaması halinde tamamlanmamış işlev V1 özelliği olarak duyurulmaz. Gerekirse aşamalar ayrı test sürümleriyle dağıtılır; kesin sürüm adları ayrıca kararlaştırılır.

## 15. Kabul ölçütleri

### Kullanılabilirlik

- Yeni kullanıcı ana ekrandaki büyük sayının anlamını ve temel işlemi açıklama almadan bulabiliyor.
- Planını ve bir uygulamanın sınırını düzenleyebiliyor; kaydedildiğini anlayabiliyor.
- Ayrıntıya gidip geri döndüğünde seçtiği cihaz, dönem veya kategori kaybolmuyor.
- Ana ekranda sıradaki işlemi seçmek için gereksiz seçenekleri okumak zorunda kalmıyor.
- İlk bakışta anlaşılabilirlik için yaklaşık beş saniyelik gözlem kullanılabilir; bu resmi bilimsel eşik değildir.

### Masaüstü güvenilirliği

- Önceki Alpha ayarları ve geçmişi korunuyor.
- Kullanılan süre, kalan süre ve kurallar doğru kaynaklardan gösteriliyor.
- Aile başlangıcında gizli PIN/oturum penceresi çakışması tekrarlanmıyor.
- Uyku, yeniden başlatma ve gün değişimi sonrasında görünüm gerçek durumu yansıtıyor.
- Mobil/internet kesintisi mevcut korumayı devre dışı bırakmıyor.

### Mobil ve aile akışı

- Çocuk talep gönderip sonucunu anlayabiliyor.
- Ebeveyn farklı ağdan ilgili talebi yanıtlayabiliyor.
- Onaylanan ile bilgisayarda uygulanan durum ayrılıyor.
- Mükerrer/gecikmiş mesaj ikinci bir izin oluşturmuyor.
- Süresi geçmiş veya yanlış cihaza ait karar reddediliyor.
- Erişimi kaldırılan telefon yeni işlem yapamıyor.
- Bildirim gelmese de uygulama içindeki talep listesi doğru çalışıyor.
- Çevrimdışı veya eski veri açıkça belirtiliyor.
- Android ilk kurulumunda Kvieta hesabı açmadan bilgisayar eşleştirilebiliyor.
- Yeni telefon, eski telefonun hesabına giriş gibi bir varsayım olmadan yetkili yeniden eşleştirmeyle bağlanabiliyor.
- QR daveti tekrar kullanılamıyor; süresi dolmuş davet ve onaysız ebeveyn eşleştirmesi reddediliyor.

### Görsel ve erişilebilir kullanım

- Küçük pencere, desteklenen ekran ölçekleri ve iki tema temel işlemleri bozmuyor.
- Türkçe/İngilizce metinler kontrol ve düğmelerden taşmıyor.
- Klavye ile temel akışlar tamamlanıyor; odak görünür kalıyor.
- Durum anlamı yalnızca renge bağlı değil.
- Gerçek telefon boyutlarında ana işlemler rahatça bulunuyor ve kullanılabiliyor.

Otomatik testler kullanıcı gözleminin, gerçek cihaz denemeleri de yetki/tekrar işlem testlerinin yerine geçmez. Kabul kanıtı hangi yöntemle elde edildiyse açıkça kaydedilir.

## 16. Açık karar listesi

| Konu | Karar verilmeden önce gereken |
| --- | --- |
| Android destek kapsamı | Minimum sürüm, test cihazları ve APK/mağaza dağıtım hedefi; iOS kapsam dışı |
| Mobil teknoloji | Ekran akışları, bildirim/eşleştirme ihtiyaçları ve bakım yükü |
| Hesapsız eşleştirme | Cihaz anahtarları, yetkili onay, yeni telefon, iptal ve kurtarma senaryosu |
| Uzak iletişim | Veri miktarı, gecikme, maliyet, güven ve çevrimdışı gereksinimleri |
| Şifreleme/anahtarlar | Hangi tarafın hangi veriyi okuyacağı ve kurtarma modeli |
| Çoklu ebeveyn | Çelişen kararlar, sahiplik ve yetki devri |
| Çoklu cihaz | Cihaz seçimi, veri anlamı ve ilk sürüm sınırları |
| Ek süre anlamı | Günlük kota, plan aralığı ve uygulama kuralı ilişkisi |
| Talep geçerliliği | Gün değişimi, bekleme süresi ve yeniden deneme davranışı |
| Kişisel uzaktan işlem | Salt görüntülemeye ek gerçek kullanıcı ihtiyacı |
| Saklama süreleri | Talep, özet, denetim ve bağlantı verilerinin yaşam döngüsü |
| Yayın takvimi | Prototip onayı, teknik çalışma ve saha testi sonuçları |

Kesin takvim, teknoloji ve maliyet bu aşamada verilmez; kararlar ölçülmüş gereksinimlerden sonra oluşturulur.

## 17. İlerleme kaydı

- [x] Ürün görüşmesi kalıcı plana dönüştürüldü.
- [ ] Mevcut ekran/özellik envanteri doğrulandı.
- [ ] Sadeleştirme karar tablosu hazırlandı.
- [ ] İlk dört ekran prototipi değerlendirildi.
- [ ] Açık teknik kararlar gerekçeleriyle kaydedildi.
- [ ] Masaüstü sadeleştirmesi uygulandı ve doğrulandı.
- [ ] Mobil eşleştirme ve salt görüntüleme doğrulandı.
- [ ] Ek süre talebi uçtan uca doğrulandı.
- [ ] Gerçek cihaz ve güncelleme matrisi tamamlandı.
- [ ] Gizlilik, rehber ve sürüm metinleri güncellendi.
- [ ] V1 yayın adayı hazırlandı.

Her aşama tamamlandığında tarih, ilgili commit/dosya, doğrulama sonucu ve kalan sorunlar buraya eklenmelidir. Yalnızca kod yazılmış olması bir aşamayı tamamlanmış saymak için yeterli değildir.

### İlk uygulama adımı — 20 Eylül 2026

- İncelenen alanlar: `MainWindow.xaml`, `TodayDashboard.xaml`, mevcut gün planı bağları ve Bugün ana işlem akışı.
- Bulgular: Plan sayfasında yedi günün düzenleme alanları sürekli görünüyordu; Bugün'de saatlik grafik varsayılan açık ayrı bir büyük bölümdü.
- Uygulandı: Plan sayfası gün bazlı okunabilir özetle açılıyor. Saat/limit kontrolleri “Planı düzenle” altında. Özet, taslak seçimleri de gösterdiğini ve üstteki Kaydet işleminin gerekli olduğunu açıklıyor. Günler henüz gruplandırılmadı.
- Uygulandı: Bugün'de saatlik grafik kapalı başlayan “Günün akışı” ayrıntısına taşındı; geçmişe doğrudan düğme eklendi.
- Mevcut kaydetme, yetkilendirme, süre hesabı ve plan uygulama yolları korunuyor.
- Doğrulama: Release derlemesi sıfır hata/uyarı ile geçti; çekirdek/WPF smoke kontrolleri geçti. Bugün önizlemeleri 980 ve 380 piksel genişliklerde, açık/koyu temada üretildi; geniş açık ve dar koyu örnekleri görsel olarak incelendi. Plan ekranının gerçek pencere etkileşim testi henüz yapılmadı.
- Bu ilk adım tüm envanterin, dört ekran prototipinin veya masaüstü dönüşümünün tamamlandığı anlamına gelmez.
- Sıradaki iş: Plan özet/düzenleme akışını gerçek pencere boyutlarında değerlendirmek; ardından dört ana bölüm ve rol odaklı Bugün hiyerarşisini tamamlamak. Android tasarımları ve eşleştirme altyapısı henüz uygulanmadı.

### İkinci uygulama adımı — dört ana bölüm

- Ana menü Bugün, Uygulamalar, Planım, Ayarlar olarak düzenlendi. Planı olmayan kullanım biçimlerinde Planım gizli kalır.
- Geçmiş artık ana menüde ayrı bölüm değildir; Bugün içindeki düğmeden açılır. Geçmişte Bugün bölümü seçili kalır ve “Bugün'e dön” düğmesi bulunur.
- Görsel menü sırası mevcut sayfa kimliklerinden ayrıldı; rehberin, uygulama işlemlerinin ve plan bağlantılarının hedefleri korunuyor.
- Sayfa kimliklerini değiştirmek veya geçmiş görünümünü yeniden oluşturmak yerine mevcut görünüm korunuyor; geçmiş veri seçiminin sıfırlanması gerekmiyor.
- Test kapsamı: dört menü öğesi, uygulamalar/plan/ayarlar eşleşmeleri, geçmişin Bugün'e bağlılığı, boş seçim ve geri dönüş.
- Doğrulama sonucu: Release derlemesi sıfır hata/uyarı ile tamamlandı; eklenen gezinme kontrolleri dahil çekirdek/WPF smoke testleri geçti. Gerçek pencere üzerinde klavye ve rehber turu saha kontrolü henüz yapılmadı.
- Sonraki adım: Bugün'de rol odaklı ana süre ve işlem hiyerarşisi; uygulamalar ayrıntı akışının sadeleştirilmesi. Android çalışması henüz başlamadı.

### Üçüncü uygulama adımı — rol odaklı özet ve kapalı ayrıntılar

- İş sırası kesinleşti: önce masaüstü deneyimi tamamlanacak, ardından Android eşlikçiye geçilecek. Mobil geliştirme henüz başlamadı.
- Bugün: Aile modunda ana metrik kalan süre; ölçülen uygulama kullanımı ikincil bilgidir. Kalan süreyi yanlış anlatmaması için kullanım dağılım halkası bu modda gösterilmez. Kişisel ve İçgörü modlarında ana metrik ölçülen uygulama kullanımıdır.
- İçgörü modunda ana işlem zaten günlük geçmişi açtığı için ikinci geçmiş düğmesi gösterilmez. Diğer modlarda geçmişe doğrudan erişim korunur.
- Uygulamalar: üç büyük içgörü kartı kapalı başlayan “Kullanım içgörüleri” altında toplandı. Kategoriler ve en çok kullanılan uygulamalara erişim ana görünümde kaldı. Kural adı ve durum özeti görünür; kural türü, dakika ve kaldırma alanları “Sınırı düzenle” ile açılır. Mevcut zamanlayıcı ve kaydetme akışları korunur.
- Ayarlar: dört bölüm kapalı başlar; birini açmak diğerini kapatır. Açık alandaki taslaklar kapanınca silinmez. Rehber metinleri yeni erişim yollarına göre güncellendi; rehber sonunda önceki açık ayar bölümü geri yüklenir.
- Doğrulama: Release derlemesi sıfır hata/uyarı ile geçti; çekirdek/WPF smoke testleri başarılı. Kişisel/İçgörü/Aile metrikleri, ayar bölümlerinin ilk durumu ve tek açık bölüm davranışı test edildi. Üç modda açık/koyu temalı 380 ve 980 piksel Bugün önizlemeleri üretildi. Dar Aile/açık ve geniş Kişisel/koyu örnekleri görsel olarak incelendi.
- Önizlemeler izole örnek veridir; cihazdaki gerçek Guardian/oturum durumunu doğrulamaz. Gerçek pencereyle klavye gezinmesi, rehber, plan ve kural düzenleme, farklı Windows ölçekleri ve yeniden açılış kontrolü hâlâ gerekir. Masaüstü kabul maddeleri bu nedenle tamamlandı olarak işaretlenmedi.
- Sonraki masaüstü adımı: bu etkileşim kontrollerini tamamlamak, kalan yoğun alanları kullanıcı değerlendirmesiyle ele almak ve ancak masaüstü kabulünden sonra Android aşamasına geçmek. Bu adımda sürüm numarası, kurulum paketi veya uzak yayın oluşturulmadı.

### Dördüncü uygulama adımı — Android ekran temeli

- Kullanıcı masaüstünün kalan elle testlerini daha sonraya erteleyip mobil aşamaya geçilmesini istedi. Önceki masaüstü kabul sırası bu talimatla değişti; ertelenen testler tamamlanmış sayılmaz.
- `companion-android` altında Kotlin/Jetpack Compose Android projesi oluşturuldu. Yalnız Android; hesap veya iOS katmanı eklenmedi. Başlangıç alt sürüm API 26; dağıtım/destek kapsamı saha denemesiyle kesinleşecek.
- Hesapsız karşılama, bağlantının nasıl işleyeceğine ilişkin açıklama, kişisel/ebeveyn demo dashboard'ları, açılır uygulama ayrıntısı ve çevrimdışı örneği eklendi.
- Demo seçimi açık ve isteğe bağlı; örnek veri canlı bağlantı gibi gösterilmez. Aile onayı sadece örnek “bilgisayar bekleniyor” durumuna geçer; gerçek süre eklenmez veya uygulandı denmez. Ekran rol seçimi gelecekteki yetkilendirme yerine kullanılmayacak.
- Ağ/kamera izinleri, QR tarama, gerçek eşleştirme, anahtar saklama, internet rölesi ve gerçek talep akışı bu adımda uygulanmadı. Önceki web eşlikçi ve masaüstü koruma kodu değişmedi.
- Gradle wrapper ve bağımlılık sürümleri sabitlendi. Ayrı araç klasörüyle ilk debug APK üretildi; Kotlin unit testleri ve lint başarılı. ADB'de bağlı cihaz olmadığından gerçek telefon/ekran doğrulaması yapılmadı.
- Sonraki iş: araç ortamı ve ilk APK doğrulaması, ardından mevcut telefon protokolünü inceleyerek güvenli eşleştirme/cihaz sahipliği temeli ve gerçek salt okunur dashboard. İnternet taşıma hizmeti sağlayıcısı henüz seçilmedi.
- Ayrıntılı proje sınırları ve derleme yolu `companion-android/README.md` içinde. Kurulum paketi, push veya yayın yapılmadı.

### Android projesinin ayrılması

- Kullanıcı isteğiyle Android projesi `C:\Users\Yağız\Desktop\kveita-android` klasörüne taşındı; artık Kvieta deposunun alt klasörü değildir.
- Android için ayrı yerel Git deposu başlatıldı. Uzak depo, commit veya push oluşturulmadı.
- Android klasörüne bu planın ayrılma anındaki kopyası ve keşif talimatları eklendi. Bundan sonraki Android ilerlemesi kendi klasöründe tutulacak; iki plan otomatik eşitlenmez.
- Yukarıdaki `companion-android` yolları önceki aşamanın tarihsel konumudur. Masaüstü uygulama kodu bu taşıma sırasında değiştirilmedi.

### Karar günlüğü

| Tarih | Karar | Durum |
| --- | --- | --- |
| 2026-09-20 | BIG V1 UPDATE, masaüstü sadeleştirmesi ve mobil eşlikçiyi birlikte kapsar | Ürün yönü |
| 2026-09-20 | Mobil, kişisel durum dashboard'u ve aile talep deneyimidir | Kullanıcıyla kararlaştırıldı |
| 2026-09-20 | Mobil yalnızca Android olacak; iOS kapsam dışı | Kullanıcıyla kesinleştirildi |
| 2026-09-20 | Kvieta kullanıcı hesabı olmayacak; bağlantı hesapsız cihaz eşleştirmesiyle kurulacak | Kullanıcıyla kesinleştirildi |
| 2026-09-20 | Cihaz anahtarları, yetkili QR onayı ve internet iletişim hizmetiyle hesapsız bağlantı | Teknik tasarım önerisi; doğrulanacak |
| 2026-09-20 | Telefonun kendi kullanımını sınırlamak ilk kapsamda değildir | Kapsam sınırı |
| 2026-09-20 | İlk çalışma mevcut deneyim envanteri ve dört ekranlık prototiptir | Önerilen iş sırası |
| 2026-09-20 | İlk talepte yalnızca plan belgesi hazırlandı | Tarihsel kayıt |
| 2026-09-20 | Kullanıcı plana uygun yapım aşamasını başlattı | Geliştirme yetkisi; ilk masaüstü adımı başladı |
| 2026-09-20 | Masaüstü elle testleri ertelendi, mobil aşama başlatıldı | Kullanıcının güncel iş sırası |
| 2026-09-20 | Android ekran temeli Kotlin/Jetpack Compose olacak | Uygulandı; araç ortamında derleme bekliyor |

### Beşinci uygulama adımı — gerçek yerel Android bağlantısı (21 Eylül 2026)

- Android 0.2.0-preview ve Windows tarafına ayrı salt okunur dashboard eşleşmesi eklendi. Eski PIN kurtarma telefonu farklı yetki olarak korundu.
- Windows Ayarlar → Gizlilik ve veri → Telefonum: Android QR bağlantısı, kullanım yetkisi ve mevcut kurtarma telefonu tek ekranda. QR/metin daveti iki dakika geçerli; iki ekranda kod karşılaştırılıp bilgisayarda onaylanır. PIN ayarlıysa doğrulama gerekir. Davet metni ve kurtarma ayrıntıları varsayılan kapalıdır. Kurtarma kayıtları korunur; protokoller/anahtarlar henüz birleşmediğinden kurtarma yetkisi ayrıca tarayıcı QR'ıyla eklenir. Android'de de QR ana işlem, elle giriş açılır yedek yoldur.
- Android özel anahtarı Keystore'da, Windows sertifikası/eşleşme kaydı CurrentUser DPAPI ile korunur. Sertifikası QR'a sabitlenmiş HTTPS ve imzalı istekler kullanılır. Onay öncesi veri paylaşılmaz; yeniden kullanılan ve süresi geçmiş istekler reddedilir.
- Eşleşme yeniden açılışta saklanır; onay bekleyen telefon da kayıtlı sunucudan durumunu tekrar sorgulayabilir. Anahtar kaydı yedek/cihaz aktarımına alınmaz. Telefonun Bağlantıyı unut işlemi yerel özel anahtarı da siler.
- Gerçek özet: bilgisayar adı, kayıt günü/zamanı, ölçülen uygulama süresi, uygulanabilir günlük kalan kota ve ilk üç uygulama. Eski/güncel olmayan kayıtlar belirtilir. Telefon ölçümü veya politika değiştirme yetkisi yoktur.
- Bu önizleme aynı özel IPv4 ağında çalışır. Windows paylaşım penceresi kapanınca sunucu durur; IP değişirse yeniden eşleştirme gerekir. Kalıcı arka plan paylaşımı, internet rölesi, gerçek aile talebi ve bildirim henüz yoktur. QR telefonun kamera uygulamasından bağlantı olarak açılır veya metni yapıştırılır; uygulama içi kamera tarayıcısı henüz yoktur.
- Doğrulama: Windows Release ve tam smoke testleri; gerçek TLS üzerinden onay, tekrar, kayıt/yeniden başlama ve iptal; Android derleme/birim test/lint; Kotlin istemcisinin izole gerçek .NET sunucusundan veri okuduğu birlikte çalışma testi başarılı. Son testte 1 test çalıştı, 0 atlandı. Fiziksel Android Keystore, Wi-Fi/güvenlik duvarı ve cihazdaki ekran/yeniden açılış kontrolleri henüz yapılmadı.
- Android APK ayrı kveita-android projesinin app/build/outputs/apk/debug/app-debug.apk yolunda. Windows tarafındaki değişiklikler eski kurulu Alpha 5'te bulunmaz; yeni derleme gerekir. Windows setup paketi veya GitHub yayını bu adımda yapılmadı.
- İnternet bağlantısı tamamlandı: ücretsiz Cloudflare Workers + SQLite Durable Object aracı, yalnız son şifreli özeti tutar. PC özeti AES-GCM ile cihazda şifreler; Android aynı Wi-Fi'da bir kez yeniledikten sonra farklı ağdan okuyabilir. Relay anahtarı yalnız açık eşleşmede verilir; yazma anahtarı telefona gönderilmez. PC kapanırsa son kayıt eski olarak gösterilir. Aile talepleri ve uzaktan plan değişikliği bu aşamada yoktur. Protokol ayrıntıları Windows deposundaki docs/dashboard-protocol-v1.md dosyasındadır.

## 18. Yeni oturum için kısa başlangıç talimatı

### Güncel mobil durum — 22 Eylül 2026 / RC2

Hesapsız Android eşlikçi ve Cloudflare üzerinden şifreli özet/ek süre karar akışı uygulanmıştır. Önceki aşama notlarındaki “henüz yok” ifadeleri tarihsel kayıttır. RC2; sade Kvieta ekranı, güvenli aynı-karar tekrarı, çevrimdışı önbellek, eşleşme/gün/son tarih kontrolü ve yalnız disk kaydından sonra PC uygulama makbuzunu içerir. Windows 5.1.1 / V1-RC2 ve Android 1.0.0-rc2-dev paketleri üretildi. Ayrıntılı doğrulama, hashler ve kalan saha sınırları: [Mobil RC2 doğrulaması](docs/mobile-rc2-validation.md). Fiziksel telefon testleri tamamlanmış sayılmaz; bildirim anlık push değil periyodik kontroldür.

> `V1 RELEASE.md` dosyasını okuyarak BIG V1 UPDATE planından devam et. Hedef, Kvieta masaüstünü sadeleştirmek ve kişisel/aile kullanımına uygun, yalnızca Android'de çalışan hesapsız mobil eşlikçi dashboard hazırlamak. iOS veya Kvieta hesap sistemi ekleme. Mevcut aşamayı ve kod durumunu önce doğrula. Kesinleşmiş kapsamı koru, açık teknik kararları seçilmiş gibi varsayma. Yeni kart veya özellik eklemeden önce mevcut deneyimdeki karmaşayı azalt. İlerlemeyi ve kararları bu dosyaya kaydet.
