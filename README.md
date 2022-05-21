# AutoCad
## Autocad .Net extensions for geodesy

![image](https://geodesist.ru/attachments/strelki_menju-png.35900/)

### Загрузка: NETLOAD -> выбрать файл IgorKL.ACAD3.Customization.dll.

### Команды:

> ## ```iCmdTest_DrawWallArrowsRandom```
> рисует в случайном порядке стрелки и значения отклонений.
>
> [![CmdTest_DrawWallArrowsRandom](https://img.youtube.com/vi/3Z33nPY3fwo/0.jpg)](https://www.youtube.com/watch?v=https://youtu.be/3Z33nPY3fwo)

<br/>

> ## ```iCmdTest_DrawWallArrows```
> рисует стрелки отклонения по верху и низу.
> 
> [![iCmdTest_DrawWallArrows](https://img.youtube.com/vi/WAs0hecJ67Q/0.jpg)](https://www.youtube.com/watch?v=https://youtu.be/WAs0hecJ67Q)

<br/>

> ## ```iCmd_EditDimensionValueRandom```
> генерирует случайные значения отклонений линейных размеров в указанном диапазоне.
>
> [![iCmd_EditDimensionValueRandom](https://img.youtube.com/vi/vVCWAB27SSw/0.jpg)](https://www.youtube.com/watch?v=https://youtu.be/vVCWAB27SSw)

<br/>

> ## ```iCmd_DrawCoordinateGrid```
> рисует сетку координат.
> 
> [![iCmd_EditDimensionValueRandom](https://img.youtube.com/vi/CU9868QrX4s/0.jpg)](https://www.youtube.com/watch?v=https://youtu.be/CU9868QrX4s)

<br/>

> ## ```iCmd_ImportSdrData```
> импортирует данные из файла sdr с координатами точек, сохранят в чертеже acad в виде точек (проверено на тахеометрах SOKKIA).
>
> [![iCmd_EditDimensionValueRandom](https://img.youtube.com/vi/hgR9Fd0hA50/0.jpg)](https://www.youtube.com/watch?v=https://youtu.be/hgR9Fd0hA50)

<br/>

> ## ```iCmd_ExportSdrData```
> экспорт точек в файл sdr для разбивки (проверено на тахеометрах SOKKIA)

> ## ```iCmd_ConvertCircleToPoint``` 
> конвертирует круги в точки автокада (с высотой).

> ## ```iCmd_EditPointElevationRandom```
> добавляет случайным образом отклонения от фактического значения отметки COGO точки в указанном диапазоне.
> 
> [![iCmd_EditDimensionValueRandom](https://img.youtube.com/vi/MpLE01xPY_4/0.jpg)](https://www.youtube.com/watch?v=https://youtu.be/MpLE01xPY_4)

<br/>

> ## ```iCmd_AddDimensionValueRandom```
> добавляет случайные значения отклонений линейных размеров в указанном диапазоне.

> ## ```iCmd_CreateCogoPointsFromBuffer```
> создает COGO точки из буфера обмена, т.е. в блокноте есть строки с координатами вида: имя x y h описание (через знак табуляции) 
> нажимаем копировать >>.(данные помещаются в буфер) в чертеже вводим команду - появляются точки).

> ## ```iCmd_DrawAnchorDeviations```
> отображает на чертеже направления и величину смещений м/у двумя указанными точками (плановое отклонение анкеров).
> Стрелки и текст - аннотативные, т.е. при изменении масштаба аннотаций меняется размер 
> (если включена кнопка автоматического добавления масштабов ```ANNOAUTOSCALE``` )
> 
> [![iCmd_EditDimensionValueRandom](https://img.youtube.com/vi/1uFGeHBuKqc/0.jpg)](https://www.youtube.com/watch?v=https://youtu.be/1uFGeHBuKqc)

> ## ```iCmd_DrawSlopeLines```
> рисует линии откоса с заданным шагом и в указанных границах. 
> Три метода рисования откосов + возможность редактирования линий без необходимости взрывать блок. Линии заключаются в анонимный блок.
> 
> [![iCmd_EditDimensionValueRandom](https://img.youtube.com/vi/Ata9Ny3_0oU/0.jpg)](https://www.youtube.com/watch?v=https://youtu.be/Ata9Ny3_0oU)

<br/>

> ## ```iiCmdTest_DrawCartogramm```
> рисует картограмму зем масс по поверхности вычисления объемов civil 3d. 
> Команда добавлена в тестовом режиме на данный момент совершенно сырая...
> 
> [![iCmd_EditDimensionValueRandom](https://img.youtube.com/vi/iGd4-en1noQ/0.jpg)](https://www.youtube.com/watch?v=https://youtu.be/iGd4-en1noQ)

> ## ```iCmd_EditCogoPointLocation
> редактирует в случайном порядке плановое положение COGO точек.
> 
> [![iCmd_EditDimensionValueRandom](https://img.youtube.com/vi/qD-tybn6gs8/0.jpg)](https://www.youtube.com/watch?v=https://youtu.be/qD-tybn6gs8)

### Требования к системе:
1. Autocad (Civil) 2013/2014/2015/2016 x64
2. Установленный .Net Framework версии 4.5.2 
