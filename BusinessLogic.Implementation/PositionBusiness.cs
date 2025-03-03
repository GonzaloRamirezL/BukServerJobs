using API.GV.DAO;
using API.GV.DTO;
using API.GV.IDAO;
using API.Helpers.VM;
using BusinessLogic.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class PositionBusiness : IPositionBusiness
    {
        private readonly IPositionDAO positionDAO;
        public PositionBusiness()
        {
            this.positionDAO = new PositionDAO();
        }

        public List<PositionVM> AddCompanyPositions(SesionVM empresa, List<PositionDTO> positions)
        {
            return this.positionDAO.AddCompanyPositions(empresa, positions);
        }

        public List<PositionVM> GetCompanyPositions(SesionVM empresa)
        {
            return this.positionDAO.GetCompanyPositions(empresa);
        }
    }
}
