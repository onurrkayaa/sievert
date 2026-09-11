# Proje calisma notlari

Kendim icin tuttugum kisa notlar: projenin amaci, verdigim teknik kararlar ve
calisirken uydugum birkac kural. Kararlarin **gerekcesi** ADR'lerde duruyor; burada
sadece kararin ne oldugu ve hangi ADR'ye bakmam gerektigi yaziyor.

Bu dosya bir calisma notu, arac yapilandirmasi degil. Aracin kendi ayar dosyasi repo
kokundeki `sievert.json`; ne yapip ne yapmadigi ADR 0009'da anlatiliyor.

## Amac

C#/.NET projelerinde commit bazli hata riski tahmini yapan bir arac. Problemi ve
yaklasimi README'nin basinda yazdim, burada tekrar etmiyorum.

## Teknik kararlar

| Konu | Karar | Durum | Gerekcesi |
|---|---|---|---|
| Dil ve hedef framework | C#, `net10.0` sabit; SDK surumu `global.json`'da pinlenmis | kullaniliyor | ADR 0002 |
| Katmanlar | `Sievert.Core` / `Sievert.Analysis` / `Sievert.Cli` | kullaniliyor | ADR 0001 |
| Kod analizi | Roslyn (`Microsoft.CodeAnalysis.CSharp`), sadece Analysis katmaninda | kullaniliyor | ADR 0004 |
| Test | xUnit | kullaniliyor | - |
| Veritabani | PostgreSQL + EF Core | **henuz yazilmadi**, Asama 4'te | ADR 0003 |
| Git okuma | LibGit2Sharp | **henuz yazilmadi**, Asama 4'te | - |
| ML | ML.NET | **henuz yazilmadi**, Asama 5'te | - |

Son uc satiri "durum" sutunuyla birlikte yaziyorum, cunku karar verilmis olmasi
yazilmis olmasi demek degil. Su an repoda ne PostgreSQL var ne LibGit2Sharp ne de
ML.NET; sadece bunlari kullanmaya karar verdim.

## Yazim dili

Iki ayri kural var, ikisi de her zaman gecerli.

**Kod Ingilizce, yorum ve dokuman Turkce.** Tip adlari, metot adlari, dosya adlari,
degisken adlari Ingilizce yaziliyor. Kod yorumlari, dokumanlar, ADR'ler ve CLI'in
ekrana bastigi metinler Turkce kaliyor. Bu ayrimin neden boyle oldugu ADR 0005'te.

**GitHub'a giden her sey sade ve dogal bir dille yazilir.** README, ADR'ler,
dokumanlar, kod yorumlari ve commit mesajlari benim yazdigim gibi dursun. Pazarlama
dili ya da kurumsal dokuman uslubu kullanmiyorum, cunku baskalari da bu repoyu okuyup
ogreniyor. Kisa ve durust cumleler: bir sey henuz yapilmadiysa yapilmis gibi
anlatilmaz. Yukaridaki tablodaki "henuz yazilmadi" satirlari tam da bu kuralin
sonucu.

## Kural kodlari

Tespit kurallarinin kodu `SV` onekiyle yazilir: SV001, SV002, ... SV006. `KR` gibi
baska bir onek kullanilmiyor. Ilk kural SV001 (async void).

Hangi kuralin ne aradigi README'deki tabloda. Kurallarin katalogda nasil durdugu ve
`sievert.json` ile nasil acilip kapatildigi ADR 0009'da; hepsinin ad temelli tahmin
yapmasinin bedeli ADR 0010'da; tek tek sinirliliklari `sinirliliklar.md`'de.

## Testler

**Console'a yazan her test sinifi `ConsoleCollection`'a girer:**

```csharp
[Collection(ConsoleCollection.Name)]
public class BenimTestlerim
```

`Console.SetOut` surec geneli bir ayar. Bu siniflar paralel kosarsa birbirinin
ciktisini calarlar ve ortaya cok kotu bir hata tablosu cikar: test tek basina
calistirildiginda geciyor, tam kosuda dusuyor. Bunu bir kez yasadim, ikinci kez
yasamamak icin buraya yaziyorum.

Yeni bir test sinifi `Console.SetOut` ya da `Console.SetError` cagiriyorsa koleksiyona
eklenmesi gerekiyor.
