using Parser;
using System.Threading.Tasks;

string startUrl = "https://www.toy.ru/catalog/boy_transport";

var watch = new System.Diagnostics.Stopwatch();
watch.Start();

var engine = new Engine(startUrl);

CSVWritter.Write(await engine.Parse());

await engine.ChangeRegion("Ростов");

CSVWritter.Write(await engine.Parse());

Console.WriteLine($"Парсинг отработал за : {watch.Elapsed.TotalSeconds} с.");
watch.Stop();

Console.WriteLine("Конец");
Console.ReadKey();