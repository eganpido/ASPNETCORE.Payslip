using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PayslipMvcClient.Models;

namespace PayslipMvcClient.Controllers
{
    public class PayslipController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PayslipController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? code)
        {
            string activeCode = !string.IsNullOrWhiteSpace(code)
                ? code.Trim()
                : (HttpContext.Session.GetString("MobileCode") ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(activeCode))
            {
                HttpContext.Session.SetString("MobileCode", activeCode);
            }

            var model = new PayslipViewModel
            {
                Code = activeCode
            };

            if (string.IsNullOrWhiteSpace(model.Code))
            {
                return RedirectToAction("Index", "Home");
            }

            // 1. Load Payroll Numbers (pinaka-latest ang ma-select by default)
            await LoadPayrollDropdownAsync(model);

            // 2. Fetch lahos ang payslip para sa default/latest Payroll Number
            if (model.SelectedPayrollId > 0)
            {
                await LoadPayslipRecordsAsync(model);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PayslipViewModel model)
        {
            string activeCode = !string.IsNullOrWhiteSpace(model.Code)
                ? model.Code.Trim()
                : (HttpContext.Session.GetString("MobileCode") ?? string.Empty);

            model.Code = activeCode;

            if (string.IsNullOrWhiteSpace(model.Code))
            {
                return RedirectToAction("Index", "Home");
            }

            await LoadPayrollDropdownAsync(model, preserveSelectedId: model.SelectedPayrollId);

            if (model.SelectedPayrollId <= 0)
            {
                model.ErrorMessage = "Please select a Payroll Number.";
                return View(model);
            }

            await LoadPayslipRecordsAsync(model);
            return View(model);
        }

        private async Task LoadPayslipRecordsAsync(PayslipViewModel model)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PayslipApi");
                string url = $"MobileRepPayroll/GetPaySlip?PayrollId={model.SelectedPayrollId}&Code={Uri.EscapeDataString(model.Code)}";

                var records = await client.GetFromJsonAsync<List<PaySlipRecordDto>>(url);
                model.Records = records ?? new List<PaySlipRecordDto>();

                if (!model.Records.Any())
                {
                    model.ErrorMessage = "No payslip record found for the selected Payroll Number.";
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Error calling the payslip API: {ex.Message}";
            }
        }

        private async Task LoadPayrollDropdownAsync(PayslipViewModel model, int preserveSelectedId = 0)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PayslipApi");
                string url = $"MobileRepPayroll/GetPayrollNumbers?Code={Uri.EscapeDataString(model.Code)}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    model.ErrorMessage = $"Unable to load Payroll Numbers (HTTP {(int)response.StatusCode}).";
                    return;
                }

                var rawJson = await response.Content.ReadAsStringAsync();
                var options = ParsePayrollOptions(rawJson);

                model.PayrollOptions = options;

                if (preserveSelectedId > 0 && options.Any(o => o.Value == preserveSelectedId.ToString()))
                {
                    model.SelectedPayrollId = preserveSelectedId;
                }
                else if (options.Count > 0 && int.TryParse(options[0].Value, out int latestId))
                {
                    // Latest payroll number (first item in the list) is selected by default
                    model.SelectedPayrollId = latestId;
                }
                else
                {
                    model.ErrorMessage ??= "No available Payroll Numbers found for this account.";
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Error loading Payroll Numbers: {ex.Message}";
            }
        }

        private static List<SelectListItem> ParsePayrollOptions(string rawJson)
        {
            var list = new List<SelectListItem>();
            if (string.IsNullOrWhiteSpace(rawJson)) return list;

            using var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return list;

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                string value = GetStringProperty(item, "Id", "id", "PayrollId", "payrollId", "Value", "value");
                string text = GetStringProperty(item, "PayrollNumber", "payrollNumber", "Number", "number", "Text", "text", "Label", "label");

                if (string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(text))
                    value = text;

                if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(value))
                    text = value;

                if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(text))
                {
                    list.Add(new SelectListItem
                    {
                        Value = value,
                        Text = text
                    });
                }
            }

            return list;
        }

        private static string GetStringProperty(JsonElement obj, params string[] names)
        {
            foreach (var name in names)
            {
                if (obj.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null)
                {
                    if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int num))
                        return num.ToString();

                    var str = prop.ToString();
                    if (!string.IsNullOrWhiteSpace(str))
                        return str;
                }
            }
            return string.Empty;
        }
    }
}