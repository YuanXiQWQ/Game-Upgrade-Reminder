# Oyun Yükseltme Hatırlatıcısı

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

Uzun zaman alan oyun yükseltmelerinin ilerlemesini kaydetmek ve takip etmek için bir araç. İlk olarak **Boom Beach** için oluşturuldu.

## Özellikler

- 🕒 Birden fazla hesaptaki yükseltme görevlerini takip edin
- ⏰ Takvim/alarmlardan farklı olarak geri sayım oyunla senkronize edilir, böylece her seferinde manuel süre hesaplama ihtiyacı ortadan kalkar
- 🔔 Yükseltme tamamlandığında sistem bildirimi gösterir
- ♻️ Tekrarlayan görevler: günlük / haftalık / aylık / yıllık / özel; isteğe bağlı bitiş süresi (varsayılan: yok); atlama kuralları desteklenir
- 🌐 27 dili destekler

## Sistem Gereksinimleri

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) veya daha yenisi
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) veya daha yenisi

> Diğer sürümlerde çalışıp çalışmayacağından emin değilim :<

## Kurulum

1. [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases) sayfasından en son sürümü indirin
2. Dosyaları herhangi bir klasöre çıkarın
3. `Game Upgrade Reminder.exe` dosyasını çalıştırın

## Kullanım

### Yükseltme Görevi Ekleme

1. Arayüzün üst kısmından hesabı seçin
2. Bir görev adı seçin veya yeni oluşturun (boş bırakılabilir)
3. Gerekli süreyi ayarlayın: başlangıç zamanı, gün, saat, dakika (başlangıç zamanı ayarlanmazsa varsayılan olarak mevcut sistem zamanı kullanılır)
4. "Ekle" düğmesine tıklayarak görevi oluşturun

### Görevleri Yönetme

- Süresi dolan görevler vurgulanır; "Tamamla"ya tıklayarak tamamlandı olarak işaretleyin
- Görevler listeden silinebilir ve silme işlemi üç saniye içinde geri alınabilir

## SSS

### Sistem bildirimleri alınmıyor

- **Odak Yardımı (Focus Assist)** özelliğini kapatın veya `Game Upgrade Reminder.exe` dosyasını öncelikli listeye ekleyin. Eğer otomatik kurallar "Yalnızca alarmlar" olarak ayarlanmışsa, "Yalnızca öncelikler" olarak değiştirin.
- Bunun dışında bilmiyorum

### Diğer garip sorunlar

- Muhtemelen bir hata (bug), görmezden gelin
- Issues sayfasında bildirilebilir, ancak muhtemelen nasıl düzelteceğimi bilmem

## Lisans

Bu proje [GNU Affero General Public License v3.0](../LICENSE) kapsamında lisanslanmıştır.