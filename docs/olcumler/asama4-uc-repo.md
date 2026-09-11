# Uc repoda tam madencilik

**Tarih:** 2026-09-11
**Repolar:** Polly `2247db24`, ShareX `b5a397ea`, Jellyfin `1d7b6d97` - Asama 3'tekilerin
ayni commit'leri, tam klon (shallow degil).

Bu dosya Asama 4'un butun boru hattini (madencilik, metrik, etiketleme) uc repoda
calistirip sayilari yan yana koyuyor. **Asama 3'un bulgu sayilariyla birlestirilmedi**,
o baska bir olcum ve kendi dosyasinda duruyor.

## Beklentiler (olcumden ONCE yazildi)

Polly'de olculenler: IsFix %10,6 (292 / 2759), etiket orani %9,9 (272 / 2759).

**ShareX** (9479 commit, masaustu uygulamasi, tek ana gelistirici agirlikli):

- IsFix (tam kelime) icin **%6-9** bekliyorum, yani Polly'nin altinda. Sebep: ShareX'in
  commit mesajlarinda gecmis zaman kalibi (`Fixed ...`) yaygin ve mevcut tanim tam kelime
  aradigi icin `Fixed` eslesmiyor.
- Kok eslesmesiyle oranin **belirgin sekilde** yukselmesini bekliyorum, Polly'deki
  %10,6 -> %12,0 farkindan daha genis: **%12-18** bandi. Iki sayi arasindaki makas
  ShareX'te Polly'dekinden buyuk olacak.
- Etiket orani IsFix'e yakin, **%6-10**. ShareX neredeyse tamamen C# oldugu icin
  ".cs dokunmayan duzeltme" kaybi Polly'deki %31'den az olmali; huninin en cok daraldigi
  yer Polly'de `.cs` dokunma kademesiydi, ShareX'te **silinen satir** kademesine kayabilir.

**Jellyfin** (30 004 commit, sunucu uygulamasi, PR akisi, cok katkici):

- IsFix (tam kelime) **%10-14**. PR basliklarinda `Fix ...` kalibi yaygin ve bu tam
  kelime olarak esliyor.
- Kok eslesmesiyle **%13-18**.
- Etiket orani **%8-14**. Cok katkicili bir repoda ayni satirlara birden fazla duzeltme
  dokunuyor, yani suclamalar ust uste binip farkli commit sayisini bastiriyor olabilir.
- Bot orani Polly'nin %31'inin **altinda** olmali; Polly'de dependabot cok yogundu.

**Sureler.** SZZ Polly'de 288 duzeltme icin 125 sn surdu. Jellyfin'de commit sayisi 10
kat, duzeltme sayisi da benzer oranda artarsa **20-45 dakika** bekliyorum; ustelik blame
daha buyuk bir tarihte geriye yurudugu icin duzeltme basina sure de artacak.

**Huninin en cok daraldigi yer.** Polly'de `.cs` dokunma kademesiydi (288 -> 200).
Uc repoda ayni kademe mi daraliyor, yoksa repo turune gore degisiyor mu - bu olcumun
dis gecerlilik sorusu. Tahminim: ShareX ve Jellyfin C# agirlikli oldugu icin o kademe
daha az daralacak ve darbogaz "silinen satiri olan" kademesine kayacak.

---

*Buradan asagisi olcum sonrasi doldurulacak.*
