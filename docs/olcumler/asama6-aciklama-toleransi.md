# Asama 6 aciklama toleransi: v1 ve v2 yan yana

**Tarih:** 2026-09-13
**Sozlesme:** `docs/urun/model-aciklama-sayisal-tolerans.md` surum 2.0, kod yazilmadan once.

Adim 2'de ilan ettigim mutlak tolerans tutmamisti. Bu dosya eski kurali ve yeni kurali
ayni veri uzerinde yan yana koyuyor. **Eski sonuc silinmedi**, asagida ve
`docs/olcumler/asama6-api-temel.md` bolum 7'de duruyor.

## 1. Nasil olculdu

Girdi dondurulmus anlik goruntu (`data/asama5/commit-metrics.csv`), veritabani degil.
Sebep: bu olcumun iki kosuda ayni baytlari vermesi gerekiyor. Ozet dogrulandiktan sonra
34 166 satirin hepsi uc profilin kendi modeliyle aciklandi.

Ureten komut:

```
dotnet run --project tools/Sievert.Measure -- explanation-tolerance . <commit> data/asama6/explanation-tolerance.json
```

Ureten kod: `577cc8e`. Cikti: `data/asama6/explanation-tolerance.json`, SHA-256
`db7f4ca71e146565d45fb65d334114f4ab80eaf4e0524d1f9bb97b18c83ebfbc`. Komut iki kez
kosuldu, iki dosya bayt bayt ayni.

## 2. Iki kural

| | v1 (Adim 2) | v2 (Adim 3) |
|---|---|---|
| Kural | `abs(fark) <= 1e-6` | `abs(fark) <= 1e-6 + u * scale` |
| `u` | - | `2^-23` = 1,1920928955078125e-7 |
| `scale` | - | `max(1, abs(intercept) + toplam(abs(katki)))` |
| Raporlanan olcu | mutlak fark | `normalizedError = mutlak fark / izin verilen` |

## 3. Sonuc

| Profil | Satir | v1'de kalan | v2'de kalan |
|---|---|---|---|
| jellyfin | 22 917 | 7 | **0** |
| polly | 2759 | 3 | **0** |
| sharex | 8490 | 1 | **0** |
| **toplam** | **34 166** | **11** | **0** |

v1 sayisi Adim 2'de olculen 11 ile birebir ayni; ayni satirlar.

| Olcu | Deger |
|---|---|
| En buyuk mutlak fark | 1,783e-6 |
| En buyuk izin verilen fark | 4,980e-6 |
| En buyuk normalize hata | 0,5098 |

## 4. Normalize hatanin dagilimi

| Profil | p50 | p95 | p99 | max |
|---|---|---|---|---|
| jellyfin | 0,0522 | 0,1623 | 0,2183 | 0,5098 |
| polly | 0,0582 | 0,1763 | 0,2356 | 0,4181 |
| sharex | 0,0386 | 0,1217 | 0,1828 | 0,3547 |

En kotu satir bile izin verilen payin yarisini kullaniyor. Yani v2 sinirda calismiyor;
bir satirin gecmesi icin "kil payi" gereken bir durum yok.

## 5. v1'de kalan 11 satirin v2 sonucu

| Profil | Commit | Mutlak fark | Olcek | Izin verilen | Normalize | v2 |
|---|---|---|---|---|---|---|
| jellyfin | `7f320ce0638c` | 1,180e-6 | 15,54 | 2,852e-6 | 0,4139 | gecti |
| jellyfin | `dce9093ba1f6` | 1,012e-6 | 14,19 | 2,691e-6 | 0,3759 | gecti |
| jellyfin | `ab3da461130b` | 1,187e-6 | 16,62 | 2,981e-6 | 0,3981 | gecti |
| jellyfin | `48facb797ed9` | 1,223e-6 | 33,38 | 4,980e-6 | 0,2456 | gecti |
| jellyfin | `f47ad85011a1` | 1,209e-6 | 23,65 | 3,819e-6 | 0,3166 | gecti |
| jellyfin | `d0b3dc1485dd` | 1,183e-6 | 11,07 | 2,320e-6 | 0,5098 | gecti |
| jellyfin | `1dbc91978ece` | 1,063e-6 | 14,60 | 2,741e-6 | 0,3877 | gecti |
| polly | `f5cc4fe6da9f` | 1,064e-6 | 18,86 | 3,248e-6 | 0,3276 | gecti |
| polly | `d05f172f77b7` | 1,062e-6 | 27,31 | 4,256e-6 | 0,2495 | gecti |
| polly | `e32730600746` | **1,783e-6** | **27,39** | 4,265e-6 | 0,4181 | gecti |
| sharex | `e770e8600f36` | 1,093e-6 | 17,46 | 3,082e-6 | 0,3547 | gecti |

Adim 2'de "en kotu satir" diye yazdigim satir `e32730600746`: logit 2,33, katkilarin
mutlak toplami 27,39. Olcek sutunu o tahmini dogruluyor - 11 satirin olcegi 11 ile 34
arasinda, yani hepsi buyuk terimli satirlar.

## 6. Mutasyon testi

Toleransin gevsemedigini gostermek gerekiyordu: her seyi kabul eden bir tolerans da
"gecti" derdi.

Her profilde tek bir katsayi, en az 1e-4 mutlak logit farki uretecek kadar bozuldu ve
aciklama gercek yoldan (`ModelExplainer.Explain`) yeniden uretildi.

| Profil | Bozulan oznitelik | Mutlak fark | Izin verilen | Normalize | Sonuc |
|---|---|---|---|---|---|
| jellyfin | `DirectoryCount` | 9,989e-5 | 3,407e-6 | 29,3 | reddedildi |
| polly | `MaxFileAgeDays` | 9,981e-5 | 3,199e-6 | 31,2 | reddedildi |
| sharex | `MaxFileAgeDays` | 9,992e-5 | 2,124e-6 | 47,0 | reddedildi |

Ucunde de normalize hata 29'un uzerinde, yani sinira yakin bile degil. Gercek satirlarin
en kotusu 0,51 iken bozuk aciklama 29-47 bandinda; iki grup arasinda yaklasik 60 kat
bosluk var.

Ayrica kesisimi 1e-4 bozan bir varyant da testlerde reddediliyor.

## 7. Beklentilerin karsiligi

Sozlesme iki sey istiyordu.

| Beklenti | Sonuc | Tuttu mu |
|---|---|---|
| v1'de kalan 11 satir v2'de gecmeli | 11/11 gecti | evet |
| Bilerek sokulmus 1e-4'luk bozukluk reddedilmeli | uc profilde de reddedildi | evet |

## 8. Olculmeyenler

- `double` ile yeniden hesaplanmis bir aciklamanin ne verecegi. Katkilar bilerek modelin
  gordugu `float` degerler uzerinden hesaplaniyor; baska turlusu modelin yaptigi islem
  olmazdi.
- 1e-4'ten kucuk bozukluklarin hangi noktada yakalanmaya basladigi. Mutasyon tek bir
  buyuklukte denendi; esik taramasi yapilmadi.
- Baska bir ML paketi ya da baska bir trainer ile ayni kuralin ne verecegi.
