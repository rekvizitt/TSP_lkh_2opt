Установка:

1. Установить .NET 8 (Desktop
   Runtime). https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.2-windows-x64-installer

UI:
![image](https://github.com/glebtyanov/TSP_lkh_2opt/assets/85569847/4d932981-0ee6-4968-bdae-aaa0e70ec415)

Для решения:

1. Выберите файл формата .xlsx. Если в документе несколько листов, выберите нужный (на котором находится матрица
   стоимостей).
2. Укажите координаты первого элемента матрицы
3. Укажите размер матрицы
4. Укажите количество итераций (попыток найти лучшее решение).
   Советы по выбору итераций:
   2opt: каждая итерация будет запущена в отдельном потоке, считаются быстро. Попробуйте начать с 100 итераций, дальше
   подбирайте под собственные нужды в соответствии с:
    1. Размером матрицы
    2. Предполагаемым временем выполнения
    3. Точностью решения

   LKH: не требуется большое количество итераций, как правило оптимальное решение находится в первые 2-3. Запускаются
   они последовательно, поэтому расчеты будут идти гораздо дольше.

6. Выберите метод решения. Тесты показывают, что до размера матрицы 40х40 2opt (с большим кол-вом итераций) и LKH
   сгенерируют ~одинаковые результаты.
   На больших размерах LKH стабильно генерирует гораздо более лучшие решения. 

