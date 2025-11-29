using System;
using System.Dynamic;

namespace RestX.Models.Interfaces
{
    public interface IEntity : IModifiableEntity
    {
        object Id { get; set; }

        DateTime CreatedDate { get; set; }

        DateTime? ModifiedDate { get; set; }

        string CreatedBy { get; set; }

        string ModifiedBy { get; set; }

        //byte[] Version { get; set; }

        //string PropertiesJson { get; set; }

        //ExpandoObject CustomProperties { get; set; }
    }
}
