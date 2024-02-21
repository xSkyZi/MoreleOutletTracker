using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreleOutletTracker.MoreleTracker.ObjectModels
{
    public class Category
    {
        public string name { get; set; }
        public ushort id { get; set; }
        public bool useInSearch { get; set; }
    }
}
