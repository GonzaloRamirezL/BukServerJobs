using API.BUK.DTO.Consts;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.Commons
{
    public static class OvertimeTypeHelper
    {
        public static int getType(int value)
        {
            switch (value)
            {
                case 85:
                    return OvertimeIdentifiers.percent85;
                case 75:
                    return OvertimeIdentifiers.percent75;
                case 100:
                    return OvertimeIdentifiers.percent100;
                case 150:
                    return OvertimeIdentifiers.percent150;
                case -55:
                    return OvertimeIdentifiers.percentminus55;
                case -46:
                    return OvertimeIdentifiers.percentminus46;
                case -70:
                    return OvertimeIdentifiers.percentminus70;
                case 30:
                    return OvertimeIdentifiers.percent30;
                default:
                    return OvertimeIdentifiers.percent50;
            }
        }


    }
}
