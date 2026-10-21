namespace PolsanIzin.Ortak.Tests;

public class SonucTests
{
    [Fact]
    public void Basari_HataYok_Basarili()
    {
        var sonuc = Sonuc.Basari();

        Assert.True(sonuc.Basarili);
        Assert.Empty(sonuc.Hatalar);
    }

    [Fact]
    public void Basarisiz_CokluHata_TumHatalarTasinir()
    {
        var sonuc = Sonuc.Basarisiz(new Hata("BR-5", "Geçmiş tarih", "Baslangic"), new Hata("ALAN", "Çok uzun", "Aciklama"));

        Assert.False(sonuc.Basarili);
        Assert.Equal(["BR-5", "ALAN"], sonuc.Hatalar.Select(h => h.Kod));
    }

    [Fact]
    public void Basarisiz_BosHataListesi_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Sonuc.Basarisiz([]));
    }

    [Fact]
    public void DegerliBasari_Deger_Okunur()
    {
        var sonuc = Sonuc.Basari(42);

        Assert.True(sonuc.Basarili);
        Assert.Equal(42, sonuc.Deger);
    }

    [Fact]
    public void DegerliBasarisiz_DegerOkunursa_InvalidOperationException()
    {
        var sonuc = Sonuc.Basarisiz<int>(new Hata("BR-2", "Yetersiz bakiye"));

        Assert.False(sonuc.Basarili);
        Assert.Throws<InvalidOperationException>(() => sonuc.Deger);
    }
}
