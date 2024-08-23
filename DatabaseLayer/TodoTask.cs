using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseLayer
{
    public record TodoTask(string? Description, DateTime? Date, int? Id = 0)
    {
        public override string ToString()
        {
            return $"Id: {Id}, Description: {Description.PadRight(55, ' ')} On: {Date.Value.ToShortDateString()}";
        }
    }
}

