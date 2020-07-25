using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModItemCreationTool.Models
{
    abstract class EntityBase
    {
        public Guid Id { get; set; }

        protected EntityBase()
        {
            Id = Guid.NewGuid();
        }
    }
}
