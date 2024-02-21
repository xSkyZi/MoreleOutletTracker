using MoreleOutletTracker.MoreleTracker.JSONObjects;
using MoreleOutletTracker.MoreleTracker.ObjectModels;
using Newtonsoft.Json;
using Serilog;
using System.Reflection;

namespace MoreleOutletTracker.MoreleTracker
{
    internal class JsonFM
    {
        public static string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string categoriesConfigName = $"{exePath}\\Config\\CategoriesConfig.json";
        private static string productListName = $"{exePath}\\Data\\ProductList.json";

        public static void SaveProductListToFile(List<Product> productList)
        {
            Log.Information("Saving fetched products...");
            try
            {
                using (StreamWriter sw = new StreamWriter(productListName))
                {
                    sw.Write(JsonConvert.SerializeObject(productList, Formatting.Indented));
                }
            } catch (Exception ex)
            {
                Log.Error($"Error while saving products! Turning off fetching system. {ex}");
                MoreleTracker.cancellationTokenSource.Cancel();
            }
        }

        public static List<Product> RetrieveProductsFromFile()
        {
            using (StreamReader sr = new StreamReader(productListName))
            {
                return JsonConvert.DeserializeObject<List<Product>>(sr.ReadToEnd());
            }
        }

        public static bool ProductFileExists()
        {
            if (File.Exists(productListName)) return true;
            return false;
        }

        public static void CreateDirectory()
        {
            if (!Directory.Exists($"{exePath}\\Data")) Directory.CreateDirectory($"{exePath}\\Data");
        }

        public static bool CategoriesConfigExists()
        {
            if (File.Exists(categoriesConfigName)) return true;
            return false;
        }

        public static void SaveGeneratedCategoryConfigToFile(Dictionary<string, CategoryGroup> categoriesList)
        {
            using (StreamWriter sw = new StreamWriter(categoriesConfigName))
            {
                sw.Write(JsonConvert.SerializeObject(categoriesList, Formatting.Indented));
            }
        }

        public static string[] RetrieveCategoriesFromFile()
        {
            List<string> categoriesToLookFor = new List<string>();
            Dictionary<string, CategoryGroup> deserializedConfig = new Dictionary<string, CategoryGroup>();
            using (StreamReader sr = new StreamReader(categoriesConfigName))
            {
                deserializedConfig = JsonConvert.DeserializeObject<Dictionary<string, CategoryGroup>>(sr.ReadToEnd());
            }

            foreach (CategoryGroup mainGroup in deserializedConfig.Values)
            {
                bool includeEverySubCategory = mainGroup.useInSearch;
                foreach (Category category in mainGroup.subCategories)
                {
                    if (includeEverySubCategory)
                    {
                        categoriesToLookFor.Add(category.id.ToString());
                        continue;
                    }

                    if (category.useInSearch) categoriesToLookFor.Add(category.id.ToString());
                }
            }
            return categoriesToLookFor.ToArray();
        }

        public static async Task GetConfig()
        {
            Log.Information($"Trying to read \"{exePath}\\Config\\BotConfig.json\" ....");

            try
            {
                using (StreamReader sr = new StreamReader($"{exePath}\\Config\\BotConfig.json"))
                {
                    var jsonData = JsonConvert.DeserializeObject<ConfigStructure>(await sr.ReadToEndAsync());
                    Config.token = jsonData.token;
                    Config.channelId = jsonData.channelId;
                    Config.mentionRoleId = jsonData.mentionRoleId;
                    Config.fetchCooldown = jsonData.fetchCooldown;
                }
                Log.Information($"Successfully loaded config!");
                Log.Information($"Fetching Cooldown: {(int)Config.fetchCooldown} minutes");
                Log.Information($"Channel Id: {Config.channelId}");
                Log.Information($"Role Id: {Config.mentionRoleId}");
            } catch (Exception ex) {
                Log.Fatal($"Error while trying to read \"{exePath}\\Config\\BotConfig.json\"! Make sure the file exists & has the same structure as Config in \"{exePath}\\Template\" folder.");
                Environment.Exit(0);
            }
        }

        public static async Task SaveToConfig(ulong channelId = 0, ulong mentionRoleId = 0, long fetchCooldown = 0)
        {
            if (channelId != 0) Config.channelId = channelId;
            if (mentionRoleId != 0) Config.mentionRoleId = mentionRoleId;
            if (fetchCooldown != 0) Config.fetchCooldown = fetchCooldown;

            using (StreamWriter sw = new StreamWriter($"{exePath}\\Config\\BotConfig.json"))
            {
                ConfigStructure configObj = new ConfigStructure()
                {
                    token = Config.token,
                    channelId = Config.channelId,
                    mentionRoleId = Config.mentionRoleId,
                    fetchCooldown = Config.fetchCooldown,
                };

                await sw.WriteAsync(JsonConvert.SerializeObject(configObj, Formatting.Indented));
            }
        }

    }

    public sealed class ConfigStructure
    {
        public string token { get; set; }
        public ulong channelId { get; set; }
        public ulong mentionRoleId { get; set; }
        public long fetchCooldown { get; set; }
    }
}