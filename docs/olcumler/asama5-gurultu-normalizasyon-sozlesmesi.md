# Sentetik gurultu sonuclarinin taban oranindan ayrilmasi

**Surum:** 1.0
**Tarih:** 2026-09-12
**Durum:** Koddan ve sonuclardan **once** yazildi.

## Sorun

Adim 4'te olculdu: sentetik gizli pozitif orani %5 → %10 → %20 arttikca **mutlak** F1 ve
PR-AUC yukseldi (Polly 0,2707 → 0,3882, ShareX 0,2250 → 0,3850, Jellyfin 0,4923 → 0,5535).

Bu tek basina "model gurultuden faydalandi" demek **degil**:

- Pozitif sinif orani yukselince rastgele bir siniflandiricinin PR-AUC'si de yukselir;
  sabit skorlu bir tabanin PR-AUC'si tam olarak taban oranidir (ADR 0017).
- F1'in olcegi sinif oranindan etkilenir.
- Sentetik etiketler yalnizca `CsFilesChanged > 0` commit'lerden secildigi icin modele
  kolay bir yapi eklenmis olabilir.
- Brier uc repoda da **kotulesti**.

## Ne degismeyecek

Mevcut sentetik deney **degistirilmiyor**: ayni flip'ler, ayni ana tohum (20260912), ayni
tekrar tohumlari, ayni %5 / %10 / %20 oranlari, repo basina 100 tekrar. Yeni hesaplar
mevcut lojistik regresyon sonuclarini degistirmemeli ve bu kontrol edilecek.

## Her tekrarda hesaplanacak uc yontem

Ayni degistirilmis train/test etiketleri uzerinde:

1. **Lojistik regresyon** (mevcut deneyin modeli, ayni protokol).
2. **Egitim pozitif oraniyla rastgele taban**: her test satirina `p` olasilikla pozitif;
   `p` = sentetik train'in pozitif orani. Skor surekli rastgele sayi.
3. **`LinesAdded` tabani**: esik **yalniz sentetik train etiketlerinde** secilir
   (en yuksek train F1, esitlikte yuksek precision, hala esitlikte yuksek esik - ADR 0017).
   Test etiketleri esik secimine **girmez**.

## Ek olculer

**PR-AUC lift:**

    lift = PR-AUC_model - test_pozitif_orani

Sabit skorlu bir tabanin PR-AUC'si taban oranina esit oldugu icin lift, "siralamanin
taban orandan ne kadar fazlasi" demek.

**Normalize PR-AUC:**

    normalize = (PR-AUC_model - test_pozitif_orani) / (1 - test_pozitif_orani)

Ust sinir 1'e olcekliyor.

**Brier climatology:** her test satirina **sentetik train'in pozitif orani** olasilik
olarak verilir ve Brier hesaplanir. Bu, "veriye hic bakmayan ama taban orani bilen" bir
tahmincinin Brier'i.

**Brier skill score:**

    skill = 1 - (Brier_model / Brier_climatology)

Pozitifse model climatology'den iyi, negatifse kotu.

## Sinir kurallari

- Test pozitif orani **1** ise normalize PR-AUC **hesaplanmaz** (payda 0).
- `Brier_climatology` **0** ise skill score **hesaplanmaz** (payda 0).
- Hesaplanamayan her deger icin **N/A sayisi raporlanir**.
- **Mutlak PR-AUC ile lift birbirinin yerine kullanilmaz.**
- `LinesAdded` tabani icin Brier gerekiyorsa **0/1 tahmin skoruyla** hesaplanir; ham
  `LinesAdded` degeri olasilik gibi kullanilmaz.
- Butun sayilar **sentetik etiketlere gore** hesaplanir ve **gercek performans iddiasi
  degildir**.

## Yorum kapisi

Su durumda **"model gurultu arttikca iyilesti" YAZILMAZ**: mutlak F1 ve PR-AUC artiyor
fakat

- lift artmiyor, **ve**
- normalize PR-AUC artmiyor, **ve**
- tabanlara fark artmiyor, **ve**
- Brier skill score dusuyor.

Bunun yerine sayiyla birlikte su yazilir:

> "Pozitif oranla birlikte mutlak metrikler yukseldi; eslenmis tabanlara gore goreli
> sonuc …"

Sebep kanitlanmadiysa **"sinif orani etkisiyle uyumlu"** denebilir; **"sinif orani
nedeniyle oldu"** denemez.

## Raporlanacaklar

Her repo ve her oran icin, her olcunun **ortalama / p2,5 / medyan / p97,5** degerleri:

- Lojistik regresyon: F1, PR-AUC, PR-AUC lift, normalize PR-AUC, Brier, Brier skill score.
- Eslenmis `LinesAdded`: F1, PR-AUC, PR-AUC lift, normalize PR-AUC.
- Eslenmis rastgele taban: F1, PR-AUC, PR-AUC lift.
- Farklar: model F1 − `LinesAdded` F1, model PR-AUC − `LinesAdded` PR-AUC, model lift −
  `LinesAdded` lift, model F1 − rastgele F1, model PR-AUC − rastgele PR-AUC.
