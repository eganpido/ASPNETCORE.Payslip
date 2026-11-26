using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PayslipMvcClient.Models;

namespace PayslipMvcClient.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var savedCode = HttpContext.Session.GetString("MobileCode") ?? string.Empty;
            return View(new LoginCodeViewModel { Code = savedCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginCodeViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                model.ErrorMessage = "Please enter your Mobile Code.";
                return View(model);
            }

            string cleanCode = model.Code.Trim();

            try
            {
                var client = _httpClientFactory.CreateClient("PayslipApi");
                var response = await client.GetAsync($"MobileRepPayroll/IsLoginValid?Code={Uri.EscapeDataString(cleanCode)}");

                if (!response.IsSuccessStatusCode)
                {
                    model.ErrorMessage = $"API error ({(int)response.StatusCode}) while validating the Mobile Code.";
                    return View(model);
                }

                var rawJson = await response.Content.ReadAsStringAsync();

                if (!IsValidLoginResponse(rawJson))
                {
                    model.ErrorMessage = "Invalid Mobile Code or no matching account found.";
                    return View(model);
                }

                HttpContext.Session.SetString("MobileCode", cleanCode);
                return RedirectToAction("Index", "Payslip", new { code = cleanCode });
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Unable to reach the API: {ex.Message}";
                return View(model);
            }
        }

        private static bool IsValidLoginResponse(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson) || rawJson.Trim() is "null" or "[]" or "{}" or "false")
                return false;

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.False)
                return false;

            if (root.ValueKind == JsonValueKind.True)
                return true;

            if (root.ValueKind == JsonValueKind.Array)
                return root.GetArrayLength() > 0;

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var propName in new[] { "IsValid", "isValid", "Valid", "valid", "Success", "success" })
                {
                    if (root.TryGetProperty(propName, out var prop))
                    {
                        if (prop.ValueKind == JsonValueKind.False) return false;
                        if (prop.ValueKind == JsonValueKind.True) return true;
                    }
                }

                foreach (var idName in new[] { "EmployeeId", "employeeId", "Id", "id" })
                {
                    if (root.TryGetProperty(idName, out var idProp) && idProp.TryGetInt32(out int idVal))
                        return idVal > 0;
                }

                return root.EnumerateObject().Any();
            }

            return false;
        }
    }
}