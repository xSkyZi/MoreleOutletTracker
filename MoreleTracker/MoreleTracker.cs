using DSharpPlus.Entities;
using HtmlAgilityPack;
using MoreleOutletTracker.MoreleTracker.JSONObjects;
using MoreleOutletTracker.MoreleTracker.ObjectModels;
using MoreleOutletTracker.MoreleTracker.PostGetJsonTemplates;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

namespace MoreleOutletTracker.MoreleTracker
{
    public static class FloatExtensions
    {
        public static string FormatCurrency(this float value)
        {
            CultureInfo cultureInfo = new CultureInfo("pl-PL"); // Polish culture
            string formattedValue = value.ToString("N2", cultureInfo); // Format with 2 decimal places
            formattedValue = formattedValue.Replace(cultureInfo.NumberFormat.NumberDecimalSeparator, ","); // Replace decimal separator with ","
            formattedValue = formattedValue.Replace(cultureInfo.NumberFormat.NumberGroupSeparator, " "); // Replace group separator with space
            formattedValue += " zł"; // Append currency symbol
            return formattedValue;
        }
    }

    public class CounterManager
    {
        private int counter = 0;
        private readonly object counterLock = new object();

        public void IncrementCounter()
        {
            lock (counterLock)
            {
                counter++;
            }
        }

        public void DecrementCounter()
        {
            lock (counterLock)
            {
                counter--;
            }
        }

        public int GetCounter()
        {
            lock (counterLock)
            {
                return counter;
            }
        }
    }

    public class MoreleTracker
    {

        private const uint outletPageRateLimit = 20;

        private const string baseApiUrl = "https://www.morele.net/api/widget/promotion_products/";
        private const string productOutletApiUrlBase = "https://www.morele.net/api/product/getProductOutlet/";
        public static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        //vars used only while checking outlet
        static Dictionary<uint, Dictionary<string, string>> outletItemList = new Dictionary<uint, Dictionary<string, string>>(); //uint - globalProductId, 1st string - Product Name, 2nd string - Product Website Link
        static List<Product> productsList = new List<Product>();

        public static async Task Initialize()
        {
            if (!JsonFM.CategoriesConfigExists())
            {
                GenerateCategoriesConfig();
                Log.Error($"Generated categories config in \"{JsonFM.categoriesConfigName}\", please set it up to your preferences and restart the bot in order to start fetching products.");
                return;
            }
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                Log.Information("Trying to get products from outlet....");
                await CheckOutlet(JsonFM.RetrieveCategoriesFromFile());
                Log.Information($"Finished getting products from outlet. waiting {Config.fetchCooldown} minutes");
                await Task.Delay(TimeSpan.FromMinutes(Config.fetchCooldown));
            }
        }

        private static async Task CheckOutlet(string[] categories)
        {
            var httpClient = new HttpClient();

            var postData = new MorelePostRequestData()
            {
                limit = 999,
                isOutlet = true,
                categories = categories
            };

            if (categories == null) postData.categories = null;

            var content = new StringContent(JsonConvert.SerializeObject(postData), System.Text.Encoding.UTF8, "application/json");

            var result = await httpClient.PostAsync(baseApiUrl, content);

            if (result.IsSuccessStatusCode && result.Content != null)
            {
                await GetProductsList(JsonConvert.DeserializeObject<MorelePostResponseData>(await result.Content.ReadAsStringAsync()).html);

                Log.Debug($"Found total amount of {productsList.Count} products");
                if (JsonFM.ProductFileExists())
                {
                    if (Config.channelId != 0)
                    {
                        await CompareFetchedProducts();
                    } else
                    {
                        Log.Warning("You don't have bot setted up on the server, use '/config setup' in order to have the bot check if new prodcuts has been added.");
                    }
                } else
                {
                    JsonFM.CreateDirectory();
                }
                JsonFM.SaveProductListToFile(productsList);
                productsList.Clear();
            }
            else
            {
                Log.Error($"Received error {result.StatusCode} from POST request");
            }
        }

        private static void GenerateCategoriesConfig()
        {
            Dictionary<string, CategoryGroup> categoriesObject = new Dictionary<string, CategoryGroup>();

            List<Category> categories = new List<Category>();

            var httpClient = new HttpClient();

            var html = httpClient.GetAsync("https://www.morele.net/outlet/").Result;

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html.Content.ReadAsStringAsync().Result);

            HtmlNodeCollection categoriesHtml = htmlDocument.DocumentNode.SelectNodes("//input[@class='filter-element filter-category']");
            foreach (HtmlNode node in categoriesHtml)
            {
                string categoryName = htmlDocument.DocumentNode.SelectSingleNode(node.XPath + "/following::span[@class='checkbox-value']").GetDirectInnerText();

                string categoryGroupName = node.SelectSingleNode("ancestor::div[@class='f-value f-collection']").SelectSingleNode(".//span[@class='checkbox-value']").GetDirectInnerText();
                categoryGroupName = categoryGroupName.Substring(0, categoryGroupName.Length - 6);

                Category category = new Category()
                {
                    name = categoryName.Substring(0, categoryName.Length - 6),
                    id = ushort.Parse(node.Attributes["value"].Value),
                    useInSearch = false
                };

                if (!categoriesObject.ContainsKey(categoryGroupName))
                {
                    categoriesObject.Add(categoryGroupName, new CategoryGroup() { useInSearch = false, subCategories = new List<Category>() });
                }
                categoriesObject[categoryGroupName].subCategories.Add(category);
            }

