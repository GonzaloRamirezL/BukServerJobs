using API.GV.DTO;
using API.GV.DTO.Filters;
using API.GV.IDAO;
using API.Helpers;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DAO
{
    public class AttendanceDAO : IAttendanceDAO
    {
        public virtual Attendance Get(AttendanceFilter filter, SesionVM empresa)
        {

            var result =  new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<Attendance, object>("AttendanceBook", new { UserIds = filter.UserIds, StartDate = filter.StartDate, EndDate = filter.EndDate });
            if (result == null)
            {
                throw new Exception("No response from GV");
            }
            return result;
        }

        
    }
}
