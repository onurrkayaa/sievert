# Ad degisimi takibinin metriklere etkisi

**Tarih:** 2026-09-11
**Repo:** App-vNext/Polly, tam klon, `2247db24`. 2759 commit (194 birlestirme haric).
**Ne olculdu:** Ayni veri uzerinde metrikler iki kez hesaplandi. Birinde ad degistiren
bir dosyanin gecmisi yeni yola tasindi, digerinde tasinmadi (dosya sifirdan basliyormus
gibi). Ikisi commit commit karsilastirildi.

**Ne olculmedi:** Ad degisimlerinin **dogru olup olmadigi**. Esik %50 ve bu git'in
varsayilani; secilmis bir deger degil. Asagidaki sayilar "esik degisirse ne kadar sey
degisir" sorusunun cevabi, "esik dogru mu" sorusunun degil.

## Sayilar

| Olcu | Deger |
|---|---|
| Commit | 2759 |
| Icinde ad degisimi olan commit | 80 |
| Ad degismis dosya satiri | 1029 |
| **Metrikleri bu secimden etkilenen commit** | **1234 (%44,7)** |
| `PriorChanges` degisen commit | 1233, en buyuk fark **3047** |
| `MaxFileAgeDays` degisen commit | 875, en buyuk fark **3636 gun** |

Toplamlar:

| Olcu | Gecmisi takip eden | Etmeyen | Fark |
|---|---|---|---|
| `PriorChanges` toplami | 361 826 | 263 506 | %37 daha fazla |
| `MaxFileAgeDays` toplami | 3 009 436 | 2 494 802 | %21 daha fazla |

## Bunun anlami

Ad degisimi iceren commit sayisi 80, yani tarihin %2,9'u. Ama **metrikleri etkilenen
commit sayisi 1234, yani %44,7.** Aradaki fark su: bir dosyanin adi bir kez degisince
etkisi o commit'te bitmiyor, o dosyaya sonradan dokunan her commit'in gecmisi degisiyor.
Tek bir yanlis ad degisimi, o dosyanin geri kalan tarihini boyuyor.

En buyuk tek fark `PriorChanges` icin 3047. Yani bir commit, ad degisimi takip edilirse
"3047 kez degismis dosyalara dokunuyor", edilmezse cok daha az. Bu iki sayi Asama 5'te
ayni modele bambaska seyler soyler.

`MaxFileAgeDays`'te en buyuk fark 3636 gun, yani yaklasik on yil. Takip edilmeyince
dosya "dun dogmus" gorunuyor.

## Karar

**Davranis bu adimda degistirilmedi.** Hesaplama ad degisimini takip ediyor
(`MetricOptions.Default`), cunku bir dosyanin adinin degismesi gecmisini yok etmemeli.
Ama esik hakkinda hicbir sey kanitlanmis degil: %50'nin ustunde kalan bir yanlis
eslesme iki ilgisiz dosyanin tarihini birlestirir ve yukaridaki sayilar o yanlisin ne
kadar yayilacagini gosteriyor.

Esik tartismasi icin gereken sonraki olcum: 1029 ad degisiminden bir orneklem alip
kaynak koda bakarak dogru/yanlis diye siniflandirmak. Bu adimda yapilmadi.
