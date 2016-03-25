using SocialNetwork.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SocialNetwork.App_Start
{
    class CheckLogin : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            SocialNetworkDataContext db = new SocialNetworkDataContext();
            int id = 0;
            int estado = 0;
            id = Convert.ToInt32(filterContext.HttpContext.Session.Contents["idUtilizador"]);
            estado  = Convert.ToInt32(filterContext.HttpContext.Session.Contents["idUtilizador"]);

            HttpSessionStateBase session = filterContext.HttpContext.Session;
            Controller controller = filterContext.Controller as Controller;

            //if (estado== 666)
            //{
            //    base.OnActionExecuting(filterContext);
            //}
            //else
            //{
            //    filterContext.Result = new RedirectResult("Login");
            //}
            if (id!= 0)
            {
                base.OnActionExecuting(filterContext);
            }
            else
            {
                //filterContext.Result = new JsonResult() { Data = "Login", JsonRequestBehavior = JsonRequestBehavior.AllowGet };
                //base.OnActionExecuting(filterContext);

                //filterContext.Result = new ViewResult
                //{
                //    ViewName = "Login"
                //};

                filterContext.Result = new HttpUnauthorizedResult();
                //filterContext.Result = new RedirectToRouteResult(
                //new RouteValueDictionary
                //{
                //     { "controller", "Home" },
                //     { "action", "Login" }
                //});


                //filterContext.Result = new RedirectResult("Login");

                // filterContext.HttpContext.Response.Redirect("/Login");
            }
        }
    }
}
