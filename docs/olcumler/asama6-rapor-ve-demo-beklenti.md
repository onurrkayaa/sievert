# Rapor ve demo beklentileri

**Beklentiler kod yazilmadan, PDF uretilmeden ve demo kosulmadan once yazildi.**

**Tarih:** 2026-09-13
**Durum:** Adim 6 (denetlenebilir PDF raporu ve tek komutluk demo) yazilmadan once
yazildi. Bu dosya sonradan degistirilmeyecek.

Bu turda iki sey uretiliyor: panelden istenebilen bir PDF raporu ve temiz bir makinede
tek komutla acilan bir demo ortami. Ikisinin de ortak derdi ayni: **cikan seyin nereden
geldigi izlenebilmeli.** Bir PDF elden ele dolasir, uretildigi veriden kopar; demo verisi
de "guzel gorunsun diye secilmis" olabilir. Asagidaki beklentilerin cogu bu iki riski
kapatmak icin.

Asagidakiler tahmin. Tutmayanlar olcum dosyasinda "tutmadi" diye yazilacak, beklenti
geriye donuk duzeltilmeyecek.

## PDF raporu

### Kapsam ve kismi sonuc

1. **Rapor varsayilan olarak yalniz tamamlanmis bir `risk-score-all` sonucu icin
   uretilebilmeli.** Kismi bir isten rapor istemek acik bir secim gerektirmeli.
2. **Kismi sonuc secildiginde PDF'nin her ilgili bolumunde kismi oldugu gorunmeli:**
   kapak, yonetici ozeti, her toplu sayi bolumu ve sinirliliklar.

### Dogruluk

3. **PDF'deki repo, model, commit, dosya ve sayim bilgileri API ile 0 fark vermeli.**
4. **Rapor kaynagi canonical input manifest ile izlenebilmeli**; ayni veri ve ayni
   parametre ayni manifest byte'larini ve ayni SHA-256'yi uretmeli.
5. **Ayni Idempotency-Key ve ayni istek ayni artefakti dondurmeli** - yeni is acilmamali,
   dosya byte olarak ayni olmali.

### Urun dili

6. **PDF'de `RawModelScore` yuzde olarak yazilmamali.**
7. **`RiskIndex`'in goreli bir endeks oldugu ilk sayfada gorunmeli.**
8. **`IsCalibrated = false` ilk sayfada gorunmeli.**
9. **Statik bulgularin model skoruna dahil olmadigi ilk sayfada gorunmeli.**

### Dosya ve butunluk

10. **Dosya diske atomik yazilmali**: once gecici dosya, sonra yeniden adlandirma.
11. **Indirmeden once SHA-256 dogrulanmali**; uyusmazsa dosya kullaniciya verilmemeli.
12. **Varsayilan Polly demo raporu 5 MB altinda olmali.**

### Bicim

13. **Turkce karakterler PDF'de bozulmamali.**
14. **PDF metin cikartmada aranabilir olmali**; rapor tek bir raster goruntu olmamali.
15. **Dosya ozeti en az 10, en fazla 100 satir; zaman cizelgesi en az 20, en fazla 500
    nokta.**

### Calisma

16. **Rapor uretimi calisan API isteklerini belirgin bicimde engellememeli**; uretim
    arka plan isinde kosmali. "Belirgin" icin somut hedef: rapor uretilirken saglik
    ucunun ortancasi 200 ms'nin altinda kalmali.

## Demo ortami

17. **Tek komut** PostgreSQL'i, migration'i, sabit demo verisinin importunu, API'yi ve
    paneli baslatmali.
18. **Demo verisi uydurma olmamali**; gercek Polly verisinin sabit ve kaynagi kayitli bir
    alt kumesi olmali.
19. **Demo seed'inde yazar e-postasi, yerel yol ya da baglanti dizesi olmamali.**
20. **Ayni veritabaninda ikinci kez import satir cogaltmamali** (fark 0).
21. **Demo hazir olunca saglik ucu ve temel sayfalar gercekten 200 vermeli.**
22. **Ctrl+C child surecleri ve PostgreSQL container'ini temiz kapatmali**; yetim surec
    ya da container kalmamali.
23. **Basarisizlikta hangi onkosulun eksik oldugu acik yazilmali** (Docker yok, port
    dolu, seed bozuk gibi).
24. **Demo internet baglantisi olmadan calisabilmeli**, gerekli container imaji ve NuGet
    paketleri onceden duruyorsa.
25. **Demo panelinde bunun sabit bir kamu reposu alt kumesi oldugu gorunmeli.**
26. **Demo ortaminda PDF olusturulup indirilebilmeli.**

## Sure beklentileri

Bunlar tahmin; olculecek ve tutmazsa oyle yazilacak.

27. **Polly demo raporunun uretimi (kuyruk beklemesi haric) 10 saniyenin altinda
    olmali.**
28. **Tek komutluk demo, imaj ve paketler yerelde hazirken 90 saniyenin altinda hazir
    olmali.**

## Beklenti yazmadigim yerler

Bu sayilar olculecek ama "tuttu/tutmadi" diye degerlendirilmeyecek:

- PDF'nin sayfa sayisi ve byte boyutu (5 MB siniri disinda).
- Rapor uretiminin tepe bellegi.
- Rapor basina veritabani komut sayisi.
- Soguk container imaji indirme suresi (ag hizina bagli).
- Jellyfin gibi buyuk depolarda raporun suresi.
