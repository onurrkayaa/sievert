# 0003 - Veritabani olarak PostgreSQL

**Baglam:** Asama 4'te commit analizlerinin sonuclarini saklamam gerekecek, su an her calistirmada her sey bastan hesaplaniyor.
**Karar:** Veritabani olarak PostgreSQL kullanacagim, erisim icin de EF Core.
**Neden:** Ucretsiz ve her yerde calisiyor, Docker ile hizlica ayaga kaldirmasi kolay, EF Core destegi olgun. SQLite de dusundum ama ileride birden fazla kullanicinin ayni veriye bakmasini istiyorum.
**Sonuc:** Asama 4'e kadar hicbir veritabani kodu yazmiyorum; simdiden paket eklemek erken olur.
