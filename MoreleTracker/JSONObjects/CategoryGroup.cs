using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreleOutletTracker.MoreleTracker.ObjectModels
{
    public class CategoryGroup
    {
        public bool useInSearch { get; set; }
        public List<Category> subCategories { get; set; }
    }
}
