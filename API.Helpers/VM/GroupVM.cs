using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.VM
{
    public class GroupVM
    {
        public string Description { get; set; }
        public string CostCenter { get; set; }
        public string Path { get; set; }
        public string CustomColumn1 { get; set; }
        public bool Enabled { get; set; }
    }
}
