using Microsoft.EntityFrameworkCore;
using RestX.BLL.Interfaces;
using RestX.Models.Enum;
using RestX.Models.Interfaces;
using System.Data;
using System.Dynamic;

namespace RestX.BLL.Services
{
    public class EntityFrameworkRepository<TContext> : EntityFrameworkReadOnlyRepository<TContext>, IRepository
    where TContext : DbContext
    {
        protected readonly TContext context;

        public EntityFrameworkRepository(TContext context) : base(context)
        {
            this.context = context;
        }

        public virtual object Create<TEntity>(TEntity entity, string createdBy = null)
            where TEntity : class, IEntity
        {
            entity.CreatedDate = DateTime.UtcNow;
            entity.CreatedBy = createdBy;
            //            entity.CustomProperties = this.ValidateCustomProperties(entity);
            var newEntity = context.Set<TEntity>().Add(entity);
            Save();
            //return newEntity.Entity.Id;
            return entity.Id;
        }

        public virtual async Task<object> CreateAsync<TEntity>(TEntity entity, string createdBy = null)
            where TEntity : class, IEntity
        {
            entity.CreatedDate = DateTime.UtcNow;
            entity.CreatedBy = createdBy ?? string.Empty;
            //            entity.CustomProperties = this.ValidateCustomProperties(entity);
            var newEntity = context.Set<TEntity>().Add(entity);
            await SaveAsync();
            //return newEntity.Entity.Id;
            return entity.Id;
        }

        public virtual void Update<TEntity>(TEntity entity, string modifiedBy = null)
            where TEntity : class, IEntity
        {
            entity.ModifiedDate = DateTime.UtcNow;
            entity.ModifiedBy = modifiedBy;
            //            entity.CustomProperties = this.ValidateCustomProperties(entity);
            context.Set<TEntity>().Attach(entity);
            context.Entry(entity).State = EntityState.Modified;
        }

        public virtual void Delete<TEntity>(object id)
            where TEntity : class, IEntity
        {
            TEntity entity = context.Set<TEntity>().Find(id);
            Delete(entity);
        }

        public virtual void Delete<TEntity>(TEntity entity)
            where TEntity : class, IEntity
        {
            var dbSet = context.Set<TEntity>();
            if (context.Entry(entity).State == EntityState.Detached)
            {
                dbSet.Attach(entity);
            }

            dbSet.Remove(entity);
        }

        public virtual void Save()
        {
            var changes = context.ChangeTracker.Entries().Where(e => (e.State == EntityState.Modified || e.State == EntityState.Deleted) && MonitoredTriggerObjects.Contains(e.Entity.GetType().Name))
                .Select(c => new TriggerCheckData
                {
                    ObjectId = c.CurrentValues["Id"],
                    ObjectName = c.Entity.GetType().Name,
                    Type = c.State == EntityState.Added ? TriggerCheckType.Added : c.State == EntityState.Modified ? TriggerCheckType.Updated : TriggerCheckType.Deleted,
                    //OriginalValues = c.OriginalValues.Properties.ToDictionary(
                    //                propertyName => propertyName.Name,
                    //                propertyName => c.OriginalValues[propertyName] != null ? GetStringValueOfProperty(c.OriginalValues[propertyName]) : c.OriginalValues[propertyName]?.ToString()),
                    //CurrentValues = c.CurrentValues.Properties.ToDictionary(
                    //                propertyName => propertyName.Name,
                    //                propertyName => c.CurrentValues[propertyName] != null ? GetStringValueOfProperty(c.CurrentValues[propertyName]) : c.CurrentValues[propertyName]?.ToString())
                }).ToList();

            // We have to get added items here so populate the Id's
            var newItems = context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added && MonitoredTriggerObjects.Contains(e.Entity.GetType().Name)).ToList();

            context.SaveChanges();

            // Add newItems to changes as they will now have correct Id's
            changes.AddRange(newItems.Select(c => new TriggerCheckData
            {
                ObjectId = c.CurrentValues["Id"],
                ObjectName = c.Entity.GetType().Name,
                Type = TriggerCheckType.Added,
                //OriginalValues = c.OriginalValues.Properties.ToDictionary(
                //                    propertyName => propertyName.Name,
                //                    propertyName => c.OriginalValues[propertyName] != null ? GetStringValueOfProperty(c.OriginalValues[propertyName]) : c.OriginalValues[propertyName]?.ToString()),
                //CurrentValues = c.CurrentValues.Properties.ToDictionary(
                //                    propertyName => propertyName.Name,
                //                    propertyName => c.CurrentValues[propertyName] != null ? GetStringValueOfProperty(c.CurrentValues[propertyName]) : c.CurrentValues[propertyName]?.ToString())
            }));

            CheckForTriggers(changes);
        }

