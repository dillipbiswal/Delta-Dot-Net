using System.Web.Mvc;
using System.Linq;
using System;
using System.Text;
using Datavail.Delta.Domain;
using Datavail.Delta.Cloud.Mvc.Infrastructure;
using System.Collections.Generic;
using Datavail.Delta.Cloud.Mvc.Models.Config;
using Datavail.Delta.Cloud.Mvc.Models;
using System.Web.Security;

namespace Datavail.Delta.Cloud.Mvc.Controllers
{
    public abstract class DeltaController : Controller
    {
        protected Guid TenantId { get; set; }

        protected override void Execute(System.Web.Routing.RequestContext requestContext)
        {
            //Save the id for controller reuse
            if (requestContext.RouteData.Values["id"] != null)
            {
                TenantId = Guid.Parse(requestContext.RouteData.Values["id"].ToString());
            }

            base.Execute(requestContext);
        }

        #region "protected methods"
        protected DeltaConfigPortalModel InitializeConfigPortalModel(ContentPageModel contentPageModel)
        {
            var configPortalModel = new DeltaConfigPortalModel();

            configPortalModel.FooterUrl = "http://www.datavail.com";
            configPortalModel.LoginMessage = "Logged in as " + User.Identity.Name;
            configPortalModel.LogOffImageUrl = "~/Content/images/navicons-small/129.png";
            configPortalModel.LogOffTitle = "Log Off";
            configPortalModel.LogOffUrl = "Authentication/LogOff";
            configPortalModel.Title = "Delta Configuration Portal";
            configPortalModel.ContentPageModel = contentPageModel;
            configPortalModel.ContentPageView = contentPageModel.ContentPageView;

            if (User.IsInRole(Constants.DELTAUSER))
            {
                //Configuration Items
                var configurationMenuItem = new MainMenuItem
                {
                    Class = "mainmenu-item",
                    ItemUrl = "Config",
                    ItemIconUrl = "Content/images/navicons/149.png",
                    ItemTitle = "Configuration",
                    ItemAlt = "Configuration",
                    ItemId = Constants.CONFIGMENUITEMID,
                };

                configurationMenuItem.ChildItems = new List<MainMenuItem> 
                { 
                    new MainMenuItem {
                        Class = "mainmenu-item",
                        ItemUrl = "Config",
                        ItemIconUrl = "Content/images/navicons/149.png",
                        ItemTitle = "Maintenance",
                        ItemAlt = "Maintenance",
                        ItemId = Constants.CONFIGMENUITEMID,
                        IsTopLevelItem = false
                    }
                };

                if (User.IsInRole(Constants.DELTAADMIN))
                {
                    configurationMenuItem.ChildItems.Add(new MainMenuItem
                    {
                        Class = "mainmenu-item",
                        ItemUrl = "Config/Metrics",
                        ItemIconUrl = "Content/images/navicons/149.png",
                        ItemTitle = "Metrics",
                        ItemAlt = "Metrics",
                        ItemId = Constants.CONFIGMENUITEMID,
                        IsTopLevelItem = false
                    });
                }

                configPortalModel.MainMenuItems.Add(configurationMenuItem);
            }

            if (User.IsInRole(Constants.DELTAADMIN))
            {
                //Admin Items
                var adminMenuItem = new MainMenuItem
                {
                    Class = "mainmenu-item",
                    ItemUrl = "Admin/UserMaintenance",
                    ItemIconUrl = "Content/images/navicons/111.png",
                    ItemTitle = "Admin",
                    ItemAlt = "Admin",
                    ItemId = Constants.ADMINMENUITEMID
                };

                adminMenuItem.ChildItems = new List<MainMenuItem> {
                     new MainMenuItem {
                        Class = "mainmenu-item",
                        ItemUrl = "Admin/UserMaintenance",
                        ItemIconUrl = "Content/images/navicons/149.png",
                        ItemTitle = "Users",
                        ItemAlt = "Users",
                        ItemId = Constants.ADMINMENUITEMID,
                        IsTopLevelItem = false
                    }
                };
                configPortalModel.MainMenuItems.Add(adminMenuItem);
            }

            //Set the current Tab based on the supplied model
            var currentMenuItem = configPortalModel.MainMenuItems.Where(x => x.ItemId == contentPageModel.MainMenuItemId).FirstOrDefault();
            currentMenuItem.Class += " current";

            return configPortalModel;
        }

