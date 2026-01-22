using Microsoft.AspNetCore.Mvc;
using RestX.BLL.Interfaces;

namespace RestX.Admin.Controllers.BaseControllers
    {
        public class BaseController : Controller
        {
            public readonly IExceptionHandler exceptionHandler;

            public BaseController(IExceptionHandler exceptionHandler)
            {
                this.exceptionHandler = exceptionHandler;
            }
        }
    }
