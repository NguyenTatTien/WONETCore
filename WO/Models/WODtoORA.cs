using System;

namespace WO.Models
{
    public class WODtoORA
    {
        public string evt_code { get; set; }
        public string gian { get; set; }
        public string bophan { get; set; }
        public string tentbi { get; set; }
        public string matbi { get; set; }
        public string MaBD { get; set; }
        public string TenBD { get; set; }
        public int nhanluc { get; set; }
        public int thoigianBD { get; set; }
        public DateTime? evt_TGBD { get; set; }
        public DateTime? evt_TGKT { get; set; }
        public bool TTHDThietBi { get; set; }
        public bool KQNghiemThu { get; set; }
        public string LanhDaoGian { get; set; }
        public string KSBBDCS
        {
            get; set;
        }
        public string TruongBoPhan { get; set; }

        public string KSVanHanh { get; set; }
        public double TGLVThietBi { get; set; }
        public string Org_TBi { get; set; }
        public string NgayLanhDao { get; set; }
        public string NgayTruongBoPhan { get; set; }
        public string NgayKSVH { get; set; }
        public string NgayKSBBDSC { get; set; }
        public string CapBD { get; set; }
        public string ViTriLapDat { get; set; }
        public string TinhTrangThietBi { get; set; }
        public string NguoiChoPhep { get; set; }
        public string ChucDanh { get; set; }
    }
}
