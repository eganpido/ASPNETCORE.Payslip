using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PayslipMvcClient.Models;

namespace PayslipMvcClient.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class DtrController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string SessionMobileCodeKey = "MobileCode";

        public DtrController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string activeCode = HttpContext.Session.GetString(SessionMobileCodeKey) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(activeCode))
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new DtrViewModel
            {
                Code = activeCode
            };

            await LoadDtrDropdownAsync(model);

            if (model.SelectedDtrId > 0)
            {
                await LoadDtrSlipAsync(model);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DtrViewModel model)
        {
            string activeCode = HttpContext.Session.GetString(SessionMobileCodeKey) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(activeCode))
            {
                return RedirectToAction("Index", "Home");
            }

            model.Code = activeCode;

            await LoadDtrDropdownAsync(model, preserveSelectedId: model.SelectedDtrId);

            if (model.SelectedDtrId <= 0)
            {
                model.ErrorMessage = "Please select a DTR period.";
                return View(model);
            }

            await LoadDtrSlipAsync(model);
            return View(model);
        }

        private async Task LoadDtrDropdownAsync(DtrViewModel model, int preserveSelectedId = 0)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PayslipApi");
                string url = $"MobileRepPayroll/GetDTRs?Code={Uri.EscapeDataString(model.Code)}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    model.ErrorMessage = $"Unable to load DTR list (HTTP {(int)response.StatusCode}).";
                    return;
                }

                var rawJson = await response.Content.ReadAsStringAsync();
                var options = ParseDtrOptions(rawJson);

                model.DtrOptions = options;

                if (preserveSelectedId > 0 && options.Any(o => o.Value == preserveSelectedId.ToString()))
                {
                    model.SelectedDtrId = preserveSelectedId;
                }
                else if (options.Count > 0 && int.TryParse(options[0].Value, out int latestId))
                {
                    model.SelectedDtrId = latestId;
                }
                else
                {
                    model.ErrorMessage ??= "No available DTR records found for this account.";
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Error loading DTR list: {ex.Message}";
            }
        }

        private async Task LoadDtrSlipAsync(DtrViewModel model)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("PayslipApi");
                string url = $"MobileRepPayroll/GetDTRSlip?DTRId={model.SelectedDtrId}&Code={Uri.EscapeDataString(model.Code)}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    model.ErrorMessage = $"Unable to load DTR details (HTTP {(int)response.StatusCode}).";
                    return;
                }

                var rawJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(rawJson) || rawJson.Trim() is "null" or "[]")
                {
                    model.ErrorMessage = "No DTR records found for the selected period.";
                    return;
                }

                List<DtrSlipRecordDto>? records = null;
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    records = JsonSerializer.Deserialize<List<DtrSlipRecordDto>>(rawJson, jsonOptions);
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("Value", out var valProp) && valProp.ValueKind == JsonValueKind.Array)
                    {
                        records = JsonSerializer.Deserialize<List<DtrSlipRecordDto>>(valProp.GetRawText(), jsonOptions);
                    }
                    else if (root.TryGetProperty("value", out var valPropLower) && valPropLower.ValueKind == JsonValueKind.Array)
                    {
                        records = JsonSerializer.Deserialize<List<DtrSlipRecordDto>>(valPropLower.GetRawText(), jsonOptions);
                    }
                }

                model.Records = records ?? new List<DtrSlipRecordDto>();

                if (!model.Records.Any())
                {
                    model.ErrorMessage = "No DTR records found for the selected period.";
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Error calling the DTR API: {ex.Message}";
            }
        }

        private static List<SelectListItem> ParseDtrOptions(string rawJson)
        {
            var list = new List<SelectListItem>();
            if (string.IsNullOrWhiteSpace(rawJson)) return list;

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            JsonElement arrayElement = root;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("Value", out var valProp) && valProp.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = valProp;
                }
                else if (root.TryGetProperty("value", out var valPropLower) && valPropLower.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = valPropLower;
                }
                else
                {
                    return list;
                }
            }

            if (arrayElement.ValueKind != JsonValueKind.Array) return list;

            foreach (var item in arrayElement.EnumerateArray())
            {
                string value = GetStringProperty(item, "Id", "id", "DTRId", "dtrId", "DtrId", "dtrid", "Value", "value");
                string text = GetStringProperty(
                    item,
                    "Dtrnumber", "DTRNumber", "dtrNumber", "dtrnumber",
                    "Period", "period",
                    "Dtrdate", "DTRDate", "dtrDate",
                    "DateRange", "dateRange",
                    "Description", "description",
                    "Text", "text", "Label", "label");

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