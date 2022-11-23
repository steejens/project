using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Application.SampleQueries
{
    public class SampleResponse
    {
        public int Id { get; set; }
        public string? SampleText { get; set; }
        public int IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
