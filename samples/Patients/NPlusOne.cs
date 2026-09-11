// Test verisi. Hicbir projeye dahil degil, derlenmesi gerekmiyor.
using System.Collections.Generic;
using System.Linq;

namespace Clinic;

// SV004'un ornekleri. Kural dongu govdesinde sorgu bitirici cagri ariyor.
// EF Core'u tanimiyor, ad temelli calisiyor.
public class PatientReport
{
    // Bulgu: her hasta icin ayri bir sorgu. Uzerindeki ifadede "Db" geciyor,
    // yani bulgu mesaji bunun gercekten veritabani olma ihtimalini soyluyor.
    public void PerPatient(IEnumerable<int> ids)
    {
        foreach (int id in ids)
        {
            var visits = _db.Visits.Where(visit => visit.PatientId == id).ToList();
        }
    }

    // Bulgu: while dongusunde FirstOrDefault.
    public void ScanQueue()
    {
        while (_hasMore)
        {
            var next = _context.Queue.FirstOrDefault();
        }
    }

    // Bulgu: for dongusunde Count.
    public void CountEach(int[] ids)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            int total = _repository.Visits.Count();
        }
    }

    // Bulgu uretmemeli: sorgu dongunun DISINDA, zaten dogru yazim bu.
    public void Prefetched(IEnumerable<int> ids)
    {
        var all = _db.Visits.ToList();

        foreach (int id in ids)
        {
            var mine = all.Count;
        }
    }

    // Bulgu uretmemeli: sorgu dongunun kaynaginda, govdesinde degil.
    public void IterateQueryOnce()
    {
        foreach (var visit in _db.Visits.ToList())
        {
        }
    }

    // Sinir durum, BULGU URETIR: bu bellekteki bir liste, veritabani degil.
    // Ad temelli tahminin bedeli; bilerek kabul edildi.
    public void InMemoryFalsePositive(IEnumerable<int> ids)
    {
        var names = new List<string>();

        foreach (int id in ids)
        {
            bool exists = names.Any();
        }
    }

    private bool _hasMore = true;
    private Database _db = new();
    private Database _context = new();
    private Database _repository = new();
}
