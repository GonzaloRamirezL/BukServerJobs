using API.BUK.DTO;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessLogic.Implementation
{
    public class ItemBusiness : IItemBusiness
    {
        public Item GetItem(CompanyConfiguration companyConfiguration, SesionVM empresa, string code)
        {
            try
            {
                List<Item> items = new List<Item>();
                PaginatedResponse<Item> itemsResponse = companyConfiguration.ItemDAO.GetItems(empresa, code);
                if (CollectionsHelper.IsNullOrEmpty<Item>(itemsResponse.data))
                {
                    throw new Exception("No response from BUK");
                }
                else
                {
                    items.AddRange(itemsResponse.data);
                }
            
                return items.Count > 1 ? throw new Exception("Multiple items detected") : items.FirstOrDefault();
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, empresa.Empresa);
                FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, "", "Error al obtener elemento tipo ITEM desde Buk " + ex.ToString(), null, empresa);
                throw new Exception("Incomplete data from BUK");
            }
        }

        public List<AssignedItem> GetAssignsForUser(CompanyConfiguration companyConfiguration, SesionVM empresa, int employeeId, string date)
        {
            try
            {
                List<AssignedItem> assignedItems = new List<AssignedItem>();
                var response = companyConfiguration.ItemDAO.GetAssignsForUser(empresa, employeeId, date);
                if (CollectionsHelper.IsNullOrEmpty<AssignedItem>(response.data))
                {
                    throw new Exception("No response from BUK");
                }
                else
                {
                    assignedItems.AddRange(response.data);
                }
                while (response.pagination != null && !string.IsNullOrWhiteSpace(response.pagination.next))
                {
                    response = companyConfiguration.ItemDAO.GetNext<AssignedItem>(response.pagination.next, empresa.Url, empresa.BukKey, empresa);
                    if (!CollectionsHelper.IsNullOrEmpty<AssignedItem>(response.data))
                    {
                        assignedItems.AddRange(response.data);
                    }
                }

                return assignedItems;
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, empresa.Empresa);
                FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, "", "Error al obtener los items asignados " + ex.ToString(), null, empresa);
                throw new Exception("Incomplete data from BUK");
            }
        }

        public void DeleteAssign(CompanyConfiguration companyConfiguration, SesionVM empresa, int assignId)
        {
            try
            {
                bool response = companyConfiguration.ItemDAO.DeleteAssign(empresa, assignId);
                if (!response)
                {
                    throw new Exception("Assigned item could not be deleted");
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, empresa.Empresa);
                throw new Exception("Assigned item could not be deleted");
            }
        }

        public void AssignItems(CompanyConfiguration companyConfiguration, SesionVM empresa, List<ItemToAssign> itemsToAssign)
        {
            try
            {
                foreach (ItemToAssign item in itemsToAssign)
                {
                    companyConfiguration.ItemDAO.AssignItem(empresa, item);
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, empresa.Empresa);
                throw new Exception("Item could not be delivered");
            }
        }
    }
}
