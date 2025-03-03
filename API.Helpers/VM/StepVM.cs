using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.VM
{
    public class StepVM : TableTupleLogVM
    {
        public string Business { get; set; }
        public string Parameters { get; set; }
        public string stepResult { get; set; }
        public string stepMessage { get; set; }
        public List<string> stepChildIds { get; set; }

       
    }
}
