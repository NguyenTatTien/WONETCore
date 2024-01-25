using System;

namespace WO.Models
{
    public class WOActivitiesDto
    {
        public string FullName { get; set; }
        public decimal HangMuc { get; set; }
        public string TrinhDo { get; set; }
        public string NoiDung { get; set; }
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public decimal SoGioDuKien { get; set; }
        public decimal SoNguoiYeuCau { get; set; }
        public string DinhMucVatTu { get; set; }
        public string DaHoanThanh { get; set; }
        public string ThueNgoai { get; set; }
        public string BaoHanh { get; set; }
    }
}
