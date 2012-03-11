﻿using System.Web.Http.Description;
namespace System.Web.Http.ApiExplorer
{
    public class HiddenActionController : ApiController
    {
        public string GetVisibleAction(int id)
        {
            return "visible action";
        }

        [HttpPost]
        [ApiExplorerSettings(IgnoreApi = true)]
        public void AddData()
        {
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public int Get()
        {
            return 0;
        }

        [NonAction]
        public string GetHiddenAction()
        {
            return "Hidden action";
        }
    }
}
