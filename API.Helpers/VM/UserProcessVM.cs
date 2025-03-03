using API.GV.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.VM
{
    public class UserProcessVM
    {
        public List<User> toAdd { get; set; }
        public List<User> toDeactivate { get; set; }
        public List<User> toActivate { get; set; }
        public List<User> toEdit { get; set; }
        public List<User> toMove { get; set; }
    }
}
