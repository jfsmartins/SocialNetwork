using SocialNetwork.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;

namespace SocialNetwork.App_Start
{
    class CheckAuthentication : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //SocialNetworkDataContext db = new SocialNetworkDataContext();
            var id = filterContext.HttpContext.Session.Contents["idUtilizador"];

            if (id != null)
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
                     { "action", "Login" }
                });
            }

            //var notificacoes = (from conv in db.Convidars
            //                    where conv.estado == "espera" && conv.idUtilizadorConvidado == id
            //                    select conv).Count();

            //filterContext.HttpContext.Session.Add("notificacoes", notificacoes);
        }
    }
}
