using System;
using Microsoft.Practices.Unity;

namespace Datavail.Delta.Cloud.Ws
{
    public class WcfServiceInstanceLifeTimeManager : LifetimeManager
    {
        private readonly Guid _key;

        public WcfServiceInstanceLifeTimeManager() { _key = Guid.NewGuid(); }

        public override object GetValue() { return WcfServiceInstanceExtension.Current.Items.Find(_key); }

        public override void SetValue(object newValue) { WcfServiceInstanceExtension.Current.Items.Set(_key, newValue); }

        public override void RemoveValue() { WcfServiceInstanceExtension.Current.Items.Remove(_key); }
    }
}