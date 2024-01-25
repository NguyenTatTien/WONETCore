using System;

namespace WO.Models
{
    public class WOPartPlanDto
    {
        public string PhieuCV { get; set; }
        public string DienGiai { get; set; }
        public string DonVi { get; set; }
        public string BoPhan { get; set; }
        public int thang { get; set; }
        public int Nam { get; set; }
        public DateTime NgayDuKien { get; set; }
        public string VatTu { get; set; }
        public string TenVatTu { get; set; }
        public string NguoiTao { get; set; }
        public string DVT { get; set; }
        public decimal soluong { get; set; }
        public string MaDinhMuc { get; set; }
    }
}
