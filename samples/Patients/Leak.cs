// Test verisi. Hicbir projeye dahil degil, derlenmesi gerekmiyor.
using System;
using System.IO;
using System.Net.Http;

namespace Clinic;

// SV005'in ornekleri. new ile olusturulan, atilmasi gereken nesneler.
public class FileArchive
{
    // Bulgu: FileStream aciliyor, hicbir zaman kapatilmiyor.
    public void Save(string path)
    {
        var stream = new FileStream(path, FileMode.Create);
        stream.WriteByte(1);
    }

    // Bulgu: HttpClient. Mesaji ozel: dogru kullanimi zaten uzun omurlu olmak.
    public void Fetch()
    {
        var client = new HttpClient();
    }

    // Bulgu: StreamReader.
    public string Read(string path)
    {
        var reader = new StreamReader(path);
        return reader.ReadToEnd();
    }

    // Muaf: using bildirimi. Kapsam bitince atiliyor.
    public void SaveWithUsing(string path)
    {
        using var stream = new FileStream(path, FileMode.Create);
        stream.WriteByte(1);
    }

    // Muaf: using deyimi.
    public void ReadWithUsing(string path)
    {
        using (var reader = new StreamReader(path))
        {
        }
    }

    // Muaf: alana atandi, sinif sahibi. Dispose baska bir yerde olabilir.
    public void OpenShared(string path)
    {
        _shared = new FileStream(path, FileMode.Open);
    }

    // Muaf: cagirana donuyor, sahiplik cagiranda.
    public Stream Open(string path)
    {
        return new FileStream(path, FileMode.Open);
    }

    // Bulgu uretmemeli: listedeki tiplerden biri degil.
    public void Plain()
    {
        var builder = new System.Text.StringBuilder();
    }

    // Alan baslangic degeri de muaf: sinif sahibi.
    private FileStream _shared = null;
    private readonly HttpClient _client = new HttpClient();
}
