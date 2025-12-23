using RestX.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestX.Models.BaseModel
{
    public abstract class Entity<T> : IEntity<T>
    {
        /// <summary>
        /// The Id
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public T Id { get; set; }
        object IEntity.Id
        {
            get => Id;
            set => Id = (T)value;
        }

        [TriggerProperty(DisplayName = "Created Date")]
        [DataType(DataType.DateTime)]
        [Column(TypeName = "datetime2")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [TriggerProperty(DisplayName = "Modified Date")]
        [DataType(DataType.DateTime)]
        [Column(TypeName = "datetime2")]
        public DateTime? ModifiedDate { get; set; } = null;

        [MaxLength(100)]
        public string? CreatedBy { get; set; } = null;

        [MaxLength(100)]
        public string? ModifiedBy { get; set; } = null;
    }
}
