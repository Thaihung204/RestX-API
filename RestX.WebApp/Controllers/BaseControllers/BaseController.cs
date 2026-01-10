using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

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
