# Asama 4 kapanis raporu

**Tarih:** 2026-09-12
**Kapsam:** `b9d0f8e` ... `00888b6` (35 commit)

Asama 4'un amaci git tarihini okumak, commit basina olcu cikarmak, bunlari
PostgreSQL'de saklamak ve hangi commit'in hata getirdigini etiketlemekti. Risk skoru,
ML.NET ve arayuz bu asamanin disindaydi, hicbiri yazilmadi.

Bu asamanin ayirt edici yani su: bir olcum urun kodunda bir hata buldu. Bosluk
esdegerligini olcerken blame satir numaralarinda bir kayma ortaya cikti; o gunku 355
testin hicbiri gormemisti.

## 1. Ne yapildi

- **Git madenciligi** (`b9d0f8e`, ADR 0011): `sievert mine` commit basina yazar, tarih,
  mesaj ve dosya degisikliklerini cikariyor; birlestirme commit'leri veriden cikip
  sayiliyor, kimlik epostayla belirleniyor, cikti JSONL.
- **Veri katmani** (`a89697b`, `160249f`, ADR 0012): dort tablo, EF Core ve Npgsql,
  idempotent yazma, uc indeks. Depo kimligi uzak adresten normalize ediliyor (`a1e62f0`).
- **Commit metrikleri** (`6b35532`, ADR 0013): 15 olcu, hepsi yalnizca o commit'ten
  ONCEKI veriyle hesaplaniyor; zaman sizintisini iki test siniyor.
- **SZZ etiketleme** (`dca4c8d`, ADR 0014): duzeltme commit'lerinin sildigi `.cs`
  satirlari blame edilip hata getiren commit isaretleniyor.
- **Satir bazli susturma** (`1a65408`, ADR 0015): `// sievert:disable SV004 gerekce`;
  gerekcesiz susturma gecersiz ve kendisi bulgu (SV007).

## 2. Olculen sayilar

### Katman maliyeti (Polly, ayni 2759 commit)

Kaynak: `docs/olcumler/asama4-mine-bellek.md` (`dca4c8d`).

| Katman | Sure | Tepe bellek |
|---|---|---|
| Git okuma + JSONL | 3,95 sn | 244 MB |
| Git okuma + PostgreSQL'e yazma | 5,44 sn | 323 MB |
| Veritabanindan metrik | 0,61 sn | 169 MB |
| SZZ etiketleme | 125,04 sn | 178 MB |

### Uc repoda etiket orani

Kaynak: `docs/olcumler/asama4-uc-repo.md` (`ad06c91`). Sayilar satir kaymasi
duzeltildikten sonraki kodla (`9592508`).

| Repo | Pay (etiketlenen) | Payda (commit) | Oran |
|---|---|---|---|
| Polly | 261 | 2759 | %9,5 |
| ShareX | 1013 | 8490 | %11,9 |
| Jellyfin | 4688 | 22 917 | %20,5 |

### Jellyfin kosusu ve maliyet modeli

| Olcu | Deger |
|---|---|
| Sure | 5456,49 sn |
| Tepe bellek | 1105 MB |
| Tahmin | 13 159 sn |
| Tahmin / gercek | 2,41 |

Katsayi (sn / duzeltme / commit): Polly 1,47e-4 · ShareX 1,41e-4 · Jellyfin 6,00e-5.

### Ana tablo: etiketlerin dogrulugu

Kaynak: `docs/olcumler/asama4-etiketleme-dogruluk.md` (`00888b6`), olcutler `1e835c7`.
30 satir, uc repoya dagitilmis (her repodan 10). Onceden ilan edilmis olcutlerle yazar
tarafindan kaynak koda bakilarak siniflandirildi; bagimsiz ikinci degerlendirici
kullanilmadi.

| Kume | Pay | Payda | Oran | Paydadan cikarilan Belirsiz |
|---|---|---|---|---|
| Etiketli - dogru suclama | 12 | 12 | %100,0 | 3 |
| Etiketsiz - kacirma | 11 | 13 | %84,6 | 2 |

Iki oran **birlestirilmedi**; paydalari farkli kumeler (12 ve 13). Toplam 5 Belirsiz.

### IsFix: tam kelime ve kok eslesmesi

Kaynak: `asama4-isfix-recall.md` (`88b0870`), `asama4-uc-repo.md` (`ad06c91`).

| Repo | Tam kelime | Kok eslesmesi | Sadece kokte yakalanan |
|---|---|---|---|
| Polly | 292 / 2759 (%10,6) | 330 / 2759 (%12,0) | 38 |
| ShareX | 642 / 8490 (%7,6) | 1341 / 8490 (%15,8) | 699 |
| Jellyfin | 3047 / 22 917 (%13,3) | 4066 / 22 917 (%17,7) | 1019 |

Polly'de kacirilan 38 satirdan 20 rastgele ornekte 14 gercek duzeltme; mevcut tanimin
yakaladiklarindan 20 ornekte 15 gercek duzeltme.

### Bosluk duyarliligi ve blame esdegerligi

