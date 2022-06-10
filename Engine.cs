using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Js;

namespace Parser
{
    public class Engine
    {
        //Победить скорость загрузки страниц стандартными заглушками библиотеки AS не удалось, проблема заключается в контейнере куков.
        //Использование контейнера в любом виде по сути превращает код в синхронный. Именно поэтому это находится здесь.
        bool needUseCookies;
        string url = string.Empty;
        IConfiguration config;
        CookieProvider cookieHandler;

        public Engine(string startUrl)
        {
            url = startUrl;
            cookieHandler = new CookieProvider();
            config = Configuration.Default.WithDefaultLoader().WithCookies(cookieHandler);
        }

        public async Task<List<Product>> Parse()
        {
            List<Product> result = new();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(url);

            bool hasNextPage;
            do
            {
                var currentPage = document.QuerySelector(".pagination > .active");
                Console.WriteLine($"Страница #{currentPage.GetContent().Trim().RegexPageNumber()}");

                List<string> pageUrls = new();

                var pageProducts = document.QuerySelectorAll(".product-card");
                Console.WriteLine($"Товаров на странице: {pageProducts.Count()}");

                foreach (var product in pageProducts)
                {
                    var url = product.QuerySelector("meta[itemprop='url']")?.GetAttribute("content");
                    if (url is not null) pageUrls.Add(url);
                }

                var tasks = pageUrls.Select(x => ParseProductAsync(x));
                var tastsResult = await Task.WhenAll(tasks);
                result.AddRange(tastsResult);

                hasNextPage = currentPage.NextElementSibling is not null;
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
            var context = needUseCookies
                ? BrowsingContext.New(Configuration.Default.WithDefaultLoader().WithDefaultCookies())
                : BrowsingContext.New(Configuration.Default.WithDefaultLoader());

            if (needUseCookies)
            {
                var Address = Url.Create(url);
                var Cookie = cookieHandler.GetCookie(Address);
                context.SetCookie(Address, Cookie.Replace(';', ','));
            }

            var document = await context.OpenAsync(url);

            var name = document.QuerySelector("[itemprop=\"name\"]").GetContent();
            var oldPrice = document.QuerySelector(".old-price").GetContent();
            var price = document.QuerySelector("[itemprop=\"price\"]").GetContent();
            var available = document.QuerySelector(".ok").GetContent(ifNull: "Нет в наличии");
            var breadcrumbs = document.QuerySelectorAll(".breadcrumb-item[href]").Select(x => x.GetAttribute("title"));
            var region = document.QuerySelector(".select-city-link > a").GetContent().Trim();
            var images = document.QuerySelector(".detail-image")?.QuerySelectorAll("img.img-fluid[src]").Select(x => x.GetAttribute("src"));

            Console.WriteLine($"{region} Product {name}");

            return new Product()
            {
                Name = name,
                Url = url,
                Available = available,
                Breadcrumbs = breadcrumbs.ToArray(),
                Price = price,
                OldPrice = oldPrice ?? "",
                Images = images.ToArray(),
                Region = region
            };
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

            var regions = document.QuerySelector(".region-links")?.QuerySelectorAll("a[href]");

            var selectedRegion = regions.FirstOrDefault(x => x.TextContent.ToUpper().Contains(regionName.ToUpper()));
            if (selectedRegion is null) throw new Exception("Такой регион не найден");

            //var jsResponse = neededRegion.FireSimpleEvent("click");
            //var jsResponse = document.ExecuteScript("document.querySelector('.region-links').querySelectorAll('a[href]')[6].click()");

            //Один из рабочих вариантов, когда не нужно глубоко копать JS и копать библиотеку AS
            var jsResponse = document.ExecuteScript($"SaveGeoCity('{selectedRegion.Attributes["rel"].Value}')");

            document = await context.OpenAsync(url);

            var region = document.QuerySelector(".select-city-link > a").GetContent().Trim();
            needUseCookies = true;
            Console.WriteLine($"Текущий регион: {region}");
        }
    }
}
