# Kademe 1'in SV001 uzerindeki etkisi

**Tarih:** 2026-09-11
**Repo:** [ShareX/ShareX](https://github.com/ShareX/ShareX) @ `b5a397ea6ccf00659cee593c981be4b01ab641fe`
**Once:** Sievert `16058bd` (Asama 2 sonu, abonelik kaniti yok)
**Sonra:** Sievert `215c0ad` + satir duzeltmesi (Kademe 1 var)

Asama 2'de SV001'in iki yanlis pozitif urettigi olculmustu ve ikisinin de sebebi ayniydi:
metot gercekten bir olaya abone ama ozel delegate imzasi yuzunden istisnaya giremiyordu.
Kademe 1 buna ikinci bir kanit kaynagi ekledi: ayni dosyada ya da ayni partial sinifin
baska bir parcasinda `+= MetotAdi` aramak (ADR 0008).

Bu olcum, o degisikligin beklenen etkiyi yapip yapmadigina bakiyor. Ayni repo, ayni
commit, sadece SV001 acik (digerleri `sievert.json` ile kapatildi).

> **Bu olcum bir dogrulama degildir.** Duzeltme bu 20 satira bakilarak tasarlandi
> (bkz. `asama2-dogrulama-listesi.md`, Uyari bolumu). Burada gorunen sey, tasarlanan
> etkinin gerceklestigidir; kuralin genel olarak duzeldigi degil. Gecerlilik ancak bu
> listede olmayan yeni bir orneklemle olculebilir - onun icin `asama3-jellyfin.md` ve
> `asama3-dogrulama-listesi.md` var.

## Sayilar yan yana

| Olcum | Once (`16058bd`) | Sonra (`215c0ad`) |
|---|---|---|
| Taranan dosya | 1138 | 1138 |
| `async void` aday | 106 | 106 |
| SV001 bulgusu | **10** | **8** |
| Muaf tutulan | 96 | **98** |
| Cikis kodu | 1 | 1 |

Aday sayisi degismedi, degismemesi de gerekiyordu: kuralin "async void bul" kismina
dokunulmadi, sadece eleme olcutu degisti. Iki bulgu muafiyet tarafina gecti.

## Kaybolan bulgular

Iki bulgu kayboldu ve ikisi de Asama 2'de elle "yanlis pozitif" diye isaretlenen
satirlar:

| Asama 2 no | Dosya | Satir | Metot | Abonelik nerede |
|---|---|---|---|---|
| 4 | `ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs` | 2131 | `OnLoadFromUrlRequested` | `EditorView.Subscriptions.cs:57` (kardes partial dosya) |
| 6 | `ShareX/Forms/MainForm.cs` | 200 | `HandleHotkeys` | `MainForm.cs:187` (ayni dosya) |

Asama 2 dogrulama listesindeki 4 ve 6 numaranin gercekten kaybolup kaybolmadigini tek
tek kontrol ettim: ikisi de sonraki ciktida yok, ve ikisi de muafiyet listesinde
`subscription` sebebiyle yer aliyor. Beklenen buydu.

## Yeni kaybolan baska bulgu var mi

**Yok.** Once ve sonra ciktilarini (dosya, satir, metot) uclusune gore karsilastirdim:

- Kaybolan: 2 (yukaridaki ikisi)
- Yeni cikan: 0
- Kalan 8 bulgunun hepsi once de vardi

Bu onemliydi, cunku fazla muafiyet yeni yanlis negatif demek olurdu. Kalan sekiz bulgu,
Asama 2'de elle "gercek sorun" diye isaretlenen sekiz satirin ta kendisi:

```
ShareX.HelpersLib/UpdateChecker/GitHubUpdateManager.cs:76        TimerCallback
ShareX.ImageEditor/.../EditorSelectionController.cs:382          ShowImagePickerForReplacement
ShareX.ImageEditor/.../EditorView.axaml.cs:1197                  AutoCopyImageToClipboard
ShareX.ImageEffectsLib/.../ImageEffectsWindow.axaml.cs:100       PackagePresetAsync
ShareX/ScreenRecordManager.cs:46                                 StartStopRecording
ShareX/TaskHelpers.cs:875                                        OpenScreenColorPicker
ShareX/UploadInfoManager.cs:429                                  SearchImageUsingGoogleLens
ShareX/UploadInfoManager.cs:437                                  SearchImageUsingBing
```

`PackagePresetAsync`'in listede kalmasi ayrica anlamli. O satir `ImageEffectsWindow.axaml.cs:40`'ta
`ViewModel.PackagePresetRequested = PackagePresetAsync;` diye **duz atamayla** baglaniyor.
Kademe 1 tasarlanirken duz atamayi da kanit saymak dusunulmus, ama elle inceleme o satira
"gercek sorun" dedigi icin sadece `+=` kabul edilmisti (ADR 0008). Olcum bu kararin
calistigini gosteriyor: duz atama kanit sayilsaydi burada yeni bir yanlis negatif olusacakti.

## Muafiyet dokumu

98 muafiyetin sebebe gore dagilimi:

| Sebep | Sayi |
|---|---|
| `signature` (imza event handler kalibina uyuyor) | 96 |
| `subscription` (`+= MetotAdi` bulundu) | **2** |

`subscription` olanlarin kaniti nerede:

| Kanit yeri | Sayi | Hangi satir |
|---|---|---|
| Ayni dosyada | 1 | `MainForm.cs:200` (`HandleHotkeys`) |
| Kardes partial dosyada | **1** | `EditorView.axaml.cs:2131` (`OnLoadFromUrlRequested`) |

**Kardes partial dosya aramasinin katkisi bu repoda tek bir satir.** Kademe 1'in iki
parcasi var: ayni dosyada aramak (ucuz, tek agac) ve kardes partial parcalarda aramak
(klasordeki diger dosyalarin agaclarina bakmak gerekiyor). 1138 dosyalik bir repoda
ikinci parcanin kazanci 1 bulgu.

Bu sayi kucuk. Maliyetin ne oldugu asagidaki performans bolumunde; orada gorunecegi
uzere sure artmadi, cunku Adim 3'te kural diski kendisi okumayi birakip zaten
ayristirilmis agaclari kullanmaya basladi. Yani ikinci parca su an bedava sayilir ve
kucuk kazanci da olsa durabilir. Bedeli olsaydi bu sayiyla savunmak zor olurdu.

## Performans

Ayni repo, ayni komut, uc kosu. Isinma kosusu sayilmadi.

| Surum | Kosu 1 | Kosu 2 | Kosu 3 |
|---|---|---|---|
| Once (`16058bd`) | 1,90 sn | 1,84 sn | 1,76 sn |
| Sonra (`215c0ad`) | 1,82 sn | 1,75 sn | 1,98 sn |

**Sure artmadi.** Asama 2'de ayni repo icin 1,93-2,02 sn olculmustu; simdiki araliklar
onunla ayni yerde. Kademe 1 ilk yazildiginda kural partial parcalari diskten kendisi
okuyordu ve burada bir yavaslama bekliyordum; Adim 3'te kurala taranan dosya kumesi
verilince o okuma ortadan kalkti. Dosyalar zaten bir kez ayristiriliyor, kural da ayni
agaclari kullaniyor.

Not: iki surumun kosulari ayni makinede arka arkaya alindi ama makine izole degildi;
0,1-0,2 sn'lik farklari gurultu saymak lazim. Buradan cikarilacak sonuc "hizlandi" degil,
"olcebildigim kadariyla yavaslamadi".

## Bellek

Adim 3'te `ScannedFileSet` butun dosyalari ayni anda bellekte tutmaya basladi. Tepe
bellek kullanimi (`/usr/bin/time -l`, macOS, maximum resident set size):

| Repo | Dosya | Tepe bellek |
|---|---|---|
| Polly `2247db24` | 797 | 129 MB |
| ShareX `b5a397ea` | 1138 | 173 MB |
| Jellyfin `1d7b6d97` | 2184 | 217 MB |

Ayni repoda once/sonra karsilastirmasi (ShareX, sadece SV001):

| Surum | Tepe bellek |
|---|---|
| Once (`16058bd`) | 98-99 MB |
| Sonra (`215c0ad`) | 169-170 MB |

Ayni is icin tepe bellek **%72 artti**. Uc repoya bakilinca egilim kabaca dogrusal:
.NET calisma zamaninin kendi tabani yaklasik 85 MB, onun ustune dosya basina 60-77 KB
ekleniyor. Bu hizla 10 000 dosyalik bir repo 800 MB civarina cikar.

Cozum onermiyorum, karari birlikte verecegiz. Sayilar burada duruyor ve egilim dogrusal
gorunuyor; ucuncu repoda 217 MB henuz rahatsiz edici degil ama buyume yonu belli.
