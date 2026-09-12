# Adim 5 repo-arasi genelleme beklentileri

**Beklentiler olculmeden once yazildi.**

**Tarih:** 2026-09-12
**Durum:** Hicbir genelleme modeli egitilmeden yazildi.

## Elde ne var

Ayni-repo sonuclari (Adim 3, train'de secilen esikle):

| Repo | Test F1 | Test PR-AUC | Brier | ECE |
|---|---|---|---|---|
| Polly | 0,2500 | 0,3033 | 0,0132 | 0,0292 |
| ShareX | 0,1868 | 0,1192 | 0,0559 | 0,0625 |
| Jellyfin | 0,4895 | 0,5199 | 0,0998 | 0,0810 |

`asama5-beklenti.md` (7. madde) zaten sunu yazmisti: repo-arasi F1 ayni-repo F1'inin
**altinda** kalacak, kabaca yarisi ile dortte ucu arasinda. O beklenti duruyor; asagidakiler
onun uzerine bu adimin ayrintilari.

## Beklentiler

1. **Repo-arasi F1 genel olarak ayni-repo F1'inin altinda kalacak.** Alti yonun her
   birinde tek tek altinda kalmasi zorunlu beklenti degil; genel egilimden soz ediyorum.

2. **Polly hedef oldugunda sonuc kararsiz olacak.** Polly'nin test bolumunde 10 pozitif
   var; tek bir satirin yer degistirmesi F1'i gozle gorulur oynatir.

3. **ShareX'in train/test oznitelik kaymasi aktarimi zorlastirabilir.** Adim 3'te
   olculdu: ShareX test satirlarinin %94,7'si en az bir ozniteligiyle kendi egitim
   araliginin disinda. Kaynak ShareX oldugunda ya da hedef ShareX oldugunda bunun etkisini
   goruyor olabiliriz.

4. **Iki repoda egitilen model tek kaynakli modele gore daha istikrarli olacak.**
   "Istikrarli" derken: uc hedefin sonuclarinin birbirine, alti tek-kaynak yonunun
   sonuclarindan daha yakin dagilmasini bekliyorum.

5. **PR-AUC F1'den daha tutarli aktarilacak.** Sebep: PR-AUC esikten bagimsiz siralamayi
   olcuyor, F1 ise hem siralamayi hem kaynakta secilen esigin hedef taban oranina uyumunu
   birlikte olcuyor. Taban oranlari uc repoda cok farkli (test: %1,21 / %5,06 / %13,48).

6. **Kaynakta secilen esigin hedefe tasinmasi precision ya da recall'u bozacak.** Yonu
   hedefin taban oranina bagli: hedefin orani kaynaktan dusukse cok fazla alarm cikar
   (precision duser), yuksekse tersi.

7. **Repo kimligi modele girmeyecek** ve bu bir beklenti degil kural (ADR 0016).

## Beklenti yazmadigim yerler

- Hangi yonun en iyi ya da en kotu sonucu verecegi.
- "Korunan performans" oraninin (repo-arasi F1 / ayni-repo F1) sayisal bandi.
- Katsayi isaretlerinin kaynaklar arasinda ne kadar korunacagi.
- Leave-one-repository-out sonucunun en iyi tek-kaynak aktarimindan yuksek mi dusuk mu
  olacagi.
