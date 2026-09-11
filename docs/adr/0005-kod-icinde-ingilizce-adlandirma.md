# 0005 - Kod icinde Ingilizce adlandirma

**Baglam:** Projeye Turkce tip ve dosya adlariyla basladim (DosyaAnalizi, TipToplayici, KaynakDosyaBulucu). Kural altyapisini eklerken IRule, Finding, Severity gibi Ingilizce adlar yazmak istedim ve ortaya karisik dilli bir kod cikti.
**Karar:** Kodun tamami Ingilizce adlandirilacak: tip adlari, metotlar, property'ler, record parametreleri, enum uyeleri ve dosya adlari. Yorumlar, XML dokumantasyonu, ADR'ler ve kullaniciya basilan CLI metinleri Turkce kalacak.
**Neden:** Iki dili ayni satirda karistirmak okumayi zorlastiriyordu. Roslyn ve .NET API'lari zaten Ingilizce, kod onlarla ayni dilde olunca gecisler daha duzgun duruyor. Yorumlarin Turkce kalmasi ise bilincli: projeyi ben anlatiyorum, anlatimi kendi dilimde yapmak istiyorum.
**Sonuc:** Asama 2'nin basinda butun mevcut kod tek seferde yeniden adlandirildi. Bunun iki gorunur etkisi var: JSON ciktisinin anahtarlari degisti (`dosyaYolu` yerine `filePath`, `ozet` yerine `summary` gibi), ve Asama 1 sirasinda yazilan olcum raporlarindaki eski adlar artik kodda yok. O raporlari oldugu gibi biraktim, cunku o gunku halini anlatiyorlar.
