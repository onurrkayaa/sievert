# 0027 - Denetlenebilir PDF raporu ve demo ortami

**Baglam:** Adim 5'ten sonra panel calisiyordu ama iki sey eksikti. Birincisi: ekrandaki
sonucu disari cikarmanin yolu yoktu; ekran goruntusu almak sayinin nereden geldigini
tasimaz. Ikincisi: araci baska bir makinede gostermek icin PostgreSQL kurmak, veri
madenciligi kosturmak ve model yuklemek gerekiyordu - jury onunde yapilacak bir sey degil.

Bu ADR iki teslimatin kararlarini tutuyor: **kaynagi izlenebilir bir PDF raporu** ve
**tek komutla acilan bir demo ortami**.

## PDF kutuphanesi ve lisans

**QuestPDF 2026.8.0**, surum `Directory.Packages.props` icinde sabit.

Kutuphane cift lisansli. Bu proje Community lisansina uc ayri maddeden uygun: bireysel
kullanim, ogrenme/degerlendirme ve OSI onayli acik kaynak (depo MIT). Uygunluk kanidi
paketin kendi `LICENSE.md` metninden okundu ve
`docs/urun/pdf-kutuphanesi-ve-lisans.md` icinde yaziyor. Lisans secimi kodda **acik**:
`QuestPDF.Settings.License = LicenseType.Community`.

Font **Lato** ve paketin icinde geliyor; lisansi da (SIL OFL) ayni pakette. Repoya font
dosyasi eklenmedi ve lisansi belirsiz bir sistem klasorunden font kopyalanmadi.

Bedeli: paket 47 MB, deponun en buyuk bagimliligi.

## Neden tarayici ya da HTML-to-PDF degil

Chromium'u alt surec olarak calistirmak, wkhtmltopdf cagirmak ya da bir SaaS'a gondermek
daha kolaydi. Ucu de reddedildi:

- **Dis program bagimliligi.** Demo makinesinde Chromium olup olmadigini bilmiyorum.
- **Ag bagimliligi.** Bir SaaS'a rapor gondermek, analiz edilen deponun ozetini disari
  cikarmak demek.
- **Tekrarlanabilirlik.** Tarayici surumu degistiginde cikti da degisirdi; "ayni girdi
  ayni cikti" iddiasi zayiflardi.

## Neden arka plan isi

Rapor uretimi HTTP istegi icinde degil, kuyruga giren bir iste kosuyor. Kuyruk, iptal ve
yeniden baslatma kurtarmasi zaten Adim 3'te kurulmustu; ikinci bir yol acmak yerine ayni
yol kullanildi. Olcum bunun bedelinin dusuk oldugunu gosterdi: istek 6-10 ms'de donuyor,
rapor 80-120 ms'de hazir oluyor ve rapor uretilirken saglik ucunun ortancasi 3 ms.

**Aktif is anahtari** rapor kimligini iceriyor (`{repo}:report-generate:{reportId}`), yani
ayni depo icin iki farkli rapor istegi birbirini engellemiyor. Sinirsiz paralel uretimi
engelleyen sey worker es zamanliligi (varsayilan 1), kuyruk anahtari degil.

## Neden canonical manifest

Bir PDF elden ele dolasir ve uretildigi veriden kopar. Manifest, raporun **neyden**
uretildigini tek bir metinde topluyor: depo, is, model, parametreler, kanit ozetleri.

Manifest canonical: UTF-8, BOM yok, LF, sabit alan sirasi, degismez kultur, UTC.
**Uretim zamani manifeste dahil degil** - olsaydi ayni girdi iki farkli ozet verirdi ve
"ayni girdi ayni cikti" olculemezdi. Uretim zamani artefakt kaydinda, manifestin disinda.

## Neden PDF kendi ozetini icermiyor

Dosyanin SHA-256'si son halinden hesaplaniyor; ozeti dosyanin icine yazmak dosyayi
degistirir ve ozet artik tutmaz. Bu dongusel probleme girilmedi: PDF, ozetin indirme
metadata'sinda oldugunu **yaziyor**, ozet de artefakt ucunda ve indirme cevabinin `ETag`
basliginda duruyor.

## Artefakt yazimi ve bozulma

Dosya once `.sievert-tmp` uzantisiyla yaziliyor, ozeti hesaplaniyor, sonra atomik olarak
yerine tasiniyor. Yari yazilmis bir dosya hicbir zaman nihai adiyla gorunmuyor.

Depolama anahtari **yalniz uygulama** tarafindan uretiliyor ve kullanicinin verdigi
hicbir metinden turemiyor; dosya adi ayri bir alan ve yalniz gosterimde kullaniliyor.
Iki katmanli kontrol var: anahtarin bicimi ve cozulen yolun kokun altinda kalmasi.

Indirmeden once boyut ve ozet yeniden dogrulaniyor. Uymazsa kayit `corrupted` oluyor ve
dosya **gonderilmiyor**; akis basladiktan sonra `ProblemDetails` donulemeyecegi icin
kontrol akistan once. Olcumde bir bayt degistirildi: indirme 500 ve
`REPORT_ARTIFACT_CORRUPTED` dondu.

## Kismi rapor politikasi

