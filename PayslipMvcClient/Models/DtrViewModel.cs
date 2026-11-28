using Microsoft.AspNetCore.Mvc.Rendering;

namespace PayslipMvcClient.Models
{
    public class DtrViewModel
    {
        public string Code { get; set; } = string.Empty;
        public int SelectedDtrId { get; set; }
        public List<SelectListItem> DtrOptions { get; set; } = new();
        public List<DtrSlipRecordDto> Records { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
    public class DtrSlipRecordDto
    {
        public int DTRId { get; set; }
        public int PayrollOtherDeductionId { get; set; }
        public string? DTRNumber { get; set; }
        public string? DateStart { get; set; }
        public string? DateEnd { get; set; }
        public string? Remarks { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public int EmployeeId { get; set; }
        public string? FullName { get; set; }
        public string? Date { get; set; }
        public string? TimeIn1 { get; set; }
        public string? TimeOut1 { get; set; }
        public string? TimeIn2 { get; set; }
        public string? TimeOut2 { get; set; }
        public bool OfficialBusiness { get; set; }
        public bool OnLeave { get; set; }
        public bool Absent { get; set; }
        public decimal RegularHours { get; set; }
        public decimal NightHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public decimal OvertimeNightHours { get; set; }
        public decimal GrossTotalHours { get; set; }
        public decimal TardyLateHours { get; set; }
        public decimal TardyUndertimeHours { get; set; }
        public decimal NetTotalHours { get; set; }
        public int DayTypeId { get; set; }
        public string? DayType { get; set; }
        public bool RestDay { get; set; }
    }
}