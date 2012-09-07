function maintWindowsGridComplete(){
    
    var tableid = this.id;
    var pageid = 'maintwindow-table-container';
    var addButtonId = 'add-maintwindow-button';
    var searchButtonId = 'maintwindow-table-search-text';
    var addCaption = "Add Maintenance Window";
    var editCaption = "Edit Maintenance Window";
    var deleteCaption = "Delete Maintenance Window";
    var deleteMessage = "Delete the selected Maintenace Window(s)?";

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

function metricThresholdGridComplete() {
    var tableid = this.id;
    var pageid = 'metricthresholds-table-container';
    var addButtonId = 'add-metricthresholds-button';
    var searchButtonId = 'metricthresholds-table-search-text';
    var addCaption = "Add Metric Threshold";
    var editCaption = "Edit Metric Threshold";
    var deleteCaption = "Delete Metric Threshold";
    var deleteMessage = "Delete the selected Metric Threshold(s)?";

    //Setup the add/edit/delete button
    setGridAddButton(addButtonId, pageid, addCaption, 475, function () {
        $("#" + tableid).trigger("reloadGrid"); 
    });
    setGridEditButton(tableid, editCaption, 475, function () {
        $("#" + tableid).trigger("reloadGrid"); 
    });
    setGridDeleteButton(tableid, deleteCaption, deleteMessage, 400, function () {
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

function scheduleGridComplete() {
    var tableid = this.id;
    var pageid = 'schedules-table-container';
    var addButtonId = 'add-schedules-button';
    var searchButtonId = 'schedules-table-search-text';
    var addCaption = "Add Schedule";
    var editCaption = "Edit Schedule";
    var deleteCaption = "Delete Schedule";
    var deleteMessage = "Delete the selected Schedule(s)?";

    //Setup the add/edit/delete button
    setGridAddButton(addButtonId, pageid, addCaption, 440, function () {
        $("#" + tableid).trigger("reloadGrid"); 
    });
    setGridEditButton(tableid, editCaption, 440, function () {
        $("#" + tableid).trigger("reloadGrid"); 
    });
    setGridDeleteButton(tableid, deleteCaption, deleteMessage, 400, function () {
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

function userGridComplete() {
    var tableid = this.id;
    var pageid = 'users-table-container';
    var addButtonId = 'add-user-button';
    var searchButtonId = 'users-table-search-text';
    var addCaption = "Add User";
    var editCaption = "Edit User";
    var deleteCaption = "Delete User";
    var deleteMessage = "Delete the selected User(s)?";

    //Setup the add/edit/delete button
    setGridAddButton(addButtonId, pageid, addCaption, 540,function () {
        $("#" + tableid).trigger("reloadGrid"); 
    });
    setGridEditButton(tableid, editCaption, 540, function () {
        $("#" + tableid).trigger("reloadGrid"); 
    });
    setGridDeleteButton(tableid, deleteCaption, deleteMessage, 400, function () {
        $("#" + tableid).trigger("reloadGrid"); 
    });

    //Initialize the search box
    initializeSearch("#" + searchButtonId, "#" + tableid);

    var dropActions = {};
}

function metricInstanceGridComplete() {
    var tableid = this.id;
    var pageid = 'metricinstances-table-container';
    var addButtonId = 'add-metricinstance-button';
    var searchButtonId = 'metricinstances-table-search-text';
    var addCaption = "Add Metric Instance";
    var editCaption = "Edit Metric Instance";
    var deleteCaption = "Delete Metric Instance";
    var deleteMessage = "Delete the selected Metric Instances(s)?";

    $(".small-button.add-button", "#" + pageid).button({
        icons: {
            primary: "ui-icon-plusthick"
        },
        text: false
    });

    //Setup the add/edit/delete button
    setGridDeleteButton(tableid, deleteCaption, deleteMessage, 400, function() {
        $("#" + tableid).trigger("reloadGrid");
    });

    //Initialize the search box
    initializeSearch("#" + searchButtonId, "#" + tableid);

    var dropActions = {};

    setStatusIndicator(tableid, 'name');

    //The metric instance doen't use the built in table add/edit, it launches a custom form returned via AJAX
    setMetricInstanceGridEditButton(tableid, function () { 
        $("#" + tableid).trigger("reloadGrid");
    });

    $("#add-metricinstance-button").unbind('click');
    $("#add-metricinstance-button").click(function () {
        var url = $(this).attr('href');
        selectMetric(url, function () { 
            $("#" + tableid).trigger("reloadGrid");
            $("#hierarchy-container").addClass("loading");
            $("#maintenance-hierarchy").jstree("refresh");
        }, 
        function () { 
            alert("Error Creating Metric Instance!!"); 
        });
        return false;
    });
}
//End - Grid Complete Methods

//Begin - Common Functions
function setStatusIndicator(tableid, columnname) {
    $("tr.jqgrow", "#" + tableid).each(function () {
        var status = $("td[aria-describedby='" + tableid + "_status']", $(this)).text();

        if (status == 'Active') {
            $("td[aria-describedby='" + tableid + "_" + columnname + "']", $(this)).addClass("status-active");
        }
        else if (status == 'InMaintenance') {
            $("td[aria-describedby='" + tableid + "_" + columnname + "']", $(this)).addClass("status-inmaintenance");
        }
        else if (status == 'Deleted') {
            $("td[aria-describedby='" + tableid + "_" + columnname + "']", $(this)).addClass("status-deleted");
        }
    });
}

function selectMetric(url, successCallback, errorCallback)
{
    var container = "<div id='add-metricinstance-container'></div>";
    var loading = "<img src='Content/images/ajax-loader-trans.gif' alt='' /><span>Loading...</span>";

    //Create the dialog
    if ($("#add-metricinstance-container").html() === null) {
        $('body').append(container);
    }
    else {
        $("#add-metricinstance-container").empty();
    }

    $("#add-metricinstance-container").html(loading);

    $("#add-metricinstance-container").dialog({
        autoopen: true,
        modal: true,
        draggable: false,
        resizable: false,
        title: "Select Metric",
        show: "slide",
        width: 470,
        height: 150,
        hide: { effect: "fade", duration: 750 },
        buttons: {
            "Submit": function () {
                $("form", "#add-metricinstance-container").submit();
                $("#add-metricinstance-container").empty().append(loading);
            },
            "Cancel": function () {
                $(this).dialog("close");
                $("#add-metricinstance-container").remove();
            }
        }
    });

    //Make the AJAX call
    $.ajax({
        url: url,
        cache: false,
        success: function (data) {
            $("#add-metricinstance-container").html(data);
            $("form", "#add-metricinstance-container").submit(function () {
                $.ajax({
                    data: $(this).serialize(),
                    type: "POST",
                    url: $(this).attr('action'),
                    success: function (data) {
                        $("#add-metricinstance-container").dialog('close');
                        addMetricInstanceData(data, successCallback);
                    },
                    error: function () {
                        errorCallback();
                    }
                });
                return false;
            })
        },
        error: function () {
            alert("Error submiting metric!!");
        },
        type: 'GET'
    });

    return false;
}

function addMetricInstanceData(data, successCallback) {
    //Create the dialog
    if ($("#metric-data").html() === null) {
        var container = "<div id='metric-data' style='display:none'></div>";
            $('body').append(container);
    }
    else {
        $("#metric-data").empty();
    }

    $("#metric-data").append(data);

    //Validation setup
    $.validator.addMethod("multivaluerequired", function (value, element) {
        var multiValueCount = $(element).parents("tr.FormData").next("tr.FormData").find("tr.multivalue-item").size();
        if (multiValueCount > 0) {
            return true;
        }

        return false;
    }, "One or more values is required");

    $.validator.addClassRules("multivaluerequired", {
        multivaluerequired: true
    });

    $("#metric-data-form").validate({
        errorContainer: $("#form-errors-container", "#metric-data-form"),
        errorLabelContainer: $("ul", $("#form-errors-container", "#metric-data-form")),
        wrapper: 'li',
        onfocusout: function (element) {
            if (!this.checkable(element) && !$(element).hasClass('multivaluerequired') && (element.name in this.submitted || !this.optional(element))) {
                this.element(element);
            }
        },
        onkeyup: function (element) {
            if (element.name in this.submitted || element == this.lastElement && !$(element).hasClass('multivaluerequired')) {
                this.element(element);
            }
        },
    });

    if ($("form", $("#metric-data")).size() != 0) {
        $("#metric-data").dialog({
            autoopen: true,
            modal: true,
            draggable: false,
            resizable: false,
            title: "Metric Data",
            show: { effect: 'slide', complete: function () {
                setTimeout(function () {
                    try { $(':input:visible', "#metric-data")[0].focus(); } catch (e) { }
                }, 50)
            } 
            }, 
            width: 600,
            hide: { effect: "fade", duration: 1000 },
            buttons: {
                "Submit": function () {
                    $("form", "#metric-data").submit();
                },
                "Cancel": function () {
                    $(this).dialog("close");
                    $("#metric-data").remove();
                }
            }
        });

        $("form", "#metric-data").submit(function () {

            if ($(this).valid()) {
                $.ajax({
                    data: $(this).serialize(),
                    type: "POST",
                    url: $(this).attr('action'),
                    success: function (data) {
                        $("#metric-data").empty();
                        $("#metric-data").append(data);
                        successCallback();

                        setTimeout(function () {
                            $("#metric-data").dialog('close');
                        }, 2000);

                    },
                    error: function () {
                        alert("Error retrieving metric data!!")
                    }
                });
            }
            return false;
        });
    }
    else {
        $("#metric-data").dialog({
            autoopen: true,
            modal: true,
            draggable: false,
            resizable: false,
            title: "Metric Added",
            show: "slide",
            width: 400,
            hide: { effect: "fade", duration: 1000 }
        });

        setTimeout(function () {
            $("#metric-data").dialog('close');
            successCallback();
            }, 1000);
    }
}

function addMetricThreshold(tableid, data) {
    //Create the dialog
    if ($("#metric-data").html() === null) {
        var container = "<div id='metric-data' style='display:none'></div>";
            $('body').append(container);
    }
    else {
        $("#metric-data").empty();
    }

    $("#metric-data").append(data);

    //Validation setup
    $.validator.addMethod("multivaluerequired", function (value, element) {
        var multiValueCount = $(element).parents("tr.FormData").next("tr.FormData").find("tr.multivalue-item").size();
        if (multiValueCount > 0) {
            return true;
        }

        return false;
    }, "One or more values is required");

    $.validator.addClassRules("multivaluerequired", {
        multivaluerequired: true
    });

    $("#metric-data-form").validate({
        onfocusout: function (element) {
            if (!this.checkable(element) && !$(element).hasClass('multivaluerequired') && (element.name in this.submitted || !this.optional(element))) {
                this.element(element);
            }
        },
        onkeyup: function (element) {
            if (element.name in this.submitted || element == this.lastElement && !$(element).hasClass('multivaluerequired')) {
                this.element(element);
            }
        },
        errorPlacement: function (error, element) {
            error.appendTo(element.parents("tr").children("td.ErrorTD"));
        }
    });

    if ($("form", $("#metric-data")).size() != 0) {
        $("#metric-data").dialog({
            autoopen: true,
            modal: true,
            draggable: false,
            resizable: false,
            title: "Metric Data",
            show: { effect: 'slide', complete: function () {
                setTimeout(function () {
                    try { $(':input:visible', "#metric-data")[0].focus(); } catch (e) { }
                }, 50)
            } 
            }, 
            width: 600,
            hide: { effect: "fade", duration: 1000 },
            buttons: {
                "Submit": function () {
                    $("form", "#metric-data").submit();
                },
                "Cancel": function () {
                    $(this).dialog("close");
                    $("#metric-data").remove();
                }
            }
        });

        $("form", "#metric-data").submit(function () {

            if ($(this).valid()) {
                $.ajax({
                    data: $(this).serialize(),
                    type: "POST",
                    url: $(this).attr('action'),
                    success: function (data) {
                        $("#metric-data").empty();
                        $("#metric-data").append(data);
                        $("#" + tableid).trigger("reloadGrid");

                        setTimeout(function () {
                            $("#metric-data").dialog('close');
                        }, 2000);

                    },
                    error: function () {
                        alert("Error retrieving metric data!!")
                    }
                });
            }
            return false;
        });
    }
    else {
        $("#metric-data").dialog({
            autoopen: true,
            modal: true,
            draggable: false,
            resizable: false,
            title: "Metric Added",
            show: "slide",
            width: 400,
            hide: { effect: "fade", duration: 1000 }
        });

        setTimeout(function () {
            $("#metric-data").dialog('close');
            $("#" + tableid).trigger("reloadGrid"); 
            }, 2000);
    }
}

function addNodesToCluster(tableid) {
    var dragdata = $("#" + tableid).data("dragdata");
    var dragtable = $('body').data("dragtable");
    $("#save-progress").dialog("open");

    $.ajax({
        url: 'Config/AddNodesToCluster',
        data: JSON.stringify(dragdata),
        success: function (data) {
            if (data.result == true) {
                $("#save-progress").html("The servers have been added to the cluster");
                
                $.each(dragdata, function () {
                    $('#' + dragtable).jqGrid('delRowData', this.dragid)
                })
                
                $("#" + tableid).removeData("dragdata");
                setTimeout(function () { $("#save-progress").dialog("close"); }, 500);
            }
            else {
                //TODO handle error in acitvation
            }
        },
        error: function () {
            alert("Cluster add Error!!");
        },
        type: 'POST',
        contentType: 'application/json, charset=utf-8',
        dataType: 'json'
    });

}

function addServersToServerGroup(tableid) {
    var dragdata = $("#" + tableid).data("dragdata");

    $("#save-progress").dialog("open");

    $.ajax({
        url: 'Config/AddServersToGroups',
        data: JSON.stringify(dragdata),
        success: function (data) {
            if (data.result == true) {
                $("#save-progress").html("The servers have been added to the group");
                $("#" + tableid).removeData("dragdata");
                setTimeout(function () { $("#save-progress").dialog("close"); }, 500);
            }
            else {
                //TODO handle error in acitvation
                alert("Server add error")
            }
        },
        error: function () {
            alert("Server Add Error!!");
        },
        type: 'POST',
        contentType: 'application/json, charset=utf-8',
        dataType: 'json'
    });
}

function setupConfirmDialog(tableid, confirmTitle, confirmbody, confirmAction, progressTitle) {

    var confirmDiv = "<div id='confirm'></div>"
    var saveProgressDiv = "<div id='save-progress'></div>";

    //Add if necessary, empty out old data
    if ($("#confirm").html() === null) {
        $('body').append(confirmDiv);
    }
    else {
        $("#confirm").empty();
    }

    if ($("#save-progress").html() === null) {
        $('body').append(saveProgressDiv);
    }
    else {
        $("#save-progress").empty();
    }

    //Add the confirm body
    $("#confirm").html($(confirmbody));

    //Add save progress indicator
    $("#save-progress").html($("<img src='Content/images/ajax-loader-trans.gif' alt='' /><span>Saving</span>"));

    //Destroy existing dialogs
    $("#confirm").dialog("destroy");
    $("#save-progress").dialog("destroy");

    //Setup the confirmation modal
    $("#confirm").dialog({
        autoopen: false,
        modal: true,
        draggable: false,
        resizable: false,
        title: confirmTitle,
        show: "slide",
        hide: { effect: "fade", duration: 500 },
        buttons: {
            "Ok": function () {
                confirmAction(tableid);
                $(this).dialog("close");
            },
            "Cancel": function () {
                $(this).dialog("close");
                $("#" + tableid + "").removeData("dragdata");
                $("#" + tableid + "").removeData("actiondata");
            }
        }
    });



    //Setup the save progress modal
    $("#save-progress").dialog({
        autoOpen: false,
        modal: true,
        draggable: false,
        resizable: false,
        title: progressTitle,
        show: "slide",
        hide: { effect: "fade", duration: 500 }
    });

    //Show the confirm
    $("#confirm").dialog("open");
}

function activateServers(tableid) {
    var dragdata = $("#" + tableid + "").data("dragdata");
    var dragtable = $('body').data("dragtable");

    $("#save-progress").dialog("open");

    $.ajax({
        url: 'Config/ActivateServer',
        data: JSON.stringify(dragdata),
        success: function (data) {
            if (data.result == true) {
                $("#save-progress").html("The servers have been activated!");

                $.each(dragdata, function () {
                    $('#' + dragtable).jqGrid('delRowData', this.dragid)
                })
                $("#" + tableid + "").removeData("dragdata");
                setTimeout(function () { $("#save-progress").dialog("close"); }, 750);
            }
            else {
                alert("Server Activation error!!");
            }
        },
        error: function () {
            alert("Server Activation error!!");
        },
        type: 'POST',
        contentType: 'application/json, charset=utf-8',
        dataType: 'json'
    });
}

function metricInstanceEdit(url, successCallback){
    var editContainer = "<div id='edit-metricinstance-container'></div>"
    
    if ($("#edit-metric-instance-container").html() === null) {
            $('body').append("<div id='edit-metric-instance-container'></div>");
        }
        else {
            $("#edit-metric-instance-container").empty()
        }

        $("#edit-metric-instance-container").html($("<img src='Content/images/ajax-loader-trans.gif' alt='' /><span>Loading Metric Data</span>"));

        $("#edit-metric-instance-container").dialog({
            autoopen: true,
            modal: true,
            draggable: false,
            resizable: false,
            title: "Edit Metric Instance",
            show: "slide",
            width: 300,
            height: 125,
            hide: { effect: "fade", duration: 500 }
        });

        $.ajax({
            url: url,
            cache: false,
            success: function (data) {
                $("#edit-metric-instance-container").dialog('close');
                addMetricInstanceData(data, successCallback);
            },
            error: function () {
                alert("Add Metric Instance Edit error!!");
            },
            type: 'GET'
        });
}

function setMetricInstanceGridEditButton(tableid, successCallback) {

    var buttonSelector = ".edit-row-button"
    var tableselector = "#" + tableid;
    var editContainer = "<div id='edit-metricinstance-container'></div>"

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
        var url = $(this).attr('href').replace("#", "");

        if ($("#edit-metric-instance-container").html() === null) {
            $('body').append("<div id='edit-metric-instance-container'></div>");
        }
        else {
            $("#edit-metric-instance-container").empty()
        }

        $("#edit-metric-instance-container").html($("<img src='Content/images/ajax-loader-trans.gif' alt='' /><span>Loading Metric Data</span>"));

        $("#edit-metric-instance-container").dialog({
            autoopen: true,
            modal: true,
            draggable: false,
            resizable: false,
            title: "Edit Metric Instance",
            show: "slide",
            width: 300,
            height: 125,
            hide: { effect: "fade", duration: 500 }
        });

        $.ajax({
            url: url,
            cache: false,
            success: function (data) {
                $("#edit-metric-instance-container").dialog('close');
                addMetricInstanceData(data, successCallback);
            },
            error: function () {
                alert("Add Metric Instance Edit error!!");
            },
            type: 'GET'
        });

        return false;
    });
}

function addEditMetricConfiguration(tableid, data) {
    //Create the dialog
    if ($("#metric-configuration").html() === null) {
        var container = "<div id='metric-configuration' style='display:none'></div>";
        $('body').append(container);
    }
    else {
        $("#metric-configuration").empty();
    }

    $("#metric-configuration").append(data);

    $("#metric-data-form").validate({
        errorPlacement: function (error, element) {
            error.appendTo(element.parents("tr").children("td.ErrorTD"));
        }
    });

    if ($("form", $("#metric-configuration")).size() != 0) {
        $("#metric-configuration").dialog({
            autoopen: true,
            modal: true,
            draggable: false,
            resizable: false,
            title: "Metric Configuration",
            show: {effect: 'slide'},
            open: function(event, ui){
                $('body').css('overflow','hidden');
                $('.ui-widget-overlay').css('width','100%'); 
            }, 
            close: function(event, ui){
                $('body').css('overflow','auto'); 
            },
            width: 700,
            height: 410,
            hide: { effect: "fade", duration: 1000 },
            buttons: {
                "Submit": function () {
                    $("form", "#metric-configuration").submit();
                },
                "Close": function () {
                    $(this).dialog("close");
                    $("#metric-configuration").remove();
                }
            }
        });

        $("form", "#metric-configuration").submit(function () {

            if ($(this).valid()) {
                $.ajax({
                    data: $(this).serialize(),
                    type: "POST",
                    url: $(this).attr('action'),
                    success: function (data) {
                        $("#metric-configuration").dialog('close');
                        $("#metric-configuration").remove();
                    },
                    error: function () {
                        alert("Error retrieving metric configuration!!")
                    }
                });
            }
            return false;
        });
    }
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