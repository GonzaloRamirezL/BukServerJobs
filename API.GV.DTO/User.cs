using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class User
    {
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Identifier { get; set; }
        public string Email { get; set; }
        public string Adress { get; set; }
        public string Phone { get; set; }
        public Nullable<short> Enabled { get; set; }
        public string Custom1 { get; set; }
        public string Custom2 { get; set; }
        public string Custom3 { get; set; }
        public string GroupIdentifier { get; set; }
        public string GroupDescription { get; set; }
        public string ContractDate { get; set; }
        public string UserProfile { get; set; }
        public string userScheduler { get; set; }
        public string userCompanyIdentifier { get; set; }
        public string weeklyHoursCode { get; set; }
        public string endContractDate { get; set; }
        public string positionIdentifier { get; set; }
        public string positionName { get; set; }
        public string integrationCode { get; set; }
        public string userCard { get; set; }
        public string legalSundayIndicator { get; set; }
        public string legacyHash { get; set; }
        public string PIS { get; set; }
        public string TipoLoginSSO { get; set; }
        public string HideInReports { get; set; }

        public override string ToString()
        {
            return "Rut: " + Identifier +
                " Apellido: " + LastName +
                " Nombre: " + Name +
                " Email: " + Email +
                " Cód. Integración: " + integrationCode +
                " Grupo (CC): " + GroupIdentifier;
        }
    }
}