        protected List<string> GetErrors(ModelStateDictionary modelState)
        {
            var errors = new List<string>();

            foreach (var item in modelState.Values)
            {
                foreach (var error in item.Errors)
                {
                    errors.Add(error.ErrorMessage);
                }
            }

            return errors;
        }

        protected string GetValidationErrors(ModelStateDictionary modelState)
        {
            var errorMessages = new StringBuilder();

            foreach (var item in modelState.Values)
            {
                foreach (var error in item.Errors)
                {
                    errorMessages.Append(error.ErrorMessage);
                }
            }

            return errorMessages.ToString();
        }

        protected string FormatStatusMarkup(Status status)
        {
            var result = "";

            switch (status)
            {
                case Status.Active:
                    result = "<img src='" + Constants.ACTIVEICON + "' alt='Active'/>Active";
                    break;
                case Status.Deleted:
                    result = "<img src='" + Constants.DELETED + "' alt='Deleted'/>Deleted";
                    break;
                case Status.Inactive:
                    result = "<img src='" + Constants.INACTIVEICON + "' alt='Inactive'/>Inactive";
                    break;
                case Status.InMaintenance:
                    result = "<img src='" + Constants.INMAINTENANCEICON + "' alt='Inmaintenance'/>In Maintenance";
                    break;
                case Status.Unknown:
                    result = "<img src='" + Constants.UNKNOWN + "' alt='Unknown'/>Unknown";
                    break;
                default:
                    break;
            }

            return result;
        }

        protected string GetActionItems(List<ActionModel> actionItems)
        {
            string htmlOutput = string.Empty;
            htmlOutput += "<ul class='toolbar'>";

            foreach (var item in actionItems)
            {
                htmlOutput += "<li>";
                htmlOutput += string.Format("<a href='{0}' title='{1}' class='small-button {2}' id='{3}'></a></li>",
                                            item.Url, item.Title, item.Class, item.Id, item.Alt);
            }

            htmlOutput += "</ul>";

            return htmlOutput;
        }

