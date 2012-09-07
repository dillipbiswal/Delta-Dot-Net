var Site = {};
var ViewBag = {};

$(function () {

    $("#tabset").tabs();
    
	//Apply defaults
	$("input[type=button], input[type=submit], a.button, span.button").button();

	$.extend($.ui.dialog.prototype.options, {
		width: 500,
		height: 350
	});

	$.ajaxSetup({ traditional: true });
});