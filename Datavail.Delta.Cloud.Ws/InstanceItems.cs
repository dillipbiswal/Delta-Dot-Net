using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace Datavail.Delta.Cloud.Ws
{
    public class InstanceItems
    {
        public object Find(object key)
        {
            return _items.ContainsKey(key) ? _items[key] : null;
        }

        public void Set(object key, object value)
        {
            _items[key] = value;
        }

        public void Remove(object key)
        {
            _items.Remove(key);
        }

        private readonly Dictionary<object, object> _items = new Dictionary<object, object>();

        public void CleanUp(object sender, EventArgs e)
        {
            foreach (var disposable in _items.Select(item => item.Value).OfType<IDisposable>())
            {
                (disposable).Dispose();
            }
        }

        internal void Hook(InstanceContext instanceContext)
        {
            instanceContext.Closed += CleanUp;
            instanceContext.Faulted += CleanUp;
        }
    }
}