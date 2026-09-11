# SV001 elle dogrulama listesi

**Tarih:** 2026-09-11
**Repo:** [App-vNext/Polly](https://github.com/App-vNext/Polly) @ `2247db2407713fa221d57011814e4be446361b1f`
**Sievert commit:** `c142311`

Bu dosya, SV001'in urettigi bulgulari tek tek elle inceleyip gercek sorun mu
degil mi diye isaretlemek icin hazirlandi.

## Liste bos

Polly taramasi hic bulgu uretmedi, o yuzden incelenecek satir yok. Detayi ve
sifir sonucun nasil dogrulandigi [asama2-sv001-polly.md](asama2-sv001-polly.md)
dosyasinda.

Kisaca: Polly'de tek bir `async void` metot tanimi yok. 991 async metodun
hepsi `Task` ya da `ValueTask` donuyor. Duz metin aramasiyla cikan 10 "async
void" satirinin onu da yorum.

| # | Dosya | Satir | Metot | GitHub | Gercek sorun mu (E/H) | Neden | Kural nasil duzelmeli |
|---|---|---|---|---|---|---|---|
| | | | | | | | |

Liste doldugunda GitHub baglantilari su kalipta olacak:
`https://github.com/App-vNext/Polly/blob/2247db2407713fa221d57011814e4be446361b1f/<dosya yolu>#L<satir>`

## Elde olan tek bulgular

Su an Sievert'in gordugu tek SV001 bulgusu, kurali test etmek icin kendi
yazdigim ornek dosyadan geliyor. Gercek kod olmadigi icin elle dogrulamaya
degmez, ama listenin bicimi gorunsun diye buraya koyuyorum.

| # | Dosya | Satir | Metot | Kaynak | Gercek sorun mu (E/H) | Neden | Kural nasil duzelmeli |
|---|---|---|---|---|---|---|---|
| 1 | samples/Patients/AsyncVoid.cs | 10 | `Save` | bilerek yazilmis test verisi | | | |
| 2 | samples/Patients/AsyncVoid.cs | 28 | `Tick` | bilerek yazilmis test verisi | | | |

## Gercek bir liste icin ne lazim

SV001'i gercek kod uzerinde dogrulamak icin `async void` iceren bir repo
taranmasi gerekiyor. `async void` cogunlukla UI event handler'larinda cikan
bir kalip oldugu icin WPF, WinForms, MAUI ya da Xamarin tarafinda bir proje
Polly'den daha uygun bir orneklem olur. Hangi repoyu sececeğimize karar
verilmedi, o yuzden ikinci bir tarama yapmadim.
