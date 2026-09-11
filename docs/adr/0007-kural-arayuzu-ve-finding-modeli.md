# 0007 - IRule Analysis'te, Finding Core'da

**Baglam:** Kural altyapisini eklerken iki tipi nereye koyacagima karar vermem gerekti: kurallarin arayuzu `IRule` ve kurallarin urettigi `Finding`.

**Karar:** `IRule` Sievert.Analysis'te, `Finding` (ve `Severity`, `CheckSummary`) Sievert.Core'da duruyor.

**Neden:** `IRule.InspectFile` Roslyn'in `SyntaxTree`'sini aliyor. Bu arayuzu Core'a koysaydim Core'a Roslyn bagimliligi girerdi ve ADR 0004'te aldigim "Roslyn Analysis sinirini gecmiyor" karari bozulurdu. `Finding` ise saf veri: icinde tek bir Roslyn tipi yok, o yuzden Core'da durabiliyor. Bu ayrim pratikte ise yariyor, cunku bulgulari ileride veritabanina yazacagim, Web API'den dondurecegim ve GitHub yorumu olarak basacagim; bu taraflarin hicbiri Roslyn'i suruklemek zorunda kalmayacak.

`Finding` kendi basina anlamli olacak kadar bilgi tasiyor. `Title` alani kuralin `Name`'inin, `Rationale` alani da kuralin `Description`'inin kopyasi. Bu bilerek yapilan bir tekrar: bulguyu ekrana basan ya da JSON'a yazan taraf, bulgunun basligini ya da neden onemli oldugunu ogrenmek icin elinde kural listesi tutmak zorunda kalmasin istedim. Serialize edilmis bir `Finding` tek basina okunabiliyor; veritabanindan cekilen eski bir bulgu, o kural silinmis ya da metni degismis olsa bile hala kendini anlatabiliyor.

**Sonuc:** Kural yazarken tek yere bakiliyor (kuralin kendi sinifi), bulguyu tuketen taraf ise sadece `Finding`'e bakiyor. Tekrarin bedeli her bulguda iki fazla string; kural katalogunu her yere tasimaya gore ucuz buldum.
