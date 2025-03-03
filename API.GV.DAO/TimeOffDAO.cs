using API.GV.DTO;
using API.GV.DTO.Filters;
using API.GV.IDAO;
using API.Helpers;
using API.Helpers.VM;
using System;
using System.Collections.Generic;

namespace API.GV.DAO
{
    public class TimeOffDAO : ITimeOffDAO
    {
        public List<TimeOff> GetList(TimeOffFilter filter, SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<List<TimeOff>, object>("TimeOff/Get", new { UserIds = filter.UserIds, StartDate = filter.StartDate, EndDate = filter.EndDate });
            if (result == null)
            {
                throw new Exception("No response from GV");
            }
            return result;
        }

        public bool Add(TimeOffToAdd timeOff, SesionVM empresa)
        {
            
            return new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<bool, TimeOffToAdd>("TimeOff/UnrestrictedUpsert", timeOff);
        }

        public bool Delete(TimeOffToDelete timeOff, SesionVM empresa)
        {
            return new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<bool, TimeOffToDelete>("TimeOff/Delete", timeOff);
        }

        public List<TimeOffType> GetTypes(SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<List<TimeOffType>, object>("TimeOff/GetTypes", new {  });
            if (result == null)
            {
                throw new Exception("No response from GV");
            }
            return result;
        }

        public List<TimeOffType> GetAllTypes(SesionVM empresa)
            
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<List<TimeOffType>, object>("TimeOff/GetAllTypes", new { });
            if (result == null)
            {
                throw new Exception("No response from GV");
            }
            return result;
        }

        public TimeOffType AddType(TimeOffType newType, SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<TimeOffType, TimeOffType>("TimeOff/AddType", newType);
            if (result == null)
            {
                throw new Exception("No response from GV");
            }
            return result;
        }
    }
}
