using PayslipMvcClient.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("PayslipApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:44395/");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Para lang sa local development SSL certificate sa localhost:44395
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Payslip}/{action=Index}/{id?}");

app.Run();