# 0009 - Kural katalogu ve sievert.json

**Baglam:** Kural listesi `CommandRunner` icinde `new RuleRunner([new AsyncVoidRule()])` diye sabit duruyordu. Ikinci kurali yazdigimda oraya elle eklemem gerekecekti, ve kullanicinin bir kurali kapatmasinin ya da seviyesini degistirmesinin hicbir yolu yoktu. CI'da bir kural gurultu yapmaya baslasa tek care onu koddan silmekti.

**Karar:** Iki parca:

1. `RuleCatalog` - kod ile kural sinifi arasindaki tek eslesme yeri. Yeni kural yazilinca sadece buraya ekleniyor, CLI'da ikinci bir liste yok.
2. `sievert.json` - opsiyonel yapilandirma dosyasi. Taranan kokte araniyor, `--config <yol>` ile baska bir yer verilebiliyor.

```json
{
  "rules": [
    { "code": "SV001", "enabled": true },
    { "code": "SV002", "enabled": true, "severity": "error" }
  ],
  "exclude": ["samples/**"]
}
```

**Yapilandirma ne yapar, ne yapmaz:** JSON sadece kural **acar/kapatir** ve bulgularin **seviyesini ezer**. Kuralin ne aradigi kodda kalir. Bu siniri bilerek koydum.

Bir kuralin mantigini JSON'a tasimak ilk bakista cazip: "async void ara" gibi seyler yapilandirilabilir olsa arac daha esnek gorunurdu. Ama o yol sonunda JSON icine gomulu bir kural dili yazmaya cikiyor, ve o dilin testi, hata mesajlari, surum uyumu derdi kuralin kendisinden buyuk oluyor. Kurallar C# ve Roslyn ile yaziliyor; orada tip guvenligi, hata ayiklama ve birim testi zaten var. Yapilandirmanin isi, yazilmis bir kuralin bu repoda gecerli olup olmadigina karar vermek.

Pratik sonucu su: seviye ezmesi kuralin kendi urettigi bulguya sonradan yaziliyor (`RuleSelection.ApplySeverity`). Kural kodunun icinde "yapilandirma" diye bir kavram yok, kural her zaman kendi dogal seviyesini uretiyor. Ezme `--fail-on` karsilastirmasindan once uygulaniyor, yani `severity: "info"` yazmak bulgunun build'i kirmasini gercekten engelliyor.

**Bilinmeyen kural kodu neden hata:** `SV001` yerine `SV01` yazan biri, kurali kapattigini sanip aslinda hicbir sey yapmamis olur; ya da acik biraktigini sanip kapali birakir. Ikisinde de arac sessizce yanlis is yapar ve kimse fark etmez. O yuzden tanimadigim bir kod gorunce cikis kodu 2 ile duruyorum ve mesajda hem yanlis kodu hem tanidigim kodlari yaziyorum. Ayni sebeple JSON'daki tanimadigim alanlar da hata: `exclude` yerine `excludes` yazan biri hicbir seyin dislanmadigini fark etmezdi.

**Varsayilanlar neden dosyasiz calisir:** Aracin calismasi icin once bir yapilandirma dosyasi yazmak gerekseydi, birinin araci merak edip bir repoda denemesi zorlasirdi. Dosya yoksa butun kurallar acik, hicbir sey dislanmiyor; bu bir hata degil, normal durum. Dosya bir **izin listesi degil, istisna listesi**: `rules` icinde adi gecmeyen kural varsayilan haliyle calismaya devam ediyor. Boylece yeni bir kural ekledigimde eski yapilandirma dosyalari o kurali sessizce kapatmis olmuyor.

`--config` ile yol verildiginde ise dosyanin olmamasi hata. Kokte dosya aramak bir kolaylik, ama kullanici acikca bir yol yazdiysa o yolun tutmasini bekliyordur.

**exclude birlesir, ezmez:** `sievert.json` icindeki `exclude` ile komut satirindaki `--exclude` birbirini ezmiyor, birlesiyor. Ezme olsaydi dosyaya yazilmis bir kalip, komut satirindan tek bir `--exclude` verildigi anda sessizce kaybolurdu.

## Glob hakkinda uc karar

Bunlar Asama 3 Adim 2'de verilmisti, buraya yaziyorum.

**Glob elle yazildi.** `Microsoft.Extensions.FileSystemGlobbing` diye hazir bir paket var ve isi gorurdu. Almadim: `ArgumentParser` da elle yazilmis, ayni cizgide kaldim ve `Sievert.Analysis` disaridan bagimsiz kaldi. Ihtiyacim olan uc kalip vardi (`samples/**`, `**/*.Designer.cs`, `**/obj/**`), karsiligi 40 satirlik bir eslestirici. Kural sayisi artip da kalip ihtiyaci buyurse bu karar geri alinabilir.

**Eslesme her platformda harf duyarsiz.** Linux'un dosya sistemi harf duyarli, macOS ve Windows'unki genelde degil. Dosya sistemini takip etseydim ayni `sievert.json` ayni repoda gelistiricinin makinesinde ve CI'da farkli sonuc verirdi; bir dosya yerelde elenir, CI'da elenmez ve kimse neden oldugunu anlamazdi. Tekrarlanabilirligi dosya sistemi sadakatine tercih ettim: `**/*.designer.cs` her yerde `Ana.Designer.cs`'i eliyor. Bedeli su: harf farkiyla ayrilan iki dosyanin ikisi birden elenir. Linux disinda boyle bir dosya cifti zaten yaratilamadigi icin bunu ucuz buldum.

**Kalip semantigi siki, gitignore degil.** `samples` yazmak `samples/` altindaki dosyalari elemez, `samples/**` yazmak gerekir. gitignore aliskanligi olan biri icin surpriz, ama gitignore'un "sonu bolu ise klasor, degilse hem dosya hem klasor" kurallarini taklit etmek, elle yazilmis 40 satiri birden buyuturdu. Bunun yerine eslesmeyen kalip icin uyari veriyorum ve kalip var olan bir klasorun adiysa mesajda `samples/**` yazmasi gerektigini soyluyorum. Yani sikiligi kullaniciya hata mesajiyla odetiyorum, sessizlikle degil.

**Sonuc:** Kural listesi tek yerde. Kullanici bir kurali kapatabiliyor ve seviyesini degistirebiliyor, ama kuralin ne yaptigini degistiremiyor. Yapilandirmayla ilgili her yanlis yazim cikis kodu 2 uretiyor; sessizce yanlis calisan bir tarama yok. Etkin ve kapali kural listesi de ozet ciktisina yazildi, boylece "hangi kurallar calisti" sorusu ciktida cevaplaniyor.
