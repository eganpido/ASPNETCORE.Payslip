namespace PayslipMvcClient.Models
{
    public class MemoViewModel
    {
        public string Code { get; set; } = string.Empty;
        public List<MemoRecordDto> Memos { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class MemoRecordDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? MemoDate { get; set; }
        public string? MemoSubject { get; set; }
        public string? MemoContent { get; set; }
        public int PreparedBy { get; set; }
        public int ApprovedBy { get; set; }
        public string? FilePath { get; set; }
    }
}