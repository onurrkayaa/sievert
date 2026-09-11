# 0004 - Roslyn sadece Analysis katmaninda

**Baglam:** Kod cozumlemesi icin Roslyn gerekiyor ama Core katmani su an hicbir dis pakete bagimli degil.
**Karar:** Microsoft.CodeAnalysis.CSharp paketini sadece Sievert.Analysis projesine ekliyorum. Core'da sonuc modelleri saf C# record olarak duruyor.
**Neden:** Core'u bagimsiz tutunca sonuc modellerini ileride raporlama, veritabani ya da CLI tarafinda Roslyn'i suruklemeden kullanabiliyorum. Roslyn tipleriyle ad cakismasi olmasin diye modelleri Sievert onekiyle adlandirdim.
**Sonuc:** Roslyn tipleri Analysis sinirini gecmiyor; disariya sadece DosyaAnalizi ve icindeki recordlar cikiyor. Analysis'i degistirmek istersem Core'a dokunmam gerekmeyecek.
