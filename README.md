# AutoCad .Net Extensions for Geodesy

![AutoCad](https://geodesist.ru/attachments/strelki_menju-png.35900/)

## Содержание
1. [Установка](#установка)
2. [Команды](#команды)
   - [`iCmdTest_DrawWallArrowsRandom`](#icmdtest_drawwallarrowsrandom)
   - [`iCmdTest_DrawWallArrows`](#icmdtest_drawwallarrows)
   - [`iCmd_EditDimensionValueRandom`](#icmd_editdimensionvaluerandom)
   - [`iCmd_DrawCoordinateGrid`](#icmd_drawcoordinategrid)
   - [`iCmd_ImportSdrData`](#icmd_importsdrdata)
   - [`iCmd_ExportSdrData`](#icmd_exportsdrdata)
   - [`iCmd_ConvertCircleToPoint`](#icmd_convertcircletopoint)
   - [`iCmd_EditPointElevationRandom`](#icmd_editpointelevationrandom)
   - [`iCmd_AddDimensionValueRandom`](#icmd_adddimensionvaluerandom)
   - [`iCmd_CreateCogoPointsFromBuffer`](#icmd_createcogopointsfrombuffer)
   - [`iCmd_DrawAnchorDeviations`](#icmd_drawanchordeviations)
   - [`iCmd_DrawSlopeLines`](#icmd_drawslopelines)
   - [`iCmdTest_DrawCartogramm`](#icmdtest_drawcartogramm)
   - [`iCmd_EditCogoPointLocation`](#icmd_editcogopointlocation)

## Установка
Для загрузки расширения используйте команду `NETLOAD` и выберите файл `IgorKL.ACAD3.Customization.dll`.

## Команды

### `iCmdTest_DrawWallArrowsRandom`
Рисует в случайном порядке стрелки и значения отклонений.

<details>
  <summary>Посмотреть изображение</summary>
  
  [![iCmdTest_DrawWallArrowsRandom](https://img.youtube.com/vi/3Z33nPY3fwo/0.jpg)](https://www.youtube.com/watch?v=3Z33nPY3fwo)
</details>

### `iCmdTest_DrawWallArrows`
Рисует стрелки отклонения по верху и низу.

<details>
  <summary>Посмотреть изображение</summary>

  [![iCmdTest_DrawWallArrows](https://img.youtube.com/vi/WAs0hecJ67Q/0.jpg)](https://www.youtube.com/watch?v=WAs0hecJ67Q)
</details>

### `iCmd_EditDimensionValueRandom`
Генерирует случайные значения отклонений линейных размеров в указанном диапазоне.

<details>
  <summary>Посмотреть изображение</summary>

  [![iCmd_EditDimensionValueRandom](https://img.youtube.com/vi/vVCWAB27SSw/0.jpg)](https://www.youtube.com/watch?v=vVCWAB27SSw)
</details>

### `iCmd_DrawCoordinateGrid`
Рисует сетку координат.

<details>
  <summary>Посмотреть изображение</summary>

  [![iCmd_DrawCoordinateGrid](https://img.youtube.com/vi/CU9868QrX4s/0.jpg)](https://www.youtube.com/watch?v=CU9868QrX4s)
</details>

### `iCmd_ImportSdrData`
Импортирует данные из файла sdr с координатами точек и сохраняет их в чертеже AutoCAD в виде точек (проверено на тахеометрах SOKKIA).

<details>
  <summary>Посмотреть изображение</summary>

  [![iCmd_ImportSdrData](https://img.youtube.com/vi/hgR9Fd0hA50/0.jpg)](https://www.youtube.com/watch?v=hgR9Fd0hA50)
</details>

### `iCmd_ExportSdrData`
Экспортирует точки в файл sdr для разбивки (проверено на тахеометрах SOKKIA).

### `iCmd_ConvertCircleToPoint`
Конвертирует круги в точки AutoCAD (с высотой).

### `iCmd_EditPointElevationRandom`
Добавляет случайные отклонения от фактического значения отметки COGO точки в указанном диапазоне.

<details>
  <summary>Посмотреть изображение</summary>

  [![iCmd_EditPointElevationRandom](https://img.youtube.com/vi/MpLE01xPY_4/0.jpg)](https://www.youtube.com/watch?v=MpLE01xPY_4)
</details>

### `iCmd_AddDimensionValueRandom`
Добавляет случайные значения отклонений линейных размеров в указанном диапазоне.

### `iCmd_CreateCogoPointsFromBuffer`
Создает COGO точки из буфера обмена. В блокноте строки с координатами имеют вид: имя, x, y, h, описание (разделены табуляцией). Нажмите копировать, затем в чертеже введите команду для создания точек.

### `iCmd_DrawAnchorDeviations`
Отображает на чертеже направления и величину смещений между двумя указанными точками (плановое отклонение анкеров). Стрелки и текст аннотативные, их размер меняется при изменении масштаба аннотаций.

<details>
  <summary>Посмотреть изображение</summary>

  [![iCmd_DrawAnchorDeviations](https://img.youtube.com/vi/1uFGeHBuKqc/0.jpg)](https://www.youtube.com/watch?v=1uFGeHBuKqc)
</details>

### `iCmd_DrawSlopeLines`
Рисует линии откоса с заданным шагом и в указанных границах. Предусмотрены три метода рисования откосов и возможность редактирования линий без необходимости разрыва блока. Линии заключаются в анонимный блок.

<details>
  <summary>Посмотреть изображение</summary>

  [![iCmd_DrawSlopeLines](https://img.youtube.com/vi/Ata9Ny3_0oU/0.jpg)](https://www.youtube.com/watch?v=Ata9Ny3_0oU)
</details>

### `iCmdTest_DrawCartogramm`
Рисует картограмму земляных масс по поверхности вычисления объемов Civil 3D. Команда добавлена в тестовом режиме и на данный момент является экспериментальной.

<details>
  <summary>Посмотреть изображение</summary>

  [![iCmdTest_DrawCartogramm](https://img.youtube.com/vi/iGd4-en1noQ/0.jpg)](https://www.youtube.com/watch?v=iGd4-en1noQ)
</details>

### `iCmd_EditCogoPointLocation`
Редактирует в случайном порядке плановое положение COGO точек.

<details>
  <summary>Посмотреть изображение</summary>

  [![iCmd_EditCogoPointLocation](https://img.youtube.com/vi/qD-tybn6gs8/0.jpg)](https://www.youtube.com/watch?v=qD-tybn6gs8)
</details>
