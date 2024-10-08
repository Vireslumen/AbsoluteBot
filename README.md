# AbsoluteBot

**AbsoluteBot** — это мощный кросс-платформенный чат-бот, разработанный на .NET 8.0 для автоматизации общения и управления на таких платформах, как Twitch, VkPlayLive, Telegram и Discord. Бот интегрирует различные функции, начиная от интерактивных команд до систем автоматизированных уведомлений, модерации, аналитики и взаимодействия с пользователями.

## Содержание

- [Общее описание](#общее-описание)
- [Основные функции](#основные-функции)
  - [Интерактивное общение и нейросеть](#интерактивное-общение-и-нейросеть)
  - [Оповещения о начале стримов](#оповещения-о-начале-стримов)
  - [Праздники и ежедневные уведомления о них](#праздники-и-ежедневные-уведомления-о-них)
  - [Автоматическое исправление раскладки](#автоматическое-исправление-раскладки)
  - [Перевод и автоперевод сообщений](#перевод-и-автоперевод-сообщений)
  - [Фильтрация сообщений](#фильтрация-сообщений)
  - [Система ролей пользователей](#система-ролей-пользователей)
  - [Перезарядка команд](#перезарядка-команд)
  - [Прогресс прохождения игр на стриме](#прогресс-прохождения-игр-на-стриме)
  - [Дни рождения пользователей](#дни-рождения-пользователей)
  - [MBTI пользователей](#mbti-пользователей)
  - [Учет игр и стримов в Google Sheets](#учет-игр-и-стримов-в-google-sheets)
  - [Система жалоб](#система-жалоб)
  - [Интерактивные команды](#интерактивные-команды)
  - [Оповещения о курсе валют](#оповещения-о-курсе-валют)
  - [Напоминания и удаление сообщений](#напоминания-и-удаление-сообщений)
  - [Команды управления ботом](#команды-управления-ботом)
  - [Дополнительные динамические команды](#дополнительные-динамические-команды)
- [Скриншоты работы](#скриншоты-работы-absolutebot)
- [Архитектура программы](#архитектура-программы)
- [Гибкость конфигурации и сервисов](#гибкость-конфигурации-и-сервисов)
- [Структура файлов](#структура-файлов)
- [Заключение](#заключение)

## Общее описание

**AbsoluteBot** поддерживает интеграцию с несколькими чат-платформами (Twitch, VkPlayLive, Telegram, Discord) и может работать даже при неполных настройках. Если для какой-либо из платформ не указаны настройки API или другие обязательные данные в конфигурационном файле, бот продолжает работу на других платформах без отключения.

## Основные функции

### Интерактивное общение и нейросеть

**AbsoluteBot** может общаться с пользователями, используя нейросетевую модель **Gemini**, которая адаптируется под индивидуальные особенности стримера и чата. Особенности включают:

- **Поддержка контекста**: Бот запоминает последние 200 сообщений в переписке и последнее изображение, что позволяет ему поддерживать связные разговоры.
- **Динамическое поведение**: Бот может взаимодействовать с пользователями как обычный участник чата. Он может использовать встроенные команды, такие как `!картинка` или `!игнор`, чтобы, например, перестать отвечать на сообщения определённого пользователя.
- **Моделирование личности**: У бота можно настроить модель поведения, включая его привычки и информацию о пользователях, что делает общение более естественным и персонализированным.
- **Управление памятью**: Встроенная команда `!амнезия` позволяет сбросить память бота, удаляя историю сообщений, но сохраняя настройки.

### Оповещения о начале стримов

Для интеграции с игровыми платформами бот поддерживает автоматические оповещения о начале стрима. Эта функция особенно полезна для уведомления подписчиков о новых трансляциях:

- **Интеграция с Discord и Telegram**: В определённые каналы отправляются уведомления о старте стрима.

### Праздники и ежедневные уведомления о них

Бот поддерживает функцию ежедневных уведомлений о праздниках:

- **Команда `!праздник`** — выводит информацию о текущем празднике.
- Ежедневно в Telegram публикуется сообщение с информацией о празднике дня, сгенерированной с помощью нейросети.

### Автоматическое исправление раскладки

Для пользователей, которые случайно набирают текст не на той раскладке клавиатуры, бот автоматически исправляет сообщение:

- Если сообщение написано на английской раскладке вместо русской, бот исправляет текст и отправляет его от лица пользователя.

### Перевод и автоперевод сообщений

Бот поддерживает перевод сообщений с использованием **Deepl API**:

- **Команда `!переведи [текст]`** — переводит текст с русского на английский или с любого языка на русский.
- **Команда `!перевод [никнейм]`** — включает автоперевод сообщений от указанного пользователя.

### Фильтрация сообщений

Для исключения затрагивания ботом острых тем, бот предоставляет инструменты для фильтрации неуместных слов:

- **Автоматическая цензура**: Определённые слова автоматически заменяются на специальное слово, заданное в настройках.
- **Управление списком слов**:
  - **Команда `!добавитьцензуру [слово]`** — добавляет слово в список для фильтрации.
  - **Команда `!удалитьцензуру [слово]`** — удаляет слово из списка.
  - **Команда `!цензура`** — выводит текущий список запрещённых слов.

### Система ролей пользователей

**AbsoluteBot** поддерживает гибкую систему ролей, что позволяет ограничивать или расширять возможности пользователей:

- **Основные роли**:
  - **Default** — базовая роль для новых пользователей.
  - **Premium** — доступ к дополнительным командам.
  - **Moderator** и **Administrator** — расширенные права для управления ботом и чатами.
  - **Ignored** — эти пользователи не могут взаимодействовать с ботом.

### Перезарядка команд

Для управления частотой использования команд бот поддерживает систему перезарядки:

- **Команда `!перезарядка [секунды]`** — настраивает время перезарядки команд для пользователей.
- Пользователи с привилегиями, такие как **Premium**, **Administrator** и **Moderator**, игнорируют перезарядку.

### Прогресс прохождения игр на стриме

Одной из уникальных функций бота является возможность отслеживания прогресса в играх на стриме:

- **Команда `!прогресс`** — выводит текущий процент завершения игры на основе данных с сайта **HowLongToBeat** и текущего времени игры на стриме.
- Все данные о прогрессе сохраняются, что позволяет вернуться к ним в будущем.

### Дни рождения пользователей

Бот автоматически поздравляет пользователей с днем рождения при написании ими первого сообщения в чате, а в Telegram по таймеру. Для управления есть следующие команды:
- **Команда `!добавитьдр [день месяц]`** — добавляет день рождения пользователя.
- **Команда `!удалитьдр`** — удаляет информацию о дне рождения.
- **Команда `!др [никнейм]`** — выводит информацию о ближайших днях рождения пользователей.
- **Команда `!списокдр`** — выводит список всех зарегистрированных пользователей с днями рождения.

### MBTI пользователей

**AbsoluteBot** поддерживает систему MBTI (типология личности), что позволяет пользователям указывать свой тип и узнавать информацию о других:

- **Команда `!mbti [никнейм] или [4 буквы MBTI]`** — позволяет пользователю указать свой MBTI-тип или узнать тип другого пользователя.
- **Команда `!whombti [игра] или [пусто]`** — определяет, каким персонажем из игры является пользователь на основе его MBTI.

### Учет игр и стримов в Google Sheets

Бот поддерживает автоматическую интеграцию с Google Sheets для ведения статистики по играм и стримам:

- Автоматическое обновление листов с информацией о прогрессе игры, оценках и комментариях.
- **Команда `!оценка [оценка]`** — позволяет изменить среднюю оценку чата для текущей игры.
- **Команда `!обигре [игра] или [пусто]`** — выводит подробную информацию об игре из таблицы пройденных на стриме игр.

### Система жалоб

Для обратной связи пользователи могут подавать жалобы на работу бота:

- **Команда `!жалоба [текст]`** — отправляет жалобу.
- **Команда `!списокжалоб`** — доступна администраторам и модераторам для просмотра последних жалоб.

### Интерактивные команды

Бот поддерживает множество команд для взаимодействия с пользователями:

- **Медиа-команды**:
  - **Команда `!видео [запрос]`** — поиск видео на YouTube.
  - **Команда `!картинка [запрос]`** — поиск изображений в Google Картинках.
  - **Команда `!гифка [запрос]`** — поиск GIF в Google Картинках.
  - **Команда `!котик`** — отправка случайного изображения с котиком.
  - **Команда `!сходство [запрос]`** — поиск похожих людей с помощью likeness.ru.
  - **Команда `!клип`** — создание клипа на Twitch.

- **Информационные команды**:
  - **Команда `!вопрос [вопрос]`** — ответ на заданным вопрос с Mail.ru Ответов.
  - **Команда `!факт`** — случайный факт из Википедии.
  - **Команда `!мудрость`** — случайная мудрая цитата стримера.
  - **Команда `!анимудрость [аниме] или [пусто]`** — случайная мудрая цитата из  выбранного или случайного аниме.
  - **Команда `!гороскоп`** — генерация гороскопа на ближайшее время.
  - **Команда `!определение [запрос]`** — вывод определения или информации о запросе.
  - **Команда `!загугли [запрос]`** — поиск информации в Google.

- **Нейросетевые команды**:
  - **Команда `!спросить [вопрос]`** — задать вопрос ChatGPT.
  - **Команда `!!спросить [вопрос]`** — использование Gemini для ответов (понимает изображения).

### Оповещения о курсе валют

Бот ежедневно отправляет график курса рубля относительно евро и доллара, что позволяет пользователям следить за финансовыми изменениями.

### Напоминания и удаление сообщений

Бот может напомнить пользователю о чем-либо или удалить нежелательные сообщения:

- **Команда `!напомнить [время] [сообщение]`** — отправляет сообщение через заданное время.
- **Команда `!удалить [число]`** — удаляет последние сообщения из чата (до 15).

### Команды управления ботом

Для администраторов предусмотрены команды управления ботом:

- **Остановка и перезагрузка**:
  - **Команда `!выключить`** — завершает работу бота (доступно только администраторам).
  - **Команда `!перезагрузка`** — перезагружает бота.

- **Управление статусом команд**:
  - **Команда `!статускоманды [команда] [платформа]`** — включает или выключает команду на платформе.
  - **Команда `!статускоманд`**: выводит список всех команд с их статусами на каждой платформе.

- **Работа с конфигурацией**:
  - **Команда `!setconfig [ключ] [значение]`**: Устанавливает значение для ключа в конфиге.
  - **Команда `!showconfig`**: Выводит текущие настройки конфигурации.

### Дополнительные динамические команды

**Администраторы** и **модераторы** могут добавлять и удалять динамические команды:

- **Команда `!добавитькоманду [название] [ответ]`** — добавляет новую команду, которая будет выводить заданный ответ.
- **Команда `!удалитькоманду [название]`** — удаляет существующую команду.

## Скриншоты работы AbsoluteBot

В этом разделе представлены примеры работы AbsoluteBot на различных платформах и демонстрация его ключевых функций, таких как интерактивное общение, фильтрация сообщений, оповещения и работа с изображениями. Бот поддерживает все практически свои команды и функции на всех поддерживаемых платформах, включая Vk Play Live, Discord, Twitch и Telegram.

---

### 1. Работа на разных платформах

AbsoluteBot функционирует на разных платформах, таких как Vk Play Live, Discord, Twitch и Telegram. Все команды доступны на всех платформах, а бот сохраняет практически одинаковый набор функций независимо от того, где он используется.

#### 1.1 Оповещения и уведомления

**Скриншот: Уведомление о начале стрима (Discord)**  
Бот может отправлять уведомления о начале стримов на различных платформах, таких как Discord и Telegram.

![Уведомление о начале стрима в Discord](https://github.com/user-attachments/assets/5ea20dc9-b606-484f-a57c-3a5f4d61ea7a)

_Пример отправки уведомления о начале стрима на платформе Discord._

---

#### 1.2 Работа с командами

**Скриншот: Вывод списка команд (Discord)**  
Бот может выводить динамический список доступных команд, который генерируется в зависимости от роли пользователя, текущей платформы и статуса команды.

![Вывод списка команд в Discord](https://github.com/user-attachments/assets/e239b3c4-66fe-494d-90f1-3b6799e35980)

_Пример использования команды `!команды`, выводящей список доступных команд._

---

#### 1.3 Контекстное общение и работа с изображениями

**Скриншот: Контекстное общение и решение капчи (Discord)**  
В этом примере бот подыгрывает пользователю в разговоре, делая вид, что понимает запрос решить капчу. Это демонстрирует способность бота запоминать контекст и вести непринужденные диалоги.

![Контекстное общение с ботом](https://github.com/user-attachments/assets/e7946608-9656-4407-9cdb-01ec53e126ac)

_Пример контекстного общения, где бот делает вид, что понимает капчу, и подыгрывает пользователю на платформе Discord._

**Скриншот: Оценка мема ботом (Discord)**  
Бот может анализировать изображения, такие как мемы, и отвечать на запросы пользователей, демонстрируя свою способность работать с мультимедийным контентом.

![Оценка изображения ботом](https://github.com/user-attachments/assets/84948a51-285c-4762-b29d-067639b47852)

_Пример того, как бот на платформе Discord анализирует и оценивает изображение, присланное пользователем._

---

#### 1.4 Игнорирование пользователя

**Скриншот: Игнорирование пользователя (Telegram)**  
Бот может самостоятельно решать игнорировать сообщения пользователей, если они надоедают ему, или по другим причинам. Это не функция модерации, а способ ограничить взаимодействие с пользователями.

![Игнорирование пользователя](https://github.com/user-attachments/assets/0a157230-9639-4c47-b43b-943dd538c08a)

_Пример работы бота на платформе Telegram и того, как бот может самостоятельно использовать свои же команды._

---

#### 1.5 Исправление раскладки и отслеживание прогресса

**Скриншот: Исправление раскладки и отслеживание прогресса (Twitch)**  
Бот автоматически исправляет неправильную раскладку клавиатуры и отслеживает прогресс игр на стримах. Например, бот может исправить введённую команду и выполнить её, показав текущий прогресс прохождения игры на стриме.

![Исправление раскладки и прогресс на Twitch](https://github.com/user-attachments/assets/c80ecbf2-39dc-4009-bac7-e4bd3fd83baf)

_Пример работы бота на платформе Twitch, где бот автоматически исправляет раскладку и выполняет команду `!прогресс`, показывающую текущий прогресс игры._

---

#### 1.6 Выполнение команд на платформе Vk Play Live

**Скриншот: Команда `!загугли` (Vk Play Live)**  
AbsoluteBot поддерживает выполнение команд на платформе Vk Play Live. Например, команда `!загугли` позволяет пользователям искать информацию в интернете прямо через чат.

![Команда !загугли на Vk Play Live](https://github.com/user-attachments/assets/fe0f23b4-b344-4076-8456-d6a5e7dccf9c)

_Пример работы бота на платформе Vk Play Live с выполнением команды `!загугли`._

---

### 2. Интеграция с внешними сервисами

**Скриншот: Таблица Google Sheets с играми (Google Sheets)**  
AbsoluteBot интегрируется с Google Sheets для автоматического обновления данных о прогрессе прохождения игр и ведения статистики по стримам.

![нтеграция с Google Sheets](https://github.com/user-attachments/assets/bdb8b109-5821-4d3e-b260-547b7d08cd04)

_Пример таблицы Google Sheets, где бот автоматически обновляет информацию о прогрессе игр, пройденных на стримах._


## Архитектура программы

Архитектура **AbsoluteBot** построена вокруг нескольких ключевых принципов и паттернов, обеспечивающих высокую гибкость и масштабируемость.

### 1. **Многоуровневая модульная структура**

Программа разделена на несколько модулей, каждый из которых отвечает за определённую функциональность, таких как работа с чатами, обработка команд, выполнение периодических задач и интеграция с внешними сервисами. Это делает систему легко расширяемой и поддерживаемой.

- **Chat Services (Сервисы чатов)**: Для каждой поддерживаемой платформы реализован отдельный сервис (`IChatService`), который отвечает за подключение, получение и отправку сообщений. Примеры: `VkPlayChatService`, `DiscordChatService`, `TelegramChatService`.
  
- **Command Services (Сервисы команд)**: Команды реализованы с использованием нескольких подходов. Некоторые команды наследуются от базовых классов, таких как `BaseCommand` или `BaseMediaCommand`, что обеспечивает общую логику выполнения. Однако другие команды, такие как `ExecuteExtraCommand` и `CommandsListCommand`, реализуют интерфейс `IChatCommand` напрямую, без наследования от базовых классов. Это позволяет гибко управлять командами и добавлять динамические или специфические команды, не изменяя общую структуру.

- **Background Services (Фоновые сервисы)**: Включает периодические задачи, такие как оповещения или проверка статуса потоков на Twitch.

### 2. **Внедрение зависимостей (Dependency Injection)**

Программа использует **Dependency Injection** (DI) через встроенный в .NET контейнер `Microsoft.Extensions.DependencyInjection`. Все основные сервисы и компоненты приложения, включая чаты, команды, службы модерации и логирование, регистрируются и управляются через DI. Это упрощает тестирование и настройку системы.

### 3. **Паттерн Command (Команда)**

В приложении реализован паттерн **Command**, где команды представляют собой объекты, которые могут быть зарегистрированы и выполнены через реестр команд (`CommandRegistry`). Важно отметить, что команды могут наследоваться от базовых классов (например, `BaseCommand`), которые предоставляют шаблон для выполнения и проверки команд, либо напрямую реализовывать интерфейс `IChatCommand`. Это даёт большую гибкость при создании как простых, так и сложных команд:

- **Наследуемые команды**: Команды, наследуемые от `BaseCommand` или `BaseMediaCommand`, используют встроенные механизмы для подготовки, выполнения и отправки результатов.
- **Самостоятельные команды**: Команды, такие как `ExecuteExtraCommand` и `CommandsListCommand`, реализуют интерфейс `IChatCommand` напрямую и содержат собственную логику выполнения.

### 4. **Observer (Наблюдатель)**

Программа использует паттерн **Observer** для управления событиями, такими как получение сообщений от пользователей в различных чатах. Для каждой платформы можно подписаться на событие `MessageReceived`, что позволяет гибко обрабатывать сообщения и маршрутизировать их к соответствующим обработчикам команд.

### 5. **Template Method (Шаблонный метод)**

Основная логика выполнения команд, таких как проверка параметров или подготовка сообщения, может задаваться в базовом классе `BaseCommand`. Однако не все команды следуют этому шаблону: некоторые реализуют свою логику выполнения напрямую через интерфейс `IChatCommand`. Это позволяет использовать шаблонный метод там, где это уместно, и давать больше свободы для динамических и специфических команд.

### 6. **Фоновая обработка и периодические задачи**

Для выполнения повторяющихся задач, таких как ежедневные уведомления или оповещения о стримах, используются фоновые службы. Эти службы инициализируются при старте приложения и работают параллельно с основным потоком, используя таймеры для выполнения задач.

### 7. **Логирование**

Все события и ошибки логируются через **Serilog**, что позволяет собирать данные по действиям и отслеживать проблемы. Логи разделены на несколько файлов по категориям, таким как события подключения к платформам и общие события бота.

## Гибкость конфигурации и сервисов

**AbsoluteBot** спроектирован таким образом, что бот может продолжать работу, даже если некоторые сервисы или платформы не настроены. Например:

- Если не настроен доступ к какой-либо платформе, бот продолжит работу с другими доступными платформами.
- Если для какого-либо сервиса не заданы конфигурационные параметры, бот продолжит работу с другими сервисами.

Это позволяет пользователю гибко настраивать бота под свои нужды и запускать его в окружении, где не все сервисы или платформы необходимы или доступны.

---

## Структура файлов

В процессе работы бот использует несколько конфигурационных и данных файлов, которые создаются и обновляются автоматически. Вот список файлов и их назначение:

- **user_roles.json** — хранит информацию о ролях пользователей. Если файл отсутствует, он генерируется пустым, и все пользователи автоматически получают роль **Default**. Для полной работы нужно вручную добавить роли администраторов и модераторов.
- **extra_commands.json** — содержит дополнительные команды, добавленные модераторами или администраторами через чат. Если файл отсутствует, он генерируется пустым.
- **commands.json** — хранит статусы команд (включена или отключена). Статусы команд можно изменять только через чат.
- **config.json** — конфигурационный файл, который содержит ключевые настройки. При первом запуске генерируется с пустыми значениями, которые нужно заполнить вручную.
- **game_progress.json** — хранит данные о прогрессе прохождения игр. Создаётся автоматически при первой добавленной игре.
- **holidays.json** — содержит информацию о праздниках. При первом запуске генерируется с двумя примерами праздников, остальные праздники нужно добавлять вручную.
- **user_birthdays.json** — хранит данные о днях рождения пользователей. Создаётся автоматически при добавлении первой даты рождения.
- **initialUserMessage.txt** — файл, содержащий начальный промпт для общения с нейросетью Gemini. Необходимо задать вручную.
- **initialModelMessage.txt** — хранит первое сообщение, отправляемое ботом в режиме общения через нейросеть Gemini.
- **wisdoms.json** — файл для хранения мудростей, добавленных через команды бота.
- **gamesRates.json** — хранит оценки игр от пользователей чата. Создаётся автоматически при первой выставленной оценке.
- **google_sheets_credentials.json** — файл, необходимый для авторизации в Google Sheets.
- **complaints.json** — хранит жалобы пользователей. Создаётся автоматически при добавлении первой жалобы.
- **auto_translate_users.json** — содержит список пользователей, сообщения которых нужно автоматически переводить на русский язык.
- **mbti_data.json** — хранит MBTI-данные пользователей. Создаётся автоматически при добавлении первой записи.
- **censor_words.json** — хранит список цензурируемых слов, добавленных через команды бота.

---

## Заключение

**AbsoluteBot** — это многофункциональный инструмент для стримеров и администраторов каналов, который помогает автоматизировать рутинные задачи, улучшать взаимодействие с аудиторией на различных платформах. Благодаря интеграции с нейросетями бот предоставляет уникальные возможности для общения и выполнения интерактивных команд, а также поддерживает множество систем для управления контентом, переводом и т.д. А благодаря гибкой архитектуре и поддержке нескольких платформ бот адаптируется под любые задачи и легко настраивается.
