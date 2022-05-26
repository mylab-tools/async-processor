# MyLab.AsyncProcessor
SDK: [![NuGet Version and Downloads count](https://buildstats.info/nuget/MyLab.AsyncProcessor.Sdk)](https://www.nuget.org/packages/MyLab.AsyncProcessor.Sdk)

Docker образ: [![Docker image](https://img.shields.io/docker/v/ozzyext/mylab-async-proc-api?sort=semver)](https://hub.docker.com/r/ozzyext/mylab-async-proc-api)

Спецификация API: [![API specification](https://img.shields.io/badge/OAS3-v2%20(actual)-green)](https://app.swaggerhub.com/apis/ozzy/my-lab_async_processor_api/2)  [![API specification](https://img.shields.io/badge/OAS3-v1-green)](https://app.swaggerhub.com/apis/ozzy/my-lab_async_processor_api/1.1)

```
Поддерживаемые платформы: .NET Core 3.1+
```
Ознакомьтесь с последними изменениями в [журнале изменений](/CHANGELOG.md).

## Обзор

`MyLab.AsyncProcessor` - платформа для разработки асинхронных обработчиков запросов с использованием микросервисной архитектуры.

![mylab-asyncproc-simple](./doc/diagramms/mylab-asyncproc-simple.png)

Архитектура асинхронного процессора подразумевает, что:

* взаимодействие происходит через `API`
* обработка запроса происходит в обработчике запросов бизнес-логики `BizProc`
* `BizProc`получает запросы по событийной модели
* `API` и `BizProc`могут быть масштабированы 

На диаграмме ниже изображено взаимодествие клиента с асинхронным процессором через `API`:

![](./doc/diagramms/mylab-asyncproc-sync-sequence.png)

Также опционально можно использовать событийную можель взаимодействия:

![](./doc/diagramms/mylab-asyncproc-event-sequence.png)

## Внутри

![](./doc/diagramms/mylab-asyncproc-struct.png)

Основные компоненты асинхронного процессора:

* `API` - обрабатывает входящие служебные запросы для подачи бизнес-запроса на обработку, получения статуса обработки, а также для получения результатов обработки;
* `BizProc` - бизнес-процессор. Выполняет обрабтку поступившего бизнес-запроса в соответствии со специической логикой;
* `Redis` - используется для временного хранения состояния выполнения запроса и информации о результате его выполнения;
* `RabbitMQ` - используется для организации событийной модели при передаче запросов от `API` к `BizProc`.

Особенности взаимодействия компонентов:

* `API` передаёт бизнес-запрос на обработку через очередь или через обменник. Запрос от клиента может содержать `routing-key`, который будет использоваться в случае с обменником;
* `BizProc` сообщает об изменении состояния обработки запроса напряму в `API`;
* `API` хранит состояние обработки запроса и результаты в `Redis`;
* обработка бизнес-запроса не обязательно должна выполняться в один поток в пределах `BizProc`. Но `BizProc`  ответственнен за начало этой обработки;
* если в настройках указан `Сallback` (обменник), то API при изменении состояния запроса будет отправлять сообщение в этот обменник с соответствующим содержанием. В качестве ключа ротуинга будет использоваться переданный в запросе параметр `callbackRouting`.

## Запрос

### Структура

```yml
CreateRequest:
      type: object
      description: Contains request parameters
      properties:
      	requestId:
          type: string
          description:  Defines predefined request id
          example: '54f5d4d35d47497a92386e46acc6d3be'
        procRouting:
          type: string
          description: Defines queue routing to send message to processor
          example: big
        callbackRouting:
          type: string
          description: Defines queue routing to send callback messages through callback exchange
          example: client1
        content: 
          type: object
          additionalProperties: true
          description: Request content
          example: { "value": "123", "dt": "10.10.2010"  }
      required:
      - content
```

### Жизненный цикл

![](./doc/diagramms/mylab-asyncproc-request-life.png)

### Время жизни запроса

В настройках `API` предусмотрено два параметра, влияющих на время жизни запроса:

* `ProcessingTimeout` - период, выделенный на обработку запроса. Если опистечении этого времени процессор не сообщил в `API` об окончании обработки, то запрос удаляется. Это приводит к истечению срока жизни записи в `Redis` и сообщения в `RabbitMQ`;
* `RestTimeout` - период действия данных о запросе, которые можно запросить через `API` после перехода запроса в состояние `Completed`. По истечение этого срока, записи о запросе в `Redis` экспирируются и `API` на все запросы по нему отвечает `404`.

## Статус запроса

### Содержание статуса

Статус запроса содержит следующие параметры:

* `processStep` - этап жизни запроса:
  * `pending`
  * `processing`
  * `completed`
* `bizStep` - этап обработки в терминах бизнес-процесса. Перечень значений зависит от конкурентной предметной области;
* `successful`- флаг, указывающий фактор успешности обработки запроса на шаге `Completed`. Если `false` - в процессе обработки, возникла ошибка, при этом должно быть заполнено поле `error`;
* `error` - содержит информацию об ошибке, возникшей в процессе обработки запроса;
  * `bizMgs` - сообщение бизнес-уровня, понятное пользователю;
  * `techMgs` - техническое сообщение. Обычно, сообщение из исключения;
  * `techInfo` - развёрнутое техническое описание ошибки. Обычно - stacktrace исключения;
* `resultSize` - размер результата обработки запроса;
* `resultMime` - `MIME` тип результата обработки запроса.

Подробнее - [тут](https://app.swaggerhub.com/apis/ozzy/my-lab_async_processor_api/1#/RequestStatus).

### Бизнес-шаг обработки (BizStep)

`BizStep` предназначен для того, чтобы детализировать этапы обработки запроса, специфичные для конкретной задачи. Эти шаги устанавливаются только в процессе обработки запроса, т.е. в то время, когда `processStep == processing`.

![](./doc/diagramms/mylab-asyncproc-biz-step.png)

## API асинхронного процессора

### Docker контейнер API

`API` доступно для развёртывания в качестве `docker` контейнера. Образ расположен на [docker-hub](https://hub.docker.com/r/ozzyext/mylab-async-proc-api).

Pull-команда:

```bash
docker pull ozzyext/mylab-async-proc-api
```

### Конфигурация API

`API` поддерживает [удалённый конфиг](https://github.com/ozzy-ext-mylab/remote-config#%D0%BA%D0%BE%D0%BD%D1%84%D0%B8%D0%B3%D1%83%D1%80%D0%B8%D1%80%D0%BE%D0%B2%D0%B0%D0%BD%D0%B8%D0%B5).

#### Настройки логики `AsyncProc`

Имя узла конфигурации - `AsyncProc`. Ниже приведён пример конфигурации:

```json
{
  "RedisKeyPrefix": "myorder-async-proc:requests:",
  "ProcessingTimeout": "01:00",
  "RestTimeout": "1",
  "QueueExchange": "myorder:proc-exchange",
  "QueueRoutingKey": "myorder:proc-queue",
  "DeadLetter": "myorder:dead-letter",
  "Callback": "myorder:callback-exchange"
}
```

, где:

* `RedisKeyPrefix` - префикс для ключей в Redis, в которых будет хранится состояние обработки запроса;
* `ProcessingTimeout` - период жизни запроса в одижании обработки. В формате `TimeSpan`;
* `RestTimeout` - период жизни запроса после перехода в конечное состояние `Completed` (после обработки). В формате `TimeSpan`; 
* `QueueExchange` - обменник`RabbitMQ`, в который будут подаваться сообщения для `BizProc`. Может быть не указан, если публикация должна осуществляться сразу в очередь; 
* `QueueRoutingKey` - очередь для публикации, если не указан `QueueExchange` или ключ маршрутизации (`routing key`) по умолчанию. Опциональный параметр; 
* `DeadLetter` - определяет имя обменника, куда будут направлены сообщения из основной очереди, если произойдут сбои обработки. Опциональный параметр;
* `Callback` - определяет обменник для отправки уведомлений об изменении состояния запроса.

Для параметров, содержащих период времени, формат значения должен соответствовать формату, поддерживаемому  `TimeSpan`:

* `6` - 6 дней;
* `03:00` - 3 часа;
* `00:01` - 1 минута.

Подробнее о форматах `TimeSpan` [тут](https://docs.microsoft.com/ru-ru/dotnet/api/system.timespan.parse?view=netcore-3.1). 

#### Настройки подключения к RabbitMQ

Нстройки подключения к `RabbitMQ` заключены в узле `Mq` и соответствуют параметрам конфигурации [MyLab.RabbitClient](https://github.com/mylab-tools/rabbit-client#%D0%BF%D0%B0%D1%80%D0%B0%D0%BC%D0%B5%D1%82%D1%80%D1%8B-%D0%BA%D0%BE%D0%BD%D1%84%D0%B8%D0%B3%D1%83%D1%80%D0%B0%D1%86%D0%B8%D0%B8).

#### Настройки подключения к Redis

Нстройки подключения к `Readis` заключены в узле `Redis` и соответствуют параметрам конфигурации [MyLab.RedisAdapter](https://github.com/mylab-tools/redis-adapter#%D0%BA%D0%BE%D0%BD%D1%84%D0%B8%D0%B3%D1%83%D1%80%D0%B0%D1%86%D0%B8%D1%8F).

#### Настройки OpenTelemetry

Настройки `OpenTelemetry` заключены в узле `Otlp` и расширяют [базовые настройки экспортёра телеметрии](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Exporter.OpenTelemetryProtocol/README.md#options-properties). 

Основные базовые параметры:

* `Endpoint` - конечная точка, куда экспортёр будет отправялть телеметрию;
* `Protocol ` - протокол передачи телеметрии:
  * `grpc` for protobuf-encoded data using gRPC wire format over HTTP/2 connection;
  * `http/protobuf` for protobuf-encoded data over HTTP connection;
  * `http/json` for JSON-encoded data over HTTP connection.

Специфичные параметры `AsyncProc`:

* `Enabled` - `true`/`false`, включена/выключена сборка телеметрии;

* `ServiceName` - имя сервиса, которе будет фигурировать в экспортированной телеметрии.

### Событийная модель

API асинхронного процессора поддерживает событийную модель взаимодействия. При изменении состояния запроса будет отправляться сообщение в обменник с соответствующим содежанием. Обменник указывается через конфигурацию - праметр `Callback`. Если требуется указать роутинг для обменникка, то необходимо указать его значение в запросе в поле `callbackRouting` .

Инициирующие события:

* изменился шаг обработки запроса
* изменился бизнес-шаг обрабтки запроса
* обработка завершена с ошибкой
* обработка завершена с результатом

Схема сообщения:

![](./doc/callback-msg-schema.png)

Схема сообщения - [тут](./doc/callback-msg-schema.json).

## Разработка процессора бизнес-запросов (BizProc)

### Порядок действий 

Для разработки процессора бизнес-запросов необходимо:

* Создать проект `.NET Core Web API`;

* Добавить `nuget` [MyLab.AsyncProcessor.Sdk](https://www.nuget.org/packages/MyLab.AsyncProcessor.Sdk);

* Добавить сборку с классом - моделью бизнес-запроса, который будет направляться в `API` для обработки;

* Разработать логику обработки запроса, реализующую интерфейс  `IAsyncProcessingLogic<T>`, где T - класс-модель бизнес-запроса;

* В методе конфигурирования приложения, интегрировать логику асинхронного процессора:

  ```C#
   public class Startup
   {
       public Startup(IConfiguration configuration)
       {
           Configuration = configuration;
       }
  
       public IConfiguration Configuration { get; }
       
       public void ConfigureServices(IServiceCollection services)
       {
           //...
           
           services.AddAsyncProcessing<TestRequest, ProcessingLogic>(Configuration);
       }
   }
  ```

  , где:

  * `TestRequest` - класс-модель бизнес-запроса;
  * `ProcessingLogic` -  логику обработки запроса.

### Логика обработки

Класс логики обработки должен реализовывать интерфейс `IAsyncProcessingLogic<T>`. Интерфейс определяет один метод:

```C#
Task ProcessAsync(AsyncProcRequest<T> request, IProcessingOperator op);
```

, где:

* `request` - объект запроса асинхронной обработки:
  * `Id` - идентификатор запроса, назначенный для асинхронной обработки;
  * `IncomingDt` - дата и время поступления запроса;
  * `Content` - содержательная часть исходного запроса, десериализованная в тип `T`;
  * `Headers` - заголовки исходного запроса;
* `op` - объект предоставляющий необходимые средства для изменений состояния запроса.

`IProcessingOperator` предоставляет следующие методы:

* `SetBizStepAsync` - устанавливает [этап обработки бизнес-уровня](#бизнес-шаг-обработки-bizstep);
* `CompleteWithErrorAsync` - завершает обработку запроса с ошибкой;
* `CompleteWithResultAsync` - завершает обработку запроса с результатом;
* `CompleteAsync `  - завершает обработку запроса без результата. Используется в случае, когда выполнение запроса не подразумевает возврат какого-либо результата.

### Необработанные ошибки

Механизм, использующий логику обработки бизнес-процессов при вызове метода `ProcessAsync` перехватывает необработанные исключения и сам завершает обработку запроса с ошибкой. Поэтому делать это в объекте логике не обязательно:

```C#
//Нет необходимости так делать!

class ProcessingLogic : IAsyncProcessingLogic<TestRequest>
{
    public Task ProcessAsync(AsyncProcRequest<TestRequest> request, IProcessingOperator op)
    {
        try
        {
        	//...
        }
        catch (Exception e)
        {
        	return op.CompleteWithErrorAsync(e);
        }
    }
}
```

## Проверка работоспособности

Проверка рбаотоспособности осуществляется через механизм `HealthCheck`. Результат проверки доступен по относительному адресу `/health`.

Пример запроса:

```http
GET http://localhost/health
```

Пример ответа:

```http
200 OK

Content-Type: text/plain

Healthy
```

