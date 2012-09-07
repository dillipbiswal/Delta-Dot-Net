// HTML5 placeholder plugin version 0.3
// Enables cross-browser* html5 placeholder for inputs, by first testing
// for a native implementation before building one.
//
// USAGE: 
//$('input[placeholder]').placeholder();

(function ($) {

    $.fn.placeholder = function (options) {
        return this.each(function () {
            if (!("placeholder" in document.createElement(this.tagName.toLowerCase()))) {
                var $this = $(this);
                var placeholder = $this.attr('placeholder');
                $this.val(placeholder).data('color', $this.css('color')).css('color', '#aaa');
                $this
          .focus(function () { if ($.trim($this.val()) === placeholder) { $this.val('').css('color', $this.data('color')) } })
          .blur(function () { if (!$.trim($this.val())) { $this.val(placeholder).data('color', $this.css('color')).css('color', '#aaa'); } });
            }
        });
    };
})(jQuery);

// detect if browser supports transition, currently checks for webkit, moz, opera, ms
var cssTransitionsSupported = false;
(function () {
    var div = document.createElement('div');
    div.innerHTML = '<div style="-webkit-transition:color 1s linear;-moz-transition:color 1s linear;-o-transition:color 1s linear;-ms-transition:color 1s linear;-khtml-transition:color 1s linear;transition:color 1s linear;"></div>';
    cssTransitionsSupported = (div.firstChild.style.webkitTransition !== undefined) || (div.firstChild.style.MozTransition !== undefined) || (div.firstChild.style.OTransition !== undefined) || (div.firstChild.style.MsTransition !== undefined) || (div.firstChild.style.KhtmlTransition !== undefined) || (div.firstChild.style.Transition !== undefined);
    delete div;
})();

// perform JavaScript after the document is scriptable.
$(document).ready(function () {

    //Click handlers for the main menu items
    $("li.mainmenu-item a").click(function () {
        mainMenuTabClick(this);
        return false;
    })

    $('input[placeholder]', "#content").placeholder();

    var target = ".login-box";

    $('input[placeholder]', target).placeholder();
});

$(window).bind('load', function () {
    resizePartialPage();
})

$(window).bind('resize', function () {
    resizePartialPage();
})

$(window).bind('drilldown', function () {
    var target = "#content";

    $('input[placeholder]', target).placeholder();
    
    $("ul.navigation").find("a").click(function () {
        $(window).trigger("hashchange");
        return false;
    });
});

$(window).bind("hashchange", function (e) {
    h = location.hash;
    showContentPageOverlay();
    if (h && h != '#menu') {
        $("a[href!=#]").filter(function () {
            var href = $(this).attr("href");
            return href == h || href == h.replace("#", "");
        }).first().each(function () {
            var a = $(this),
            link = a.attr("href").replace(/^\#/, ""),
            id = link.replace(/[\/\.]/, "-");
            $('#' + id).length && $('#' + id).remove();
            $.ajax(link, {
                dataType: "html",
                cache: false,
                success: function (data, textStatus, jqXHR) {
                    return partialPageDownloaded(data);
                },
                complete: function (jqXHR, textStatus) {
                }
            });
        });
    }
});

function initilaizeMenu(container) {
    var config = {
        sensitivity: 3, // number = sensitivity threshold (must be 1 or higher)    
        interval: 200,  // number = milliseconds for onMouseOver polling interval    
        over: doOpen,   // function = onMouseOver callback (REQUIRED)    
        timeout: 200,   // number = milliseconds delay before onMouseOut    
        out: doClose    // function = onMouseOut callback (REQUIRED)    
    };

    function doOpen() {
        $(this).addClass("hover");
        $('ul:first', this).css('visibility', 'visible');
    }

    function doClose() {
        $(this).removeClass("hover");
        $('ul:first', this).css('visibility', 'hidden');
    }

    $("ul.mainmenu li", "#" + container).hoverIntent(config);

    $("ul.mainmenu li ul li:has(ul)", "#" + container).find("a:first").append(" &raquo; ");
}

function resizePartialPage() {
    var wrapperHeight = $("#wrapper").height();
    var contentHeaderHeight = $("#content-header").height();
    $("#content").height(wrapperHeight - contentHeaderHeight);

    if ($("div.main-content-page").size() != 0) {
        var mainContentTop = $("div.main-content-page").offset().top;
        $("div.main-content-page").height($("#content").height() - 20);
        $("div.main-content-page").trigger("maincontentresize");
    }
}

function mainMenuTabClick(menuitem) {
    showContentPageOverlay();
    var url = $(menuitem).attr('href');

    //set the parent li as the current tab
    $("ul.mainmenu").find("li").each(function () {
        $(this).removeClass("current");

    });

    if ($(menuitem).parents("ul").hasClass("sub-menu")) {
        $(menuitem).parents("ul").parents("li").addClass("current");
    }
    else {
        $(menuitem).parents("li").addClass("current");
    }

    $.ajax(url, {
        dataType: "html",
        cache: false,
        success: function (data, textStatus, jqXHR) {
            return contentPageDownloaded(data);
        },
        complete: function (jqXHR, textStatus) {
        }
    });
}

function showContentPageOverlay() {
    var offset = $("#content").offset();
    $("#content-overlay").css('top', offset.top);
    $("#content-overlay").css('left', offset.left);

    $("#content-overlay").height($("#content").height());
    $("#content-overlay").width($("#content").width());
    
    $("#content-overlay").css('display', 'block');
}

function hideContentPageOverlay() {
    $("#content-overlay").css('display', 'none');
}

function contentPageDownloaded(data) {
    var target = "#content"

    $("div.main-content-page").remove();
    $(data).appendTo($(target));
    $(window).trigger('drilldown');
    resizePartialPage();
    hideContentPageOverlay();
}

function partialPageDownloaded(data) {
    var target = "#page-content";

    $("div.partial-page").remove();
    $(data).appendTo($(target));
    $(window).trigger('drilldown');
    resizePartialPage();
    hideContentPageOverlay();
}

function resizeChildToParent(selector, margin) {
    var parentBottom = $(selector).parent().offset().top + $(selector).parent().height();
    var childContainerTop = $(selector).offset().top;
    $(selector).height(parentBottom - childContainerTop - margin);
}