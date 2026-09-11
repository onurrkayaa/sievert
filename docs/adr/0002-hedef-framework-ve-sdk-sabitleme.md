# 0002 - Hedef framework net10.0 ve global.json ile SDK sabitleme

**Baglam:** Makinemde .NET SDK 10.0.401 kurulu ve .NET 10 su an LTS surumu.
**Karar:** Tum projelerde TargetFramework olarak net10.0 yazdim ve koke global.json ekleyip SDK surumunu 10.0.401'e sabitledim (rollForward: latestPatch).
**Neden:** Surumu "kurulu olan SDK ne ise o" seklinde birakirsam ayni kod baska bir makinede veya CI'da farkli bir surumle derlenir, sonra da nedenini anlamadigim farkliliklar cikar.
**Sonuc:** CI icin ayrica surum yazmiyorum, setup-dotnet global-json-file ile ayni dosyayi okuyor; surum tek yerde duruyor.
