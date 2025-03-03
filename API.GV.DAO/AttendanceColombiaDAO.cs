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
    public class AttendanceColombiaDAO : AttendanceDAO, IAttendanceDAO
    {
        public override Attendance Get(AttendanceFilter filter, SesionVM empresa)
        {

            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<AttendanceColombia, object>("AttendanceBook/GetAttendanceColombia", new { UserIds = filter.UserIds, StartDate = filter.StartDate, EndDate = filter.EndDate });
            if (result == null)
            {
                throw new Exception("No response from GV");
            }
            return (AttendanceColombia) result;
        }
    }
}
