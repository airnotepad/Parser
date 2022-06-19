using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Io.Network;
using AngleSharp.Js;
using RestSharp;
using System.Collections.Concurrent;

namespace Parser
{
    public class Engine
    {
        string url = string.Empty;
        IConfiguration config;
        CookieProvider cookieHandler;
        ParallelOptions parallelOptions;

        public Engine(string startUrl, ParallelOptions? options = null)
        {
            url = startUrl;
            cookieHandler = new CookieProvider();
            config = Configuration.Default.WithDefaultLoader().WithCookies(cookieHandler);
            parallelOptions = options is not null ? options : new ParallelOptions() { MaxDegreeOfParallelism = 15 };
        }

        string cookies = string.Empty;

        public async Task<IEnumerable<Product>> Parse()
        {
            ConcurrentBag<Product> result = new();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(url);

            bool hasNextPage;
            do
            {
                var currentPage = document.QuerySelector(".pagination > .active");
                Console.WriteLine($"Страница #{currentPage.GetContent().Trim().RegexPageNumber()}");

                List<string> pageUrls = new();

                var pageProducts = document.QuerySelectorAll(".product-card");
                foreach (var product in pageProducts)
                {
                    var url = product.QuerySelector("meta[itemprop='url']")?.GetAttribute("content");
                    if (url is not null) pageUrls.Add(url);
                }

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                Parallel.ForEach(pageUrls, parallelOptions, async x => result.Add(await ParseProductAsync(x)));

                watch.Stop();
                Console.WriteLine($"Страница отработана за : {watch.Elapsed.TotalSeconds} с. -> Количество продуктов: {result.Count}");

                hasNextPage = currentPage?.NextElementSibling is not null;
                if (hasNextPage)
                {
                    var href = currentPage.NextElementSibling.QuerySelector("a[href]").GetAttribute("href");
                    document = await context.OpenAsync(new Url(document.BaseUrl, href));
                }
            }
            while (hasNextPage);

            return result;
        }

        private async Task<Product> ParseProductAsync(string url)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            var content = await GetContent(url);
            if (content is null) throw new Exception($"Content is null for {url}");
            var document = await BrowsingContext.New().OpenAsync(request => request.Header("Content-Type", "text/html; charset=utf-8").Address(url).Content(content));

            watch.Stop();

            var name = document.QuerySelector("[itemprop=\"name\"]").GetContent();
            var oldPrice = document.QuerySelector(".old-price").GetContent();
            var price = document.QuerySelector("[itemprop=\"price\"]").GetContent();
            var available = document.QuerySelector(".ok").GetContent(ifNull: "Нет в наличии");
            var breadcrumbs = document.QuerySelectorAll(".breadcrumb-item[href]").Select(x => x.GetAttribute("title"));
            var region = document.QuerySelector(".select-city-link > a").GetContent().Trim();
            var images = document.QuerySelector(".detail-image")?.QuerySelectorAll("img.img-fluid[src]").Select(x => x.GetAttribute("src"));

            Console.WriteLine($"Получен за: {watch.Elapsed.Seconds} с. -> Регион {region} Продукт {name}");

            return new Product()
            {
                Name = name,
                Url = url,
                Available = available,
                Breadcrumbs = breadcrumbs?.ToArray(),
                Price = price,
                OldPrice = oldPrice ?? "",
                Images = images?.ToArray(),
                Region = region
            };
        }

        private async Task<string?> GetContent(string url)
        {
            var client = new RestClient();
            var request = new RestRequest(url);

            string cookie = string.IsNullOrEmpty(cookies) ? string.Empty : cookies;
            request.AddParameter("Cookie", cookie, ParameterType.HttpHeader);

            var response = client.Execute(request);

            return response?.Content;
        }

        public async Task ChangeRegion(string regionName)
        {
            Console.WriteLine($"Смена региона ...");

            var conf = Configuration.Default
                .WithDefaultLoader(new AngleSharp.Io.LoaderOptions()
                {
                    IsResourceLoadingEnabled = true,
                    IsNavigationDisabled = false
                })
                .WithJs()
                .WithCookies(cookieHandler);

            var context = BrowsingContext.New(conf);
            var document = await context.OpenAsync(url).WhenStable();

            var oldCookies = cookieHandler.GetCookie(Url.Create(url)) + ";";

            var regions = document.QuerySelector(".region-links")?.QuerySelectorAll("a[href]");

            var selectedRegion = regions.FirstOrDefault(x => x.TextContent.ToUpper().Contains(regionName.ToUpper()));
            if (selectedRegion is null) throw new Exception("Такой регион не найден");

            //var jsResponse = neededRegion.FireSimpleEvent("click");
            //var jsResponse = document.ExecuteScript("document.querySelector('.region-links').querySelectorAll('a[href]')[6].click()");

            //Один из рабочих вариантов, когда не нужно глубоко копать JS и копать библиотеку AS
            var jsResponse = document.ExecuteScript($"SaveGeoCity('{selectedRegion.Attributes["rel"].Value}')");

            var newCookies = cookieHandler.GetCookie(Url.Create(url));
            cookies = newCookies.Replace(oldCookies, "");

            if (!string.IsNullOrEmpty(cookies)) Console.WriteLine($"Текущий регион успешно сменен");
            else Console.WriteLine($"Что-то пошло не так");
        }
    }
}
