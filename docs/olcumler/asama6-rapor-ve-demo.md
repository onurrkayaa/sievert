# Rapor ve demo olcumu

**Tarih:** 2026-09-13
**Beklentiler:** `docs/olcumler/asama6-rapor-ve-demo-beklenti.md`, kod yazilmadan once yazildi.

Bu dosyanin 1. ve 2. bolumu **olcum kosulmadan once** yazildi ve kendi commit'inde
duruyor. Sebebi: hangi kosuyu kac kez kosacagimi sonuclara bakarak secersem, iyi gorunen
kosuyu secmis olurum.

## 1. Kosulun sabitlenmesi

- Derleme **Release**, `dotnet publish` ciktisindan kosuyor.
- API ayri bir surecte, loopback'te.
- Veritabani ayni makinede, Docker'da (PostgreSQL 17.11).
- Tek istemci.
- Rapor uretimi arka plan isinde; olculen sure **istek atildigi andan** artefakt hazir
  olana kadar.
- Rapor uretilirken ayni anda saglik ucu ve bir risk ucu cagriliyor; onlarin sureleri de
  yaziliyor. Amac: rapor uretimi normal istekleri bekletiyor mu.
- Her rapor ayri bir tekrar anahtariyla isteniyor; ayni artefaktin geri donmesi ayri bir
  olcum.
- Makine: Apple M2, 8 cekirdek, 16 GB, macOS 26.6.2.

## 2. Kosu sayisi (olcumden once yazildi)

Uc deponun uculunde de rapor uretiliyor. Kosu sayilari farkli, cunku Jellyfin'in isi
22 bin satirlik ve her kosu pahali:

| Depo | Kosu |
|---|---|
| Polly | 5 |
| ShareX | 3 |
| Jellyfin | 3 |

Rapor parametreleri butun kosularda ayni ve varsayilan: pencere 200, dosya 50, zaman
cizelgesi 100, commit 20, bulgu 50.

Ayrica birer kez olculecek, tekrarsiz:

- Ayni tekrar anahtariyla ikinci istek (yeni is aciliyor mu, dosya ayni mi).
- Bir bayti degistirilmis artefaktin indirilmeye calisilmasi.
- Kuyruktaki ve kosan bir rapor isinin iptali.
- Tek komutluk demo: **en az 5 temiz kosu**.

Demo kosularinda soguk imaj indirme suresi **ayri** yaziliyor; ana tabloya katilmiyor,
cunku o ag hizina bagli.