        public virtual async Task SaveAsync()
        {
            var changes = context.ChangeTracker.Entries().Where(e => (e.State == EntityState.Modified || e.State == EntityState.Deleted) && MonitoredTriggerObjects.Contains(e.Entity.GetType().Name))
                .Select(c => new TriggerCheckData
                {
                    ObjectId = c.CurrentValues["Id"],
                    ObjectName = c.Entity.GetType().Name,
                    Type = c.State == EntityState.Added ? TriggerCheckType.Added : c.State == EntityState.Modified ? TriggerCheckType.Updated : TriggerCheckType.Deleted,
                    //OriginalValues = c.OriginalValues.Properties.ToDictionary(
                    //                propertyName => propertyName.Name,
                    //                propertyName => c.OriginalValues[propertyName] != null ? GetStringValueOfProperty(c.OriginalValues[propertyName]) : c.OriginalValues[propertyName]?.ToString()),
                    //CurrentValues = c.CurrentValues.Properties.ToDictionary(
                    //                propertyName => propertyName.Name,
                    //                propertyName => c.CurrentValues[propertyName] != null ? GetStringValueOfProperty(c.CurrentValues[propertyName]) : c.CurrentValues[propertyName]?.ToString())
                }).ToList();

            var added = context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList();

            // We have to get added items here so populate the Id's
            var newItems = context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added && MonitoredTriggerObjects.Contains(e.Entity.GetType().Name)).ToList();

            var result = await context.SaveChangesAsync();

            // Add newItems to changes as they will now have correct Id's
            changes.AddRange(newItems.Select(c => new TriggerCheckData
            {
                ObjectId = c.CurrentValues["Id"],
                ObjectName = c.Entity.GetType().Name,
                Type = TriggerCheckType.Added,
                //OriginalValues = c.OriginalValues.Properties.ToDictionary(
                //                    propertyName => propertyName.Name,
                //                    propertyName => c.OriginalValues[propertyName] != null ? GetStringValueOfProperty(c.OriginalValues[propertyName]) : c.OriginalValues[propertyName]?.ToString()),
                //CurrentValues = c.CurrentValues.Properties.ToDictionary(
                //                    propertyName => propertyName.Name,
                //                    propertyName => c.CurrentValues[propertyName] != null ? GetStringValueOfProperty(c.CurrentValues[propertyName]) : c.CurrentValues[propertyName]?.ToString())
            }));

