﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.225
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Datavail.Delta.Infrastructure.Agent.CollectionServiceProxy {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="CollectionServiceProxy.ICollectionService")]
    public interface ICollectionService {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ICollectionService/PostCollection", ReplyAction="http://tempuri.org/ICollectionService/PostCollectionResponse")]
        bool PostCollection(System.DateTime timestamp, System.Guid tenantId, System.Guid serverId, string hostname, string ipAddress, string data);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface ICollectionServiceChannel : Datavail.Delta.Infrastructure.Agent.CollectionServiceProxy.ICollectionService, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class CollectionServiceClient : System.ServiceModel.ClientBase<Datavail.Delta.Infrastructure.Agent.CollectionServiceProxy.ICollectionService>, Datavail.Delta.Infrastructure.Agent.CollectionServiceProxy.ICollectionService {
        
        public CollectionServiceClient() {
        }
        
        public CollectionServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public CollectionServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public CollectionServiceClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public CollectionServiceClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public bool PostCollection(System.DateTime timestamp, System.Guid tenantId, System.Guid serverId, string hostname, string ipAddress, string data) {
            return base.Channel.PostCollection(timestamp, tenantId, serverId, hostname, ipAddress, data);
        }
    }
}
