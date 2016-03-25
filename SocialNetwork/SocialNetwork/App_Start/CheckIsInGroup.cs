using SocialNetwork.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SocialNetwork.App_Start
{
    public class CheckIsInGroup: ActionFilterAttribute 
    {
        public string IdParamName { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.ActionParameters.ContainsKey(IdParamName))
            {
                SocialNetworkDataContext db = new SocialNetworkDataContext();
                var idGrupo = filterContext.ActionParameters[IdParamName] as Int32?;
                var idUtilizador = filterContext.HttpContext.Session.Contents["idUtilizador"];

                var pertenceGrupo = db.Perfil_Grupos.Where(x => x.idGrupo == idGrupo && x.idUtilizador == Convert.ToInt32(idUtilizador)).FirstOrDefault();

                if (pertenceGrupo != null)
                {
                    base.OnActionExecuting(filterContext);
                }
                else
                {
                    //filterContext.Result = new RedirectResult("Login");
                    //filterContext.HttpContext.Response.Redirect("/Login");
                    filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                     { "controller", "Home" },
                     { "action", "Feed" }
                    });
                }

            }
        }
    }
}