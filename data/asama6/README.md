# Asama 6 referans dosyalari

Burada API'nin okudugu, urettigi degil, **okudugu** dosyalar duruyor.

| Dosya | Ne |
|---|---|
| `model-score-reference.json` | Uc model profilinin egitim bolumundeki skor dagilimi |
| `model-score-reference.sha256` | Dosyanin SHA-256 ozeti |
| `api-baseline.json` | API temel olcumunun ciktisi |

`model-score-reference.json` **dondurulmus kanit**: ayni girdiyle her kosuda ayni
baytlari veriyor, o yuzden ozeti var.

`api-baseline.json` dondurulmus kanit **degil** ve ozeti yok: icinde sure ve bellek gibi
makineye bagli sayilar var, iki kosu ayni dosyayi vermiyor. Sayilarin yorumu
`docs/olcumler/asama6-api-temel.md` icinde.

Kontrol:

```
cd data/asama6
shasum -a 256 -c model-score-reference.sha256
```

Ureten komut:

```
dotnet run --project tools/Sievert.Measure -c Release -- \
  score-reference data/asama5 <commit> data/asama6/model-score-reference.json
```

## Ne ise yariyor

Goreli risk endeksi (`riskIndex`) bu dagilimdan hesaplaniyor: bir commit'in ham model
skorunun, ayni profilin egitim skorlari arasindaki yuzdelik sirasi. Dagilimi istek
aninda hesaplamak, her aciliste 24 bin satiri yeniden skorlamak olurdu; ustelik hangi
surumle uretildigi izlenemezdi.

## Nasil uretildi

Yeni model **egitilmedi**. Asama 5'te dondurulan `data/asama5/models/*.zip` dosyalari
oldugu gibi yuklendi, olcekleyici `model-results.json` icindeki egitim istatistiklerinden
kuruldu ve egitim satirlari skorlandi.

Dosya yazilmadan once uretilen skorlar egitim esiginde sayildi ve `model-results.json`
icinde kayitli TP/FP/FN/TN degerleriyle karsilastirildi. Uc repoda da aynen tuttu:

| Profil | Egitim satiri | TP | FP | FN | TN |
|---|---|---|---|---|---|
| polly | 1931 | 191 | 226 | 60 | 1454 |
| jellyfin | 16 041 | 2456 | 1836 | 1305 | 10 444 |
| sharex | 5943 | 462 | 865 | 422 | 4194 |

Komut iki kez kosuldu, iki dosya bayt bayt ayni cikti.

## Bicim

UTF-8, BOM yok. Satir sonu `\n`. Ondalik sayilar `R` (tam donus) formatinda, nokta
ayracli. `trainScores` artan sirada; esitlikte orta sira (mid-rank) kurali endeksi
hesaplayan tarafta uygulaniyor.
