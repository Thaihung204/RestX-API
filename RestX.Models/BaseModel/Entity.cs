using Newtonsoft.Json;
using RestX.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;

namespace RestX.Models.BaseModel
{
    public abstract class Entity<T> : IEntity<T>
    {
        /// <summary>
        /// The Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public T Id { get; set; } = typeof(T) == typeof(Guid)
            ? (T)(object)Guid.NewGuid()
            : default;
        object IEntity.Id
        {
            get => Id;
            set => Id = (T)value;
        }

        [DataType(DataType.DateTime)]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow.AddHours(7);

        [DataType(DataType.DateTime)]
        [Column(TypeName = "datetime2")]
        public DateTime? ModifiedDate { get; set; } = null;

        [MaxLength(100)]
        public string? CreatedBy { get; set; } = null;

        [MaxLength(100)]
        public string? ModifiedBy { get; set; } = null;

        [Display(Description = "For internal use")]
        public string PropertiesJson { get; set; }

        [NotMapped]
        public ExpandoObject CustomProperties
        {
            get => string.IsNullOrEmpty(this.PropertiesJson) ? new ExpandoObject() : JsonConvert.DeserializeObject<ExpandoObject>(this.PropertiesJson);
            set => this.PropertiesJson = JsonConvert.SerializeObject(value);
        }

        public void SetCustomProperty<TType>(TType customPropertyId, object value)
        {
            IDictionary<string, object> memberCustomProperties = this.CustomProperties;
            memberCustomProperties[customPropertyId.ToString()] = value;
            this.CustomProperties = (ExpandoObject)memberCustomProperties;
        }

        public T GetCustomProperty<T>(object customPropertyId)
        {
            IDictionary<string, object> properties = this.CustomProperties;
            if (properties != null && properties.ContainsKey(customPropertyId.ToString()))
            {
                var value = properties[customPropertyId.ToString()].ToString();
                return JsonConvert.DeserializeObject<T>(value);
            }

            return default(T);
        }
    }
}
