# Demo veri kumesi

Bu klasordeki dosyalar **gercek** Polly gecmisinin sabit bir alt kumesi. Uydurma veri
yok; hicbir sayi elle yazilmadi.

## Secim kurali

Kural kod yazilmadan ve sonuc gorulmeden sabitlendi:

> Kaynak deponun tarih sirasinda **en yeni 200 commit'i**. Risk dagilimina bakilarak
> secim yapilmadi.

Yani "grafik guzel gorunsun" diye commit secilmedi. Ne ciktiysa o.

## Dosyalar

| Dosya | Ne |
|---|---|
| `demo-seed.json` | Veri kumesinin kendisi |
| `demo-seed.sha256` | Seed'in ozeti; importer okumadan once dogruluyor |
| `source-manifest.json` | Verinin nereden geldigi, sayilar, uretim komutu |
| `source-manifest.sha256` | Manifestin ozeti |

## Icinde ne var, ne yok

**Var:** commit'ler (sha, tarih, baslik, satir sayilari, SZZ etiketi, bot isareti),
dokunulan dosyalar, 15 oznitelik, kaydedilmis risk skorlari ve statik tarama bulgulari.

**Yok:** yazar adi, yazar e-postasi, yerel dosya yolu, baglanti dizesi. Bunlar seed'e
hic yazilmiyor; importer da veritabaninda bos birakiyor.

## Bu alt kumenin bilinmesi gereken ozelligi

Secilen 200 commit'te **SZZ pozitifi sifir**. Sebep tahmin degil, tanim: SZZ etiketi bir
commit'i ancak sonradan bir duzeltme onun satirlarina dokununca pozitif yapabiliyor. En
yeni commit'ler icin o duzeltme henuz yazilmamis olabilir - buna sag sansurleme deniyor
ve raporun sinirliliklar bolumunde de yaziyor.

Ayni sebeple bu alt kumede bot commit'i cok: 200 commit'in 172'si bagimlilik guncellemesi.

Bunlar kurali degistirmek icin sebep sayilmadi. Demo, modelin ne kadar iyi oldugunu
gostermek icin degil, aracin nasil calistigini gostermek icin var.

## Yeniden uretme

```bash
export SIEVERT_DB="..."
dotnet run --project tools/Sievert.Demo -- export-seed data/asama6/demo <commit>
```

Ayni veritabanindan iki uretim **bayt olarak ayni** dosyayi veriyor: hicbir yerde uretim
zamani, rastgele kimlik ya da kultura bagli bicimlendirme yok.
