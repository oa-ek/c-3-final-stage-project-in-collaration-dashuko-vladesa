<<<<<<< HEAD
﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsGroup.Web.Controllers
{
    
    [Authorize]
    public class DriverCabinetController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
=======
﻿namespace LogisticsGroup.Web.Controllers
{
    public class DriverCabinetController
    {
    }
}
>>>>>>> main
