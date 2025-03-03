using API.BUK.DTO;
using API.BUK.IDAO;
using API.Helpers;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DAO
{
    public class NonWorkedHoursDAO : BUKDAO, INonWorkedHoursDAO
    {
        public void AddNonWorkedHours(NonWorkedHours nwh, SesionVM empresa)
        {
            string uri = "attendances/non-worked-hours";
            new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).PutResponse<object, NonWorkedHours>(uri, nwh);
        }
    }
}
