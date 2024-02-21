using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoreleOutletTracker.MoreleTracker.JSONObjects
{
    public static class Config
    {
        public static string token { get; set; }
        public static ulong channelId { get; set; }
        public static ulong mentionRoleId { get; set; }
        public static long fetchCooldown { get; set; }
    }
}
