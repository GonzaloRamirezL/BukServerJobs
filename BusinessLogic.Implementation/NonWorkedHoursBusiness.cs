using API.BUK.DTO;
using API.BUK.IDAO;
using API.Helpers;
using API.Helpers.VM;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class NonWorkedHoursBusiness :  INonWorkedHoursBusiness
    {
        public void AddNonWorkedHours(NonWorkedHours nwh, SesionVM empresa, CompanyConfiguration companyConfiguration)
        {
            companyConfiguration.NonWorkedHoursDAO.AddNonWorkedHours(nwh, empresa);
        }
    }
}
