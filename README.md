# Parser
Html parser via AngleSharp

Проект еще будет дорабатываться.

Алгоритм парсинга:
- 1 - Парсинг страницы каталога - собираются данные о текущей странице в блоке пагинации и ссылки на товары;
- 2 - Парсинг страницы товара - собираются необходимые данные о товаре;
- 3 - При наличии следующей страницы каталога последовательно повторяются 1 и 2 шаг.

> Проект написан исключительно с использованием одной библиотеки - AngleSharp.
> Реализация с использованием логического флага 'needUseCookies' является временным решением.
> Необходимость его использования обусловлена падением производительности при использовании куки-контейнера.