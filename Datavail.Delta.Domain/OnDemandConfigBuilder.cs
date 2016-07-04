using System;
using System.Collections.Generic;

namespace Datavail.Delta.Domain
{
    public class OnDemandConfigBuilder : DomainBase
    {
        #region Fields
        #endregion

        #region Properties

        public DateTime BeginDate { get; set; }
        public DateTime EndDate { get; set; }
        public string User { get; set; }
        public string StatusFlag { get; set; }

        #endregion

        #region ctor
        [Obsolete("Use static NewOnDemandConfigBuilder factory instead")]
        public OnDemandConfigBuilder()
        {
            Initialize();
        }

#pragma warning disable 612,618
        public static OnDemandConfigBuilder NewOnDemandConfigBuilder(DateTime begindate, DateTime enddate, string user, string statusflag)
        {
            var entity = new OnDemandConfigBuilder { BeginDate = begindate, EndDate = enddate, User = user, StatusFlag = statusflag };
            return entity;
        }
#pragma warning restore 612,618

        private void Initialize()
        {
        }
        #endregion

        #region Methods
        #endregion
    }
}
