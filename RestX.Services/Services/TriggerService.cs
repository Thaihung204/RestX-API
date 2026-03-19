using AutoMapper;
using Hangfire;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace RestX.BLL.Services
{
    using Hangfire.Server;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using RestX.AdminDAL.Context;
    using RestX.BLL.DataTranferObjects.Share;
    using RestX.BLL.DataTranferObjects.TriggerActionTasks;
    using RestX.BLL.DataTransferObjects.Triggers;
    using RestX.BLL.Exceptionhandling;
    using RestX.BLL.Helpers;
    using RestX.BLL.Interfaces;
    using RestX.DAL.Context;
    using RestX.Models.Attributes;
    using RestX.Models.Enum;
    using RestX.Models.Tenants;
    using RestX.Models.Triggers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Trigger = Models.Triggers.Trigger;
    using TriggerAction = Models.Triggers.TriggerAction;
    using TriggerCriteria = Models.Triggers.TriggerCriteria;
    using TriggerObject = Models.Triggers.TriggerObject;
    using TriggerType = Models.Triggers.TriggerType;

    public class TriggerService : BaseService, ITriggerService
    {
        private RestxAdminContext adminContext;
        private readonly IEnumerable<IHttpContextAccessor> context;
        private IServiceScopeFactory serivceScopeFactory;
        private IMapper mapper;
        private IOptions<AppSettings> settings;
        private IBackgroundJobClient jobClient;
        private HttpClient client;
        private IRepoHelper repoHelper;
        public TriggerService( IBackgroundJobClient jobClient, IRepoHelper repoHelper, IOptions<AppSettings> settings, IMapper mapper, IRedisService redisService, IRepository Repo, IServiceScopeFactory serivceScopeFactory, IEnumerable<IHttpContextAccessor> context, IEnumerable<ActiveTenant> tenant = null) : base(Repo, redisService, tenant)
        {
            //this.clientFactory = clientFactory;
            this.jobClient = jobClient;
            this.settings = settings;
            this.mapper = mapper;
            //this.exceptionHandler = exceptionHandler;
            this.context = context;
            this.serivceScopeFactory = serivceScopeFactory;
            this.repoHelper = repoHelper;
        }

        public async Task<List<DataTransferObjects.Triggers.TriggerObject>> GetTriggerObjects()
        {
            return this.mapper.Map<List<TriggerObject>, List<DataTransferObjects.Triggers.TriggerObject>>((await this.Repo.GetAllAsync<TriggerObject>(orderBy: o => o.OrderBy(t => t.Name))).ToList());
        }

        public async Task<List<DataTransferObjects.Triggers.TriggerObjectProperties>> GetTriggerObjectProperties(int objectId)
        {
            var triggerObject = await this.Repo.GetFirstAsync<TriggerObject>(o => o.Id == objectId);
            if (triggerObject == null)
            {
                throw new AppException($"No trigger object fund with passed if {objectId}");
            }

            if (!string.IsNullOrEmpty(triggerObject.FullAssemblyName))
            {
                var type = Type.GetType($"{triggerObject.FullAssemblyName}, RestX.Models");

                var mainProperties = this.GetTriggerObjectProperties(type);

                return mainProperties;
            }

            return new List<DataTransferObjects.Triggers.TriggerObjectProperties>();
        }

        public List<SelectOption> GetTriggerTypes()
        {
            return UtilitiesHelper.ConvertEnumToList(typeof(TriggerType));
        }

        public List<SelectOption> GetTriggerCriteriaTypes()
        {
            return UtilitiesHelper.ConvertEnumToList(typeof(TriggerCriteriaType));
        }

        public async Task<List<SelectOption>> GetTriggerActionTypes()
        {
            var actionTypes = UtilitiesHelper.ConvertEnumToList(typeof(TriggerActionType)).Where(a => a.Id != "6").ToList();
            var serviceScope = serivceScopeFactory.CreateScope();
            return actionTypes;
        }

        public async Task<List<DataTransferObjects.Triggers.Trigger>> GetTriggers()
        {
            var data = (await this.Repo.GetAsync<Trigger>(orderBy: o => o.OrderBy(t => t.Name))).ToList();
            return this.mapper.Map<List<Trigger>, List<DataTransferObjects.Triggers.Trigger>>(data);
        }

        public async Task<DataTransferObjects.Triggers.Trigger> GetTriggerById(Guid triggerId)
        {
            var trigger = await this.Repo.GetFirstAsync<Trigger>(t => t.Id == triggerId, includeProperties: "Object,Criteria.Group");
            var mappedTrigger = this.mapper.Map<Trigger, DataTransferObjects.Triggers.Trigger>(trigger);

            mappedTrigger.Groups = mappedTrigger.Criteria.Where(c => c.TriggerCriteriaGroupId > 0).DistinctBy(c => c.TriggerCriteriaGroupId).Select(g => new DataTransferObjects.Triggers.TriggerGroup
            {
                Id = g.Group.Id,
                Name = g.Group.Name,
                LogicType = g.Group.LogicType
            }).ToList();

            return mappedTrigger;
        }

        public async Task DeleteTrigger(Guid triggerId, string userId)
        {
            var trigger = await this.Repo.GetFirstAsync<Trigger>(t => t.Id == triggerId, includeProperties: "Actions.ScheduledCriteria,Criteria");
            if (trigger == null)
            {
                throw new AppException($"No trigger found with id '{triggerId}'");
            }

            // Delete criteria groups
            var groupIds = trigger.Criteria.Where(c => c.TriggerCriteriaGroupId > 0).Select(c => c.TriggerCriteriaGroupId).Distinct();
            foreach (var groupId in groupIds)
            {
                this.Repo.Delete<Models.Triggers.TriggerGroup>(groupId);
            }

            // Delete criteria
            foreach (var criteria in trigger.Criteria)
            {
                this.Repo.Delete(criteria);
            }

            this.Repo.Delete(trigger);
            await this.Repo.SaveAsync();
        }

        public async Task UpsertTrigger(DataTransferObjects.Triggers.Trigger data, string userId)
        {
            if (data.Id.HasValue)
            {
                var existingTrigger = await this.Repo.GetFirstAsync<Trigger>(t => t.Id == data.Id, includeProperties: "Actions,Criteria");
                if (existingTrigger == null)
                {
                    throw new AppException($"No trigger found with id '{data.Id}'");
                }

                existingTrigger.Description = data.Description;
                existingTrigger.Name = data.Name;
                existingTrigger.TriggerObjectId = data.TriggerObjectId;
                existingTrigger.IsActive = data.IsActive;
                existingTrigger.Type = data.Type;

                this.Repo.Update(existingTrigger, userId);
                await this.Repo.SaveAsync();

                var actionsToRemove = existingTrigger.Actions.Where(a => !data.Actions.Any(m => m.Id == a.Id)).ToList();
                var enquiriesIds = new List<Guid>();
                await this.Repo.SaveAsync();
                await this.UpsertTriggerActions(existingTrigger.Id, data.Actions, userId);
                var criteriaToRemove = existingTrigger.Criteria.Where(a => !data.Criteria.Any(m => m.Id == a.Id)).ToList();
                foreach (var criteria in criteriaToRemove)
                {
                    this.Repo.Delete(criteria);
                }
                await this.Repo.SaveAsync();
                await this.UpsertTriggerCriteria(existingTrigger.Id, data.Groups, data.Criteria, userId);
            }
            else
            {
                var newTrigger = new Trigger()
                {
                    Description = data.Description,
                    Name = data.Name,
                    TriggerObjectId = data.TriggerObjectId,
                    IsActive = data.IsActive,
                    Type = data.Type
                };
                var triggerId = (Guid)await this.Repo.CreateAsync(newTrigger, userId);
                await this.UpsertTriggerActions(triggerId, data.Actions, userId);
                await this.UpsertTriggerCriteria(triggerId, data.Groups, data.Criteria, userId);
            }
        }

        public async Task SetTriggerStatus(Guid triggerId, bool isActive, string userId)
        {
            var trigger = await this.Repo.GetFirstAsync<Trigger>(t => t.Id == triggerId);
            if (trigger == null)
            {
                throw new AppException("Trigger not found");
            }
            if (trigger.IsActive != isActive)
            {
                trigger.IsActive = isActive;
                Repo.Update(trigger, userId);
                await Repo.SaveAsync();
            }
        }

        private async Task UpsertTriggerCriteria(Guid triggerId, List<DataTransferObjects.Triggers.TriggerGroup> groups, List<DataTransferObjects.Triggers.TriggerCriteria> triggerCriteria, string userId)
        {
            // Update the groups
            var newGroupIdMapping = new Dictionary<int, int>();
            var groupIdsUpserted = new List<int>();
            foreach (var group in groups.Where(g => g.Id != -1))
            {
                if (!groupIdsUpserted.Contains(group.Id.Value))
                {
                    var tempGroupId = group.Id.Value;
                    var groupId = await this.UpsertTriggerGroup(group, userId);
                    newGroupIdMapping.Add(tempGroupId, groupId);
                    groupIdsUpserted.Add(tempGroupId);
                }
            }

            // Update the criteria
            foreach (var criteria in triggerCriteria)
            {
                if (criteria.TriggerCriteriaGroupId != -1)
                {
                    criteria.TriggerCriteriaGroupId = newGroupIdMapping[criteria.TriggerCriteriaGroupId.Value];
                }

                if (criteria.Id.HasValue)
                {
                    var existingCriteria = await this.Repo.GetFirstAsync<Models.Triggers.TriggerCriteria>(a => a.Id == criteria.Id);
                    if (existingCriteria == null)
                    {
                        throw new AppException($"No trigger criteria found with id {criteria.Id}");
                    }

                    existingCriteria.TriggerCriteriaGroupId = criteria.TriggerCriteriaGroupId.HasValue && criteria.TriggerCriteriaGroupId.Value != -1 ? criteria.TriggerCriteriaGroupId.Value : default(int?);
                    existingCriteria.LogicType = criteria.LogicType;
                    existingCriteria.PropertyName = criteria.PropertyName;
                    existingCriteria.PropertyValue = criteria.PropertyValue;
                    existingCriteria.Type = criteria.Type;
                    existingCriteria.ComputedDescription = criteria.ComputedDescription;

                    this.Repo.Update(existingCriteria, userId);
                    await this.Repo.SaveAsync();
                }
                else
                {
                    await this.Repo.CreateAsync(new TriggerCriteria
                    {
                        TriggerId = triggerId,
                        PropertyName = criteria.PropertyName,
                        PropertyValue = criteria.PropertyValue,
                        Type = criteria.Type,
                        LogicType = criteria.LogicType,
                        ComputedDescription = criteria.ComputedDescription,
                        TriggerCriteriaGroupId = criteria.TriggerCriteriaGroupId.HasValue && criteria.TriggerCriteriaGroupId.Value != -1 ? criteria.TriggerCriteriaGroupId.Value : default(int?)
                    }, userId);
                }
            }
        }

        private async Task<int> UpsertTriggerGroup(DataTransferObjects.Triggers.TriggerGroup group, string userId)
        {
            // Groups with -1 are the holding group for criteria that are not in a group
            // If the Id is less than -1 then this is a new group and we need to create it. 
            if (group.Id <= -1)
            {
                var newGroup = new Models.Triggers.TriggerGroup()
                {
                    Name = group.Name,
                    LogicType = group.LogicType
                };
                group.Id = (int)await this.Repo.CreateAsync<Models.Triggers.TriggerGroup>(newGroup, userId);
            }
            else
            {
                var existingGroup = await this.Repo.GetFirstAsync<Models.Triggers.TriggerGroup>(g => g.Id == group.Id);
                if (existingGroup == null)
                {
                    throw new AppException($"No trigger criteria group found with id {group.Id}");
                }

                existingGroup.Name = group.Name;
                existingGroup.LogicType = group.LogicType;

                this.Repo.Update(existingGroup, userId);
                await this.Repo.SaveAsync();
            }

            return group.Id.Value;
        }

        private async Task UpsertTriggerActions(Guid triggerId, List<DataTransferObjects.Triggers.TriggerAction> actions, string userId)
        {
            foreach (var action in actions)
            {
                // Update the groups
                var newGroupIdMapping = new Dictionary<int, int>();
                var groupIdsUpserted = new List<int>();
                foreach (var group in action.Groups.Where(g => g.Id != -1))
                {
                    if (!groupIdsUpserted.Contains(group.Id.Value))
                    {
                        var tempGroupId = group.Id.Value;
                        var groupId = await this.UpsertTriggerGroup(group, userId);
                        newGroupIdMapping.Add(tempGroupId, groupId);
                        groupIdsUpserted.Add(tempGroupId);
                    }
                }
                if (action.Id.HasValue)
                {
                    var existingAction = await this.Repo.GetFirstAsync<TriggerAction>(a => a.Id == action.Id, includeProperties: "ScheduledCriteria");
                    if (existingAction == null)
                    {
                        throw new AppException($"No trigger action found with id {action.Id}");
                    }

                    existingAction.Action = action.Action;
                    existingAction.Type = action.Type;

                    if (action.Type == TriggerActionType.ChangeOrderDetailStatus)
                    {
                        ChangeOrderDetailStatus properties = JsonConvert.DeserializeObject<ChangeOrderDetailStatus>(JsonConvert.SerializeObject(action.CustomProperties));
                        existingAction.PropertiesJson = JsonConvert.SerializeObject(properties);
                    }
                    this.Repo.Update(existingAction, userId);
                    await this.Repo.SaveAsync();
                }
                else
                {
                    var newAction = new TriggerAction
                    {
                        TriggerId = triggerId,
                        Action = action.Action,
                        Type = action.Type,
                    };

                    if (action.Type == TriggerActionType.ChangeOrderDetailStatus)
                    {
                        ChangeOrderDetailStatus properties = JsonConvert.DeserializeObject<ChangeOrderDetailStatus>(JsonConvert.SerializeObject(action.CustomProperties)); // action.CustomProperties.ToObject<CreateEnquiryHistoryProperties>();
                        //newAction.PropertiesJson = JsonConvert.SerializeObject(properties);
                    }
                    await this.Repo.CreateAsync(newAction, userId);
                }
            }
        }

        #region Trigger workings

        public async Task CheckForTriggers(Guid tenantId,List<TriggerCheckData> changes)
        {
            string objectName = null; object id = null;
            try
            {
                // Setup the Repo
                var scope = this.serivceScopeFactory.CreateScope();
                this.adminContext = scope.ServiceProvider.GetRequiredService<RestxAdminContext>();

                var tenants = this.adminContext.Tenants.Where(t => t.Id == tenantId).ToList();
                if (tenants.Count == 0)
                {
                    return;
                }

                var activeTenant = await repoHelper.GetAllActiveTenantsObject(tenantId);

                var repo = new EntityFrameworkRepository<TenantDbContext>(new TenantDbContext(activeTenant, this.context), jobClient, RedisService, activeTenant);
                
                var allTriggers = (repo.Get<Trigger>(includeProperties: "Actions,Object,Criteria")).ToList();
                
                foreach (var item in changes)
                {
                    objectName = item.ObjectName;
                    var triggers = allTriggers.Where(t => t.Object.ObjectName == item.ObjectName && t.IsActive != false);

                    var enquiryDictionary = new Dictionary<string, string>();

                    // Check for insert object triggers
                    if (item.Type == TriggerCheckType.Added)
                    {
                        var insertTriggers = triggers.Where(t => t.Type == TriggerType.Insert).ToList();
                        if (insertTriggers.Any())
                        {
                            foreach (var trigger in insertTriggers)
                            {
                                var criterGroups = trigger.Criteria.GroupBy(t => t.TriggerCriteriaGroupId ?? -1).Select(g => new
                                {
                                    GroupId = g.Key,
                                    Criteria = g.ToList()
                                }).ToList();

                                var passedTriggerCriteria = criterGroups.Count() == 0;
                                foreach (var group in criterGroups)
                                {
                                    var criteriaPassed = 0;
                                    foreach (var criteria in group.Criteria)
                                    {
                                        if (criteria.Type == TriggerCriteriaType.AnyPropertyChange || criteria.Type == TriggerCriteriaType.IsUpdated)
                                        {
                                            criteriaPassed++;
                                        }

                                        if (criteria.Type == TriggerCriteriaType.Contains && !criteria.PropertyName.Contains("Enquiry."))
                                        {
                                            if (item.CurrentValues.Any(p => p.Key == criteria.PropertyName && item.CurrentValues[p.Key].Contains(criteria.PropertyValue)))
                                            {
                                                criteriaPassed++;
                                            }
                                        }
                                        else if (criteria.Type == TriggerCriteriaType.Contains && criteria.PropertyName.Contains("Enquiry."))
                                        {
                                            var propertyName = criteria.PropertyName.Split(".").Last();
                                            var value = enquiryDictionary[propertyName];

                                            if (value.Contains(criteria.PropertyValue))
                                            {
                                                criteriaPassed++;
                                            }
                                        }

                                        if ((criteria.Type == TriggerCriteriaType.SpecificPropertyValue ||
                                            criteria.Type == TriggerCriteriaType.SpecificPropertyNewValue ||
                                            criteria.Type == TriggerCriteriaType.SpecificPropertyValueNotEquals ||
                                            criteria.Type == TriggerCriteriaType.IsGreaterThan ||
                                            criteria.Type == TriggerCriteriaType.IsLessThan) && !criteria.PropertyName.Contains("Enquiry."))
                                        {
                                            if (item.CurrentValues.Any(p => p.Key == criteria.PropertyName && ParseValuesAndCheckIfLogicIsMet(item.CurrentValues[p.Key], criteria.PropertyValue, criteria.Type)))
                                            {
                                                criteriaPassed++;
                                            }
                                        }
                                        else if ((criteria.Type == TriggerCriteriaType.SpecificPropertyValue ||
                                            criteria.Type == TriggerCriteriaType.SpecificPropertyValueNotEquals ||
                                            criteria.Type == TriggerCriteriaType.IsGreaterThan ||
                                            criteria.Type == TriggerCriteriaType.IsLessThan) && criteria.PropertyName.Contains("Enquiry."))
                                        {
                                            // Convert enquiry to a dictionary
                                            var propertyName = criteria.PropertyName.Split(".").Last();
                                            if (enquiryDictionary.Any(p => p.Key == propertyName && ParseValuesAndCheckIfLogicIsMet(enquiryDictionary[p.Key], criteria.PropertyValue, criteria.Type)))
                                            {
                                                criteriaPassed++;
                                            }
                                        }

                                        // No need to check for type SpecificPropertyOldValue as this is an insert
                                    }

                                    if (group.Criteria.Any(t => t.LogicType == TriggerLogicType.And) && criteriaPassed == group.Criteria.Count)
                                    {
                                        passedTriggerCriteria = true;
                                    }
                                    else if (group.Criteria.Any(t => t.LogicType == TriggerLogicType.Or) && criteriaPassed > 0)
                                    {
                                        passedTriggerCriteria = true;
                                    }
                                }

                                if (passedTriggerCriteria)
                                {
                                    await this.ProcessAction(item, repo, trigger.Actions, activeTenant);
                                }
                            }
                        }
                    }

                    if (item.Type == TriggerCheckType.Updated)
                    {
                        var updateTriggers = triggers.Where(t => t.Type == TriggerType.Update).ToList();
                        if (updateTriggers.Any())
                        {
                            foreach (var trigger in updateTriggers)
                            {
                                var criteriaGroups = trigger.Criteria.GroupBy(t => t.TriggerCriteriaGroupId ?? -1).Select(g => new
                                {
                                    GroupId = g.Key,
                                    Criteria = g.ToList()
                                });

                                var passedTriggerCriteria = criteriaGroups.Count() == 0;
                                foreach (var group in criteriaGroups)
                                {
                                    var criteriaPassed = 0;

                                    foreach (var criteria in group.Criteria)
                                    {
                                        if (criteria.Type == TriggerCriteriaType.AnyPropertyChange)
                                        {
                                            criteriaPassed++;
                                        }

                                        if (criteria.Type == TriggerCriteriaType.SpecificPropertyNewValue)
                                        {
                                            var newValue = item.CurrentValues[criteria.PropertyName]?.ToString();
                                            var oldValue = item.OriginalValues[criteria.PropertyName]?.ToString();

                                            // Avoid the guid value have upper case it can raise bug FH-15
                                            var propertyValue = criteria.PropertyValue;
                                            if (!string.IsNullOrEmpty(propertyValue) && Guid.TryParse(propertyValue, out Guid guidValue))
                                            {
                                                propertyValue = guidValue.ToString();
                                            }

                                            if (newValue != oldValue && newValue == propertyValue)
                                            {
                                                criteriaPassed++;
                                            }
                                        }

                                        if (criteria.Type == TriggerCriteriaType.SpecificPropertyOldValue)
                                        {
                                            var newValue = item.CurrentValues[criteria.PropertyName]?.ToString();
                                            var oldValue = item.OriginalValues[criteria.PropertyName]?.ToString();
                                            var propertyValue = criteria.PropertyValue;
                                            if (!string.IsNullOrEmpty(propertyValue) && Guid.TryParse(propertyValue, out Guid guidValue))
                                            {
                                                propertyValue = guidValue.ToString();
                                            }
                                            bool isEqual = (string.IsNullOrEmpty(oldValue) && string.IsNullOrEmpty(newValue)) || oldValue == propertyValue;
                                            if (newValue != oldValue && isEqual)
                                            {
                                                criteriaPassed++;
                                            }
                                        }

                                        if (criteria.Type == TriggerCriteriaType.IsUpdated)
                                        {
                                            var newValue = item.CurrentValues[criteria.PropertyName]?.ToString();
                                            var oldValue = item.OriginalValues[criteria.PropertyName]?.ToString();
                                            if (newValue != oldValue)
                                            {
                                                criteriaPassed++;
                                            }
                                        }

                                        if (criteria.Type == TriggerCriteriaType.Contains && !criteria.PropertyName.Contains("Enquiry."))
                                        {
                                            if (item.CurrentValues.Any(p => p.Key == criteria.PropertyName && item.CurrentValues[p.Key].Contains(criteria.PropertyValue)))
                                            {
                                                criteriaPassed++;
                                            }
                                        }
                                        else if (criteria.Type == TriggerCriteriaType.Contains && criteria.PropertyName.Contains("Enquiry."))
                                        {
                                            var propertyName = criteria.PropertyName.Split(".").Last();
                                            var value = enquiryDictionary[propertyName];

                                            if (value.Contains(criteria.PropertyValue))
                                            {
                                                criteriaPassed++;
                                            }
                                        }

                                        if ((criteria.Type == TriggerCriteriaType.SpecificPropertyValue ||
                                            criteria.Type == TriggerCriteriaType.SpecificPropertyValueNotEquals ||
                                            criteria.Type == TriggerCriteriaType.IsGreaterThan ||
                                            criteria.Type == TriggerCriteriaType.IsLessThan ||
                                            criteria.Type == TriggerCriteriaType.IsUpdated) && !criteria.PropertyName.Contains("Enquiry."))
                                        {
                                            if (item.CurrentValues.Any(p => p.Key == criteria.PropertyName && ParseValuesAndCheckIfLogicIsMet(item.CurrentValues[p.Key], criteria.PropertyValue, criteria.Type)))
                                            {
                                                criteriaPassed++;
                                            }
                                        }
                                        else if ((criteria.Type == TriggerCriteriaType.SpecificPropertyValue ||
                                            criteria.Type == TriggerCriteriaType.SpecificPropertyValueNotEquals ||
                                            criteria.Type == TriggerCriteriaType.IsGreaterThan ||
                                            criteria.Type == TriggerCriteriaType.IsLessThan) && criteria.PropertyName.Contains("Enquiry."))
                                        {
                                            // Convert enquiry to a dictionary
                                            var propertyName = criteria.PropertyName.Split(".").Last();
                                            if (enquiryDictionary.Any(p => p.Key == propertyName && ParseValuesAndCheckIfLogicIsMet(enquiryDictionary[p.Key], criteria.PropertyValue, criteria.Type)))
                                            {
                                                criteriaPassed++;
                                            }
                                        }
                                    }

                                    if (group.Criteria.Any(t => t.LogicType == TriggerLogicType.And) && criteriaPassed == group.Criteria.Count)
                                    {
                                        passedTriggerCriteria = true;
                                    }
                                    else if (group.Criteria.Any(t => t.LogicType == TriggerLogicType.Or) && criteriaPassed > 0)
                                    {
                                        passedTriggerCriteria = true;
                                    }
                                }

                                if (passedTriggerCriteria)
                                {

                                    await this.ProcessAction(item, repo, trigger.Actions, activeTenant);
                                }
                            }
                        }
                    }

                    if (item.Type == TriggerCheckType.Deleted)
                    {
                        // Deleted always get triggered.
                        var insertTriggers = triggers.Where(t => t.Type == TriggerType.Delete).ToList();
                        if (insertTriggers.Any())
                        {
                            foreach (var trigger in insertTriggers)
                            {
                                await this.ProcessAction(item, repo, trigger.Actions, activeTenant);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing triggers, TenantID '{tenantId}', Object Name '{objectName}', ObjectId '{id?.ToString()}' ", ex);
            }
        }

        private async Task ProcessAction(TriggerCheckData item, IRepository repo, ICollection<TriggerAction> actions, ActiveTenant tenant)
        {
            foreach (var triggerAction in actions)
            {
                // Check if this is scheduled first, the scheduled action handles all the task types internally 
                if (triggerAction.Type == TriggerActionType.ChangeOrderDetailStatus)
                {
                    //await emailTask.ProcessTask(item, triggerAction, true);
                }
                //...
            }
        }

        public async Task TriggerActionScheduledTaskTriggered(Guid tenantId, Guid tenantBrandId, int scheduledActionId, PerformContext jobContext) { }


        #endregion

        #region Private Functions

        private List<DataTransferObjects.Triggers.TriggerObjectProperties> GetTriggerObjectProperties(Type type, string parentName = "", HashSet<string> visitedProperties = null)
        {
            var properties = type.GetProperties().Where(x => x.GetCustomAttributes(typeof(TriggerProperty), true).Any());
            var result = new List<DataTransferObjects.Triggers.TriggerObjectProperties>();
            if (visitedProperties == null)
            {
                visitedProperties = new HashSet<string>();
            }

            var updateableProperties = new List<string>() { "System.String", "System.Int", "System.Decimal", "System.DateTime", "System.Boolean" };

            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;

                if (Nullable.GetUnderlyingType(propertyType) != null)
                {
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                }

                if (property.PropertyType.BaseType != null && property.PropertyType.BaseType == typeof(Enum))
                {
                    propertyType = property.PropertyType.BaseType;
                }

                //var propertyDisplayName = AddSpacesBeforeCapitalLetters(property.Name);

                if (propertyType.IsPrimitive || propertyType == typeof(string) || propertyType.Namespace.StartsWith("System") || !property.GetGetMethod(true).IsVirtual)
                {
                    var triggerObjectProperty = new DataTransferObjects.Triggers.TriggerObjectProperties()
                    {
                        Name = property.Name,
                        DisplayName = property.GetCustomAttribute<TriggerProperty>()?.DisplayName ?? property.Name,
                        ValueType = propertyType.FullName,
                        LookupUrl = property.GetCustomAttribute<TriggerProperty>()?.LookupUrl ?? string.Empty,
                        Value = string.IsNullOrEmpty(parentName) ? $"{property.Name}" : $"{parentName}.{property.Name}",
                        CanBeUpdated = (updateableProperties.Contains(propertyType.FullName) && property.Name != "CreatedDate" && property.Name != "ModifiedDate") || !string.IsNullOrEmpty(property.GetCustomAttribute<TriggerProperty>()?.LookupUrl)
                    };

                    result.Add(triggerObjectProperty);
                }
                else if (propertyType.FullName.StartsWith("RestX") && !visitedProperties.Contains(propertyType.FullName))
                {
                    visitedProperties.Add(propertyType.FullName);
                    var childProperties = GetTriggerObjectProperties(propertyType, string.IsNullOrEmpty(parentName) ? $"{property.Name}" : $"{parentName}.{property.Name}", visitedProperties);

                    var triggerObjectProperty = new DataTransferObjects.Triggers.TriggerObjectProperties()
                    {
                        Name = property.Name,
                        DisplayName = property.GetCustomAttribute<TriggerProperty>()?.DisplayName ?? property.Name,
                        ValueType = propertyType.FullName,
                        LookupUrl = property.GetCustomAttribute<TriggerProperty>()?.LookupUrl ?? string.Empty,
                        Value = string.IsNullOrEmpty(parentName) ? property.Name : $"{parentName}.{property.Name}",
                        ChildProperties = childProperties,
                        CanBeUpdated = (updateableProperties.Contains(propertyType.FullName) && property.Name != "CreatedDate" && property.Name != "ModifiedDate") || !string.IsNullOrEmpty(property.GetCustomAttribute<TriggerProperty>()?.LookupUrl)
                    };

                    result.Add(triggerObjectProperty);
                }
            }

            return result.OrderBy(r => r.DisplayName).ToList();
        }

        private string AddSpacesBeforeCapitalLetters(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var sb = new StringBuilder();
            sb.Append(input[0]);

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    sb.Append(' ');
                }

                sb.Append(input[i]);
            }

            return sb.ToString();
        }

        public bool ParseValuesAndCheckIfLogicIsMet(string value, string valueToCheckAgainst, TriggerCriteriaType criteriaType)
        {
            if (DateTime.TryParse(valueToCheckAgainst, out var dateTimePropertyValue) && DateTime.TryParse(value, out var dateTimeValue))
            {
                if (criteriaType == TriggerCriteriaType.IsGreaterThan && dateTimeValue > dateTimePropertyValue)
                {
                    return true;
                }

                if (criteriaType == TriggerCriteriaType.IsLessThan && dateTimeValue < dateTimePropertyValue)
                {
                    return true;
                }

                if (criteriaType == TriggerCriteriaType.SpecificPropertyValue || criteriaType == TriggerCriteriaType.SpecificPropertyOldValue || criteriaType == TriggerCriteriaType.SpecificPropertyNewValue)
                {
                    if (dateTimeValue == dateTimePropertyValue)
                    {
                        return true;
                    }
                }

                if (criteriaType == TriggerCriteriaType.SpecificPropertyValueNotEquals && dateTimeValue != dateTimePropertyValue)
                {
                    return true;
                }
            }
            else if (decimal.TryParse(valueToCheckAgainst, out var decimalPropertyValue) && decimal.TryParse(value, out var decimalValue))
            {
                if (criteriaType == TriggerCriteriaType.IsGreaterThan & decimalValue > decimalPropertyValue)
                {
                    return true;
                }

                if (criteriaType == TriggerCriteriaType.IsLessThan & decimalValue < decimalPropertyValue)
                {
                    return true;
                }

                if (criteriaType == TriggerCriteriaType.SpecificPropertyValue || criteriaType == TriggerCriteriaType.SpecificPropertyOldValue || criteriaType == TriggerCriteriaType.SpecificPropertyNewValue)
                {
                    if (decimalPropertyValue == decimalValue)
                    {
                        return true;
                    }
                }

                if (criteriaType == TriggerCriteriaType.SpecificPropertyValueNotEquals && decimalPropertyValue != decimalValue)
                {
                    return true;
                }
            }
            else if (bool.TryParse(valueToCheckAgainst, out var boolPropertyValue) && bool.TryParse(value, out var boolValue))
            {
                if (criteriaType == TriggerCriteriaType.SpecificPropertyValue || criteriaType == TriggerCriteriaType.SpecificPropertyOldValue || criteriaType == TriggerCriteriaType.SpecificPropertyNewValue)
                {
                    if (boolPropertyValue == boolValue)
                    {
                        return true;
                    }
                }

                if (criteriaType == TriggerCriteriaType.SpecificPropertyValueNotEquals && boolPropertyValue != boolValue)
                {
                    return true;
                }
            }
            else if (int.TryParse(valueToCheckAgainst, out var intPropertyValue) && int.TryParse(value, out var intValue))
            {
                if (criteriaType == TriggerCriteriaType.IsGreaterThan & intValue > intPropertyValue)
                {
                    return true;
                }

                if (criteriaType == TriggerCriteriaType.IsLessThan & intValue < intPropertyValue)
                {
                    return true;
                }

                if (criteriaType == TriggerCriteriaType.SpecificPropertyValue || criteriaType == TriggerCriteriaType.SpecificPropertyOldValue || criteriaType == TriggerCriteriaType.SpecificPropertyNewValue)
                {
                    if (intPropertyValue == intValue)
                    {
                        return true;
                    }
                }

                if (criteriaType == TriggerCriteriaType.SpecificPropertyValueNotEquals && intPropertyValue != intValue)
                {
                    return true;
                }
            }
            else
            {
                // This will just compare the string values
                if (criteriaType == TriggerCriteriaType.SpecificPropertyValue || criteriaType == TriggerCriteriaType.SpecificPropertyOldValue || criteriaType == TriggerCriteriaType.SpecificPropertyNewValue)
                {
                    if (string.Equals(valueToCheckAgainst, value, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                if (criteriaType == TriggerCriteriaType.SpecificPropertyValueNotEquals && valueToCheckAgainst != value)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
