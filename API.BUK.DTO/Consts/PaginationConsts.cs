using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO.Consts
{
    public static class PaginationConsts
    {
        public const int REGISTERS_PER_PAGE = 100;
        public const int DEFAULT_PAGE = 1;
    }

    public static class BukStatusConsts
    {
        public const string APPROVED = "approved";
        public const string REJECTED = "rejected";
    }
}
