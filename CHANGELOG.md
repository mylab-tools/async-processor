# Лог изменений

Все заметные изменения в этом проекте будут отражаться в этом документе.

Формат лога изменений базируется на [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [1.8.12] - 2023-03-10

### Исправлено

* не устанавливался флаг `successful` у запроса при успешном завершении без результата

## [1.8.11] - 2023-02-01

### Добавлено

* добавляется идентификатор запроса в каждую запись лога в контексте обработки запроса в обработчике

### Изменено

* обновлены зависимости логирования и работы с `RabbitMQ`

## [1.8.10] - 2022-05-26

### Добавлено

* `IncomingDt` - дата и время поступления запроса. Поле доступно в объектной модели запроса в логике обработчика.

## [1.7.10] - 2022-02-24

### Добавлено

* сбор и отправка телеметрии в составе трассировок

### Изменено

* обновлены зависимости, в т.ч. `MyLab.Log` для логирования трассировок

## [1.6.8] - 2021-12-07

### Добавлено

* APIv2 с поддержкой предоставления детального объекта запроса с возможностью включения в него результата выполнения запроса

## [1.5.8] - 2021-09-21

### Изменено

* обновлён `MyLab.RabbitClient` с целью изменения работы с каналами `Rabbit`

## [1.5.7] - 2021-09-16

### Добавлено

* в `SDK` в контракте сервиса добавлена возможность передавать дополнительные заголовки при создании запроса

## [1.4.7] - 2021-09-16 

### Исправлено

* ошибка добавления потребителя очереди `dead letter`, если она не указана в конфигурации

## [1.4.6] - 2021-09-16 

### Изменено 

* подключение к `Redis` в режиме `Background` 
* подключение к `Rabbit` в режиме `Background` 

### Добавлено

* проверка работоспособности `health-check` 

## [1.3.5] - 2021-09-09 

### Изменено 

* переход `MyLab.Mq` -> `MyLab.RabbitClient` 

### Удалено

* настройки времени жизни запроса `MaxStoreTime` и `MaxIdleTime`

### Добавлено

* настройки времени жизни запроса `RestTimeout` и `ProcessingTimeout`
* идентификатор запроса и заголовки исходного `HTTP` запроса в объекте запроса для логики обработки в процессоре

## [1.2.5] - 2021-07-28

### Добавлено

* предусмотрена адекватная реакция процессора на отсутствие запроса в `Redis` - прекращение обработки + `warining` в лог

### Исправлено

* урезание содержимого результата работы процессора при больших объёмах данных 
* не устанавливалась экспирация при создании ключа с именем callback-очереди запроса  

## [1.2.3] - 2021-02-08

### Изменено:

* обновление библиотек `MyLab.Mq` до `v1.7.13` и `MyLab.StatusProvider` до `v1.5.9`

## [1.2.2] - 2021-02-04

### Добавлено

* Добавлена возможность при подаче запроса устанавливать предустановленный идентификатор.

## [1.1.2] - 2020-12-11

### Добавлено

* Установка таймаута для `Redis`-ключа с именем `callback`-очереди

## [1.1.1] - 2020-11-25

### Исправлено

* Подход к `Callback` уведомлениям. Теперь это `Exchange`, а роутинг указывается в запросе.

## [1.1.0] - 2020-11-24

### Добавлено

* Поддержка `ErrorId` в контракте ошибки обработки `ProcessingError`
* Поддержка `Callback` очереди для уведомлений об изменении состояния запроса