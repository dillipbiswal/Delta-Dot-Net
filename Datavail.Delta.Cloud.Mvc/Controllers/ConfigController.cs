using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Datavail.Delta.Infrastructure.Logging;
using Datavail.Delta.Infrastructure.Repository;
using Datavail.Delta.Domain;
using AutoMapper;
using Datavail.Delta.Cloud.Mvc.Models.Config;
using Datavail.Delta.Cloud.Mvc.Models;
using System.Collections;
using System.Text;
using Datavail.Delta.Application.Interface;
using System.Threading;
using Datavail.Delta.Infrastructure.Specification;
using Datavail.Delta.Domain.Specifications;
using System.Net;
using Datavail.Delta.Cloud.Mvc.Infrastructure;
using Datavail.Delta.Application;

namespace Datavail.Delta.Cloud.Mvc.Controllers
{
    [Authorize(Roles = Constants.DELTAUSER)]
    public class ConfigController : DeltaController
    {
        #region "Private Variables"

        private readonly IDeltaLogger _logger;
        private readonly IServerService _serverService;
        #endregion

        #region "CTOR"
        public ConfigController(IDeltaLogger logger, IServerService serverService)
        {
            _logger = logger;
            _serverService = serverService;
        }
        #endregion

        #region "Customer Actions"
        [HttpPost]
        public JsonResult CustomerList(string searchTerm)
        {
            return Json(_serverService.GetCustomerNames(searchTerm));
        }

        [HttpPost]
        public JsonResult CustomerHierarchy(Guid id, Constants.ItemHierarchyType type)
        {
            object items = new { };
            switch (type)
            {
                case Constants.ItemHierarchyType.None:
                    Customer customer = null;

                    customer = id.Equals(Guid.Empty) ? _serverService.Find<Customer>(new Specification<Customer>(x => x.Status != Status.Deleted)).OrderBy(x => x.Name).First() : _serverService.GetByKey<Customer>(id);

                    if (customer != null && customer.Status != Status.Deleted)
                    {
                        //Get the server groups
                        var serverGroups = customer.ServerGroups.Where(x => x.Status != Status.Deleted).Select(x => new
                        {
                            data = x.Name,
                            attr = new
                            {
                                id = x.Id.ToString(),
                                rel = Constants.ItemHierarchyType.ServerGroup.ToString(),
                                @class = x.Status == Status.InMaintenance ? "in-maintenance" : string.Empty,
                                name = x.Name
                            },
                            state = "closed"
                        }).ToList();

                        var serverGroupFolder = new
                        {
                            data = "Server Groups",
                            attr = new
                            {
                                id = Constants.SERVERGROUPFOLDERID.ToString(),
                                rel = Constants.ItemHierarchyType.ServerGroupFolder.ToString(),
                                name = "Server Groups"
                            },
                            children = new[] { serverGroups.OrderBy(x => x.data) },
                            state = serverGroups.Count > 0 ? "open" : "leaf"
                        };

                        //Get the clusters
                        var clusters = customer.Clusters.Where(x => x.Status != Status.Deleted).Select(x => new
                        {
                            data = x.Name,
                            attr = new
                            {
                                id = x.Id.ToString(),
                                rel = Constants.ItemHierarchyType.Cluster.ToString(),
                                @class = x.Status == Status.InMaintenance ? "in-maintenance" : string.Empty,
                                name = x.Name
                            },
                            state = "closed"
                        }).ToList();

                        var clusterFolder = new
                        {
                            data = "Clusters",
                            attr = new
                            {
                                id = Constants.CLUSTERFOLDERID.ToString(),
                                rel = Constants.ItemHierarchyType.ClusterFolder.ToString(),
                                name = "Clusters"
                            },
                            children = new[] { clusters.OrderBy(x => x.data) },
                            state = clusters.Count > 0 ? "open" : "leaf"
                        };

                        var customerItems = new
                        {
                            data = customer.Name,
                            attr = new
                            {
                                id = customer.Id.ToString(),
                                rel = Constants.ItemHierarchyType.Customer.ToString(),
                                @class = customer.Status == Status.InMaintenance ? "in-maintenance" : string.Empty,
                                name = customer.Name
                            },
                            children = new[] { serverGroupFolder, clusterFolder },
                            state = "open"
                        };

                        items = customerItems;
                    }
                    break;
                case Constants.ItemHierarchyType.Cluster:
                    var cluster = _serverService.GetByKey<Cluster>(id);

                    if (cluster != null && cluster.Status != Status.Deleted)
                    {
                        //Get the child nodes and virtual servers
                        var nodes = cluster.Nodes.Where(x => x.Status != Status.Deleted).Select(x => new
                        {
                            data = x.Hostname,
                            attr = new
                            {
                                id = x.Id.ToString(),
                                rel = Constants.ItemHierarchyType.ClusterNode.ToString(),
                                @class = x.Status == Status.InMaintenance ? "in-maintenance" : string.Empty,
                                name = x.Hostname
                            },
                            state = "closed"
                        }).ToList();

                        var nodeFolder = new
                        {
                            data = "Cluster Nodes",
                            attr = new
                            {
                                id = Constants.CLUSTERNODEFOLDERID.ToString(),
                                rel = Constants.ItemHierarchyType.ClusterNodeFolder.ToString(),
                                name = "Cluster Nodes"
                            },
                            children = new[] { nodes.OrderBy(x => x.data) },
                            state = nodes.Count > 0 ? "closed" : "leaf"
                        };

                        var virtualServers = cluster.VirtualServers.Where(x => x.Status != Status.Deleted).Select(x => new
                        {
                            data = x.Hostname,
                            attr = new
                            {
                                id = x.Id.ToString(),
                                rel = Constants.ItemHierarchyType.VirtualServer.ToString(),
                                @class = x.Status == Status.InMaintenance ? "in-maintenance" : string.Empty,
                                name = x.Hostname
                            },
                            state = "closed"
                        }).ToList();

                        var virtualServerFolder = new
                        {
                            data = "Virtual Servers",
                            attr = new
                            {
                                id = Constants.VIRTUALSERVERFOLDERID.ToString(),
                                rel = Constants.ItemHierarchyType.VirtualServerFolder.ToString(),
                                name = "Cluster Nodes"
                            },
                            children = new[] { virtualServers.OrderBy(x => x.data) },
                            state = virtualServers.Count > 0 ? "closed" : "leaf"
                        };

                        items = new[] { nodeFolder, virtualServerFolder };
                    }
                    break;
                case Constants.ItemHierarchyType.ServerGroup:
                    var serverGroup = _serverService.GetByKey<ServerGroup>(id);

                    if (serverGroup != null && serverGroup.Status != Status.Deleted)
                    {
                        //Get the child servers
                        var serverGroupServers = serverGroup.Servers.Where(x => x.Status != Status.Deleted).Select(x => new
                        {
                            data = x.Hostname,
                            attr = new
                            {
                                id = x.Id.ToString(),
                                rel = x.IsVirtual ? Constants.ItemHierarchyType.VirtualServer.ToString() : Constants.ItemHierarchyType.Server.ToString(),
                                @class = x.Status == Status.InMaintenance ? "in-maintenance" : string.Empty,
                                name = x.Hostname
                            },
                            state = "closed"
                        }).ToList();

                        var serverFolder = new
                        {
                            data = "Servers",
                            attr = new
                            {
                                id = Constants.SERVERFOLDERID.ToString(),
                                rel = Constants.ItemHierarchyType.ServerFolder.ToString(),
                                name = "Servers"
                            },
                            children = new[] { serverGroupServers.OrderBy(x => x.attr.rel).ThenBy(x => x.data) },
                            state = serverGroupServers.Count > 0 ? "closed" : "leaf"
                        };

                        items = new[] { serverFolder };
                    }
                    break;
                case Constants.ItemHierarchyType.Server:
                case Constants.ItemHierarchyType.VirtualServer:
                case Constants.ItemHierarchyType.ClusterNode:
                    var server = _serverService.GetByKey<Server>(id);

                    if (server != null && server.Status != Status.Deleted)
                    {
                        //Get the child db instances
                        var serverInstances = server.Instances.Where(x => x.Status != Status.Deleted).Select(x => new
                        {
                            data = x.Name,
                            attr = new
                            {
                                id = x.Id.ToString(),
                                rel = Constants.ItemHierarchyType.DatabaseInstance.ToString(),
                                @class = x.Status == Status.InMaintenance ? "in-maintenance" : string.Empty,
                                name = x.Name
                            },
                            state = "closed"
                        }).ToList();

                        var databaseInstanceFolder = new
                        {
                            data = "Database Instances",
                            attr = new
                            {
                                id = Constants.DATABASEINSTANCEFOLDERID.ToString(),
                                rel = Constants.ItemHierarchyType.DatabaseInstanceFolder.ToString(),
                                name = "Database Instances"
                            },
                            children = new[] { serverInstances.OrderBy(x => x.data) },
                            state = serverInstances.Count > 0 ? "closed" : "leaf"
                        };

                        //Get the child metric instances
                        var serverMetricInstances = server.MetricInstances.Where(x => x.Status != Status.Deleted && x.DatabaseInstance == null
                                                                                        && x.Database == null).Select(x => new
                                                                                        {
                                                                                            data = x.Label,
                                                                                            attr = new
                                                                                            {
                                                                                                id = x.Id.ToString(),
                                                                                                rel = Constants.ItemHierarchyType.MetricInstance.ToString(),
                                                                                                @class = x.Status == Status.InMaintenance ? "in-maintenance" : string.Empty,
                                                                                                name = x.Label
                                                                                            },
                                                                                            state = "leaf"
                                                                                        }).ToList();

                        var serverMetricInstanceFolder = new
                        {
                            data = "Metric Instances",
                            attr = new
                            {
                                id = Constants.METRICINSTANCEFOLDERID.ToString(),
                                rel = Constants.ItemHierarchyType.MetricInstanceFolder.ToString(),
                                name = "Metric Instances"
                            },
                            children = new[] { serverMetricInstances.OrderBy(x => x.data) },
                            state = serverMetricInstances.Count > 0 ? "closed" : "leaf"
                        };

                        items = new[] { databaseInstanceFolder, serverMetricInstanceFolder };
                    }
                    break;
                case Constants.ItemHierarchyType.DatabaseInstance:
                    var instance = _serverService.GetByKey<DatabaseInstance>(id);

                    if (instance != null && instance.Status != Status.Deleted)
                    {
                        //Get the child dbs
                        var databases = instance.Databases.Where(x => x.Status != Status.Deleted).Select(x => new
                        {
                            data = x.Name,
                            attr = new
                            {
                                id = x.Id.ToString(),
                                rel = Constants.ItemHierarchyType.Database.ToString(),
                                @class = x.Status == Status.InMaintenance ? "in-maintenance" : string.Empty,
                                name = x.Name
                            },
                            state = "closed"
                        }).ToList();

                        var databaseFolder = new
                        {
                            data = "Databases",
                            attr = new
                            {
                                id = Constants.DATABASEFOLDERID.ToString(),
                                rel = Constants.ItemHierarchyType.DatabaseFolder.ToString(),
                                name = "Databases"
                            },
                            children = new[] { databases.OrderBy(x => x.data) },
                            state = databases.Count > 0 ? "closed" : "leaf"
                        };

                        //Get the child metric instances
                        var instanceMetricInstances = instance.Server.MetricInstances.Where(x => x.Status != Status.Deleted &&
                                                                                                 x.DatabaseInstance != null && x.DatabaseInstance.Id.Equals(id))
                                                                                                 .Select(x => new
                                                                                                 {
                                                                                                     data = x.Label,
                                                                                                     attr = new
                                                                                                     {
                                                                                                         id = x.Id.ToString(),
                                                                                                         rel = Constants.ItemHierarchyType.MetricInstance.ToString(),
                                                                                                         @class = x.Status == Status.InMaintenance ? "in-maintenance" : string.Empty,
                                                                                                         name = x.Label
                                                                                                     },
                                                                                                     state = "leaf"
                                                                                                 }).ToList();

                        var instanceMetricInstanceFolder = new
                        {
                            data = "Metric Instances",
                            attr = new
                            {
                                id = Constants.METRICINSTANCEFOLDERID.ToString(),
                                rel = Constants.ItemHierarchyType.MetricInstanceFolder.ToString(),
                                name = "Metric Instances"
                            },
                            children = new[] { instanceMetricInstances.OrderBy(x => x.data) },
                            state = instanceMetricInstances.Count > 0 ? "closed" : "leaf"
                        };

                        items = new[] { databaseFolder, instanceMetricInstanceFolder };
                    }
                    break;
                case Constants.ItemHierarchyType.Database:
                    var database = _serverService.GetByKey<Database>(id);

                    if (database != null && database.Status != Status.Deleted)
                    {
                        //Get the child metric instances
                        var dbMetricInstances = database.Instance.Server.MetricInstances.Where(x => x.Status != Status.Deleted && x.Database != null
                                                                                                    && x.Database.Id.Equals(id)).Select(x => new
                                                                                                    {
                                                                                                        data = x.Label,
                                                                                                        attr = new
                                                                                                        {
                                                                                                            id = x.Id.ToString(),
                                                                                                            rel = Constants.ItemHierarchyType.MetricInstance.ToString(),
                                                                                                            @class = x.Status == Status.InMaintenance ? "in-maintenance" : string.Empty,
                                                                                                            name = x.Label
                                                                                                        },
                                                                                                        state = "leaf"
                                                                                                    }).ToList();

                        var databaseMetricInstanceFolder = new
                        {
                            data = "Metric Instances",
                            attr = new
                            {
                                id = Constants.METRICINSTANCEFOLDERID.ToString(),
                                rel = Constants.ItemHierarchyType.MetricInstanceFolder.ToString(),
                                name = "Metric Instances"
                            },
                            children = new[] { dbMetricInstances.OrderBy(x => x.data) },
                            state = dbMetricInstances.Count > 0 ? "closed" : "leaf"
                        };

                        items = new[] { databaseMetricInstanceFolder };
                    }
                    break;
                default:
                    break;
            }

            return Json(items);
        }