            CheckForTriggers(changes);
        }

        public virtual async Task<T> ExecuteStoredProcedureAsync<T>(string storedProcedure, object[] parameters = null)
        {
            var conn = context.Database.GetDbConnection();
            using (var command = conn.CreateCommand())
            {
                command.CommandText = storedProcedure;
                command.CommandType = CommandType.StoredProcedure;
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                if (conn.State == ConnectionState.Closed)
                {
                    await conn.OpenAsync();
                }

                var result = await command.ExecuteScalarAsync();
                if (result != null)
                {
                    return (T)result;
                }
            }
            return default;
        }

        public virtual async Task<T> ExecuteSqlCommandAsync<T>(string query, object[] parameters = null)
        {
            var conn = context.Database.GetDbConnection();
            using (var command = conn.CreateCommand())
            {
                command.CommandText = query;
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                if (conn.State == ConnectionState.Closed)
                {
                    await conn.OpenAsync();
                }

                var result = await command.ExecuteScalarAsync();
                if (result != null)
                {
                    return (T)result;
                }
            }
            return default;
        }

        public virtual async Task<List<T>> ExecuteSqlSelectAsync<T>(string query, object[] parameters = null, int commandTimeout = 600) where T : new()
        {
            var conn = context.Database.GetDbConnection();
            using (var command = conn.CreateCommand())
            {
                // Set the timeout to 6 minutes
                command.CommandTimeout = commandTimeout;

                if (query != null) command.CommandText = query;
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                if (conn.State == ConnectionState.Closed)
                {
                    await conn.OpenAsync();
                }

                using (var result = command.ExecuteReader())
                {
                    var results = new List<T>();
                    if (result != null)
                    {
                        var dataTable = result.GetSchemaTable();
                        while (result.Read())
                        {
                            var index = 0;
                            var t = new T();

                            if (dataTable != null)
                            {
                                foreach (DataRow row in dataTable.Rows)
                                {
                                    var value = result.GetValue(index);
                                    if (value == DBNull.Value)
                                    {
                                        t.GetType().GetProperty(row["BaseColumnName"].ToString())
                                            .SetValue(t, null, null);
                                    }
                                    else
                                    {
                                        t.GetType().GetProperty(row["BaseColumnName"].ToString())
                                            .SetValue(t, result.GetValue(index), null);
                                    }

                                    index++;
                                }
                            }

                            results.Add(t);
                        }

                        return results;
                    }
                }
            }

            return new List<T>();
        }

        public virtual async Task<int> ExecuteNonQueryAsync(string query, object[] parameters = null, int commandTimeout = 600)
        {
            var conn = context.Database.GetDbConnection();
            using (var command = conn.CreateCommand())
            {
                command.CommandTimeout = commandTimeout;
                command.CommandText = query;
                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters);
                }

                if (conn.State == ConnectionState.Closed)
                {
                    await conn.OpenAsync();
                }

                return await command.ExecuteNonQueryAsync();

            }
        }

        /// <summary>
        /// This returns a list of objects that are monitored for triggers. Add the class name to trigger the monitor.
        /// You also need to add to the TriggerObjects table for the individual customer databases.
        /// </summary>
        private List<string> MonitoredTriggerObjects
        {
            get
            {
                if (context.GetType().Name != "AdminDbContext")
                {
                    return new List<string>()
                    {
                        "Tenants",
                        "Enquiry",
                        "Quote",
                        "EnquiryHistory",
                        "EnquiryHistoryUnassigned",
                        "EnquiryTask",
                        "BookingPassenger",
                        "QuoteFeedback",
                        "Booking",
                        "BookingDocuments",
                        "PaymentTransaction",
                        "Notification"
                    };
                }

                return new List<string>();
            }

        }
        private void CheckForTriggers(List<TriggerCheckData> changes)
        {
            // Check if any objects changed are being monitored for triggers
            if (changes.Count == 0)
            {
                return;
            }

            if (context.GetType().Name != "AdminDbContext")
            {
                //jobClient.Enqueue<ITriggerService>(x => x.CheckForTriggers(this.tenant.Id, this.tenant.ActiveBrand.Id, changes));
            }
        }

        /// <summary>
        /// This method validates the custom properties set against an entity to ensure they are still current
        /// and that they haven't been deleted etc. since the last time this entity was saved.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <returns>
        /// The <see cref="ExpandoObject"/>.
        /// </returns>
        //private ExpandoObject ValidateCustomProperties(IEntity entity)
        //{
        //    if (this.context.GetType().Name != "AdminDbContext" && entity.GetType().Name != "TriggerAction")
        //    {
        //        var currentProperties = GetCustomProperties(entity);

        //        IDictionary<string, object> entityProperties = entity.CustomProperties;

        //        var propertiesToRemove = entityProperties
        //            .Where(p => !currentProperties.Any(c => c.Id.ToString() == p.Key || c.InternalName == p.Key) && p.Key != "-1")
        //            .Select(p => p.Key)
        //            .ToList();

        //        foreach (var propertyId in propertiesToRemove)
        //        {
        //            entityProperties.Remove(propertyId);
        //        }
        //        return (ExpandoObject)entityProperties;
        //    }
        //    else
        //    {
        //        IDictionary<string, object> entityProperties = entity.CustomProperties;
        //        return (ExpandoObject)entityProperties;
        //    }
        //}


        //private string GetStringValueOfProperty(object propertyData)
        //{
        //    var type = propertyData.GetType();
        //    if (type == typeof(List<string>))
        //    {
        //        return string.Join(",", ((List<string>)propertyData));
        //    }

        //    if (type == typeof(List<Models.Content.ContentItem>))
        //    {
        //        return string.Join(",", ((List<Models.Content.ContentItem>)propertyData).Select(c => c.Value));
        //    }

        //    return propertyData.ToString();
        //}

        //private IEnumerable<CustomProperty> GetCustomProperties(IEntity entity)
        //{
        //    string entityName = GetCustomEntityName(entity);
        //    return this.context.Set<CustomProperty>()
        //            .AsNoTracking()
        //            .Where(c => c.Entity == entityName)
        //            .Select(c => new CustomProperty
        //            {
        //                Id = c.Id,
        //                InternalName = c.InternalName
        //            })
        //            .ToList();
        //}

        //private string GetCustomEntityName(IEntity entity)
        //{
        //    var type = entity.GetType();
        //    var attribute = type.GetCustomAttribute<CustomObjectAttribute>();
        //    return attribute?.ObjectType ?? type.Name;
        //}

        private class TriggerCheckData
        {
            public object ObjectId { get; set; }
            public string ObjectName { get; set; }
            public object Type { get; set; }
            public Dictionary<string, string> OriginalValues { get; set; }
            public Dictionary<string, string> CurrentValues { get; set; }
        }
    }
}
