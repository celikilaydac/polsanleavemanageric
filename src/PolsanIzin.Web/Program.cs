using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PolsanIzin.Bakiyeler;
using PolsanIzin.Calisanlar;
using PolsanIzin.Izinler;
using PolsanIzin.Takvim;
using PolsanIzin.Web;

var builder = WebApplication.CreateBuilder(args);

var baglantiDizesi = builder.Configuration.GetConnectionString("PolsanIzin")
    ?? throw new InvalidOperationException("ConnectionStrings:PolsanIzin tanımlı değil.");

builder.Services.TryAddSingleton(TimeProvider.System);

CalisanlarModulu.Kaydet(builder.Services, baglantiDizesi);
TakvimModulu.Kaydet(builder.Services, baglantiDizesi);
BakiyelerModulu.Kaydet(builder.Services, baglantiDizesi);
IzinlerModulu.Kaydet(builder.Services, baglantiDizesi);

builder.Services.Configure<GirisSecenekleri>(builder.Configuration.GetSection(GirisSecenekleri.Bolum));
builder.Services.AddSingleton<GirisSinirlayici>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Giris";
        o.LogoutPath = "/Cikis";
        o.AccessDeniedPath = "/Giris";
        o.Cookie.Name = "PolsanIzin.Oturum";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        // Spec 0002 R-7: 8 saat hareketsizlik. Varsayılan kayan süre çerezi yalnızca sürenin yarısı
        // geçince yeniler; her istekte yenileyerek "son istekten itibaren 8 saat" sağlanır (plan Risk 4).
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
        o.Events.OnValidatePrincipal = baglam =>
        {
            baglam.ShouldRenew = true;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(o =>
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddRazorPages();

var app = builder.Build();

var demoVeriYukle = app.Environment.IsDevelopment();
CalisanlarModulu.SemaKur(app.Services, demoVeriYukle);
TakvimModulu.SemaKur(app.Services, demoVeriYukle);
BakiyelerModulu.SemaKur(app.Services, demoVeriYukle);
IzinlerModulu.SemaKur(app.Services, demoVeriYukle);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Hata");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

public partial class Program;
