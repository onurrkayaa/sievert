# 0014 - SZZ ile hata getiren commit'lerin etiketlenmesi

**Baglam:** Asama 5'in modeli "bu commit hata getirir mi" sorusunu ogrenecek. Bunun icin
gecmisteki commit'lerin hangilerinin hata getirdigini soyleyen bir etikete ihtiyac var.
Elimizde boyle bir kayit yok: Polly'nin hata takip sistemi kapali bir veri ve zaten
commit'lerle baglanmis degil.

## SZZ nedir

SZZ, yazarlarinin bas harflerinden gelen bir yontem (Sliwerski, Zimmermann, Zeller,
2005). Fikri tek cumleyle: **bir hatayi duzelten commit'in degistirdigi satirlari en son
kim yazdiysa, hatayi o getirmistir.**

Uygulanan adimlar:

1. `IsFix` olan her commit icin, ebeveyniyle arasindaki diff alinir.
2. Degisen `.cs` dosyalarinda **silinen** satirlarin ebeveyn surumundeki satir numaralari
   cikarilir.
3. O dosya ebeveyn surumunde blame edilir; her satirin hangi commit'ten geldigi bulunur.
4. Bulunan commit `IsBugIntroducing = true`, `LabelSource = "szz"` diye isaretlenir.

Eklenen satirlar suclanmaz: eklenen bir satirin gecmisi yoktur. Bu, SZZ'nin en bilinen
sinirlarindan biri - eksik kod yuzunden cikan hatalar (bir kontrol hic yazilmamis) hicbir
commit'i suclamiyor.

## Hangi varyant

Literaturde bircok SZZ varyanti var (B-SZZ, AG-SZZ, MA-SZZ, RA-SZZ...). Buradaki
**temel SZZ (B-SZZ)**, uzerine iki suzgec eklenmis hâli: zaman tutarliligi ve bosluk
duyarsizligi. Daha gelismis varyantlar (ornegin degisiklik turune gore ayirma, yeniden
duzenleme tespiti) uygulanmadi; onlar semantik cozumleme ya da ek arac istiyor ve bu
adimda amac calisan bir temel elde etmekti.

## Ilan edilmis kararlar

Bunlar olcumden **once** yazildi ve oldugu gibi uygulandi.

**50'den fazla dosyaya dokunan duzeltmeler atlaniyor.** Buyuk yeniden duzenlemeler,
hatayla ilgisi olmayan yuzlerce satiri da degistiriyor ve o satirlarin yazarlarini
suclamak gurultu uretiyor. Polly'de bu esige takilan 4 commit var. Sinir "50'den fazla",
yani tam 50 dosyaya dokunan commit isleniyor; bir sinir testi bunu siniyor.

**Sadece `.cs` dosyalari.** Arac C# icin. Bunun bedeli olculdu ve buyuk cikti:
duzeltmelerin %31'i (88 commit) hic `.cs` degistirmiyor, yani hicbir sey etiketlemiyor.

**Duzeltmeden sonra yazilmis gorunen suclamalar atiliyor.** Git'te yazar tarihi serbestce
verilebiliyor; rebase, cherry-pick ve elle ayarlanmis tarihler yuzunden suclanan commit
duzeltmeden yeni gorunebiliyor. Boyle bir etiket anlamsiz. Polly'de 5 tane cikti.

**Bir commit hem duzeltme hem hata getiren olabilir.** `CommitMetrics.IsFix` ile
`Commits.IsBugIntroducing` ayri alanlar. Bir duzeltmenin yeni bir hata getirmesi cok
siradan bir sey.

**Birlestirme commit'leri suclanmaz.** Kendi diff'leri yok; getirdikleri satirlar baska
bir dalda yazilmis. Polly'de 11 suclama bu yuzden atildi.

## Bosluk duyarliligi olculdu

Bu adimin en buyuk riski buydu: girinti ve bicimlendirme degisiklikleri blame'de masum
commit'leri sucluyor. Tahmin etmek yerine olculdu.

| | Blame'e sorulan satir | Etiket |
|---|---|---|
| Bosluk yok sayilarak (varsayilan) | 5102 | 272 |
| Bosluk sayilarak | 6384 | 286 |

Bosluk degisiklikleri sayilinca %25 daha fazla satir blame'e gidiyor ve **14 fazla etiket**
cikiyor. Ters yonde kayip yok: bosluk yok sayilinca kaybolan etiket 0. Davranis bosluk
yok sayacak sekilde birakildi.

