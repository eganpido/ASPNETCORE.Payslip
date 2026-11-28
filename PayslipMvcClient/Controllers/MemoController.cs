using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PayslipMvcClient.Models;

namespace PayslipMvcClient.Controllers
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class MemoController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string SessionMobileCodeKey = "MobileCode";

        public MemoController(IHttpClientFactory httpClientFactory)
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

            var model = new MemoViewModel
            {
                Code = activeCode
            };

            try
            {
                var client = _httpClientFactory.CreateClient("PayslipApi");
                string url = $"MobileRepPayroll/GetMemos?Code={Uri.EscapeDataString(model.Code)}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    model.ErrorMessage = $"Unable to load memos (HTTP {(int)response.StatusCode}).";
                    return View(model);
                }

                var rawJson = await response.Content.ReadAsStringAsync();
                model.Memos = ParseMemoRecords(rawJson);

                if (!model.Memos.Any())
                {
                    model.ErrorMessage = "No memos found for this account.";
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Error calling the memo API: {ex.Message}";
            }

            return View(model);
        }

        private static List<MemoRecordDto> ParseMemoRecords(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson) || rawJson.Trim() is "null" or "[]")
                return new List<MemoRecordDto>();

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<List<MemoRecordDto>>(rawJson, jsonOptions) ?? new List<MemoRecordDto>();
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("Value", out var valProp) && valProp.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<MemoRecordDto>>(valProp.GetRawText(), jsonOptions) ?? new List<MemoRecordDto>();
                }

                if (root.TryGetProperty("value", out var valPropLower) && valPropLower.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<MemoRecordDto>>(valPropLower.GetRawText(), jsonOptions) ?? new List<MemoRecordDto>();
                }
            }

            return new List<MemoRecordDto>();
        }
    }
}