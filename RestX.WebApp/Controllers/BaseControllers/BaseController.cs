using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RestX.BLL.Interfaces;
using IExceptionHandler = RestX.BLL.Interfaces.IExceptionHandler;

namespace RestX.Admin.Controllers.BaseControllers
    {
        public class BaseController : Controller
        {
            public readonly BLL.Interfaces.IExceptionHandler exceptionHandler;

            public BaseController(BLL.Interfaces.IExceptionHandler exceptionHandler)
            {
                this.exceptionHandler = exceptionHandler;
            }
        }
    }
