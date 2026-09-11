# 0015 - Satir bazli muafiyet: // sievert:disable

**Baglam:** Aracin yapilandirmasi bir kurali tumden acip kapatabiliyordu (ADR 0009), arada
hicbir sey yoktu. Tek bir satirda yanlis pozitif cikinca secenekler sunlardi: kurali butun
repo icin kapat, dosyayi `--exclude` ile taramadan cikar, ya da kodu degistir.

Bu, kendi kodumuzda yasandi. SV004, `RepositoryMiner` icinde dongu govdesindeki
`commit.Parents.Count()` cagrisini "dongu icinde sorgu" sandi. `Parents` bellekteki bir
koleksiyon, yani bulgu yanlis pozitif. CI'in varsayilan kume adimi bulgu gorunce kirildigi
icin sayimi ayri bir metoda aldim ve bulgu kayboldu.

**O degisikligin sebebi kuralin ikna olmasi degildi, CI'in kirilmasiydi.** Degisiklik
yerinde duruyor ve geri alinmadi, cunku gercek bir faydasi da var: `Parents` tembel
numaralandiriliyor ve ayni sayim commit basina iki kez yapiliyordu, o mukerrer sayim
gercekten kalkti. Ama kaybolma sebebi bu degil; cagrinin dongu govdesinden cikmasi.
Arac kendi yanlis pozitifini bize kod degistirterek "cozdurdu" ve bu iyi bir sey degil.

**Karar:** Tek satirlik susturma yorumu.

```csharp
// sievert:disable SV004 sayilar bellekte, veritabani sorgusu degil
_ = sayilar.Any();
```

## Gerekce neden zorunlu

Gerekcesiz susturma **gecersiz**: bulguyu elemez ve kendisi bir bulgu uretir (SV007,
"gerekcesiz susturma"). Susturma bir karardir; bir aydan sonra o satira bakan kisinin
bilmesi gereken sey, bulgunun neden gormezden gelindigi. Gerekce opsiyonel olsaydi
pratikte hic yazilmazdi ve dosyada birikip anlamsizlasan bir isaret olurdu.

Gerekcesiz susturmanin sessizce yok sayilmasi da yeterli degildi: o zaman yazan kisi
bulgunun susturuldugunu sanir, oysa susturulmamistir. Onun icin gorunur bir bulgu.

## Neden sayiliyor

Susturulan her bulgu sebebiyle birlikte kaydediliyor, `check --json` ciktisinda
`suppressions` alaninda veriliyor ve ekran ozetinde "Susturulan : N" satiri var. Bu,
muafiyetlerde (ADR 0008) verilen kararin aynisi: bir kuralin ciktisini cogu zaman
istisnasi belirliyor ve "neden listede yok" sorusu cevaplanabilmeli.

Sayilmasaydi susturma gizli bir kolaylik olurdu: bulgu sayisi duserdi ve bunun sebebinin
kodun duzelmesi mi yoksa susturmanin artmasi mi oldugu anlasilmazdi. Olculebilir olmasi,
ileride "su repoda 40 bulgu susturulmus" diye bakilabilmesi demek.

**Muafiyet ile susturma ayni sey degil ve ayri duruyorlar.** Muafiyet kuralin kendi
karari: kural "bu metot zaten olay isleyicisi" deyip gecer. Susturma ise kullanicinin
karari: kural bulgusunda israr ediyor, kullanici katilmiyor. Ikisini ayni listede
toplasaydim, kuralin kendi elemeleriyle insanin elemeleri birbirine karisirdi.

## Neden dosya geneli susturma yok

Susturma yalnizca **bir sonraki satiri** etkiliyor. Dosya ya da blok geneli susturma
bilerek yok.

Dosya geneli bir susturma, yazildigi gun dogru olur ve sonra unutulur. Dosya buyur, yeni
kod eklenir ve o kodun urettigi bulgular da sessizce elenir - kimse fark etmez, cunku
susturmayi yazan kisi o satirlari hic gormemistir. Tek satirlik susturma ise her yeni
bulgu icin yeniden karar vermeyi zorunlu kilar.

Bunun bir bedeli var: ayni yanlis pozitif bir dosyada on kez cikiyorsa on ayri yorum
yazmak gerekir. Bu bedeli bilerek kabul ediyorum; on kez tekrar eden bir yanlis pozitif
zaten kuralin duzeltilmesi gereken bir sey oldugunu soyluyor, susturulmasi gereken degil.

Yorumun kendi satiriyla ilgili bir ayrinti: susturma her zaman yorumun **bir altindaki**
satira bakiyor, kodun sonundaki yorumlar (`_ = x.Any(); // sievert:disable ...`) bir sey
susturmuyor. Iki ayri kural olsaydi hangisinin gecerli oldugunu tahmin etmek gerekirdi.

## Kural kodu zorunlu

`// sievert:disable` tek basina hicbir sey susturmuyor; kural kodu yazilmak zorunda.
Kodsuz bir susturmayi "hepsini sustur" diye yorumlasaydim, o satirda ileride cikacak
bambaska bir kuralin bulgusu da elenirdi.

## SV007 kapatilabilir mi

SV007 katalogda duruyor, yani teknik olarak `sievert.json` ile kapatilabilir ve bunu
engellemiyorum. Ama sessiz de birakmiyorum: kapatildiginda hem ekran ozetine hem
`--json` ciktisindaki `summary.notices` alanina su satir dusuyor:

> SV007 kapali - susturmalar denetlenmiyor, gerekcesiz susturmalar bulgu uretmiyor.

Engellemek yerine gorunur kilmanin sebebi, aracin geri kalaninda verilen kararla ayni:
bir seyin kapali oldugunu bilmeden "temiz" raporu okumak, kapatabilmekten daha tehlikeli.
Kapali kural zaten "Kapali kural" satirinda gorunuyordu, ama orada SV007 digerleriyle ayni
gorunurdu; oysa bu kural kapaninca sadece kendi bulgulari degil, BASKA kurallarin
susturulmasini denetleyen mekanizma da devre disi kaliyor.

## Ilk kullanicisi kendi kodumuz oldu

Mekanizmayi yazar yazmaz `RuleRunner.Apply` icindeki `valid.FirstOrDefault(...)` cagrisi
SV004 bulgusu uretti - `valid` bellekteki kisa bir liste, yani yine yanlis pozitif. Bu
sefer kodu degistirmedim, gerekcesiyle susturdum. Ozet artik "Bulgu 0, Susturulan 1"
diyor; yani bulgu kaybolmadi, gormezden gelindigi yazili.

**Sonuc:** Yanlis pozitifi olan tek bir satir, kurali repo genelinde kapatmadan ve kodu
kurala uydurmak icin bozmadan gecilebiliyor. Bedeli her seferinde bir cumle yazmak ve o
cumlenin sayilmasi.
