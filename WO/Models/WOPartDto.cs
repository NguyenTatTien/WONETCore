using System;

namespace WO.Models
{
    public class WOPartDto
    {
        //public string FullName { get; set; }
        //public string VatTu { get; set; }
        //public string TenVatTu { get; set; }
        //public string DonViTinh { get; set; }
        //public decimal SoLuongKeHoach { get; set; }
        //public string TonKho { get; set; }
        //public string MuaTrucTiep { get; set; }
        //public string Kho { get; set; }
        //public decimal DaSuDung { get; set; }


        public int id { get; set; }
        public string partorganization { get; set; }
        public string dbactivity { get; set; }
        public string dbworkordernum { get; set; }
        public string matlist_lineno { get; set; }
        public string partcode { get; set; }
        public string storecode { get; set; }
        public string usedqty { get; set; }
        public string partuom { get; set; }
        public string manufactpart { get; set; }
        //public string storecode { get; set; }
        public string source { get; set; }
        public string partdescription { get; set; }
        public string stock { get; set; }
        public string preventreorders { get; set; }
        public string parentpart { get; set; }
        public string conditioncode { get; set; }
        public bool trackbycondition { get; set; }
        public string activity { get; set; }
        public string job { get; set; }
        public string activity_display { get; set; }
        public string plannedqty { get; set; }
        public string reservedqty { get; set; }
        public string allocatedqty { get; set; }
        public string reservedforworkorder { get; set; }
        public string workordernum { get; set; }
        public string woattachto { get; set; }
        public string woattachtoorg { get; set; }
        public string multiequip { get; set; }
        public string partlongdescription { get; set; }
    }
}
