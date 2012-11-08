using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using Datavail.Delta.Cloud.Mvc.Models;
using Datavail.Delta.Cloud.Mvc.Models.Config;

namespace Datavail.Delta.Cloud.Mvc.Helpers
{
    public static class DeltaWebHelpers
    {
        #region "AJAX Helpers"
        public static string ImageActionLink(this AjaxHelper helper, string imageUrl, string altText, string actionName, object routeValues, AjaxOptions ajaxOptions)
        {
            var builder = new TagBuilder("img");
            builder.MergeAttribute("src", imageUrl);
            builder.MergeAttribute("alt", altText);
            var link = helper.ActionLink("[replaceme]", actionName, routeValues, ajaxOptions);

            return link.ToString().Replace("[replaceme]", builder.ToString(TagRenderMode.SelfClosing));
        }
        #endregion

        #region "HTML Helpers"
        public static HtmlString MainMenu(this HtmlHelper helper, List<MainMenuItem> menuItems)
        {
            string htmlOutput = string.Empty;

            if (menuItems.Count() > 0)
            {
                if (menuItems.First().IsTopLevelItem)
                {
                    htmlOutput += " <ul class=' mainmenu clearfix'>";
                }
                else
                {
                    htmlOutput += "<ul class='sub_menu'>";
                }

                foreach (var item in menuItems)
                {
                    if (!string.IsNullOrEmpty(item.Class))
                    {
                        htmlOutput += "<li class='" + item.Class + "'>";
                    }
                    else
                    {
                        htmlOutput += "<li>";
                    }

                    htmlOutput += string.Format("<a href='{0}' title='{1}'>{1}</a>",
                                                item.ItemUrl, item.ItemTitle);
                    htmlOutput += helper.MainMenu(item.ChildItems);

                    htmlOutput += "</li>";
                }

                htmlOutput += "</ul>";
            }

            return new HtmlString(htmlOutput);
        }

        public static HtmlString ActionItems(this HtmlHelper helper, List<ActionModel> actionItems)
        {
            string htmlOutput = string.Empty;
            htmlOutput += "<ul class='toolbar'>";

            foreach (var item in actionItems)
            {
                htmlOutput += "<li>";
                htmlOutput += string.Format("<a href='#' title='{0}' class='icon-only edit-customer-button table-button' id='{1}'>" +
                                            "<img src='{2}' alt='{3}'/></a></li>",
                                            item.Title, item.Id, item.Icon, item.Alt);
            }

            htmlOutput += "</ul>";

            return new HtmlString(htmlOutput);
        }

        public static HtmlString NavigationItems(this HtmlHelper helper, List<NavigationModel> navigationItems)
        {
            string htmlOutput = string.Empty;
            htmlOutput += "<ul class='toolbar'>";

            foreach (var item in navigationItems)
            {
                htmlOutput += "<li>";
                htmlOutput += string.Format("<a href='#{0}' title='{1}' class='icon-only table-button'>" +
                                            "<img src='{2}' alt='{3}'/></a></li>",
                                            item.Url, item.Title, item.Icon, item.Alt);
            }

            htmlOutput += "</ul>";

            return new HtmlString(htmlOutput);
        }
        #endregion

    }
}