Onemli bir ayrinti: bu filtre blame'in kendi ayari **degil**. LibGit2Sharp'in
`BlameOptions` sinifinda bosluk secenegi yok (`Strategy`, `StartingAt`, `StoppingAt`,
`MinLine`, `MaxLine` var, hepsi bu). Onun yerine blame'e hangi satirlarin sorulacagi
suzuluyor: tamamen bos satirlar ve kirpilmis hâli ayni hunk'ta eklenen satirlarda
bulunanlar atlaniyor. `git blame -w` ile ayni sey degil - o, blame'in kendi eslestirmesini
bosluga duyarsiz yapiyor - ama ayni sorunu hedefliyor. Farkin olculmedigini yazmak lazim.

## Ad degisimi

LibGit2Sharp'in `BlameStrategy` enum'unda tek deger var: `Default`. Kopya ve ad degisimi
takibi kutuphanede tanimli ama kullanilamiyor. Yani blame, bir dosyanin gecmisini adi
degistigi noktada birakiyor.

Maruziyet olculdu: duzeltmelerin dokundugu 625 farkli `.cs` dosyasindan **407'si (%65)**
tarihinin bir yerinde ad degistirmis. Bu dosyalarda suclama, ad degisimini yapan
commit'te duruyor. Kac etiketin bu yuzden yanlis oldugu olculmedi; bunun icin etiketlerin
elle dogrulanmasi gerekir. Davranis degistirilmedi.

## Etiket orani beklenenin altinda cikti ve sebebi arandi

Olcumden once "literaturde %10-30" diye yazilmisti. Cikan sayi %9,9, yani bandin altinda.
Tanimi degistirip sayiyi bandin icine cekmedim; sebebini aradim ve huni
`docs/olcumler/asama4-szz.md` dosyasinda duruyor. Ozetle: en buyuk kayip `.cs`
dokunmayan duzeltmeler (%31) ve etiket oraninin `IsFix` oranina (%10,6) bagli olmasi.

## Idempotent

Etiketleme once deponun butun etiketlerini temizliyor, sonra yenilerini yaziyor. Ayni
girdiden ayni sonuc cikiyor ve onceki kosudan kalan etiket birikmiyor. Uc test bunu
siniyor: ayni depoda iki kez calistirma, bos listeyle temizleme ve Mining tarafinda ayni
commit icin iki kez blame.

## Olcum ve kontrol araclari

SZZ'nin ciktisini denetleyen araclar `tools/` altinda ve urunun parcasi degil:
`Sievert.Measure` icindeki `blame-w`, `dogrulama`, `satir-kontrol` ve `sizinti` modlari.

Bunlarin arasinda **`tools/liste-kontrol.py` Python**, deponun geri kalani C# oldugu hâlde.
Sebebi isin kendisi: uretilen markdown listesini ayristirip her satir icin `git` cagirmak.
Tek kullanimlik bir denetim araci, urun kodu degil, hicbir sey ona bagimli degil ve
derlemeye girmiyor. Ayni isi C# ile yazmak duzenli ifade ve surec cagirma kodunu iki kat
uzatirdi. Baska bir Python dosyasi eklemeyi planlamiyorum.

## Bilinen kusurlar

- **Temel varsayim her zaman dogru degil.** "Duzeltmenin dokundugu satir hatanin
  kaynagidir" cumlesi cok sey varsayiyor. Hata baska bir yerdeki bir degisiklikten
  cikmis olabilir, duzeltme semptomu duzeltiyor olabilir, ya da sorun hic yazilmamis bir
  kod olabilir - o zaman suclanacak satir yok.
- **`IsFix` bir heuristik** (ADR 0013) ve SZZ'nin girdisi o. `IsFix` yanlissa etiket de
  yanlis; olculen precision %75 civari (`asama4-isfix-recall.md`).
- **Etiketler dogrulanmadi.** 272 etiketten hicbiri kaynak koda bakilarak kontrol
  edilmedi. Bu adimda uretilen sey bir etiket kumesi, dogrulanmis bir gercek degil.

**Sonuc:** `sievert label <repo-adi>` duzeltme commit'lerinden geriye dogru blame
calistirip hata getiren commit'leri isaretliyor. Polly'de 2759 commit'in 272'si (%9,9)
etiketlendi. Asama 5'in hedef degiskeni bu sutun olacak.
