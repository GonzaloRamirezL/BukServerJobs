using API.BUK.DTO;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.IDAO
{
    public interface INonWorkedHoursDAO : IBUKDAO
    {
        void AddNonWorkedHours(NonWorkedHours nwh, SesionVM empresa);
    }
}
