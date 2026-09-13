# PDF kutuphanesi ve lisans karari

**Tarih:** 2026-09-13
**Durum:** Paket eklenmeden once yazildi.

## Secim

**QuestPDF 2026.8.0.** Surum `Directory.Packages.props` icinde sabit.

## Neden tarayici ya da HTML-to-PDF degil

Once neyi **yapmadigimi** yazayim, cunku en kolay yol oydu:

- Chromium'u alt surec olarak calistirip sayfayi PDF'e basmak.
- wkhtmltopdf ya da LibreOffice cagirmak.
- Bir SaaS servisine gonderip PDF almak.

Ucu de disaridan bir program ya da servis gerektiriyor. Arac zaten "internet olmadan
calissin" diye kuruluyor; demo makinesinde Chromium olup olmadigini bilmiyorum ve bir
SaaS'a rapor gondermek, analiz edilen deponun ozetini disari cikarmak demek. Ayrica
tarayici surumu degistiginde ciktinin da degismesi, "ayni girdi ayni cikti" iddiasini
zayiflatirdi.

QuestPDF ise kod icinde calisan bir kutuphane: dis surec yok, ag yok.

## Lisans

QuestPDF **cift lisansli**. Paketin icindeki `LICENSE.md` (50 145 bayt) uc metin
tasiyor: secim kilavuzu, Community lisansi ve Professional/Enterprise lisansi.
Paketin `nuspec`'inde lisans `<license type="file">LICENSE.md</license>` olarak
tanimli; SPDX ifadesi yok, o yuzden metnin kendisi okundu.

Community lisansina uygunluk kosullari (metnin "Community License Eligibility"
bolumu, yururluk 6 Temmuz 2026, surum 3.0). Bu proje **uc ayri maddeden** uygun:

| Madde | Metin | Bu proje |
|---|---|---|
| 1 - Bireysel kullanim | yillik geliri esigin altinda olan bireyin kisisel projesi | Proje bir ogrencinin tez calismasi, sirket projesi degil |
| 2 - Ogrenme ve degerlendirme | ogrenme, degerlendirme ya da egitim amacli bireysel kullanim | Projenin amaci tam olarak bu |
| 5 - Acik kaynak projeler | OSI onayli bir lisansla dagitilan acik kaynak proje | Deponun kokunde MIT lisansi var |

Uygun **olmayan** kategoriler de metinde yaziyor: kamu kurumlari, halka acik sirketler
ve yillik geliri 1 000 000 USD ustundeki kuruluslar. Bu proje bunlarin hicbiri degil.

Uygunluk degisirse metin 90 gunluk bir gecis suresi taniyor. Bu proje ticari bir urune
donuserse karar yeniden verilmeli; o yuzden bu dosya duruyor.

Kodda lisans secimi **acikca** yapiliyor:
`QuestPDF.Settings.License = LicenseType.Community;` - varsayilana guvenilmiyor.

## Font

QuestPDF varsayilan fontu **Lato** ve font dosyalari **paketin icinde** geliyor
(`LatoFont/` altinda 18 ttf). Paket ayni klasorde `LatoFont/OFL.txt` dosyasini da
tasiyor: SIL Open Font License.

Sonuc: **repoya font dosyasi eklenmiyor.** Lisansi belirsiz bir sistem klasorunden font
kopyalanmiyor. Turkce karakterler (ı, İ, ş, ğ, ç, ö, ü) Lato'da var; bu ayrica testle
ve PDF metin cikartmasiyla dogrulaniyor.

## Bedeli

Paket **47 MB**: icinde butun isletim sistemleri icin Skia yerel kutuphaneleri var. Bu
deponun en buyuk bagimliligi. Kabul ediliyor, cunku alternatifi disaridan bir program
cagirmak.

QuestPDF ayrica kendi bagimlilik lisanslarini `ExternalDependencyLicenses/` altinda
tasiyor (skia, harfbuzz, libpng, zlib ve digerleri).

## Bu turda eklenmeyenler

- Ikinci bir PDF paketi yok.
- Grafik kutuphanesi yok; zaman cizelgesi PDF icinde vektor olarak ciziliyor.
- HTML-to-PDF ya da tarayici otomasyonu paketi yok.
