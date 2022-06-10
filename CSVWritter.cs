using System.Text;

namespace Parser
{
    internal static class CSVWritter
    {
        public static string Path => System.IO.Path.Combine(Environment.CurrentDirectory, "result.csv");

        public static void Write(IEnumerable<Product> products)
        {
            Console.WriteLine($"Запись в файл ... {Path}");

            if (!File.Exists(Path))
                File.Create(Path).Close();

            var csv = new StringBuilder();

            foreach (var product in products)
            {
                csv.AppendLine($"{product.Region};{product.Url};{product.Name};{product.Price};{product.OldPrice};{product.Available};{string.Join(" > ", product.Breadcrumbs)};{string.Join(";", product.Images)}");
            }

            File.AppendAllText(Path, csv.ToString(), Encoding.UTF8);
        }
    }
}
