# V1 sonrası: Android

Android çalışması PC v1 kapsamından çıkarıldı. PC tarafındaki güvenlik doğrulaması
korunur; eski telefon istemcilerinin korunan aile ayarlarını kanıtsız değiştirmesine
izin verilmez. PC üzerindeki yerel yönetim ve kurtarma akışları kullanılabilir.

Android turunda:

- `mobile-parent-policy-v1.md` protokolünü istemciye ekle: telefonda yönetici PIN onayı,
  komuta bağlı kanıt, geçici anahtarın bellekten temizlenmesi; PIN/hash saklamama.
- Applying / Applied / Failed / Expired makbuzlarını göster; aynı komutu aynı
  kimlik ve içerikle yeniden dene; süresi dolmuş komutu yeni onaysız çalıştırma.
- Eski istemcide desteklenmeyen aile ayarlarını açıkça devre dışı göster.
- Fiziksel telefon, Wi-Fi/mobil veri, offline toparlanma, Keystore, bildirim ve
  OEM pil kısıtlarını doğrula.

Bu işler PC kaynak kodunun commit/push edilmesini bekletmez; mobil yönetim
özelliğinin yeni istemciyle ilan edilmesinden önce tamamlanır.
