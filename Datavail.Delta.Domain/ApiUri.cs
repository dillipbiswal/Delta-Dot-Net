using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public class ApiUri : DomainBase
    {
        #region Fields
        #endregion

        #region Properties

        public string PlugInName { get; set; }
        public string URIAddress { get; set; }
        public string AgentServerId { get; set; }

        public Guid CustomerId { get; set; }

        //public DateTime BeginDate { get; set; }
        //public DateTime EndDate { get; set; }
        //public virtual Status ParentPreviousStatus { get; set; }
        #endregion

        #region ctor
        [Obsolete("Use static NewApiUri factory instead")]
        public ApiUri()
        {
            Initialize();
        }

#pragma warning disable 612,618
        //public static ApiUri NewApiUri(string pluginname, string uriaddress, string agentserverid)
        //{
        //    var entity = new ApiUri { PlugInName = pluginname, URIAddress = uriaddress, AgentServerId = agentserverid };
        //    return entity;
        //}
#pragma warning restore 612,618

        private void Initialize()
        {
        }
        #endregion

        #region Methods
        #endregion
    }
}