        protected TableModel GetTableModel(Constants.TableType tableType, Guid parentId, bool isTabTable, bool hideActions = false,
                                            bool hideNavigation = false, int actionColWidth = 70, int navColWidth = 175)
        {
            var tableModel = new TableModel();
            var columnData = GetJsonColumnData(tableType, hideActions, hideNavigation, actionColWidth, navColWidth);

            switch (tableType)
            {
                case Constants.TableType.Server:
                    tableModel = new TableModel
                    {
                        Id = "servers-table",
                        Caption = "Servers",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/ServerEdit",
                        EmptyRecords = "No Servers to view",
                        GridComplete = isTabTable ? "serverTabGridComplete" : "serverGridComplete",
                        HoverRows = isTabTable ? "true" : "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = isTabTable ? "true" : "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "servers-table-pager",
                        RecordText = "Servers {0} - {1} of {2}",
                        TableAddOptions = "{}",
                        TableEditOptions = "{}",
                        Url = "Config/ServersTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.ServerGroup:
                    tableModel = new TableModel
                    {
                        Id = "servergroups-table",
                        Caption = "Server Groups",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/ServerGroupEdit/",
                        EmptyRecords = "No Groups to view",
                        GridComplete = isTabTable ? "serverGroupTabGridComplete" : "serverGroupGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "servergroups-table-pager",
                        RecordText = "Customers {0} - {1} of {2}",
                        TableAddOptions = "{addCaption: 'Add Server Group', width: 400}",
                        TableEditOptions = "{}",
                        Url = "Config/ServerGroupsTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.Customer:
                    tableModel = new TableModel
                    {
                        Id = "customers-table",
                        Caption = "Customers",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/CustomerEdit",
                        EmptyRecords = "No Customers to view",
                        GridComplete = isTabTable ? "customerTabGridComplete" : "customerGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "customers-table-pager",
                        RecordText = "Customers {0} - {1} of {2}",
                        TableAddOptions = "{addCaption: 'Add Customer', width: 400, beforeShowForm: beforeAddCustomer}",
                        TableEditOptions = "{}",
                        Url = "Config/CustomersTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.Cluster:
                    tableModel = new TableModel
                    {
                        Id = "clusters-table",
                        Caption = "Clusters",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/ClusterEdit/",
                        EmptyRecords = "No Clusters to view",
                        GridComplete = isTabTable ? "clusterTabGridComplete" : "clusterGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "clusters-table-pager",
                        RecordText = "Clusters {0} - {1} of {2}",
                        TableAddOptions = "{addCaption: 'Add Cluster', width: 400}",
                        TableEditOptions = "{}",
                        Url = "Config/ClustersTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.ClusterNode:
                    tableModel = new TableModel
                    {
                        Id = "clusternodes-table",
                        Caption = "Cluster Nodes",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/ServerEdit/",
                        EmptyRecords = "No Nodes to view",
                        GridComplete = isTabTable ? "clusterNodeTabGridComplete" : "clusterNodeGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "clusternodes-table-pager",
                        RecordText = "Nodes {0} - {1} of {2}",
                        TableAddOptions = "{addCaption: 'Add Virtual Server', width: 500, beforeShowForm: beforeAddVirtualServer}",
                        TableEditOptions = "{}",
                        Url = "Config/ServersTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.DatabaseInstance:
                    tableModel = new TableModel
                    {
                        Id = "databaseinstances-table",
                        Caption = "Database Instances",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/DatabaseInstanceEdit/",
                        EmptyRecords = "No Instances to view",
                        GridComplete = isTabTable ? "databaseInstanceTabGridComplete" : "databaseInstanceGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "databaseinstances-table-pager",
                        RecordText = "Groups {0} - {1} of {2}",
                        TableAddOptions = "{addCaption: 'Add Database Instance', width: 500, beforeShowForm: beforeAddDatabaseInstance}",
                        TableEditOptions = "{}",
                        Url = "Config/DatabaseInstancesTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.Database:
                    tableModel = new TableModel
                    {
                        Id = "databases-table",
                        Caption = "Databases",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/DatabaseEdit/",
                        EmptyRecords = "No Databases to view",
                        GridComplete = isTabTable ? "databaseTabGridComplete" : "databaseGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "databases-table-pager",
                        RecordText = "Databases {0} - {1} of {2}",
                        TableAddOptions = "{addCaption: 'Add Database'}",
                        TableEditOptions = "{width: 400}",
                        Url = "Config/DatabasesTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.Metric:
                    tableModel = new TableModel
                    {
                        Id = "metrics-table",
                        Caption = "Metrics",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "",
                        EmptyRecords = "No Metrics to view",
                        GridComplete = isTabTable ? "metricTabGridComplete" : "metricGridComplete",
                        HoverRows = isTabTable ? "true" : "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = isTabTable ? "true" : "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "metrics-table-pager",
                        RecordText = "Metrics {0} - {1} of {2}",
                        TableAddOptions = "{}",
                        TableEditOptions = "{}",
                        Url = "Config/MetricsTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.MetricInstance:
                    tableModel = new TableModel
                    {
                        Id = "metricinstances-table",
                        Caption = "Metric Instances",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/DeleteMetricInstance",
                        EmptyRecords = "No Metrics to view",
                        GridComplete = isTabTable ? "metricInstanceTabGridComplete" : "metricInstanceGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "true",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "metricinstances-table-pager",
                        RecordText = "Metrics {0} - {1} of {2}",
                        TableAddOptions = "{}",
                        TableEditOptions = "{}",
                        Url = "Config/MetricInstancesTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.User:
                    tableModel = new TableModel
                    {
                        Id = "users-table",
                        Caption = "Users",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Admin/UserEdit",
                        EmptyRecords = "No Users to view",
                        GridComplete = "userGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "true",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "users-table-pager",
                        RecordText = "Users {0} - {1} of {2}",
                        TableAddOptions = "{addCaption: 'Add User', width: 400}",
                        TableEditOptions = "{}",
                        Url = "Admin/UsersTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.MetricThresholds:
                    tableModel = new TableModel
                    {
                        Id = "metricthresholds-table",
                        Caption = "Metric Thresholds",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/MetricThresholdEdit/",
                        EmptyRecords = "No Thresholds to view",
                        GridComplete = "metricThresholdGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "metricthresholds-table-pager",
                        RecordText = "Thresholds {0} - {1} of {2}",
                        TableAddOptions = "{}",
                        TableEditOptions = "{}",
                        Url = "Config/MetricThresholdsTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.Schedules:
                    tableModel = new TableModel
                    {
                        Id = "schedules-table",
                        Caption = "Schedules",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/ScheduleEdit/",
                        EmptyRecords = "No schedules to view",
                        GridComplete = "scheduleGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "schedules-table-pager",
                        RecordText = "Schedules {0} - {1} of {2}",
                        TableAddOptions = "{}",
                        TableEditOptions = "{}",
                        Url = "Config/SchedulesTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.MetricConfig:
                    tableModel = new TableModel
                    {
                        Id = "mettricconfig-table",
                        Caption = "Metric Configurations",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/MetricConfigurationEdit/",
                        EmptyRecords = "No configurations to view",
                        GridComplete = "metricConfigGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "mettricconfig-table-pager",
                        RecordText = "Configurations {0} - {1} of {2}",
                        TableAddOptions = "{addCaption: 'Add Metric Configuration', width: 550}",
                        TableEditOptions = "{}",
                        Url = "Config/MetricConfigurationsTable?parentId=" + parentId,
                    };
                    break;
                case Constants.TableType.MaintWindow:
                    tableModel = new TableModel
                    {
                        Id = "maintwindow-table",
                        Caption = "Maintenance Windows",
                        ColumnModel = columnData[1],
                        ColumnNames = columnData[0],
                        EditUrl = "Config/MaintenanceWindowEdit/",
                        EmptyRecords = "No windows to view",
                        GridComplete = "maintWindowsGridComplete",
                        HoverRows = "false",
                        MultiKey = "ctrlKey",
                        MultiSelect = "false",
                        NavGridOptions = "{ edit: false, add: false,del: false, search: false}",
                        Pager = "maintwindow-table-pager",
                        RecordText = "Windows {0} - {1} of {2}",
                        TableAddOptions = "{addCaption: 'Add Maintenance Window', width: 400, afterShowForm: beforeShowMaintWindows}",
                        TableEditOptions = "{}",
                        Url = "Config/MaintenanceWindowsTable?parentId=" + parentId,
                    };
                    break;
            }

            tableModel.IsTabTable = isTabTable;
            return tableModel;
        }

        protected List<string> GetJsonColumnData(Constants.TableType tableType, bool hideActions, bool hideNavigation, int actionColWidth, int navColWidth)
        {
            var data = new List<string>();
            var columnNames = "";
            var columnModel = "[{{ name: 'action', width: " + actionColWidth.ToString() + ", {0} search: false }},{1}{{ name: 'navigation', index: 'navigation', width: " + navColWidth.ToString() + ", {2} search: false }}]";
            var columns = string.Empty;

            var hideActionText = hideActions ? "hidden: true," : "hidden: false,";
            var hideNavigationText = hideNavigation ? "hidden: true," : "hidden: false,";

            switch (tableType)
            {
                case Constants.TableType.ClusterNode:
                case Constants.TableType.Server:
                    columnNames = "['', 'Hostname', 'IP Address', 'Status', 'Last Check In', 'Agent Version','Cluster Group Name', 'Navigation']";
                    columns = "{ name: 'hostname', index: 'hostname', editable: true, editoptions: {size: 40}, width: 350 }," +
                                "{ name: 'ipaddress', index: 'ipaddress', editable: true, width: 125 }," +
                                "{ name: 'status', index: 'status', width: 90, editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/StatusSelect' }, search: false}," +
                                "{ name: 'lastcheckin', index: 'lastcheckin', hidden: true, editable: true, editrules: {edithidden: false}, width: 110 }," +
                                "{ name: 'agentversion', index: 'agentversion', hidden: true, editable: true, editrules: {edithidden: true}, width: 100 }," +
                                "{ name: 'clustergroupname', index: 'clustergroupname', hidden: true, editable: true, editrules: {edithidden: true}, editoptions: {size: 40}, width: 100 },";
                    break;
                case Constants.TableType.ServerGroup:
                    columnNames = "['', 'Name', 'Status', 'Priority', 'Navigation']";
                    columns = "{ name: 'name', index: 'name', width: 350, editable: true, edittype: 'text', editrules: { required: true }, editoptions: {size: 40}, search: true }," +
                                "{ name: 'status', index: 'status', width: 90, editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/StatusSelect' }, search: false }," +
                                "{ name: 'priority', index: 'priority', width: 60, editable: true, edittype: 'text', editrules: { required: true, number: true }, search: true },";
                    break;
                case Constants.TableType.Customer:
                    columnNames = "['', 'Name', 'Id', 'Status', 'Navigation']";
                    columns = "{ name: 'name', index: 'name', width: 350, editable: true, edittype: 'text', editrules: { required: true }, editoptions: {size: 40}, search: true }," +
                                "{ name: 'id', index: 'id', width: 300, editable: true, hidden: true, editrules: {edithidden: false}, editoptions: {size: 40, dataInit: function(element) {$(element).attr('disabled','disabled'); } }}," +
                                "{ name: 'status', index: 'status', width: 90, editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/StatusSelect' }, search: false },";
                    break;
                case Constants.TableType.Cluster:
                    columnNames = "['', 'Name', 'Status', 'Navigation']";
                    columns = "{ name: 'name', index: 'name', width: 350, editable: true, edittype: 'text', editrules: { required: true }, editoptions: {size: 40}, search: true }," +
                                    "{ name: 'status', index: 'status', width: 90, editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/StatusSelect' }, search: false },";
                    break;
                case Constants.TableType.DatabaseInstance:
                    columnNames = "['', 'Name', 'Integrated Security', 'Username', 'Password', 'Status', 'Parent', 'Database Version', 'Navigation']";
                    columns = "{ name: 'name', index: 'name', width: 350, editable: true, edittype: 'text', editrules: { required: true }, editoptions: {size: 40}, search: true }," +
                                "{ name: 'useintegratedsecurity', index: 'useintegratedsecurity', width: 30, hidden: true, editable: true, edittype: 'checkbox', editrules: { required: true, edithidden: true }, editoptions: { value: 'True:False', defaultValue: 'True'}, search: true }," +
                                "{ name: 'username', index: 'username', width: 100, hidden: true, editable: true, edittype: 'text', search: true }," +
                                "{ name: 'password', index: 'password', width: 100, hidden: true, editable: true, edittype: 'password', search: true }," +
                                "{ name: 'status', index: 'status', width: 90, editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/StatusSelect' }, search: false }," +
                                "{ name: 'parent', index: 'parent', width: 90, editable: true, hidden: true, editrules: {edithidden: false} }," +
                                "{ name: 'databaseversion', index: 'databaseversion', width: 90, editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/DatabaseVersionSelect' }, search: false },";
                    break;
                case Constants.TableType.Database:
                    columnNames = "['', 'Name', 'Status', 'Navigation']";
                    columns = "{ name: 'name', index: 'name', width: 350, editable: true, edittype: 'text', editrules: { required: true }, editoptions: {size: 40}, search: true }," +
                                "{ name: 'status', index: 'status', width: 90, editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/StatusSelect' }, search: false },";
                    break;
                case Constants.TableType.Metric:
                    columnNames = "['','Metric Name', 'Status','Navigation']";
                    columns = "{ name: 'name', index: 'name', editable: false, width: 350 }," +
                                "{ name: 'status', index: 'status', width: 90, editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/StatusSelect' }, search: false },";
                    break;
                case Constants.TableType.MetricInstance:
                    columnNames = "['', 'Metric Name', 'Metric Label','Status','Navigation']";
                    columns = "{ name: 'name', index: 'name', editable: false, width: 325 }," +
                                "{ name: 'label', index: 'label', editable: false, width: 375 }," +
                                "{ name: 'status', index: 'status', width: 90, editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/StatusSelect' }, search: false },";
                    break;
                case Constants.TableType.User:
                    columnNames = "['', 'Email Address', 'First Name', 'Last Name', 'Password', 'Navigation']";
                    columns = "{ name: 'emailaddress', index: 'emailaddress', width: 225, editable: true, edittype: 'text', editrules: { required: true }, editoptions: {size: 30}, search: true }," +
                                "{ name: 'firstname', index: 'firstname', width: 225, editable: true, edittype: 'text', editrules: { required: true }, editoptions: {size: 30}, search: true }," +
                                "{ name: 'lastname', index: 'lastname', width: 225, editable: true, edittype: 'text', editrules: { required: true }, editoptions: {size: 30}, search: true }," +
                                "{ name: 'password', index: 'password', width: 100, hidden: true, editable: true, edittype: 'password', editrules: { edithidden: true }, editoptions: {size: 30}, search: true },";
                    break;
                case Constants.TableType.MetricThresholds:
                    columnNames = "['', 'Threshold Comparison', 'Severity','Time Period','Ceiling Value', 'Floor Value', 'Match Value', 'Number of Occurences', 'Threshold Value', 'Threshold Description','']";
                    columns = "{ name: 'thresholdcomparisonfunction', index: 'thresholdcomparisonfunction', editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/ThresholdComparisonSelect' }, search: false, width: 75 }," +
                                "{ name: 'severity', index: 'severity', editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/SeveritySelect' }, search: false, width: 75 }," +
                                "{ name: 'timeperiod', index: 'timeperiod', editable: true, hidden: true, editrules: {edithidden: true}, width: 75 }," +
                                "{ name: 'ceilingvalue', index: 'ceilingvalue', editable: true, hidden: true, editrules: {edithidden: true}, width: 75 }," +
                                "{ name: 'floorvalue', index: 'floorvalue', editable: true, hidden: true, editrules: {edithidden: true}, width: 75 }," +
                                "{ name: 'matchvalue', index: 'matchvalue', editable: true, hidden: true, editrules: {edithidden: true}, width: 75 }," +
                                "{ name: 'numberofoccurrences', index: 'numberofoccurences', editable: true, hidden: true, editrules: {edithidden: true}, width: 75 }," +
                                "{ name: 'thresholdvaluetype', index: 'thresholdvalue', editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/ThresholdValueSelect' }, search: false, width: 75 }," +
                                "{ name: 'thresholddescription', index: 'thresholddescription', editable: false, hidden: false, editrules: {edithidden: false}, width: 750 },";
                    break;
                case Constants.TableType.Schedules:
                    columnNames = "['', 'Month', 'Day','Hour','Minute', 'Day of Week', 'Schedule Type', 'Interval', 'Schedule Description', '']";
                    columns = "{ name: 'month', index: 'month', editable: true, hidden: true, editrules: {edithidden: true}, width: 75 }," +
                                "{ name: 'day', index: 'day', editable: true, hidden: true, editrules: {edithidden: true}, width: 75 }," +
                                "{ name: 'hour', index: 'hour', editable: true, hidden: true, editrules: {edithidden: true}, width: 75 }," +
                                "{ name: 'minute', index: 'minute', editable: true, hidden: true, editrules: {edithidden: true}, width: 75 }," +
                                "{ name: 'dayofweek', index: 'dayofweek', editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/DayofWeekSelect' }, search: false, width: 75 }," +
                                "{ name: 'scheduletype', index: 'scheduletype', editable: true, hidden: true, editrules: {edithidden: true}, edittype: 'select', editoptions: { dataUrl: 'Config/ScheduleTypeSelect' }, search: false, width: 75 }," +
                                "{ name: 'interval', index: 'interval', editable: true, hidden: true, editrules: {edithidden: true}, width: 75 }," +
                                "{ name: 'scheduledescription', index: 'scheduledescription', editable: false, hidden: false, editrules: {edithidden: false}, width: 750 },";
                    break;
                case Constants.TableType.MetricConfig:
                    columnNames = "['', 'Configuration Name', 'Metric Name', 'Metric', '']";
                    columns = "{ name: 'name', index: 'name', width: 375, editable: true, edittype: 'text', editrules: { required: true }, editoptions: {size: 50}, search: true }," +
                               "{ name: 'metricname', index: 'metricname', width: 375, editable: false, edittype: 'text', search: false }," +
                               "{ name: 'metricid', index: 'metricid', hidden: true, editable: true, editrules: {edithidden: true}, edittype: 'select', edittype: 'select', editoptions: { dataUrl: 'Config/MetricSelect' },  search: false },";
                    break;
                case Constants.TableType.MaintWindow:
                    columnNames = "['', 'Begin Date', 'End Date', 'Description','']";
                    columns = "{ name: 'begindate', index: 'begindate', hidden: true, width: 200, editable: true, edittype: 'text', editrules: { required: true }, search: true }," +
                                "{ name: 'enddate', index: 'enddate', hidden: true, width: 200, editable: true, edittype: 'text', editrules: { required: true }, search: true }," +
                                "{ name: 'description', index: 'description', width: 800, editable: false, search: true },";
                    break;
            }

            columnModel = string.Format(columnModel, hideActionText, columns, hideNavigationText);

            data.Add(columnNames);
            data.Add(columnModel);

            return data;
        }
        #endregion
    }
}