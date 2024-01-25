using System;

namespace WO.Models
{
    public class NormMaterialDto
    {
        public string FullName { get; set; }
        public string DinhMuc { get; set; }
        public string MoTa { get; set; }
        public string PhanLoai { get; set; }
        public string NguoiYeuCau { get; set; }
        public DateTime NgayYeuCau { get; set; }
        public DateTime NgayPheDuyet { get; set; }
        public string DonVi { get; set; }
        public int SoMuc { get; set; }
    }
}