            JsonFM.SaveGeneratedCategoryConfigToFile(categoriesObject);
        }

        private static async Task GetProductsList(string postHtmlData)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(postHtmlData);

            HtmlNodeCollection productElement = htmlDocument.DocumentNode.SelectNodes("//div[@class='owl-item productItemData ']");

            foreach (HtmlNode node in productElement)
            {
                if (!outletItemList.ContainsKey(UInt32.Parse(node.Attributes["data-product-id"].Value)))
                {
                    Dictionary<string, string> additionalData = new Dictionary<string, string>(); //Product name, Product Site Link
                    additionalData.Add(node.Attributes["data-product-name"].Value, node.SelectSingleNode(".//a[@class='link-top productLink']").Attributes["href"].Value);

                    outletItemList.Add(UInt32.Parse(node.Attributes["data-product-id"].Value), additionalData);
                }
            }
            int totalCount = outletItemList.Count;
            Log.Information($"Found total of {totalCount} items");
            CounterManager countmng = new CounterManager();
            foreach (uint itemId in outletItemList.Keys)
            {
                GetProductOutletPageAndStuff(itemId, countmng);
            }

            while (countmng.GetCounter() != 0)
            {
                await Task.Delay(1250);
            }
            outletItemList.Clear();
        }

        private static async void GetProductOutletPageAndStuff(uint itemId, CounterManager countmngInstance)
        {
            countmngInstance.IncrementCounter();
            Log.Information($"Invoke with current checks {countmngInstance.GetCounter()}");
            var httpClient = new HttpClient();

            var httpGetResponse = await httpClient.GetAsync(productOutletApiUrlBase + itemId.ToString());

            while (!httpGetResponse.IsSuccessStatusCode)
            {
                CounterManager retryCounter = new CounterManager();
                Log.Error($"Received Error {httpGetResponse.StatusCode} while requesting GET for Item with ID: {itemId} retrying...");
                retryCounter.IncrementCounter();
                await Task.Delay(850);
                if (retryCounter.GetCounter() > 5)
                {
                    countmngInstance.DecrementCounter();
                    return;
                }
            }
            var productHtml = JsonConvert.DeserializeObject<MoreleGetOutletResponseData>(await httpGetResponse.Content.ReadAsStringAsync()); //pain
            if (productHtml.template == String.Empty)
            {
                countmngInstance.DecrementCounter();
                return;
            }

            var productHtmlDoc = new HtmlDocument();
            productHtmlDoc.LoadHtml(productHtml.template);

            HtmlNodeCollection productsElement = productHtmlDoc.DocumentNode.SelectSingleNode("//div[@class='outlet-grid']").ChildNodes;

            foreach (HtmlNode node in productsElement)
            {
                if (node.Name != "div") continue;
                string thumbnailUrl = String.Empty;
                try
                {
                    thumbnailUrl = node.SelectSingleNode(".//img").Attributes["src"].Value;
                }
                catch (Exception ex)
                {
                    //no img lmao
                }
                Product product = new Product()
                {
                    Id = Int32.Parse(node.SelectSingleNode(".//div[@class='ogi-id']").InnerHtml.Substring(4)),
                    Name = outletItemList[itemId].Keys.First(),
                    Condition = node.SelectSingleNode(".//div[@class='ogi-status font-semibold']").InnerText,
                    Description = node.SelectSingleNode(".//div[@class='ogi-desc']").InnerHtml,
                    oldPrice = float.Parse(new string(node.SelectSingleNode(".//div[@class='price-old']").InnerHtml.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray()).Replace(',', '.')),
                    newPrice = float.Parse(new string(node.SelectSingleNode(".//div[@class='price-new']").InnerHtml.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray()).Replace(',', '.')), //at least it's more understanable than JS regex
                    thumbnail = thumbnailUrl,
                    link = outletItemList[itemId].Values.First()
                };

                productsList.Add(product);
            }
            countmngInstance.DecrementCounter();
            Log.Information($"Invoke finished current checks {countmngInstance.GetCounter()}");
        }

        private static async Task CompareFetchedProducts()
        {
            List<Product> productsFromFile = JsonFM.RetrieveProductsFromFile();

            foreach (Product fetchedProduct in productsList)
            {
                await CheckProduct(fetchedProduct, productsFromFile);
            }

        }

        private static async Task CheckProduct(Product fetchedProduct, List<Product> productsFromFile)
        {
            Log.Information($"started check for {fetchedProduct.Id}");
            bool existsInFile = false;
            foreach (Product localProduct in productsFromFile)
            {
                if (productsFromFile.Contains(localProduct))
                {
                    existsInFile = true;
                    break;
                }
            }
            if (!existsInFile)
            {
                Log.Information("Found new product, sending message to discord....");
                await NotifyAboutNewProduct(fetchedProduct);
            }
            Log.Information($"finished check for {fetchedProduct.Id}");
        }

        private static async Task NotifyAboutNewProduct(Product product)
        {
            DiscordChannel channel = await Program.Client.GetChannelAsync(Config.channelId);
            DiscordRole role = channel.Guild.GetRole(Config.mentionRoleId);

            var embed = new DiscordEmbedBuilder
            {
                Title = product.Name,
                Color = new DiscordColor(255, 80, 60),
                ImageUrl = product.thumbnail,
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = "https://www.morele.net/pwa/icons/icon48.png"
                },
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"ID: {product.Id}"
                },
            };

            embed.AddField("Cena:", $"~~{product.oldPrice.FormatCurrency()}~~ \n {product.newPrice.FormatCurrency()}", false);
            embed.AddField(product.Condition, product.Description, false);

            var linkButton = new DiscordLinkButtonComponent(product.link, "Link");

            var message = new DiscordMessageBuilder().AddEmbed(embed).AddComponents(linkButton);
            message.WithAllowedMention(new RoleMention());
            message.Content = role.Mention;

            await channel.SendMessageAsync(message);
        }
    }
}
