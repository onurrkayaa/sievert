# 0001 - Katmanli mimari

**Baglam:** Proje tek bir konsol projesi olarak baslayabilirdi ama analiz mantigi ile ekrana basma isi hemen birbirine karisirdi.
**Karar:** Kodu dort projeye ayirdim: Core (ortak model ve yardimcilar), Analysis (Roslyn ve git analizi), Cli (terminal arayuzu), Tests (xUnit).
**Neden:** Analiz mantigini Console.WriteLine'dan ayirinca test yazmak kolaylasiyor, ileride Cli disinda baska bir arayuz eklemek de mumkun oluyor.
**Sonuc:** Referanslar tek yone akiyor (Analysis -> Core, Cli -> Analysis, Tests -> Analysis), yani Core hicbir seye bagimli degil.
