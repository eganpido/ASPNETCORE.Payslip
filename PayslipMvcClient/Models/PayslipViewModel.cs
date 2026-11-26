using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PayslipMvcClient.Models
{
    public class PayslipViewModel
    {
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Payroll Number")]
        public int SelectedPayrollId { get; set; }

        public List<SelectListItem> PayrollOptions { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public List<PaySlipRecordDto> Records { get; set; } = new();
    }

    public class PaySlipRecordDto
    {
        public bool IsLocked { get; set; }
        public int PayrollId { get; set; }
        public string? PayrollNumber { get; set; }
        public DateTime? PayrollDate { get; set; }
        public string? Remarks { get; set; }
        public string? Company { get; set; }
        public int EmployeeId { get; set; }
        public string? FullName { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal OtherSalary { get; set; }
        public decimal TotalSalaryAmount { get; set; }
        public decimal TotalTardyAmount { get; set; }
        public decimal TotalAbsentAmount { get; set; }
        public decimal GrossIncome { get; set; }
        public decimal TotalOtherIncomeNonTaxable { get; set; }
        public decimal GrossIncomeWithNonTaxable { get; set; }
        public decimal SSSContribution { get; set; }
        public decimal PHICContribution { get; set; }
        public decimal HDMFContribution { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalDeduction { get; set; }
        public decimal TotalOtherDeduction { get; set; }
        public decimal NetIncome { get; set; }
        public decimal TotalWorkingHours { get; set; }
        public string? OtherDeductionBreakdown { get; set; }
        public string? OtherIncomeNonTaxableBreakdown { get; set; }
        public string? LoanBalancesBreakdown { get; set; }
        public decimal LoanBalances { get; set; }
        public string? LeaveBalanceBreakdown { get; set; }
        public string? OtBreakdown { get; set; }
    }
}