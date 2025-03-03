using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class Item
    {
        public int id { get; set; }
        public string name { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
        public string type { get; set; }
        public string remuneration_type { get; set; }
        public string amount { get; set; }
        public bool? income_tax { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public string calculation_method { get; set; }
        public bool? base_extra_hours { get; set; }
        public string currency { get; set; }
        public bool? gratification { get; set; }
        public bool? base_seventh_work_day { get; set; }
        public bool? advance_payment_day { get; set; }
        public string uf_day { get; set; }
        public bool? taxable { get; set; }
        public bool? base_contribution_license { get; set; }
        public string amount_type { get; set; }
        public string formula { get; set; }
        public string calculation_description { get; set; }
        public bool? blocked { get; set; }
        public string group_ine { get; set; }
        public bool? affects_overdraft { get; set; }
        public string previous_bono_id { get; set; }
        public string editable { get; set; }
        public bool? limit_areas { get; set; }
        public string order_section { get; set; }
        public string order_number { get; set; }
        public bool? requestable { get; set; }
        public string assignable { get; set; }
        public bool? after_section { get; set; }
        public string code { get; set; }
        public string agrupacion_lre { get; set; }

    }
}
