using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SocialNetwork.App_Start
{
    public class CheckReferrer: ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var url = filterContext.HttpContext.Request.Url;

            //var referrer = filterContext.RequestContext.HttpContext.Request.UrlReferrer.AbsolutePath;

            var referrer2 = filterContext.HttpContext.Request.UrlReferrer;


            if (referrer2!=null)
            {
                base.OnActionExecuting(filterContext);

            }
            else
            {

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