Varsayilan: kismi bir isten rapor **uretilmiyor** (422). Kullanici acikca isterse
uretiliyor ve kapakta buyuk bir uyari, yonetici ozetinde, her toplu sayida ve
sinirliliklarda tekrar ediyor; dosya adinda `-partial` var.

Sebep: kismi bir isin "en riskli 20 commit"i, tam isin en riskli 20 commit'i degildir.

## Statik ve model ayrimi

Rapor statik bulgulari gosteriyor ama **model skoruna dahil etmiyor** ve bunu uc ayri
yerde yaziyor: kapak, dosya bolumu ve statik bolumu. Statik analiz verilmezse
"calistirilmadi / rapora eklenmedi" yaziyor; **0 bulgu gibi sunulmuyor**.

## Rapor bolumleri

Kapak, yonetici ozeti, risk sozlesmesi, zaman cizelgesi, dosya etkinligi, commit listesi,
statik bulgular, model aciklamasi, sinirliliklar, kaynak. Sira rastgele degil: okuyan
kisi sayilari gormeden once neyin ne oldugunu okuyor.

Zaman cizelgesi PDF icinde **vektor** (SVG) olarak ciziliyor, ekran goruntusu gomulmuyor.
Cizim kodu API tarafinda ayri duruyor cunku API panele referans veremez (ADR 0025);
ikisinin ayni sayiyi gostermesi, ikisinin de ayni uctan beslenmesiyle ve bagimsiz
dogrulamayla saglaniyor.

## Turkce karakterler ve kultur

Rapor iki kulturde uretilebiliyor (`tr-TR`, `en-US`). Sayi bicimleri kulturden geliyor.
Bir kusur tam buradan cikti: cizelgedeki esik etiketleri degismez kulturle yaziliyordu ve
ayni sayfada "93,3" ile "93.3" birlikte gorunuyordu. Duzeltildi; koordinatlar degismez
kulturde kaldi cunku oradaki virgul SVG'yi bozar.

## Neden gercek Polly alt kumesi

Demo verisi uydurulmadi. Uydurulmus veri, modelin ne yaptigini degil ne yaptigini
sandigimizi gosterir.

**Secim kurali sonuc gorulmeden sabitlendi:** kaynak deponun tarih sirasinda en yeni 200
commit'i. Risk dagilimina bakarak commit secilmedi.

Kuralin bedeli de kayitli: bu 200 commit'te **SZZ pozitifi sifir** ve 172'si bot
commit'i. Sebep tanimdan geliyor (sag sansurleme: en yeni commit'lerin duzeltmesi henuz
yazilmamis). Kural bu yuzden degistirilmedi; demo modelin ne kadar iyi oldugunu degil,
aracin nasil calistigini gosteriyor.

## PII azaltma

Seed'de yazar adi, e-posta, yerel yol ve baglanti dizesi **yok**; importer da
veritabaninda bos birakiyor ve `LocalPath` yazmiyor. Testler bunu her kosuda ariyor.

## Neden gecici PostgreSQL container'i

Demo kendi veritabanini aciyor ve cikista siliyor. Mevcut veritabanina yazsaydi
kullanicinin gercek verisiyle demo verisi karisirdi. Testcontainers zaten testlerde
kullaniliyor; ikinci bir orkestrasyon paketi eklenmedi.

Demo deposu `IsDemoData` isaretiyle geliyor ve panelde gorunur bir kutu cikariyor. Bu
bayrak **yalniz kaynak sunumu**: skoru, siralamayi ya da API sonucunu degistirmiyor.

## Tek komut orkestratoru ve temizlik

Arac depo kokunu `Sievert.slnx` ile buluyor, onkosullari kontrol ediyor ve eksikse tek
cumleyle soyluyor. Portlar verilmezse bos loopback portu seciliyor; disa acik dinleme yok.

Kapanista sira onemli: once panel (API'ye bagli), sonra API, en son container. Kapatma
isareti olarak yalniz Ctrl+C dinlenmiyor; `SIGTERM` ve `SIGQUIT` de yakalaniyor, cunku
arac cogu zaman `dotnet run` altinda kosuyor ve disaridan gelen sonlandirma once
sarmalayiciya gidiyor. Ilk denemede tam bu yuzden bir container ortada kalmisti.

## Demo verisi canli analiz degil

Panelde, demo senaryosunda ve bu ADR'de ayni cumle duruyor: demo verisi kamuya acik bir
deponun sabit alt kumesi, canli bir depo analizi degil. Demo ekraninda gorulen sayilar
Asama 5'in olcum sonuclari yerine gecmez.

## Bilinen sinirlar

- Rapor uretimi tek bir kutuphane cagrisinda bitiyor; **o cagrinin ortasinda iptal
  edilemiyor**. Iptal asamalar arasinda kontrol ediliyor ve iptal edilen bir uretimin
  dosyasi diske yazilmiyor.
- Es zamanli iki rapor istegi olculmedi (worker es zamanliligi 1).
- 20 MB sinirina yaklasan bir rapor uretilmedi.
- Otomatik silme yok: uretilen dosyalar `data/runtime/reports` altinda birikiyor.
- Ctrl+C temizligi gercek bir terminalde dogrulanamadi (arac test ortaminda arka planda
  kosuyor ve arka plan islerinde SIGINT yok sayiliyor); ayni kod yolundan gecen SIGTERM
  ile dogrulandi.
