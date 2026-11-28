using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PayslipMvcClient.Models;

namespace PayslipMvcClient.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string SessionMobileCodeKey = "MobileCode";

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Clear session whenever user returns to login / logs off
            HttpContext.Session.Remove(SessionMobileCodeKey);
            HttpContext.Session.Clear();

            return View(new LoginCodeViewModel());
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove(SessionMobileCodeKey);
            HttpContext.Session.Clear();

            return RedirectToAction(nameof(Index));
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
                    HttpContext.Session.Remove(SessionMobileCodeKey);
                    model.ErrorMessage = "Invalid Mobile Code or no matching account found.";
                    return View(model);
                }

                HttpContext.Session.SetString(SessionMobileCodeKey, cleanCode);
                return RedirectToAction("Index", "Payslip");
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