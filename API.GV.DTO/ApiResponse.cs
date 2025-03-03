using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace API.GV.DTO
{
    public class ApiResponse
    {
        public string _message { get; set; }
        public HttpStatusCode _statusCode { get; set; }
        public string _responseStatus { get; set; }
    }
}
