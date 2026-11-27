<div align="center">

<img src="../assets/branding/kvieta-mark.svg" alt="Kvieta logosu" width="132" />

# Kvieta

### Her şeyin bir zamanı var.

Windows'ta ekran süresini anlamanın ve yönetmenin sakin, yerel yolu.

[**English**](../README.md)

![Windows](https://img.shields.io/badge/Windows-yerel-87946B?style=flat-square&labelColor=292B26)
![.NET](https://img.shields.io/badge/.NET-10-87946B?style=flat-square&labelColor=292B26)
![WPF](https://img.shields.io/badge/UI-WPF-C9B98E?style=flat-square&labelColor=292B26)
![Privacy](https://img.shields.io/badge/gizlilik-yerel--öncelikli-87946B?style=flat-square&labelColor=292B26)
![Status](https://img.shields.io/badge/durum-v1.0.0-C9B98E?style=flat-square&labelColor=292B26)
![License](https://img.shields.io/badge/lisans-MIT-87946B?style=flat-square&labelColor=292B26)

</div>

Kvieta, bilgisayar kullanımını cezaya çevirmeden zamanı görünür ve bilinçli hale getirir. Planlar, kurallar, kullanım geçmişi, kimlik bilgileri ve kurtarma verileri Windows cihazında kalır. Kvieta hesabı gerekmez.

## Windows x64 için Kvieta v1.0.0

[**Windows x64 için Kvieta Setup'ı indir**](https://github.com/Rel0adediso/kvieta-app/releases/download/v1.0.0/Kvieta-Setup-v1.0.0.exe)

Self-contained kurucu Türkçe ve English destekler; .NET SDK gerektirmez. Bu
sürüm bilerek imzasızdır, bu nedenle Windows SmartScreen
**Bilinmeyen yayıncı** uyarısı gösterebilir.

Bağımsız MSI, checksum dosyaları, release manifesti, ayrıntılı notlar ve bilinen
sınırlar [Kvieta v1.0.0 yayın sayfasında](https://github.com/Rel0adediso/kvieta-app/releases/tag/v1.0.0) bulunur. Çalıştırmadan önce Setup EXE'yi ekli `.sha256` dosyasıyla doğrulayın.

> **Önemli:** v1.0.0, önceki Alpha paketlerinin yerini alır. Mevcut Kvieta kurulumunun üzerine doğrudan kurulabilir; ayarlar, kullanım geçmişi, kurtarma verileri ve korunan policy korunur.

## Zamanla nasıl bir ilişki kuracağını seç

| Biçim | Kime göre? | Deneyim |
|---|---|---|
| **Farkındalık** | Alışkanlıklarını anlamak isteyenlere | Yapılandırılan uygulamaların kullanımını yalnızca cihazda kaydeder; hiçbir kısıtlama uygulamaz. |
| **Kişisel** | Kendi düzenini kurmak isteyenlere | Plan, limit, mola, odak oturumu ve isteğe bağlı kişisel koruma ekler. |
| **Aile** | Bir aile üyesinin standart Windows hesabına | Kuralları yönetici PIN'i ve Kvieta Guardian servisiyle korur. |

## Kvieta neler yapar?

| | |
|---|---|
| **Zamanı planlar** | Haftalık planlar, günlük limitler, kontrollü molalar, geçici izinler ve yönetici onaylı ek süre. |
| **Uygulamaları yönetir** | Günlük sayaç ve dayanıklı süreç tanımayla engelli, süreli veya serbest uygulama kuralları. |
| **Ritmi gösterir** | Yedi günlük içgörü, 90 günlük yerel geçmiş, haftalık toplam, günlük ortalama ve aktivite olayları. |
| **Kurtarılabilir kalır** | Çevrimdışı kurtarma kodları; isteğe bağlı güvenilir telefon, QR aktarımı, iptal ve yerel PIN sıfırlama onayı. |
| **Kuralları korur** | Guardian gözetimi, korumalı policy alanı, sağlık kontrolleri, onarım yolları ve doğrulanmış yönetici çıkışı. |
| **Gerçek hayata dayanır** | Atomik kayıt, son sağlam yedek, bozulma kurtarması, saat geri alma algısı ve eşzamanlı yazma koruması. |

## Kvieta v1.0.0 ile gelenler

- Zen arayüz yenilemesi: Mica arka plan efekti, sakin adaçayı renk paleti ve dairesel Bugün kontrol paneli.
- Mindful Dopamin Bariyeri: Shorts, Reels ve TikTok gibi kısa video döngülerinde araya giren sakinleştirici mola penceresi ve ana sayfaya yönlendirme.
- Sıkıştırılmış tek dosya dağıtımı; Setup EXE ve içindeki uygulamanın boyutları farklıdır. Kesin boyutlar son manifestte yer alır.
- Kvieta tarafından değiştirilen politikaları geri alma; diğer uygulamalara ait ayarları koruma. Çökme ve yükseltme senaryoları son Windows test matrisinde doğrulanır.
- Standart kullanıcı hesabında Windows süreç erişim izinleri ve Guardian gözetimi. Kernel sürücüsü veya yöneticiye karşı mutlak koruma değildir.

## Gizlilik tasarımın parçası

Kvieta zorunlu bulut hesabı kullanmaz. Kullanım geçmişi bilgisayarda kalır. İsteğe bağlı dışarıdan telefon erişimi açıldığında uçtan uca şifrelenmiş özet Cloudflare üzerinden iletilir; yerel paylaşım da kullanılabilir. Eşleştirme, telefon ekranında açıklanan yetkileri verir. Yönetici PIN doğrulama verileri ve kurtarma kodları özete eklenmez.

Community paketlerinde Authenticode imzası yoktur. Stable kanalı yapılandırıldığında ilk Setup, güncellemeleri arka planda indirip doğrulayan Windows görevlerini kaydeder. Hazır güncelleme, Kvieta’yı bir sonraki açışında kurulur. Windows yönetici onayı ilk Setup sırasında gerekir. Eski bir sürümden geçiş için yeni Setup’ı bir kez çalıştır.

## Projenin durumu

**Kvieta v1.0.0 Windows x64 için yayında.**

Görünen sürüm v1.0.0. MSI/dosya sürümü 7.0.1. Windows testlerinde Alpha 6.5 ve 7.0.0 önizlemesinden geçişte Guardian ayarlarının ve PIN kaydının korunduğu doğrulandı. Eski Alpha sürümlerinden geçmek için Setup'ı elle indirip çalıştırın.

- Kaynak kod bugün çalıştırılabilir; Windows paket hattı hazırdır.
- Debug ve Release derlemeleri, smoke testleri, belge kontrolleri ve public-build bypass kontrolleri kalite kapısı olarak çalışır.
- Community kurucular bilerek imzasızdır; Windows SmartScreen **Bilinmeyen yayıncı** uyarısı gösterebilir.
- v1.0.0 Setup EXE, MSI, checksum ve manifest tam kaynak commit'ini belirtir.

Kalan doğrulamalar için [yol haritasına](ROADMAP.md), ayrıntılı geçmiş için [sürüm notlarına](RELEASE_NOTES.md) bakın.

## Kaynaktan çalıştırma

Gereksinim: Windows ve .NET 10 SDK.

```powershell
dotnet run --project src/Kvieta.App/Kvieta.App.csproj
```

Oturum yüzünü doğrudan açmak için:

```powershell
dotnet run --project src/Kvieta.App/Kvieta.App.csproj -- --session
```

Ana kalite kontrolleri:

```powershell
dotnet build Kvieta.slnx -c Release
dotnet run --project tests/Kvieta.Core.SmokeTests/Kvieta.Core.SmokeTests.csproj -c Release
```

<details>
<summary><strong>Proje yapısı</strong></summary>

| Parça | Sorumluluk |
|---|---|
| `Kvieta.Core` | Plan, oturum, policy, model ve dayanıklı yerel veri katmanı |
| `Kvieta.App` | WPF Kontrol Merkezi, oturum yüzü, tray ve Windows entegrasyonları |
| Guardian servisi | Korumalı oturum ve otoriter policy alanını gözeten Windows servisi |
| `Kvieta.SetupApp` | İki dilli kurulum, güncelleme, onarım, yapılandırma ve kaldırma deneyimi |
| `Kvieta.Core.SmokeTests` | Çekirdek davranış, güvenlik regresyonu ve gerçek süreç testleri |

</details>

## Güvenlik sınırı

Korumalı mod esas olarak ayrı bir yönetici hesabı tarafından yönetilen **standart Windows kullanıcısı** için tasarlanır. Fiziksel erişimi ve Windows yönetici yetkisi bulunan birine karşı hiçbir masaüstü uygulaması mutlak direnç garanti edemez. Development test geçitleri Public build'lere derlenmez. Korumalı moda güvenmeden önce [güvenlik modelini ve sınırlarını](SECURITY.tr.md) okuyun.

## Belgeler

- [Türkçe kullanım rehberi](KULLANIM.tr.md) · [English user guide](USAGE.md)
- [Yol haritası](ROADMAP.md) · [Sürüm notları](RELEASE_NOTES.md)
- [Destek](../.github/SUPPORT.md) · [Katkıda bulunma](../.github/CONTRIBUTING.md)

## Geliştirme yaklaşımı

**İnsan tarafından yönlendirilen ürün · AI destekli geliştirme.** Ürün yönü, UX kararları ve gerçek kullanım testleri [Rel0adediso](https://github.com/Rel0adediso) tarafından yürütülür. Mimari, uygulama ve test geliştirme süreci yapay zekâ desteğiyle yürütülür.

Kvieta, [MIT Lisansı](../LICENSE) altında yayımlanan açık kaynak bir yazılımdır.

---

<div align="center">

**Kvieta** · *Her şeyin bir zamanı var.*

</div>
