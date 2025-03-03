using API.GV.DTO;
using API.GV.IDAO;
using API.Helpers;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DAO
{
    public class PositionDAO : IPositionDAO
    {
        public List<PositionVM> AddCompanyPositions(SesionVM empresa, List<PositionDTO> positions)
        {
            return new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<List<PositionVM>, List<PositionDTO>>("Position/AddList", positions);
        }

        public List<PositionVM> GetCompanyPositions(SesionVM empresa)
        {
            return new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<List<PositionVM>, object>("Position/List", new { });
        }
    }
}
