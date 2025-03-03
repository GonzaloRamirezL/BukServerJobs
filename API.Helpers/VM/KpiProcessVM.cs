using API.BUK.DTO;
using System.Collections.Generic;

namespace API.Helpers.VM
{
    public class KpiProcessVM
    {
        public List<KpiData> toAdd { get; set; }
        public List<KpiData> toEdit { get; set; }
    }
}
