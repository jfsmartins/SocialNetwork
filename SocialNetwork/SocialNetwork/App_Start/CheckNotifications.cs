using SocialNetwork.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SocialNetwork.App_Start
{
    class CheckNotifications : ActionFilterAttribute
    {
        //public override void OnActionExecuting(ActionExecutingContext filterContext)
        //{
        //    SocialNetworkDataContext db = new SocialNetworkDataContext();

        //    var notificacoes = (from conv in db.Convidars
        //                        where conv.estado == "espera" && conv.idUtilizadorConvidado == )
        //                      select conv).Count();
        //}

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            SocialNetworkDataContext db = new SocialNetworkDataContext();
            int id = Convert.ToInt32(filterContext.HttpContext.Session.Contents["idUtilizador"]);

            var notificacoes = (from conv in db.Convidars
                                where conv.estado == "espera" && conv.idUtilizadorConvidado == id
                                select conv).Count();

            filterContext.HttpContext.Session.Add("notificacoes", notificacoes);
        }

        //public override void OnActionExecuted(ActionExecutedContext context)
        //{
        //    SocialNetworkDataContext db = new SocialNetworkDataContext();
        //    int id = Convert.ToInt32(context.HttpContext.Session.Contents["idUtilizador"]);

        //    var notificacoes = (from conv in db.Convidars
        //                        where conv.estado == "espera" && conv.idUtilizadorConvidado == id
        //                        select conv).Count();

        //    context.HttpContext.Session.Add("notificacoes", notificacoes);
        //}
    }
}