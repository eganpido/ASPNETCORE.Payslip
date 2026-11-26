using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Index()
        {
            return View(new PayslipViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PayslipViewModel model)
        {
            if (model.PayrollId <= 0 || string.IsNullOrWhiteSpace(model.Code))
            {
                model.ErrorMessage = "Palihug i-input ang valid nga PayrollId ug Code.";
                return View(model);
            }

            try
            {
                var client = _httpClientFactory.CreateClient("PayslipApi");
                string url = $"MobileRepPayroll/GetPaySlip?PayrollId={model.PayrollId}&Code={Uri.EscapeDataString(model.Code.Trim())}";

                var records = await client.GetFromJsonAsync<List<PaySlipRecordDto>>(url);
                model.Records = records ?? new List<PaySlipRecordDto>();

                if (!model.Records.Any())
                {
                    model.ErrorMessage = "Walay nakit-an nga payslip record para sa gi-input nga PayrollId ug Code.";
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Error sa pag-tawag sa API: {ex.Message}";
            }

            return View(model);
        }
    }
}