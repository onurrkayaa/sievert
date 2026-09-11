# Sievert commit atiflari: yeniden yazma oncesi ve sonrasi

**Tarih:** 2026-09-11

Repo gecmisi iki kez bastan yazildi ve force push edildi. Birincisinde commit
mesajlari duzeltildi ve repo kokundeki calisma notu gecmisten cikarildi;
ikincisinde yazar kimligi GitHub hesabina bagli adrese cevrildi. Ikisi de butun
commit hash'lerini degistirdi.

Olcum ve rapor dosyalarinda "su olcum su Sievert commit'inde yapildi" diye atiflar
vardi ve o hash'ler artik uzaktaki repoda yok. Bu tablo eski hash'i yeni hash'e
bagliyor.

Atiflari guncellerken eski hash'i **silmedim**, `yeni (yeniden yazma oncesi: eski)`
bicimini kullandim. Sebebi su: eski hash'ler daha once yazilmis raporlarda ve
disariya verilmis notlarda geciyor. Sessizce degistirseydim, elinde eski hash olan
biri hangi sayinin hangi surumle uretildigini bir daha izleyemezdi.

Bu tablodaki eski hash'ler uzaktaki repoda cozulmez. Yerelde `yedek-eski-gecmis`
dali eski gecmisi tutuyor, oradan hala ulasilabiliyorlar.

## Nasil eslestirildi

Iki bagimsiz sinyal kullandim ve ikisi de 12 satirin 12'sinde ayni sonucu verdi:

1. **Yazar tarihi.** Iki yeniden yazma da yazar tarihine dokunmadi. Bu repoda her
   commit'in yazar tarihi benzersiz, yani tek basina ayirt edici.
2. **Commit govdesi.** Sadece konu satiri degistirildi (onekler kaldirildi), govde
   oldugu gibi kaldi. Eski govdeden `Co-Authored-By` satirlari cikarilinca iki
   govde birebir ayni cikiyor.

Agac hash'leri ayni degil, cunku birinci yeniden yazma repo kokundeki calisma
notunu her commit'ten cikardi. Bu beklenen bir fark, eslesmeyi bozmuyor.

## Esleme

| Eski hash | Yeni hash | Commit (yeni konu satiri) | Yazar tarihi | Nasil eslestirildi |
|---|---|---|---|---|
| `1a7f116` | `8aed65d` | Roslyn ile dosya cozumlemeyi ekle | 2026-09-11 09:44:41 +0300 | yazar tarihi + govde metni |
| `6211c80` | `1c1ab97` | Cikti tutarliligini ve JSON sozlesmesini duzelt | 2026-09-11 09:53:37 +0300 | yazar tarihi + govde metni |
| `5b1f5ce` | `53c3b7c` | Enum, kor nokta olcumu ve dagilim istatistiklerini ekle | 2026-09-11 10:10:28 +0300 | yazar tarihi + govde metni |
| `ae049e2` | `4c8b5dc` | Kor nokta olcumu sonrasi taramayi guncelle | 2026-09-11 10:12:20 +0300 | yazar tarihi + govde metni |
| `7eac257` | `6fde0c3` | Tip ve dosya adlarini Ingilizceye cevir | 2026-09-11 10:27:27 +0300 | yazar tarihi + govde metni |
| `c142311` | `79544b1` | Yaniltici test adini duzelt | 2026-09-11 10:39:00 +0300 | yazar tarihi + govde metni |
| `3a81e84` | `2963802` | SV001'in Polly olcumunu ve dogrulama listesini yaz | 2026-09-11 10:42:31 +0300 | yazar tarihi + govde metni |
| `f7dda08` | `5279968` | ShareX olcumunu ve SV001 dogrulama listesini yaz | 2026-09-11 10:48:23 +0300 | yazar tarihi + govde metni |
| `b60e07e` | `889fbef` | SV001 dogrulama sonuclarini ve precision'i yaz | 2026-09-11 10:56:36 +0300 | yazar tarihi + govde metni |
| `16058bd` | `ba43b97` | Asama 2 kapanis raporunu yaz | 2026-09-11 11:00:02 +0300 | yazar tarihi + govde metni |
| `215c0ad` | `6bbc389` | SV004, SV005 ve SV006 kurallarini ekle | 2026-09-11 14:02:45 +0300 | yazar tarihi + govde metni |
| `5fc4846` | `f8906a4` | Kademe 1'in etkisini olc | 2026-09-11 14:10:53 +0300 | yazar tarihi + govde metni |

Toplam 12 atif, 12 tanesi eslesti, 0 tanesi bulunamadi.

## Dokunulmayanlar

Olculen repolarin hash'lerine el surulmedi, onlar baska repolara ait ve degismediler:

| Repo | Commit |
|---|---|
| App-vNext/Polly | `2247db2407713fa221d57011814e4be446361b1f` |
| ShareX/ShareX | `b5a397ea6ccf00659cee593c981be4b01ab641fe` |
| jellyfin/jellyfin | `1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139` |