Kaynak: `asama4-szz.md` (`df9e492`), `asama4-uc-repo.md` (`ad06c91`).

| Olcu | Deger |
|---|---|
| Bosluk sayilinca eklenen etiket | 14 / 272 (+%5,1) |
| Bosluk yok sayilinca kaybolan etiket | 0 |
| Bizimki vs duz `git blame` | 2190 / 2243 (%97,6) |
| Bizimki vs `git blame -w` | 762 / 2243 (%34,0) |
| `git blame -w` vs duz `git blame` farkli | 1428 / 2243 (%63,7) |

### Bot ve ad degisimi

Kaynak: `asama4-bot-etkisi.md`, `asama4-ad-degisimi.md` (`88b0870`, `6b35532`).

| Olcu | Deger |
|---|---|
| Polly'de bot bayrakli commit | 854 / 2759 (%31,0) |
| Metrigi degisen bot disi commit | 372 / 1905 (%19,5) |
| Ad degisimi iceren commit | 80 / 2759 (%2,9) |
| Metrigi ad degisimi secimine bagli commit | 1234 / 2759 (%44,7) |

## 3. Beklemedigim bulgular

**Kayma hatasi.** Bekledigim: bosluk esdegerligi olcumu iki yontemin ne kadar ortustugunu
gosterecek. Cikan: bizim blame'imiz duz `git blame` ile yalnizca 1541 / 2243 (%68,7)
satirda ayni commit'i sucluyordu. Satir numarasi bir geri kaydirilinca 2188 / 2243
(%97,5) oldu - `BlameHunk.FinalStartLineNumber` 0 tabanli, bizim satirlarimiz 1 tabanli.
SZZ her seferinde bir alttaki satiri sucluyordu. Onemi: o gunku 355 testin hicbiri
gormemisti, cunku test depolarindaki dosyalarin butun satirlari tek commit'ten geliyordu
ve bir satirlik kayma ayni commit'e denk geliyordu. Duzeltme sonrasi etiket sayilari:
Polly 272 -> 261, ShareX 1142 -> 1013.

**Sayac hatasi.** Bekledigim: SZZ etiket orani literaturdeki %10-30 bandinda. Cikan:
Polly'de %9,9. Bandin altinda kaldigi icin huniyi actim ve "birini suclayabilen duzeltme"
sayacinin yanlis oldugunu buldum: kumeye YENI sha ekleyen duzeltmeleri sayiyordu, 111
gosteriyordu; dogrusu 176. Onemi: bant disi bir sayi, sayiyi degistirmeye degil hata
ayiklamaya goturdu.

**Dogrulama listesi kontrolu.** Bekledigim: listeyi yeniden uretip karsilastirmak
gecerliligini gosterir. Cikan: 30 / 30 ayni - ama bu yalnizca ureticinin deterministik
oldugunu soyluyordu. Ureticiden bagimsiz bir kontrol yazilinca etiketsiz satirlarin
ikisinin aslinda ayni duzeltme tarafindan baska bir hunk'ta suclandigi gorundu. Onemi:
bir kontrol, kontrol ettigi kodu kullanirsa yalnizca tekrarlanabilirligi olcuyor.

**Carpimsal maliyet modeli.** Bekledigim: sure / duzeltme / commit katsayisi Polly ve
ShareX'te %4 farkla ayni ciktigi icin Jellyfin'e uzatilabilir. Cikan: Jellyfin'in
katsayisi 6,00e-5, digerlerinin %41'i; tahmin 2,41 kat fazla. Onemi: iki noktadan
cikarilan model ucuncu noktada curudu.

**Squash-merge.** Bekledigim: "birlestirme commit'i" olcutu mesaj basligindan
uygulanabilir. Cikan: dogrulama listesindeki bir satirin suclanan commit'inin basligi
`Merge pull request #7941 from jellyfin/fix-overflow` ama commit tek ebeveynli. Onemi:
olcut ebeveyn sayisina bakmali; mesaja bakan bir siniflandirma bu satiri yanlis
isaretlerdi.

## 4. Alinan kararlar

