using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreleOutletTracker.MoreleTracker.ObjectModels
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Condition { get; set; }
        public string Description { get; set; }
        public float oldPrice { get; set; }
        public float newPrice { get; set; }
        public string thumbnail { get; set; }
        public string link { get; set; }

        public void PrintToConsole()
        {
            Console.WriteLine($"ID: {Id}");
            Console.WriteLine($"Name: {Name}");
            Console.WriteLine($"Condition: {Condition}");
            Console.WriteLine($"Description: {Description}");
            Console.WriteLine($"Old Price: {oldPrice}");
            Console.WriteLine($"New Price: {newPrice}");
            Console.WriteLine($"link: {link} \n");
        }
    }
}
