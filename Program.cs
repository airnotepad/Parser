using Parser;

string startUrl = "https://www.toy.ru/catalog/boy_transport";

var engine = new Engine(startUrl);

var products = await engine.Parse();

CSVWritter.Write(products);

await engine.ChangeRegion("Ростов");

products = await engine.Parse();

CSVWritter.Write(products);

Console.WriteLine("Конец");
Console.ReadKey();