function apiuriWindowsGridComplete() {

    var tableid = this.id;
    var pageid = 'apiuriwindow-table-container';
    var addButtonId = 'add-apiuriwindow-button';
    var searchButtonId = 'apiuritwindow-table-search-text';
    var addCaption = "Add APIURI Window";
    var editCaption = "Edit APIURI Window";
    var deleteCaption = "Delete APIURI Window";
    var deleteMessage = "Delete the selected APIURI Window(s)?";

    //Setup the add/edit/delete button
    setGridAddButton(addButtonId, pageid, addCaption, 465, function () {
        $("#" + tableid).trigger("reloadGrid");
    });
    setGridEditButton(tableid, editCaption, 465, function () {
        $("#" + tableid).trigger("reloadGrid");
    });
    setGridDeleteButton(tableid, deleteCaption, deleteMessage, 465, function () {
        $("#" + tableid).trigger("reloadGrid");
    });

    //Initialize the search box
    initializeSearch("#" + searchButtonId, "#" + tableid);

    $(".edit-row-button", "#" + tableid).button({
        icons: {
            primary: "ui-icon-pencil"
        },
        text: false
    });
}


function setGridEditButton(tableid, dialogTitle, formWidth, callback) {
    var buttonSelector = ".edit-row-button"
    var tableselector = "#" + tableid;

    $(".small-button.edit-row-button").button({
        icons: {
            primary: "ui-icon-pencil"
        },
        text: false
    });

    //Remove all prior handlers
    $(buttonSelector, tableselector).unbind('click');

    //Setup the edit customer button
    $(buttonSelector, tableselector).click(function () {
        getEditForm($(this).attr('href'), dialogTitle, formWidth, callback);
        return false;
    });
}

function setGridAddButton(buttonid, containerid, dialogTitle, formWidth, callback) {
    var buttonSelector = "#" + buttonid;
    var containerselector = "#" + containerid;

    //Buttons
    $(".add-button", containerselector).button({
        icons: {
            primary: "ui-icon-plusthick"
        },
        text: true
    });

    //Setup the add button
    $(buttonSelector, containerselector).click(function () {
        var url = $(this).attr('href');
        getEditForm(url, dialogTitle, formWidth, callback);
        return false;
    });
}

function getDeleteForm(url, dialogTitle, dialogMessage, formWidth, callback) {
    var container = "<div id='deleteform'></div>";

    //Create the dialog
    if ($("#deleteform").html() === null) {
        $('body').append(container);
    }
    else {
        $("#deleteform").empty();
    }

    $("#deleteform").html("<div id='form-errors-container'><ul></ul></div><div class='form-text'>" + dialogMessage + "</div>");

    $("#deleteform").dialog({
        autoopen: true,
        modal: true,
        draggable: false,
        resizable: false,
        title: dialogTitle,
        show: "slide",
        open: function (event, ui) {
            $('body').css('overflow', 'hidden');
            $('.ui-widget-overlay').css('width', '100%');
        },
        close: function (event, ui) {
            $('body').css('overflow', 'auto');
        },
        width: formWidth,
        height: "auto",
        hide: { effect: "fade", duration: 750 },
        buttons: {
            "Submit": function () {
                $.ajax({
                    data: "",
                    type: "POST",
                    url: url,
                    success: function (data) {

                        if (data.success) {
                            if(callback)
                            {
                                callback();
                            }
                            $("#deleteform").dialog('close');
                        }
                        else {
                            //Show the errors
                            for (var i = 0; i < data.errors.length; i++) {
                                var errorItem = "<li>" + data.errors[i] + "</li>";
                                $("#form-errors-container > ul", "#deleteform").append(errorItem);
                            }
                        }
                    },
                    error: function () {
                        alert("Delete Error")
                    }
                });
            },
            "Cancel": function () {
                $(this).dialog("close");
                $("#deleteform").remove();
            }
        }
    });
}

function getEditForm(url, dialogTitle, formWidth, callback)
{
    var container = "<div id='editform'></div>";

    //Create the dialog
    if ($("#editform").html() === null) {
        $('body').append(container);
    }
    else {
        $("#editform").empty();
    }

    showContentPageOverlay();

    //Make the AJAX call
    $.ajax({
        url: url,
        cache: false,
        success: function (data) {
            $("#editform").html(data);
            $("#editform").dialog({
                autoopen: true,
                modal: true,
                draggable: false,
                resizable: false,
                title: dialogTitle,
                show: {
                    effect: 'slide',
                    complete: function () {
                        setTimeout(function() {
                            try {
                                $(':input:visible', "#editform form")[0].focus();
                            } catch(e) {
                            }
                        }, 40);
                    }
                },
                open: function (event, ui) {
                    $('body').css('overflow', 'hidden');
                    $('.ui-widget-overlay').css('width', '100%');
                },
                close: function (event, ui) {
                    $('body').css('overflow', 'auto');
                },
                width: formWidth,
                height: "auto",
                hide: { effect: "fade", duration: 750 },
                buttons: {
                    "Save": function () {
                        $("form", "#editform").submit();
                    },
                    "Cancel": function () {
                        $(this).dialog("close");
                        $("#editform").remove();
                    }
                }
            });
            hideContentPageOverlay();
            $("form", "#editform").submit(function () {
                if ($(this).valid()) {
                    $.ajax({
                        data: $(this).serialize(),
                        type: "POST",
                        url: $(this).attr('action'),
                        success: function (data) {

                            if (data.success) {
                                if(callback)
                                {
                                    callback(data.id);                                    
                                }
                                $("#editform").dialog('close');
                                $("#editform").remove();
                            }
                            else {
                                //Show the errors
                                var errorObject = {};
                                var errorTarget = $("form input[type='hidden']", "#editform").first().attr('name');
                                for (var i = 0; i < data.errors.length; i++) {
                                    errorObject[errorTarget] = data.errors[i];
                                    $("form", "#editform").validate().showErrors(errorObject);
                                }
                            }
                        },
                        error: function () {
                            alert("Add/Edit Error");
                        }
                    });
                }
                return false;
            })
        },
        error: function () {
            alert("Error retrieving edit data!!");
        },
        type: 'GET'
    });
}

function setGridDeleteButton(tableid, dialogTitle, dialogMessage, formWidth, callback) {
    var buttonSelector = ".delete-row-button";
    var tableselector = "#" + tableid;

    $(".small-button.delete-row-button, .small-button.remove-row-button").button({
        icons: {
            primary: "ui-icon-close"
        },
        text: false
    });

    //Remove all prior handlers
    $(buttonSelector, tableselector).unbind('click');
    $(buttonSelector, tableselector).click(function () {
        getDeleteForm($(this).attr('href'), dialogTitle, dialogMessage, formWidth, callback);
        return false;
    });
}

function initializeSearch(inputIdSelector, tableIdSelector) {
    var searchTimeout;
    $(inputIdSelector).keyup(function () {
        if (searchTimeout != null) {
            clearTimeout(searchTimeout);
        }
        searchTimeout = setTimeout(function () {
            var text = $(inputIdSelector).val();
            //Custom method added to jqgrid for searching
            $(tableIdSelector).jqGrid("SearchValue", "name", text);
        }, 700);
    });
}
//END - Common Functions