**ADR 0011 - git madenciligi (`e72dfda`).** Birlestirme commit'leri veriden cikariliyor
(kendi diff'leri yok, ebeveyn secimine gore degisiyor) ama sayilip raporlaniyor. Kimlik
eposta, tarih yazar tarihi ve UTC. Cikti JSONL, cunku tek dizi yazan taraf butun tarihi
bellege alirdi.

**ADR 0012 - veri modeli (`b500747`).** Dort tablo; ham veri ile turetilmis veri ayri,
cunku bir olcunun tanimi degisince o tablo silinip yeniden hesaplanabilmeli. Uc indeks
Adim 5'in sorgularina gore secildi. Baglanti dizesi koda yazilmiyor. Yazma idempotent.

**ADR 0013 - metrikler (`6b35532`).** Hesaplama veritabanindan, git'e tekrar
gidilmiyor (3,95 sn'ye karsi 0,61 sn). Her olcu yalnizca o commit'ten onceki veriyle
hesaplaniyor; commit'ler tarih sirasinda isleniyor ve durum commit islendikten sonra
guncelleniyor.

**ADR 0014 - SZZ (`ad06c91`).** Temel SZZ, uzerine iki suzgec: zaman tutarliligi ve
bosluk duyarsizligi. 50'den fazla dosyaya dokunan duzeltmeler atlaniyor, yalnizca `.cs`
bakiliyor, birlestirme commit'leri suclanmiyor.

**ADR 0015 - satir bazli susturma (`1028c75`).** Gerekce zorunlu, susturmalar sayiliyor
ve `--json` ciktisinda listeleniyor, dosya geneli susturma yok. SV007 kapatilirsa ozete
uyari dusuyor.

## 5. Bilerek birakilan sinirliliklar

- **Blame ad degisimini takip edemiyor.** Kutuphanede tek strateji var. Maruziyet:
  duzeltmelerin dokundugu 407 / 625 `.cs` dosyasi (%65) tarihinin bir yerinde ad
  degistirmis. Kac etiketin bu yuzden yanlis oldugu olculmedi.
- **`-w` esdegerligi gosterilemedi.** Bizim suzgecimiz `git blame -w` ile 762 / 2243
  (%34,0) satirda ayni sonucu veriyor; `-w` git'in kendi cevabini 1428 / 2243 (%63,7)
  satirda degistiriyor.
- **Belirsiz kovasi iki durumu ayirmiyor.** 5 Belirsiz'in hepsi once bos birakilmis
  satirlardan geldi; "veri yetmedi" ile "karar verilmedi" ayni kovada.
- **%100 dar ornekten geliyor.** Dogru suclama orani 12 / 12; payda 12 ve guven araligi
  hesaplanmadi.
- **Bagimsiz ikinci degerlendirici kullanilmadi.** Siniflandirmayi tek kisi yapti.
- **Kararsiz test mekanizmasi sinanmadi.** CI artik test sonuclarini artefakt olarak
  sakliyor ama bu, gercek bir dususte henuz kullanilmadi; bir kosuda dusen test bir daha
  dusmedi ve hangisi oldugu bilinmiyor.

## 6. Asama 5'e tasinan acik sorular

- **Kacirma orani 11 / 13 (%84,6).** Etiketsiz orneklemin buyuk kismi "suclanmaliydi"
  diye isaretlendi. Egitim verisinin pozitif sinifi eksikse model eksik ogrenir; bu
  oranin daha genis bir orneklemle olculmesi gerekiyor.
- **Bot commit'lerinin haric tutulmasi.** Polly'de commit'lerin %31'i bot bayrakli,
  metrigi degisen bot disi commit orani %19,5. Karar Asama 5'e birakildi.
- **Ad degisimi esigi.** Esik %50 ve git'in varsayilani; 1234 / 2759 commit'in metrikleri
  bu secime bagli. Esigin dogrulugu olculmedi.
- **Olcut dosyasinda Belirsiz ikiye ayrilmali:** "veri karar vermeye yetmiyor" ile
  "degerlendirici karar vermedi" ayri kovalar olmali.

## Tez icin kullanilabilecek bulgular

- Bir olcum, urun kodunda test edilemeyen bir hatayi buldu: blame satir numaralarinda bir
  kayma, 355 test gormemisti. Test verisinin tekduze olmasi (dosyalarin butun satirlari
  tek commit'ten) hatayi gizliyordu.
- SZZ etiket orani repoya gore degisiyor: 261 / 2759, 1013 / 8490, 4688 / 22 917. Tek
  repoda olculen bir oran yontemin ozelligi degil, o reponun ozelligi olabiliyor.
- `IsFix` gibi bir metin heuristiginin recall'u repoya gore iki katina cikabiliyor:
  ShareX'te tam kelime 642, kok eslesmesi 1341.
- Blame uygulamalari birbirinden ayrilabiliyor: iki blame ayni dosyada 2243 satirin
  53'unde farkli commit sucluyor; `-w` secenegi farki 1428 satira cikariyor.
- Kaynak maliyeti katmanlara gore uc mertebe degisiyor: 0,61 sn'den 5456 sn'ye.

## Mulakatta anlatilabilecek hikayeler

- Beklenen araligin disina dusen bir sayiyi duzeltmek yerine sebebini aramak: %9,9'luk
  etiket orani once bir sayac hatasini (111 yerine 176), sonra reponun yapisini ortaya
  cikardi.
- Bir kontrolun, kontrol ettigi kodu kullanmasi: liste 30 / 30 "ayni" cikmisti, bagimsiz
  kontrol iki yanlis satir buldu.
- Iki veri noktasindan model kurmanin bedeli: katsayilar %4 farkla ortusuyordu, ucuncu
  repoda tahmin 2,41 kat sasti.
- Aracin kendi kuralinin kendi kodunda yanlis pozitif uretmesi ve bunun satir bazli
  susturma mekanizmasini dogurmasi; susturmalar sayilir, gerekcesiz susturma bulgudur.
- Ayni harfin iki kumede zit anlam tasimasi: etiketsiz satirlarda `E` "arac kacirdi"
  demekti, karar degerleri ayrildi.
