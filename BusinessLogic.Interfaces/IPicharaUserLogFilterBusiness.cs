using API.BUK.DTO;
using API.GV.DTO;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IPicharaUserLogFilterBusiness : IUserBusiness
    {
        /// <summary>
        /// Filtra los usuarios por la Columna Personalizada 1 en caso de que se haya especificado un cargo especifico
        /// En caso de no tener ningún valor en la Columna Personalizada 1, se deja un registro en el Storage y se omite
        /// </summary>
        /// <param name="users"></param>
        /// <param name="employees"></param>
        /// <param name="job"></param>
        /// <param name="Empresa"></param>
        /// <returns></returns>
        List<User> LogFilterUsers(List<User> users, List<Employee> employees, SesionVM Empresa, bool filter = true);
        /// <summary>
        /// Filtra a los empleados
        /// </summary>
        /// <param name="employees"></param>
        /// <param name="Empresa"></param>
        /// <returns></returns>
        List<Employee> FilterEmployees(List<Employee> employees, SesionVM Empresa);
    }
}
