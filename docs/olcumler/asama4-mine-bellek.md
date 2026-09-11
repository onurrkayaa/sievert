# mine komutunun bellegi ve suresi

**Tarih:** 2026-09-11
**Olcum:** `/usr/bin/time -l`, macOS, Release yapisi. Olculen sey **varsayilan GC ayariyla
tepe RSS**; projeye hicbir GC ayari konmadi, asagidaki yigin siniri satirlari sadece
buyumenin nereden geldigini anlamak icin yapilmis denemeler.
**Repo:** App-vNext/Polly, tam klon (`--filter` yok), `2247db24`'e checkout edilmis.
Tarihin tamami 2953 commit, bunun 194'u birlestirme, yani okunan 2759 commit.

Amac, Asama 3'te tarama tarafinda yasanan bellek buyumesini (B031: 99 MB -> 170 MB) bu
sefer bastan onlemekti. Onlenemedi; asagisi ne olctugum ve ne bulamadigim.

## Commit sayisina gore tepe bellek

| Kosu | Commit | Sure | Tepe bellek (RSS) | Yonetilen yigin tepe |
|---|---|---|---|---|
| Sievert'in kendi reposu | 42 | 0,29 sn | 66 MB | 42 MB |
| Polly `--max-commits 100` | 100 | 0,14 sn | 69 MB | 43 MB |
| Polly `--max-commits 1000` | 1000 | 0,95 sn | 124 MB | 95 MB |
| Polly, tarihin tamami | 2759 | 3,95 sn | 244 MB | 212 MB |

Bellek commit sayisiyla birlikte buyuyor. Kodda commit'ler biriktirilmiyor: miner
`yield return` ile tek tek veriyor, CLI her commit'i JSONL dosyasina yazip birakiyor.
Bellekte tutulan tek buyuyen sey farkli eposta adreslerinin kumesi ve o da Polly'de
140 kayit.

## Ne denendi

**libgit2'nin nesne onbellegi (kapatildi, fark etmedi).** `GlobalSettings.SetEnableCaching(false)`
ile onbellek kapatilip ayni uc kosu tekrarlandi: 65 / 119 / 222 MB. Yani bellegin buyumesini
acikalamiyor, ustelik tam tarih 3,95 sn yerine 5,40 sn surdu. Degisiklik geri alindi.
`SetCacheMaxSize` LibGit2Sharp'in public yuzeyinde yok, sadece onbellegi tamamen
acip kapatabiliyorum.

**Yonetilen yigina sert sinir (ise yaradi).** `DOTNET_GCHeapHardLimit` ile:

| Yigin siniri | Sonuc | Tepe bellek (RSS) | Cikti |
|---|---|---|---|
| 32 MB | **Out of memory**, cikis kodu 134 | - | yarim |
| 64 MB | tamamlandi | 185 MB | 2759 satir, birebir ayni |
| 96 MB | tamamlandi | 214 MB | 2759 satir, birebir ayni |
| sinir yok | tamamlandi | 244 MB | 2759 satir |

2759 commit'in tamami 64 MB'lik bir yiginda okunabiliyor ve cikti degismiyor.

## Sonuc

Buyuyen sey tutulan veri degil, henuz toplanmamis cop. Cop toplayici bol bellek varken
toplama ihtiyaci duymuyor; sinir konunca topluyor ve is yine bitiyor. Bu bir sizinti
degil - sizinti olsaydi 64 MB'lik sinirda da patlardi.

Ama "tepe bellek commit sayisindan bagimsiz" diyemem, cunku degil. Asama 3'teki durumdan
farki su: orada veri gercekten bellekte tutuluyordu, burada tutulmuyor ve bunu sinir
koyarak gosterebiliyorum. Projeye bir GC ayari koymadim; ayar koymak olcumu degistirir
ama sorunu cozmez, ve su an bir sorun oldugundan emin degilim.

Olculmeyen sey: daha buyuk bir repoda (ornegin Jellyfin'in tarihi) ayni egrinin nereye
gittigi. Sadece Polly olculdu.

## Ciktinin buyuklugu

Polly'nin tam tarihi JSONL olarak 3,9 MB, 2759 satir. Ayni veriyi tek bir JSON dizisi
olarak yazsaydim yazarken de okurken de bu 3,9 MB'nin tamami bellege girecekti; JSONL
tercihinin gerekcesi ADR 0011'de.