        [HttpPost]
        public JsonResult CustomerDelete(Guid customerId)
        {
            var customerIds = new List<Guid>();
            customerIds.Add(customerId);

            if (!_serverService.DeleteCustomers(customerIds))
            {
                return Json(new { success = false, errors = new string[] { "The Customer Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult CustomerEdit(Guid? customerId)
        {
            if (customerId != null)
            {
                //Edit
                var customer = _serverService.GetByKey<Customer>(customerId.Value);

                Mapper.CreateMap<Customer, CustomerModel>();
                var customerModel = Mapper.Map<Customer, CustomerModel>(customer);

                customerModel.TenantId = TenantId;

                return View(customerModel);
            }

            return View(new CustomerModel { TenantId = TenantId });
        }

        [HttpPost]
        public JsonResult CustomerEdit(CustomerModel customerModel)
        {
            var errorMessages = new StringBuilder();
            var serverErrors = new List<string>();

            Mapper.CreateMap<CustomerModel, Customer>();
            var customer = Mapper.Map<CustomerModel, Customer>(customerModel);

            //Validate
            if (!_serverService.ValidateCustomer(customer, out serverErrors))
            {
                foreach (var error in serverErrors)
                {
                    ModelState.AddModelError("Validation", error);
                }
            }
            else
            {
                if (!_serverService.SaveCustomer(ref customer, customerModel.TenantId))
                {
                    ModelState.AddModelError("Validation", "The server encountered and error during Save");
                }
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = GetErrors(ModelState).ToArray() });
            }

            return Json(new { success = true, id = customer.Id });
        }

        #endregion

        #region "Server Actions"
        [HttpPost]
        public JsonResult ServerList(Guid customerId, string searchTerm)
        {
            return Json(_serverService.GetServerNames(customerId, searchTerm));
        }

        [HttpGet]
        public ActionResult AddCustomerServers(Guid customerId)
        {
            var customer = _serverService.GetByKey<Customer>(customerId);

            var selectedServers = customer.Servers.Where(x => x.Status != Status.Deleted).OrderBy(s => s.Hostname).ToList();
            var potentialServers = _serverService.GetUnknownServers(customer.Tenant.Id).Concat(selectedServers).OrderBy(s => s.Hostname).ToList();
            var potentialServersSelectListItems = from s in potentialServers
                                                  select new SelectListItem
                                                      {
                                                          Value = s.Id.ToString(),
                                                          Text = string.Format("{0} ({1})", s.Hostname, string.IsNullOrEmpty(s.IpAddress) ? "n/a" : s.IpAddress)
                                                      };
            var addServerModel = new AddServerModel(potentialServers, potentialServersSelectListItems, selectedServers.Select(x => x.Id), customer.Id);

            addServerModel.SelectedServersHeader = "Customer Servers    ";
            addServerModel.PotentialServersHeader = "Unknown Servers";
            addServerModel.UnavailableMessage = "There are no servers available at this time";

            return View("AddServers", addServerModel);
        }

        [HttpPost]
        public JsonResult AddCustomerServers(AddServerModel addServerModel)
        {
            if (!_serverService.SetActiveServers(addServerModel.SelectedServerIds.ToList(), addServerModel.ParentId))
            {
                return Json(new { success = false, errors = new[] { "There was an error activating the selected servers" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult AddServerGroupServers(Guid serverGroupId)
        {
            var serverGroup = _serverService.GetByKey<ServerGroup>(serverGroupId);

            var selectedServers = serverGroup.Servers.Where(x => x.Status != Status.Deleted).OrderBy(s => s.Hostname);
            var potentialServers = serverGroup.ParentCustomer.Servers.Concat(selectedServers).OrderBy(s => s.Hostname).Distinct();
            var potentialServersSelectListItems = from s in potentialServers
                                                  select new SelectListItem
                                                  {
                                                      Value = s.Id.ToString(),
                                                      Text = string.Format("{0} ({1})", s.Hostname, string.IsNullOrEmpty(s.IpAddress) ? "n/a" : s.IpAddress)
                                                  };

            var addServerModel = new AddServerModel(potentialServers, potentialServersSelectListItems, selectedServers.Select(x => x.Id), serverGroup.Id);

            addServerModel.SelectedServersHeader = "Server Group Servers";
            addServerModel.PotentialServersHeader = "Customer Servers";
            addServerModel.UnavailableMessage = "There are no servers available at this time";

            return View("AddServers", addServerModel);
        }

        [HttpPost]
        public JsonResult AddServerGroupServers(AddServerModel addServerModel)
        {
            if (!_serverService.SetServerGroupServers(addServerModel.SelectedServerIds.ToList(), addServerModel.ParentId))
            {
                return Json(new { success = false, errors = new[] { "There was an error adding the selected servers" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult AddClusterNodes(Guid clusterId)
        {
            var cluster = _serverService.GetByKey<Cluster>(clusterId);

            var selectedServers = cluster.Nodes.Where(x => x.Status != Status.Deleted).Concat(cluster.VirtualServers.Where(x => x.Status != Status.Deleted)).OrderBy(s => s.Hostname);
            var potentialServers = cluster.Customer.Servers.Where(x => x.Cluster == null).Concat(selectedServers).Distinct().OrderBy(s => s.Hostname);
            var potentialServersSelectListItems = from s in potentialServers
                                                  select new SelectListItem
                                                  {
                                                      Value = s.Id.ToString(),
                                                      Text = string.Format("{0} ({1})", s.Hostname, string.IsNullOrEmpty(s.IpAddress) ? "n/a" : s.IpAddress)
                                                  };
            var addServerModel = new AddServerModel(potentialServers, potentialServersSelectListItems, selectedServers.Select(x => x.Id), cluster.Id);
            
            addServerModel.SelectedServersHeader = "Nodes & Virtual Servers";
            addServerModel.PotentialServersHeader = "Customer Servers";
            addServerModel.UnavailableMessage = "There are no servers available for this cluster";

            return View("AddServers", addServerModel);
        }

        [HttpPost]
        public JsonResult AddClusterNodes(AddServerModel addServerModel)
        {
            if (!_serverService.SetClusterNodes(addServerModel.SelectedServerIds.ToList(), addServerModel.ParentId))
            {
                return Json(new { success = false, errors = new[] { "There was an error activating the selected servers" } });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult ServerRemoveServerGroup(Guid serverId, Guid serverGroupId)
        {
            var serverIds = new List<Guid>();
            serverIds.Add(serverId);

            if (!_serverService.RemoveServersFromServerGroup(serverIds, serverGroupId))
            {
                return Json(new { success = false, errors = new string[] { "The Server Could Not Be Removed!!" } });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult ServerRemoveCluster(Guid serverId, Guid clusterId)
        {
            var serverIds = new List<Guid>();
            serverIds.Add(serverId);

            if (!_serverService.RemoveNodesFromCluster(serverIds, clusterId))
            {
                return Json(new { success = false, errors = new string[] { "The Server Could Not Be Removed!!" } });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult ServerRemoveCustomer(Guid serverId, Guid customerId)
        {
            var serverIds = new List<Guid>();
            serverIds.Add(serverId);

            if (!_serverService.RemoveServersFromCustomer(serverIds, customerId))
            {
                return Json(new { success = false, errors = new string[] { "The Server Could Not Be Removed!!" } });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult ServerDelete(Guid serverId)
        {
            var serverIds = new List<Guid>();
            serverIds.Add(serverId);

            if (!_serverService.DeleteServers(serverIds))
            {
                return Json(new { success = false, errors = new string[] { "The Server Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult ServerEdit(Guid? serverId, Guid? clusterId)
        {
            if (serverId != null)
            {
                //Edit
                var server = _serverService.GetByKey<Server>(serverId.Value);

                Mapper.CreateMap<Server, ServerModel>();
                var serverModel = Mapper.Map<Server, ServerModel>(server);

                return View(serverModel);
            }

            return View(new ServerModel { IsVirtual = true, ClusterId = clusterId.GetValueOrDefault() });
        }

        [HttpPost]
        public JsonResult ServerEdit(ServerModel serverModel)
        {
            var errorMessages = new StringBuilder();
            var serverErrors = new List<string>();

            Mapper.CreateMap<ServerModel, Server>();
            var server = Mapper.Map<ServerModel, Server>(serverModel);

            if (serverModel.IsVirtual && serverModel.Id.Equals(Guid.Empty))
            {
                if (!_serverService.AddVirtualServer(serverModel.ClusterId, ref server))
                {
                    ModelState.AddModelError("Validation", "The server encountered and error during Save");
                }
            }
            else
            {
                if (!_serverService.SaveServer(ref server))
                {
                    ModelState.AddModelError("Validation", "The server encountered and error during Save");
                }
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = GetErrors(ModelState).ToArray() });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult ActivateServer(List<ServerActivationModel> activationData)
        {
            var success = false;

            if (activationData == null || activationData.Count == 0)
            {
                return Json(new { result = success }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                if (string.IsNullOrEmpty(activationData.First().DropId))
                {
                    return Json(new { result = success }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var customerId = activationData.Select(x => Guid.Parse(x.DropId)).First();
                    var serverIds = activationData.Select(x => Guid.Parse(x.DragId)).ToList();

                    success = _serverService.ActivateServers(serverIds, customerId);
                }
            }

            return Json(new { result = success }, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult AddNodesToCluster(List<AddNodesToClusterModel> nodeClusterData)
        {
            var success = false;

            if (nodeClusterData == null || nodeClusterData.Count == 0)
            {
                return Json(new { result = success }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                if (string.IsNullOrEmpty(nodeClusterData.First().DropId))
                {
                    return Json(new { result = success }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var clusterId = nodeClusterData.Select(x => Guid.Parse(x.DropId)).First();
                    var serverIds = nodeClusterData.Select(x => Guid.Parse(x.DragId)).ToList();

                    success = _serverService.AddNodesToCluster(serverIds, clusterId);
                }
            }

            return Json(new { result = success }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult AddServersToGroups(List<ServerActivationModel> serverGroupingData)
        {
            var success = false;

            if (serverGroupingData == null || serverGroupingData.Count == 0)
            {
                return Json(new { result = success }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                if (string.IsNullOrEmpty(serverGroupingData.First().DropId))
                {
                    return Json(new { result = success }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var serverGroupId = serverGroupingData.Select(x => Guid.Parse(x.DropId)).First();
                    var serverIds = serverGroupingData.Select(x => Guid.Parse(x.DragId)).ToList();

                    success = _serverService.AddServersToGroup(serverIds, serverGroupId);
                }
            }

            return Json(new { result = success }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region "ServerGroup Actions"
        [HttpPost]
        public JsonResult ServerGroupDelete(Guid serverGroupId)
        {
            var serverGroupIds = new List<Guid>();
            serverGroupIds.Add(serverGroupId);

            if (!_serverService.DeleteServerGroups(serverGroupIds))
            {
                return Json(new { success = false, errors = new string[] { "The Server Group Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult ServerGroupEdit(Guid? serverGroupId, Guid? parentId)
        {
            if (serverGroupId != null)
            {
                //Edit
                var serverGroup = _serverService.GetByKey<ServerGroup>(serverGroupId.Value);

                Mapper.CreateMap<ServerGroup, ServerGroupModel>();
                var serverGroupModel = Mapper.Map<ServerGroup, ServerGroupModel>(serverGroup);

                serverGroupModel.ParentId = serverGroup.Parent.Id;

                return View(serverGroupModel);
            }

            return View(new ServerGroupModel { ParentId = parentId.GetValueOrDefault() });
        }

        [HttpPost]
        public JsonResult ServerGroupEdit(ServerGroupModel serverGroupModel)
        {
            var errorMessages = new StringBuilder();
            var serverErrors = new List<string>();

            Mapper.CreateMap<ServerGroupModel, ServerGroup>();
            var serverGroup = Mapper.Map<ServerGroupModel, ServerGroup>(serverGroupModel);

            //Validate
            if (!_serverService.ValidateServerGroup(serverGroup, serverGroupModel.ParentId, out serverErrors))
            {
                foreach (var error in serverErrors)
                {
                    ModelState.AddModelError("Validation", error);
                }
            }
            else
            {
                if (!_serverService.SaveCustomerServerGroup(ref serverGroup, serverGroupModel.ParentId))
                {
                    ModelState.AddModelError("Validation", "The server encountered and error during Save");
                }
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = GetErrors(ModelState).ToArray() });
            }

            return Json(new { success = true });
        }
        #endregion

        #region "Cluster Actions"
        [HttpPost]
        public JsonResult ClusterDelete(Guid clusterId)
        {
            var clusterIds = new List<Guid>();
            clusterIds.Add(clusterId);

            if (!_serverService.DeleteClusters(clusterIds))
            {
                return Json(new { success = false, errors = new string[] { "The Cluster Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult ClusterEdit(Guid? clusterId, Guid? customerId)
        {
            if (clusterId != null)
            {
                //Edit
                var cluster = _serverService.GetByKey<Cluster>(clusterId.Value);

                Mapper.CreateMap<Cluster, ClusterModel>();
                var clusterModel = Mapper.Map<Cluster, ClusterModel>(cluster);

                clusterModel.CustomerId = cluster.Customer.Id;

                return View(clusterModel);
            }

            return View(new ClusterModel { CustomerId = customerId.GetValueOrDefault() });
        }

        [HttpPost]
        public JsonResult ClusterEdit(ClusterModel clusterModel)
        {
            var errorMessages = new StringBuilder();
            var serverErrors = new List<string>();

            Mapper.CreateMap<ClusterModel, Cluster>();
            var cluster = Mapper.Map<ClusterModel, Cluster>(clusterModel);

            if (!_serverService.SaveCluster(ref cluster, clusterModel.CustomerId))
            {
                ModelState.AddModelError("Validation", "The server encountered and error during Save");
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = GetErrors(ModelState).ToArray() });
            }

            return Json(new { success = true });
        }
        #endregion

        #region "DatabaseInstance Actions"
        [HttpPost]
        public JsonResult DatabaseInstanceDelete(Guid instanceId)
        {
            var instanceIds = new List<Guid>();
            instanceIds.Add(instanceId);

            if (!_serverService.DeleteInstances(instanceIds))
            {
                return Json(new { success = false, errors = new string[] { "The Database Instance Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult DatabaseInstanceEdit(Guid? instanceId, Guid? serverId)
        {
            if (instanceId != null)
            {
                //Edit
                var databaseInstance = _serverService.GetByKey<DatabaseInstance>(instanceId.Value);

                Mapper.CreateMap<DatabaseInstance, DatabaseInstanceModel>();
                var databaseInstanceModel = Mapper.Map<DatabaseInstance, DatabaseInstanceModel>(databaseInstance);

                databaseInstanceModel.ServerId = databaseInstance.Server.Id;

                return View(databaseInstanceModel);
            }

            return View(new DatabaseInstanceModel { ServerId = serverId.GetValueOrDefault() });
        }

        [HttpPost]
        public JsonResult DatabaseInstanceEdit(DatabaseInstanceModel databaseInstanceModel)
        {
            var errorMessages = new StringBuilder();
            var serverErrors = new List<string>();

            Mapper.CreateMap<DatabaseInstanceModel, DatabaseInstance>();
            var databaseInstance = Mapper.Map<DatabaseInstanceModel, DatabaseInstance>(databaseInstanceModel);

            if (!_serverService.SaveDatabaseInstance(ref databaseInstance, databaseInstanceModel.ServerId))
            {
                ModelState.AddModelError("Validation", "The server encountered and error during Save");
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = GetErrors(ModelState).ToArray() });
            }

            return Json(new { success = true });
        }
        #endregion

        #region "Database Actions"
        [HttpPost]
        public JsonResult DatabaseDelete(Guid databaseId)
        {
            var databaseIds = new List<Guid>();
            databaseIds.Add(databaseId);

            if (!_serverService.DeleteDatabases(databaseIds))
            {
                return Json(new { success = false, errors = new string[] { "The Database Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult DatabaseEdit(Guid? databaseId, Guid? databaseInstanceId)
        {
            if (databaseId != null)
            {
                //Edit
                var databaseInstance = _serverService.GetByKey<Database>(databaseId.Value);

                Mapper.CreateMap<Database, DatabaseModel>();
                var databaseModel = Mapper.Map<Database, DatabaseModel>(databaseInstance);

                databaseModel.DatabaseInstanceId = databaseModel.DatabaseInstanceId;

                return View(databaseModel);
            }

            return View(new DatabaseModel { DatabaseInstanceId = databaseInstanceId.GetValueOrDefault() });
        }

        [HttpPost]
        public JsonResult DatabaseEdit(DatabaseModel databaseModel)
        {
            var errorMessages = new StringBuilder();
            var serverErrors = new List<string>();

            Mapper.CreateMap<DatabaseModel, Database>();
            var database = Mapper.Map<DatabaseModel, Database>(databaseModel);

            if (!_serverService.SaveDatabase(ref database, databaseModel.DatabaseInstanceId))
            {
                ModelState.AddModelError("Validation", "The server encountered and error during Save");
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = GetErrors(ModelState).ToArray() });
            }

            return Json(new { success = true });
        }
        #endregion

        #region "Metric Configuration Actions"
        [HttpPost]
        public JsonResult MetricHierarchy()
        {
            var metricCriteria = new Specification<Metric>(x => x.Status != Status.Deleted);
            var metrics = _serverService.Find(metricCriteria).ToList();

            var serverMetrics = metrics.Where(x => ((x.MetricType & MetricType.Server) == MetricType.Server)).Select(x => new
            {
                data = x.Name,
                attr = new
                {
                    id = x.Id.ToString(),
                    rel = Constants.MetricHierarchyType.Metric.ToString(),
                    name = x.Name
                },
                state = "leaf"
            }).ToList();

            var virtualServerMetrics = metrics.Where(x => ((x.MetricType & MetricType.VirtualServer) == MetricType.VirtualServer)).Select(x => new
            {
                data = x.Name,
                attr = new
                {
                    id = x.Id.ToString(),
                    rel = Constants.MetricHierarchyType.Metric.ToString(),
                    name = x.Name
                },
                state = "leaf"
            }).ToList();

            var databaseInstanceMetrics = metrics.Where(x => ((x.MetricType & MetricType.Instance) == MetricType.Instance)).Select(x => new
            {
                data = x.Name,
                attr = new
                {
                    id = x.Id.ToString(),
                    rel = Constants.MetricHierarchyType.Metric.ToString(),
                    name = x.Name
                },
                state = "leaf"
            }).ToList();

            var databaseMetrics = metrics.Where(x => ((x.MetricType & MetricType.Database) == MetricType.Database)).Select(x => new
            {
                data = x.Name,
                attr = new
                {
                    id = x.Id.ToString(),
                    rel = Constants.MetricHierarchyType.Metric.ToString(),
                    name = x.Name
                },
                state = "leaf"
            }).ToList();

            var serverMetricFolder = new
            {
                data = "Server Metrics",
                attr = new
                {
                    id = Constants.SERVERMETRICFOLDERID.ToString(),
                    rel = Constants.MetricHierarchyType.MetricFolder.ToString(),
                    name = "Server Metrics"
                },
                state = "open",
                children = new[] { serverMetrics.OrderBy(x => x.data) }
            };

            var virtualServerMetricFolder = new
            {
                data = "Virtual Server Metrics",
                attr = new
                {
                    id = Constants.VIRTUALSERVERMETRICFOLDERID.ToString(),
                    rel = Constants.MetricHierarchyType.MetricFolder.ToString(),
                    name = " Virtual Server Metrics"
                },
                state = "open",
                children = new[] { virtualServerMetrics.OrderBy(x => x.data) }
            };

            var databaseInstanceMetricFolder = new
            {
                data = "Database Instance Metrics",
                attr = new
                {
                    id = Constants.DATABASEINSTANCEMETRICFOLDERID.ToString(),
                    rel = Constants.MetricHierarchyType.MetricFolder.ToString(),
                    name = "Database Instance Metrics"
                },
                state = "open",
                children = new[] { databaseInstanceMetrics.OrderBy(x => x.data) }
            };

            var databaseMetricFolder = new
            {
                data = "Database Metrics",
                attr = new
                {
                    id = Constants.DATABASEMETRICFOLDERID.ToString(),
                    rel = Constants.MetricHierarchyType.MetricFolder.ToString(),
                    name = "Database Metrics"
                },
                state = "open",
                children = new[] { databaseMetrics.OrderBy(x => x.data) }
            };


            var metricsFolder = new
            {
                data = "Delta Metrics",
                attr = new
                {
                    id = Constants.METRICFOLDERID.ToString(),
                    rel = Constants.MetricHierarchyType.MetricFolder.ToString(),
                    name = "Delta Metrics"
                },
                state = "open",
                children = new[] { serverMetricFolder, virtualServerMetricFolder, databaseInstanceMetricFolder, databaseMetricFolder }
            };

            return Json(metricsFolder);
        }

        [HttpGet]
        public ActionResult MetricDetail(Guid itemId)
        {
            var metricDetailModel = new ItemDetailModel("metric-details-tabs");
            var metricName = _serverService.GetByKey<Metric>(itemId).Name;

            metricDetailModel.Details.TabList.Add(InitializeMetricConfigSummaryModel(MetricConfigurationParentType.Metric, itemId, metricName));
            metricDetailModel.Details.TabList.Add(InitializeMaintenanceWindowSumaryModel(MaintenanceWindowParentType.Metric, itemId, metricName));

            return View(metricDetailModel);
        }

        [HttpGet]
        public ActionResult MaintenanceDetail(Guid itemId, Constants.ItemHierarchyType type)
        {
            var maintenanceDetailModel = new ItemDetailModel("maintenance-details-tabs");
            Thread.Sleep(5000);
            switch (type)
            {
                case Constants.ItemHierarchyType.Customer:
                    var customerName = _serverService.GetByKey<Customer>(itemId).Name;
                    maintenanceDetailModel.Details.TabList.Add(InitializeMetricConfigSummaryModel(MetricConfigurationParentType.Customer, itemId, customerName));
                    maintenanceDetailModel.Details.TabList.Add(InitializeMaintenanceWindowSumaryModel(MaintenanceWindowParentType.Customer, itemId, customerName));
                    break;
                case Constants.ItemHierarchyType.Cluster:
                    break;
                case Constants.ItemHierarchyType.ServerGroup:
                    var serverGroupName = _serverService.GetByKey<ServerGroup>(itemId).Name;
                    maintenanceDetailModel.Details.TabList.Add(InitializeMetricConfigSummaryModel(MetricConfigurationParentType.ServerGroup, itemId, serverGroupName));
                    maintenanceDetailModel.Details.TabList.Add(InitializeMaintenanceWindowSumaryModel(MaintenanceWindowParentType.ServerGroup, itemId, serverGroupName));
                    break;
                case Constants.ItemHierarchyType.Server:
                case Constants.ItemHierarchyType.ClusterNode:
                    var serverName = _serverService.GetByKey<Server>(itemId).Hostname;
                    maintenanceDetailModel.Details.TabList.Add(InitializeMetricConfigSummaryModel(MetricConfigurationParentType.Server, itemId, serverName));
                    maintenanceDetailModel.Details.TabList.Add(InitializeMaintenanceWindowSumaryModel(MaintenanceWindowParentType.Server, itemId, serverName));
                    maintenanceDetailModel.Details.TabList.Add(InitializeMetricInstanceSumaryModel(MetricInstanceParentType.Server, itemId, serverName));
                    break;
                case Constants.ItemHierarchyType.VirtualServer:
                    var virtualServerName = _serverService.GetByKey<Server>(itemId).Hostname;
                    maintenanceDetailModel.Details.TabList.Add(InitializeMetricConfigSummaryModel(MetricConfigurationParentType.Server, itemId, virtualServerName));
                    maintenanceDetailModel.Details.TabList.Add(InitializeMaintenanceWindowSumaryModel(MaintenanceWindowParentType.Server, itemId, virtualServerName));
                    maintenanceDetailModel.Details.TabList.Add(InitializeMetricInstanceSumaryModel(MetricInstanceParentType.VirtualServer, itemId, virtualServerName));
                    break;
                case Constants.ItemHierarchyType.DatabaseInstance:
                    var dbInstanceName = _serverService.GetByKey<DatabaseInstance>(itemId).Name;
                    maintenanceDetailModel.Details.TabList.Add(InitializeMetricInstanceSumaryModel(MetricInstanceParentType.Instance, itemId, dbInstanceName));
                    break;
                case Constants.ItemHierarchyType.Database:
                    var databaseName = _serverService.GetByKey<Database>(itemId).Name;
                    maintenanceDetailModel.Details.TabList.Add(InitializeMetricInstanceSumaryModel(MetricInstanceParentType.Database, itemId, databaseName));
                    break;
                case Constants.ItemHierarchyType.MetricInstance:
                    var metricInstanceName = _serverService.GetByKey<MetricInstance>(itemId).Label;
                    maintenanceDetailModel.Details.TabList.Add(InitializeMetricConfigSummaryModel(MetricConfigurationParentType.MetricInstance, itemId, metricInstanceName));
                    maintenanceDetailModel.Details.TabList.Add(InitializeMaintenanceWindowSumaryModel(MaintenanceWindowParentType.MetricInstance, itemId, metricInstanceName));
                    break;
                default:
                    break;
            }

            return View(maintenanceDetailModel);
        }

        [Authorize(Roles = "DeltaAdmin")]
        public ActionResult Metrics()
        {
            var metricsPageModel = InitializeMetricsPageModel();

            if (Request.IsAjaxRequest())
            {
                return View(metricsPageModel);
            }

            var configPortalModel = InitializeConfigPortalModel(metricsPageModel);

            return View("DeltaConfigPortal", configPortalModel);
        }

        public ActionResult Maintenance()
        {
            var maintenancePageModel = InitializeMaintenancePageModel();

            if (Request.IsAjaxRequest())
            {
                return View(maintenancePageModel);
            }

            var configPortalModel = InitializeConfigPortalModel(maintenancePageModel);

            return View("DeltaConfigPortal", configPortalModel);
        }

        [HttpGet]
        public ActionResult MetricConfigurationSummary(MetricConfigurationParentType parentType, Guid parentId)
        {
            var parentName = string.Empty;

            switch (parentType)
            {
                case MetricConfigurationParentType.Customer:
                    parentName = _serverService.GetByKey<Customer>(parentId).Name;
                    break;
                case MetricConfigurationParentType.ServerGroup:
                    parentName = _serverService.GetByKey<ServerGroup>(parentId).Name;
                    break;
                case MetricConfigurationParentType.Server:
                    parentName = _serverService.GetByKey<Server>(parentId).Hostname;
                    break;
                case MetricConfigurationParentType.MetricInstance:
                    parentName = _serverService.GetByKey<MetricInstance>(parentId).Label;
                    break;
                case MetricConfigurationParentType.Metric:
                    parentName = _serverService.GetByKey<Metric>(parentId).Name;
                    break;
            }

            var metricConfigSummaryModel = InitializeMetricConfigSummaryModel(parentType, parentId, parentName);

            return View(metricConfigSummaryModel);
        }

        [HttpPost]
        public ActionResult MetricConfigurationSummary(MetricConfigurationSummaryModel metricConfigSummaryModel)
        {
            var metricConfigModel = InitializeMetricConfigModel(metricConfigSummaryModel.MetricId, metricConfigSummaryModel.ParentType, metricConfigSummaryModel.ParentId);

            return View("MetricConfiguration", metricConfigModel);
        }

        [HttpPost]
        public JsonResult MaintenanceWindowDelete(Guid maintWindowId)
        {
            var maintWindowIds = new List<Guid>();
            maintWindowIds.Add(maintWindowId);

            if (!_serverService.DeleteMaintenanceWindows(maintWindowIds))
            {
                return Json(new { success = false, errors = new string[] { "The Maintenance Window Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult MaintenanceWindowEdit(Guid? maintWindowId, Guid? parentId)
        {
            if (maintWindowId != null)
            {
                //Edit
                var maintWindow = _serverService.GetByKey<MaintenanceWindow>(maintWindowId.Value);

                Mapper.CreateMap<MaintenanceWindow, MaintenanceWindowModel>();
                var maintWindowModel = Mapper.Map<MaintenanceWindow, MaintenanceWindowModel>(maintWindow);

                maintWindowModel.ParentId = maintWindow.Parent.Id;

                return View(maintWindowModel);
            }

            return View(new MaintenanceWindowModel { ParentId = parentId.GetValueOrDefault() });
        }

        [HttpPost]
        public JsonResult MaintenanceWindowEdit(MaintenanceWindowModel maintWindowModel)
        {
            var errorMessages = new StringBuilder();
            var serverErrors = new List<string>();

            Mapper.CreateMap<MaintenanceWindowModel, MaintenanceWindow>();
            var maintWindow = Mapper.Map<MaintenanceWindowModel, MaintenanceWindow>(maintWindowModel);

            //Validate
            if (!_serverService.ValidateMaintenanceWindow(maintWindow, out serverErrors))
            {
                foreach (var error in serverErrors)
                {
                    ModelState.AddModelError("Validation", error);
                }
            }
            else
            {
                if (!_serverService.SaveMaintenanceWindow(ref maintWindow, maintWindowModel.ParentId, maintWindowModel.ParentType))
                {
                    ModelState.AddModelError("Validation", "The server encountered and error during Save");
                }
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = GetErrors(ModelState).ToArray() });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult MetricConfigDelete(Guid metricConfigId)
        {
            var metricConfigIds = new List<Guid>();
            metricConfigIds.Add(metricConfigId);

            if (!_serverService.DeleteMetricConfigurations(metricConfigIds))
            {
                return Json(new { success = false, errors = new string[] { "The Metric Configuration Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult MaintenanceWindowsTable(string sidx, string sord, int page, int rows,
                                            bool _search, string searchField, string searchOper, string searchString, Guid parentId)
        {
            IEnumerable<MaintenanceWindow> maintWindows = null;
            MaintenanceWindowsByParentSpecification criteria = new MaintenanceWindowsByParentSpecification(parentId);

            var totalRecords = _serverService.GetPagedEntities<MaintenanceWindow>(page, rows, criteria, x => "", out maintWindows);
            var totalPages = (int)Math.Ceiling((float)totalRecords / (float)rows);

            var result = new
            {
                total = totalPages,
                page = page,
                records = totalRecords,
                rows = maintWindows.Select(x => new { x.Id, x.BeginDate, x.EndDate })
                            .ToList()
                            .Select(x => new
                            {
                                id = x.Id.ToString(),
                                cell = new string[] {
                            GetActionItems(new List<ActionModel> {
                                new ActionModel {   
                                    Id =x.Id.ToString(), 
                                    Title="Edit Maintenance Window", 
                                    Class = "edit-row-button",
                                    Url = "Config/MaintenanceWindowEdit?parentid=" + parentId + "&maintwindowid=" + x.Id.ToString(),
                                    Icon=Constants.EDITICON, 
                                    Alt="Edit"},
                                new ActionModel { 
                                    Id =x.Id.ToString(), 
                                    Title="Delete Maintenance Window",
                                    Url = "Config/MaintenanceWindowDelete?parentid=" + parentId + "&maintwindowid=" + x.Id.ToString(),
                                    Class = "delete-row-button", 
                                    Icon=Constants.DELETEICON, 
                                    Alt="Delete"},
                            }),
                            x.BeginDate.ToString("MM/dd/yyyy hh:mm tt"),
                            x.EndDate.ToString("MM/dd/yyyy hh:mm tt"),
                            GetMaintWindowDisplayText(maintWindows.Where(y => y.Id.Equals(x.Id)).FirstOrDefault())
                        }
                            })
                            .ToArray(),
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }



        #region "MetricThreshold Actions"
        [HttpPost]
        public JsonResult MetricThresholdDelete(Guid metricThresholdId)
        {
            var metricThresholdIds = new List<Guid>();
            metricThresholdIds.Add(metricThresholdId);

            if (!_serverService.DeleteMetricThresholds(metricThresholdIds))
            {
                return Json(new { success = false, errors = new string[] { "The Metric Threshold Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult MetricThresholdEdit(Guid? metricThresholdId, Guid? metricConfigId)
        {
            if (metricThresholdId != null)
            {
                var metricThreshold = _serverService.GetByKey<MetricThreshold>(metricThresholdId.Value);

                Mapper.CreateMap<MetricThreshold, MetricThresholdModel>();
                var metricThresholdModel = Mapper.Map<MetricThreshold, MetricThresholdModel>(metricThreshold);

                metricThresholdModel.MetricThresholdType = metricThreshold.MetricConfiguration.Metric.MetricThresholdType;
                metricThresholdModel.MetricConfigId = metricThreshold.MetricConfiguration.Id;

                return View(metricThresholdModel);
            }

            var metricConfiguration = _serverService.GetByKey<MetricConfiguration>(metricConfigId.GetValueOrDefault());
            var newMetricThresholdModel = new MetricThresholdModel(metricConfiguration.Metric.MetricThresholdType) { MetricConfigId = metricConfigId.GetValueOrDefault() };

            return View(newMetricThresholdModel);
        }

        [HttpPost]
        public JsonResult MetricThresholdEdit(MetricThresholdModel metricThresholdModel)
        {
            var errorMessages = new StringBuilder();
            var serverErrors = new List<string>();

            Mapper.CreateMap<MetricThresholdModel, MetricThreshold>();
            var metricThreshold = Mapper.Map<MetricThresholdModel, MetricThreshold>(metricThresholdModel);

            //Validate
            if (!_serverService.ValidateMetricThreshold(metricThreshold, out serverErrors))
            {
                foreach (var error in serverErrors)
                {
                    ModelState.AddModelError("Validation", error);
                }
            }
            else
            {
                if (!_serverService.SaveMetricThreshold(ref metricThreshold, metricThresholdModel.MetricConfigId))
                {
                    ModelState.AddModelError("Validation", "The server encountered and error during Save");
                }
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = GetErrors(ModelState).ToArray() });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult MetricThresholdsTable(string sidx, string sord, int page, int rows,
                                            bool _search, string searchField, string searchOper, string searchString, Guid parentId)
        {
            IEnumerable<MetricThreshold> metricThresholds = null;
            Specification<MetricThreshold> criteria = new Specification<MetricThreshold>(x => x.MetricConfiguration.Id.Equals(parentId));

            if (_search)
            {
                criteria = criteria.And(x => x.Severity.ToString().Contains(searchString));
            }

            var totalRecords = _serverService.GetPagedEntities<MetricThreshold>(page, rows, criteria, x => "", out metricThresholds);
            var totalPages = (int)Math.Ceiling((float)totalRecords / (float)rows);

            var result = new
            {
                total = totalPages,
                page = page,
                records = totalRecords,
                rows = metricThresholds.Select(x => new { x.Id, x.CeilingValue, x.FloorValue, x.MatchValue, x.NumberOfOccurrences, x.Severity, x.ThresholdComparisonFunction, x.ThresholdValueType, x.TimePeriod })
                            .ToList()
                            .Select(x => new
                            {
                                id = x.Id.ToString(),
                                cell = new string[] {
                            GetActionItems(new List<ActionModel> {
                                new ActionModel {   
                                    Id =x.Id.ToString(), 
                                    Title="Edit Threshold", 
                                    Class = "edit-row-button",
                                    Url = "Config/MetricThresholdEdit?metricconfigid=" + parentId + "&metricthresholdid=" + x.Id.ToString(),
                                    Icon=Constants.EDITICON, 
                                    Alt="Edit"},
                                new ActionModel { 
                                    Id =x.Id.ToString(), 
                                    Title="Delete Threshold",
                                    Url = "Config/MetricThresholdDelete?metricthresholdid=" + x.Id.ToString(),
                                    Class = "delete-row-button", 
                                    Icon=Constants.DELETEICON, 
                                    Alt="Delete"},
                            }),
                            x.ThresholdComparisonFunction.ToString(),
                            x.Severity.ToString(),
                            x.TimePeriod.ToString(),
                            x.CeilingValue.ToString(),
                            x.FloorValue.ToString(),
                            x.MatchValue,
                            x.NumberOfOccurrences.ToString(),
                            x.ThresholdValueType.ToString(),
                            GetThresholdDisplayText(metricThresholds.Where(y => y.Id.Equals(x.Id)).FirstOrDefault())
                        }
                            })
                            .ToArray(),
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region "Schedules"
        [HttpPost]
        public JsonResult ScheduleDelete(Guid scheduleId)
        {
            var scheduleIds = new List<Guid>();
            scheduleIds.Add(scheduleId);

            if (!_serverService.DeleteSchedules(scheduleIds))
            {
                return Json(new { success = false, errors = new string[] { "The Schedule Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult ScheduleEdit(Guid? scheduleId, Guid? metricConfigId)
        {
            if (scheduleId != null)
            {
                //Edit
                var schedule = _serverService.GetByKey<Schedule>(scheduleId.Value);

                Mapper.CreateMap<Schedule, ScheduleModel>();
                var scheduleModel = Mapper.Map<Schedule, ScheduleModel>(schedule);

                if (scheduleModel.Hour != null && scheduleModel.Minute != null)
                    scheduleModel.Time = scheduleModel.Hour + ":" + scheduleModel.Minute;

                scheduleModel.MetricConfigId = schedule.MetricConfiguration.Id;

                return View(scheduleModel);
            }

            return View(new ScheduleModel { MetricConfigId = metricConfigId.GetValueOrDefault() });
        }

        [HttpPost]
        public JsonResult ScheduleEdit(ScheduleModel scheduleModel)
        {
            var errorMessages = new StringBuilder();
            var serverErrors = new List<string>();

            Mapper.CreateMap<ScheduleModel, Schedule>();
            var schedule = Mapper.Map<ScheduleModel, Schedule>(scheduleModel);

            //Validate
            if (!_serverService.ValidateSchedule(schedule, out serverErrors))
            {
                foreach (var error in serverErrors)
                {
                    ModelState.AddModelError("Validation", error);
                }
            }
            else
            {
                if (!_serverService.SaveSchedule(ref schedule, scheduleModel.MetricConfigId))
                {
                    ModelState.AddModelError("Validation", "The server encountered and error during Save");
                }
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = GetErrors(ModelState).ToArray() });
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult SchedulesTable(string sidx, string sord, int page, int rows,
                                            bool _search, string searchField, string searchOper, string searchString, Guid parentId)
        {
            IEnumerable<Schedule> schedules = null;
            Specification<Schedule> criteria = new Specification<Schedule>(x => x.MetricConfiguration.Id.Equals(parentId));

            var totalRecords = _serverService.GetPagedEntities<Schedule>(page, rows, criteria, x => "", out schedules);
            var totalPages = (int)Math.Ceiling((float)totalRecords / (float)rows);

            if (_search)
            {
                criteria = criteria.And(x => x.ScheduleType.ToString().Contains(searchString));
            }

            var result = new
            {
                total = totalPages,
                page = page,
                records = totalRecords,
                rows = schedules.Select(x => new { x.Id, x.Month, x.Day, x.Hour, x.Minute, x.DayOfWeek, x.ScheduleType, x.Interval })
                            .ToList()
                            .Select(x => new
                            {
                                id = x.Id.ToString(),
                                cell = new string[] {
                            GetActionItems(new List<ActionModel> {
                                new ActionModel {   
                                    Id =x.Id.ToString(), 
                                    Title="Edit Schedule", 
                                    Class = "edit-row-button",
                                    Url = "Config/ScheduleEdit?metricconfigid=" + parentId + "&scheduleid=" + x.Id.ToString(),
                                    Icon=Constants.EDITICON, 
                                    Alt="Edit"},
                                new ActionModel { 
                                    Id =x.Id.ToString(), 
                                    Title="Delete Schedule",
                                    Url = "Config/ScheduleDelete?scheduleid=" + x.Id.ToString(),
                                    Class = "delete-row-button", 
                                    Icon=Constants.DELETEICON, 
                                    Alt="Delete"},
                            }),
                            x.Month.ToString(),
                            x.Day.ToString(),
                            x.Hour.ToString(),
                            x.Minute.ToString(),
                            x.DayOfWeek.ToString(),
                            x.ScheduleType.ToString(),
                            x.Interval.ToString(),
                            GetScheduleDisplayText(schedules.Where(y => y.Id.Equals(x.Id)).FirstOrDefault())
                        }
                            })
                            .ToArray(),
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion
        #endregion

        #region "Metric Instance Actions"
        public ActionResult MetricInstanceAdd(MetricType type, Guid parentId)
        {
            var metrics = _serverService.GetMetrics(type, parentId);
            var metricSelectModel = new MetricSelectModel(metrics);
            metricSelectModel.MetricSelectFormAction = "/Config/GetMetricData?parentId=" + parentId + "&parenttype=" + type.ToString();

            return View("MetricSelect", metricSelectModel);
        }

        public ActionResult GetMetricInstanceData(Guid metricInstanceId, Guid parentId, MetricInstanceParentType parentType)
        {
            var formAction = "/Config/SaveMetricData?parentid=" + parentId + "&parenttype=" + parentType.ToString();
            var metricInstanceDataModel = InitializeMetricInstanceDataModel(Guid.Empty, parentId, metricInstanceId, formAction);

            return View("MetricInstanceData", metricInstanceDataModel);
        }

        [HttpPost]
        public JsonResult DeleteMetricInstance(Guid metricInstanceId)
        {
            var metricInstanceIds = new List<Guid>();
            metricInstanceIds.Add(metricInstanceId);

            if (!_serverService.DeleteMetricInstances(metricInstanceIds))
            {
                return Json(new { success = false, errors = new string[] { "The Metric Instance Could Not Be Deleted!!" } });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult GetMetricData(Guid metricId, Guid parentId, MetricInstanceParentType parentType)
        {
            var formAction = "/Config/SaveMetricData?parentId=" + parentId + "&parenttype=" + parentType.ToString();
            var metricInstanceDataModel = InitializeMetricInstanceDataModel(metricId, parentId, Guid.Empty, formAction);

            if (metricInstanceDataModel.DataItems.Count() == 0)
            {
                if (_serverService.SaveMetricInstance(Guid.Empty, metricId, parentId, new MetricData(), Status.Active, parentType))
                {
                    ViewBag.Result = "The metric instance was added successfully";
                }
                else
                {
                    ViewBag.Result = "There was an error adding the metric instance";
                }


                return View("AddMetricInstanceResult");
            }

            return PartialView("MetricInstanceData", metricInstanceDataModel);
        }

        [HttpPost]
        public ActionResult SaveMetricData(FormCollection formcollection, Guid parentId, MetricInstanceParentType parentType)
        {
            var metricInstanceModel = ExtractMetricInstanceData(formcollection);

            var metricId = metricInstanceModel.MetricId;
            var metricInstanceId = metricInstanceModel.MetricInstanceId;
            var status = metricInstanceModel.Status;
            var metricData = ExtractMetricDataFromModel(metricInstanceModel);

            if (_serverService.SaveMetricInstance(metricInstanceId, metricId, parentId, metricData, status, parentType))
            {
                ViewBag.Result = "The metric instance was updated successfully";
            }
            else
            {
                ViewBag.Result = "There was an error updating the metric instance";
            }

            return View("AddMetricInstanceResult");
        }

        [HttpGet]
        public JsonResult MetricInstancesTable(string sidx, string sord, int page, int rows,
                                            bool _search, string searchField, string searchOper, string searchString, string specType, Guid parentId)
        {
            IEnumerable<MetricInstance> metricInstances = null;
            Specification<MetricInstance> criteria = new Specification<MetricInstance>(x => x.Status != Status.Deleted);
            Server server = null;

            //Convert the posted specification type and create the criteria
            Constants.SpecificationType specificationType = (Constants.SpecificationType)Enum.Parse(typeof(Constants.SpecificationType), specType, true);

            switch (specificationType)
            {
                case Constants.SpecificationType.ServerMetrics:
                    server = _serverService.GetByKey<Server>(parentId);
                    criteria = criteria.And(x => ((x.Metric.MetricType & MetricType.Server) == MetricType.Server));
                    criteria = criteria.And(x => x.Server.Id.Equals(parentId));
                    break;
                case Constants.SpecificationType.DatabaseInstanceMetrics:
                    var instance = _serverService.GetByKey<DatabaseInstance>(parentId);
                    server = instance.Server;
                    criteria = criteria.And(x => ((x.Metric.MetricType & MetricType.Instance) == MetricType.Instance));
                    criteria = criteria.And(x => x.DatabaseInstance.Id.Equals(parentId));
                    break;
                case Constants.SpecificationType.DatabaseMetrics:
                    var database = _serverService.GetByKey<Database>(parentId);
                    server = database.Instance.Server;
                    criteria = criteria.And(x => ((x.Metric.MetricType & MetricType.Database) == MetricType.Database));
                    criteria = criteria.And(x => x.Database.Id.Equals(parentId));
                    break;
                case Constants.SpecificationType.VirtualServerMetrics:
                    server = _serverService.GetByKey<Server>(parentId);
                    criteria = criteria.And(x => (((x.Metric.MetricType & MetricType.VirtualServer) == MetricType.VirtualServer) ||
                                            (x.Metric.MetricType & MetricType.Server) == MetricType.Server));
                    criteria = criteria.And(x => x.Server.Id.Equals(parentId));
                    break;
                default:
                    break;
            }

            criteria = criteria.And(x => x.Server.Id.Equals(server.Id));

            if (_search)
            {
                criteria = criteria.And(x => (x.Label.Contains(searchString) || x.Metric.Name.Contains(searchString)));
            }

            var totalRecords = _serverService.GetPagedEntities<MetricInstance>(page, rows, criteria, x => x.Label, out metricInstances);
            var totalPages = (int)Math.Ceiling((float)totalRecords / (float)rows);

            var result = new
            {
                total = totalPages,
                page = page,
                records = totalRecords,

                rows = metricInstances.ToList()
                            .Select(x => new
                            {
                                id = x.Id.ToString(),
                                cell = new string[] {
                                    GetMetricInstanceTableActionItems(specificationType, metricInstances.Where(y => y.Id.Equals(x.Id)).FirstOrDefault(), parentId),
                                    x.Metric.Name,
                                    x.Label,
                                    x.Status.ToString()
                                }
                            })
                            .ToArray(),
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region "Private Methods"
        private MaintenanceWindowSumaryModel InitializeMaintenanceWindowSumaryModel(MaintenanceWindowParentType parentType, Guid parentId, string parentName)
        {
            var maintenanceWindowSummaryModel = new MaintenanceWindowSumaryModel(parentType, parentId, parentName);
            maintenanceWindowSummaryModel.TabListName = "Maintenance Windows";
            maintenanceWindowSummaryModel.TabListId = "maintwindow-tab";
            maintenanceWindowSummaryModel.MaintenanceWindowTable = GetTableModel(Constants.TableType.MaintWindow, parentId, false, false, true);
            maintenanceWindowSummaryModel.MaintenanceWindowTable.EditUrl += "?parenttype=" + parentType.ToString();
            maintenanceWindowSummaryModel.MaintenanceWindowTable.IsSearchEnabled = false;

            //Toolbar
            maintenanceWindowSummaryModel.MaintenanceWindowTable.ToolbarItems.Add(new ToolbarItemModel
            {
                ToolbarItemId = "add-maintwindow-button",
                ToolbarItemIconUrl = "~/Content/images/navicons-small/171.png",
                ToolbarItemUrl = "Config/MaintenanceWindowEdit?parentId=" + parentId + "&parenttype=" + parentType.ToString(),
                ToolbarItemTitle = "Add Maintenance Window",
                ToolbarItemAction = "",
                ToolbarItemClass = "small-button add-button"
            });

            return maintenanceWindowSummaryModel;
        }

        private MetricInstanceSummaryModel InitializeMetricInstanceSumaryModel(MetricInstanceParentType parentType, Guid parentId, string parentName)
        {

            var addMetricInstanceAction = "Config/MetricInstanceAdd?parentid=";
            var specificationType = Constants.SpecificationType.None;

            switch (parentType)
            {
                case MetricInstanceParentType.Server:
                    addMetricInstanceAction += parentId + "&type=Server";
                    specificationType = Constants.SpecificationType.ServerMetrics;
                    break;
                case MetricInstanceParentType.Instance:
                    addMetricInstanceAction += parentId + "&type=Instance";
                    specificationType = Constants.SpecificationType.DatabaseInstanceMetrics;
                    break;
                case MetricInstanceParentType.Database:
                    addMetricInstanceAction += parentId + "&type=Database";
                    specificationType = Constants.SpecificationType.DatabaseMetrics;
                    break;
                case MetricInstanceParentType.VirtualServer:
                    addMetricInstanceAction += parentId + "&type=VirtualServer";
                    specificationType = Constants.SpecificationType.VirtualServerMetrics;
                    break;
                default:
                    break;
            }

            var maintenanceWindowSummaryModel = new MetricInstanceSummaryModel(parentType, parentId, parentName);
            maintenanceWindowSummaryModel.TabListName = "Metric Instances";
            maintenanceWindowSummaryModel.TabListId = "metricinstance-tab";
            maintenanceWindowSummaryModel.MetricInstanceTable = GetTableModel(Constants.TableType.MetricInstance, parentId, false, false, true);
            maintenanceWindowSummaryModel.MetricInstanceTable.IsSearchEnabled = true;
            maintenanceWindowSummaryModel.MetricInstanceTable.SpecType = specificationType.ToString();

            //Toolbar
            maintenanceWindowSummaryModel.MetricInstanceTable.ToolbarItems.Add(new ToolbarItemModel
            {
                ToolbarItemId = "add-metricinstance-button",
                ToolbarItemUrl = addMetricInstanceAction,
                ToolbarItemTitle = "Add Metric Instance",
                ToolbarItemClass = "small-button add-button"
            });

            return maintenanceWindowSummaryModel;
        }

        private MetricConfigurationSummaryModel InitializeMetricConfigSummaryModel(MetricConfigurationParentType parentType, Guid parentId, string parentName)
        {
            var metricConfigSummaryModel = new MetricConfigurationSummaryModel(parentType, parentId, parentName);

            metricConfigSummaryModel.Metrics = _serverService.GetMetrics(parentType, parentId).OrderBy(x => x.Name);
            metricConfigSummaryModel.MetricConfig = InitializeMetricConfigModel(metricConfigSummaryModel.Metrics.FirstOrDefault().Id, parentType, parentId);
            metricConfigSummaryModel.TabListName = "Configurations";
            metricConfigSummaryModel.TabListId = "configuration-tab";

            return metricConfigSummaryModel;
        }

        private MetricConfigurationModel InitializeMetricConfigModel(Guid metricId, MetricConfigurationParentType parentType, Guid metricConfigParentId)
        {
            MetricConfigurationModel metricConfigModel = null;

            Specification<MetricConfiguration> criteria = new Specification<MetricConfiguration>(x => x.IsTemplate == false);
            criteria = criteria.And(x => x.Metric.Id.Equals(metricId));
            criteria = criteria.And(new MetricConfigurationsByParentSpecification(metricConfigParentId));

            //Try to retrieve the an existing config
            var metricConfig = _serverService.Find<MetricConfiguration>(criteria).FirstOrDefault();

            if (metricConfig == null)
            {
                //None existing, so create new
                _serverService.AddMetricConfiguration(metricId, metricConfigParentId, parentType, ref metricConfig);
            }

            Mapper.CreateMap<MetricConfiguration, MetricConfigurationModel>();
            metricConfigModel = Mapper.Map<MetricConfiguration, MetricConfigurationModel>(metricConfig);

            //Setup the schedules and thresholds tables
            metricConfigModel.SchedulesTable = GetTableModel(Constants.TableType.Schedules, metricConfig.Id, false, false, true);
            metricConfigModel.SchedulesTable.IsSearchEnabled = false;

            metricConfigModel.SchedulesTable.ToolbarItems.Add(new ToolbarItemModel
            {
                ToolbarItemId = "add-schedules-button",
                ToolbarItemIconUrl = "~/Content/images/navicons-small/171.png",
                ToolbarItemUrl = "Config/ScheduleEdit?metricconfigid=" + metricConfig.Id,
                ToolbarItemTitle = "Add Schedule",
                ToolbarItemAction = "",
                ToolbarItemClass = "small-button add-button"
            });

            metricConfigModel.ThresholdsTable = GetTableModel(Constants.TableType.MetricThresholds, metricConfig.Id, false, false, true);
            metricConfigModel.ThresholdsTable.IsSearchEnabled = false;

            metricConfigModel.ThresholdsTable.ToolbarItems.Add(new ToolbarItemModel
            {
                ToolbarItemId = "add-metricthresholds-button",
                ToolbarItemIconUrl = "~/Content/images/navicons-small/171.png",
                ToolbarItemUrl = "Config/MetricThresholdEdit?metricconfigid=" + metricConfig.Id,
                ToolbarItemTitle = "Add Threshold",
                ToolbarItemAction = "",
                ToolbarItemClass = "small-button add-button"
            });


            return metricConfigModel;
        }

        private MetricInstanceDataModel ExtractMetricInstanceData(FormCollection formCollection)
        {
            var multiValueKeys = new List<string>();
            var model = new MetricInstanceDataModel();

            model.DataItems = new List<MetricDataItemModel>();

            model.MetricId = Guid.Parse(formCollection.GetValue("MetricId").AttemptedValue.ToString());
            model.MetricInstanceId = Guid.Parse(formCollection.GetValue("MetricInstanceId").AttemptedValue.ToString());
            model.Status = (Status)Enum.Parse(typeof(Status), formCollection.GetValue("Status").AttemptedValue);

            formCollection.Remove("MetricId");
            formCollection.Remove("MetricInstanceId");
            formCollection.Remove("Status");

            foreach (var key in formCollection.AllKeys)
            {
                if (multiValueKeys.Contains(key) || key.Contains("tagname"))
                {
                    continue;
                }

                var metricDataItemModel = new MetricDataItemModel();
                metricDataItemModel.DisplayName = key.Replace("-", " ");

                var tagNameKey = formCollection.AllKeys.Where(x => x.Contains(key) && x.Contains("tagname")).FirstOrDefault();
                metricDataItemModel.TagName = formCollection.GetValue(tagNameKey).AttemptedValue;
                formCollection.Remove(tagNameKey);

                //Look for other keys with the same prefix
                var multiValues = formCollection.AllKeys.Where(x => x.Contains(key) && !x.Contains("tagname") && x != key).ToList();

                if (multiValues.Count() > 0)
                {
                    metricDataItemModel.Children = new List<MetricDataItemModel>();

                    foreach (var item in multiValues)
                    {
                        var multiValue = formCollection.GetValue(item).AttemptedValue;
                        metricDataItemModel.Children.Add(new MetricDataItemModel
                        {
                            RenderType = Constants.MetricDataRenderType.Text,
                            Value = multiValue,
                        });

                        multiValueKeys.Add(item);
                        formCollection.Remove(item);
                    }

                    metricDataItemModel.RenderType = Constants.MetricDataRenderType.MultipleValues;
                }
                else
                {
                    metricDataItemModel.Value = formCollection.GetValue(key).AttemptedValue;
                    metricDataItemModel.RenderType = Constants.MetricDataRenderType.Text;
                }

                model.DataItems.Add(metricDataItemModel);
            }

            return model;
        }

        private HierarchyModel InitializeMetricsPageModel()
        {
            var metricsPageModel = new HierarchyModel();

            metricsPageModel.MainMenuItemId = Constants.CONFIGMENUITEMID;
            metricsPageModel.PageHeader = "Metrics";
            metricsPageModel.ContentPageView = "Metrics";
            metricsPageModel.IsSearchEnabled = false;

            return metricsPageModel;
        }

        private HierarchyModel InitializeMaintenancePageModel()
        {
            var maintenancePageModel = new HierarchyModel();

            maintenancePageModel.MainMenuItemId = Constants.CONFIGMENUITEMID;
            maintenancePageModel.PageHeader = "Customers";
            maintenancePageModel.ContentPageView = "Maintenance";
            maintenancePageModel.SelectMessage = "Please Select a Customer...";
            maintenancePageModel.SearchPlaceholder = "Search Customers...";
            maintenancePageModel.IsSearchEnabled = true;

            //Toolbar
            maintenancePageModel.ToolbarItems.Add(new ToolbarItemModel
            {
                ToolbarItemId = "add-customer-button",
                ToolbarItemIconUrl = "~/Content/images/navicons-small/171.png",
                ToolbarItemUrl = "Config/CustomerEdit",
                ToolbarItemTitle = "Add Customer",
                ToolbarItemAction = "",
                ToolbarItemClass = "small-button add-button"
            });

            return maintenancePageModel;
        }

        private MetricInstanceDataModel InitializeMetricInstanceDataModel(Guid metricId, Guid parentId, Guid metricInstanceId, string formAction)
        {
            var metricInstanceDataModel = new MetricInstanceDataModel();
            metricInstanceDataModel.MetricInstanceParentId = parentId;
            metricInstanceDataModel.MetricInstanceDataFormAction = formAction;

            MetricData metricData = null;

            //For add new metric instance
            if (metricInstanceId == Guid.Empty)
            {
                metricData = _serverService.GetMetricData(metricId, parentId);
                metricInstanceDataModel.MetricId = metricId;
                metricInstanceDataModel.MetricInstanceId = Guid.Empty;
                metricInstanceDataModel.MetricInstanceParentId = parentId;
                metricInstanceDataModel.Status = Status.Active;
            }
            else
            {
                var metricInstance = _serverService.GetMetricInstance(metricInstanceId, parentId, out metricData);
                metricInstanceDataModel.MetricInstanceId = metricInstance.Id;
                metricInstanceDataModel.MetricId = metricInstance.Metric.Id;
                metricInstanceDataModel.MetricInstanceParentId = parentId;
                metricInstanceDataModel.Status = metricInstance.Status;
            }

            foreach (var item in metricData.Data)
            {
                var dataItem = new MetricDataItemModel();
                dataItem.ItemId = item.DisplayName.Replace(" ", "-");
                dataItem.DisplayName = item.DisplayName;
                dataItem.Value = item.Value;
                dataItem.TagName = item.TagName;
                dataItem.IsRequired = item.IsRequired;

                if (item.ValueOptions != null && item.ValueOptions.Count() > 0)
                {
                    dataItem.ValueOptions = item.ValueOptions;
                    dataItem.SelectedValueOption = item.SelectedValueOption;
                    dataItem.IsRequired = item.IsRequired;
                    dataItem.RenderType = Constants.MetricDataRenderType.SelectList;
                }

                if (item.MultipleValues)
                {
                    dataItem.RenderType = Constants.MetricDataRenderType.MultipleValues;
                }

                if (item.Children != null)
                {
                    var index = 0;
                    foreach (var child in item.Children)
                    {
                        dataItem.Children.Add(new MetricDataItemModel
                        {
                            ItemId = dataItem.ItemId + "_" + index.ToString(),
                            DisplayName = child.DisplayName,
                            RenderType = Constants.MetricDataRenderType.Text,
                            SelectedValueOption = string.Empty,
                            TagName = child.TagName,
                            Value = child.Value,
                            Children = null
                        });
                        index++;
                    }
                }

                metricInstanceDataModel.DataItems.Add(dataItem);
            }

            return metricInstanceDataModel;
        }

        private string GetMetricInstanceTableActionItems(Constants.SpecificationType specType, MetricInstance metricInstance, Guid parentId)
        {
            var actionItems = new List<ActionModel>();
            var parentType = MetricInstanceParentType.Server;

            switch (specType)
            {
                case Constants.SpecificationType.ServerMetrics:
                    break;
                case Constants.SpecificationType.DatabaseInstanceMetrics:
                    parentType = MetricInstanceParentType.Instance;
                    break;
                case Constants.SpecificationType.DatabaseMetrics:
                    parentType = MetricInstanceParentType.Database;
                    break;
                default:
                    break;
            }

            actionItems = new List<ActionModel> {
                                new ActionModel { 
                                    Id = metricInstance.Id.ToString(), 
                                    Title="Edit Metric Instance", 
                                    Class="edit-row-button",
                                    Url = "Config/GetMetricInstanceData?parentId=" + parentId.ToString() + "&metricinstanceid=" + metricInstance.Id.ToString() + "&parenttype=" + parentType.ToString(),
                                    Icon=Constants.EDITICON,
                                    Alt="Edit"
                                }
                     };

            if (metricInstance.Metric.AdapterClass != "CheckInPlugin")
            {
                actionItems.Add(new ActionModel
                {
                    Id = metricInstance.Id.ToString(),
                    Title = "Remove Metric Instance",
                    Url = "Config/DeleteMetricInstance?metricinstanceid=" + metricInstance.Id.ToString(),
                    Class = "delete-row-button",
                    Icon = Constants.DELETEICON,
                    Alt = "Delete"
                });
            }

            return GetActionItems(actionItems);
        }

        private string GetMaintWindowDisplayText(MaintenanceWindow maintWindow)
        {
            var displayText = new StringBuilder();

            displayText.Append("Maintenance is scheduled to begin at ");
            displayText.Append(maintWindow.BeginDate.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt"));
            displayText.Append(", and to end at ");
            displayText.Append(maintWindow.EndDate.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt"));

            return displayText.ToString();
        }

        private string GetScheduleDisplayText(Schedule schedule)
        {
            var displayText = new StringBuilder();

            if (schedule.ScheduleType == (int)ScheduleType.Once)
            {
                displayText.Append("Schedule the metric to run once");
            }
            else
            {
                displayText.Append("Schedule the metric to run every " + schedule.Interval + " " + schedule.ScheduleType.ToString().ToLower());

                switch (schedule.ScheduleType)
                {
                    case ScheduleType.Hours:
                        displayText.Append(" at " + schedule.Minute + " past the hour");
                        break;
                    case ScheduleType.Days:
                        displayText.Append(" at " + schedule.Hour + ":" + schedule.Minute);
                        break;
                    case ScheduleType.Weeks:
                        displayText.Append(" on " + schedule.DayOfWeek + " at " + schedule.Hour + ":" + schedule.Minute);
                        break;
                    case ScheduleType.Months:
                        displayText.Append(" on the " + schedule.Day + " day of the month at " + schedule.Hour + ":" + schedule.Minute);
                        break;
                    case ScheduleType.Year:
                        displayText.Append(" on the " + schedule.Day + " day of the year at " + schedule.Hour + ":" + schedule.Minute);
                        break;
                }
            }

            return displayText.ToString();
        }

        private string GetThresholdDisplayText(MetricThreshold metricThreshold)
        {
            var displayText = new StringBuilder();

            displayText.Append("Raise an alert level of '" + metricThreshold.Severity + "'");

            if (metricThreshold.ThresholdComparisonFunction.Value == (int)ThresholdComparisonFunction.Match)
            {
                displayText.Append(", If the metric value matches '" + metricThreshold.MatchValue + "'");
                displayText.Append(", and occurs " + metricThreshold.NumberOfOccurrences + " times");
            }
            else
            {
                displayText.Append(", If the metric " + metricThreshold.ThresholdComparisonFunction.ToString().ToLower());
                displayText.Append(" is between " + metricThreshold.FloorValue + " and " + metricThreshold.CeilingValue);

                if (metricThreshold.ThresholdValueType.Value == (int)ThresholdValueType.Percentage)
                {
                    displayText.Append(" percent");
                }

                if (metricThreshold.ThresholdComparisonFunction.Value == (int)ThresholdComparisonFunction.Value)
                {
                    displayText.Append(", and occurs " + metricThreshold.NumberOfOccurrences + " times");
                }
            }

            displayText.Append(" within a time period of " + metricThreshold.TimePeriod + " minutes");

            return displayText.ToString();
        }

        private MetricData ExtractMetricDataFromModel(MetricInstanceDataModel metricInstanceDataModel)
        {
            var metricData = new MetricData();

            metricData.MetricId = metricInstanceDataModel.MetricId;
            metricData.MetricInstanceId = metricInstanceDataModel.MetricInstanceId;

            foreach (var item in metricInstanceDataModel.DataItems)
            {
                var metricDataItem = new MetricDataItem
                {
                    DisplayName = item.DisplayName,
                    Value = item.Value,
                    TagName = item.TagName,
                };

                metricData.Data.Add(metricDataItem);

                if (item.Children != null)
                {
                    var metricDataItemChildren = new List<MetricDataItem>();

                    foreach (var child in item.Children)
                    {
                        metricDataItemChildren.Add(new MetricDataItem
                        {
                            DisplayName = child.DisplayName,
                            Value = child.Value,
                            TagName = child.TagName
                        });
                    }

                    metricDataItem.Children = metricDataItemChildren;
                }
            }

            return metricData;
        }
        #endregion
    }
}
