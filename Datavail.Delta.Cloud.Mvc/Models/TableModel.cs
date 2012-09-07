using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Datavail.Delta.Cloud.Mvc.Models
{
    public class TableModel
    {
        public List<ToolbarItemModel> ToolbarItems { get; set; }
        public bool IsSearchEnabled { get; set; }

        public string Id { get; set; }
        public string Url { get; set; }
        public string EditUrl { get; set; }
        public string ColumnNames { get; set; }
        public string ColumnModel { get; set; }
        public string Pager { get; set; }
        public string Caption { get; set; }
        public string RecordText { get; set; }
        public string EmptyRecords { get; set; }
        public string HoverRows { get; set; }
        public string GridComplete { get; set; }
        public string MultiSelect { get; set; }
        public string MultiKey { get; set; }
        public string Height { get; set; }
        public string SortName { get; set; }
        public string DataType { get; set; }
        public string RowNum { get; set; }
        public string RowList { get; set; }
        public string ViewRecords { get; set; }
        public string SortOrder { get; set; }
        public string AutoWidth { get; set; }
        public string ShrinkToFit { get; set; }
        public string LoadText { get; set; }
        public string PgText { get; set; }
        public string NavGridOptions { get; set; }
        public string TableAddOptions { get; set; }
        public string TableEditOptions { get; set; }
        public string BeforeAddForm { get; set; }
        public string ContextMenuBindings { get; set; }
        public string Width { get; set; }
        public bool IsTabTable { get; set; }
        public string SpecType { get; set; }

        public TableModel()
        {
            ToolbarItems = new List<ToolbarItemModel>();
            IsSearchEnabled = true;

            //Defaults
            DataType = "json";
            RowNum = "10";
            RowList = "[20,50,100]";
            SortName = "id";
            ViewRecords = "true";
            SortOrder = "desc";
            AutoWidth = "true";
            ShrinkToFit = "false";
            LoadText = "";
            PgText = "Page {0} of {1}";
            Height = "350";
            IsTabTable = false;
            BeforeAddForm = "function() {}";
            SpecType = "none";
        }
    }
}