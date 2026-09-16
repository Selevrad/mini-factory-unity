# Mini Factory

Небольшая мобильная idle/incremental-игра: открывай и улучшай производственные машины,
используй временный буст производства, копи валюту в офлайне и покупай
пак валюты через Unity IAP.

**Готовый APK** (не нужно собирать самому, чтобы просто попробовать):
[Mini Factory v0.1.0](https://github.com/Selevrad/mini-factory-unity/releases/tag/v0.1.0).

## Запуск

1. Открой проект в **Unity `6000.5.5f1`**.
2. Открой `Assets/Scenes/SampleScene.unity`.
3. Нажми Play. Сцена полностью настроена — дополнительных действий не
   требуется.
4. Покупки идут через встроенный в Unity IAP **Fake Store** — как в
   редакторе, так и в Android-сборке (реальный аккаунт стора или продукт
   не нужны). Подтверждено рабочим и в редакторе, и на реальном
   Android-телефоне.

## Android-сборка

Активная платформа сборки проекта — уже **Android**, с параметрами:

- Scripting Backend: **IL2CPP**
- Target Architectures: **ARM64 + ARMv7**
- Минимальный API level: **26** (Android 8.0 — нижняя граница, которую
  требует эта версия редактора)
- Ориентация по умолчанию: **Portrait**
- Active Input Handling: **Both** (см. «Известные ограничения» — новая
  Input System управляет игровым UI, но окно Fake Store из Unity IAP
  получает клики только через старый (legacy) backend)

Чтобы собрать: **File > Build Settings** (список сцен и платформа уже
настроены) **> Build**.

## Архитектура

```
Assets/MiniFactory/Scripts/
  Config/    - MachineDefinition, EconomyConfig (ScriptableObject), IConfigProvider, LocalConfigProvider
  Domain/    - Factory, Machine, SaveData - чистый C#, без зависимости от UnityEngine/MonoBehaviour
  Services/
    Save/    - ISaveService, JsonFileSaveService
    Analytics/ - IAnalyticsProvider, AnalyticsService, ConsoleAnalyticsProvider
    IAP/     - IPurchasingService, UnityPurchasingService
  Runtime/   - GameBootstrap (composition root + мобильный lifecycle), FactoryUIController,
               MachineRowView, SafeArea
Assets/MiniFactory/Tests/EditMode/ - FactoryTests (6 тестов)
```

**Почему так:** `Factory`/`Machine` держат всю экономическую математику
(открытие, улучшение, производство, буст, офлайн-прогресс) как обычный
C# без единой зависимости от Unity API — поэтому их легко покрыть
юнит-тестами, и они не могут случайно обзавестись зависимостью от
MonoBehaviour. Всё остальное — небольшой интерфейс (`IConfigProvider`,
`ISaveService`, `IAnalyticsProvider`, `IPurchasingService`) с ровно одной
реализацией на сегодня, так что конкретную интеграцию (Remote Config,
реальный analytics SDK, другой backend сохранений) можно подставить позже
без изменения игровой логики. `GameBootstrap` — единственный composition
root, который связывает всё это вместе и владеет частями, которым
*действительно* нужен MonoBehaviour — тиком производства каждый кадр и
сохранением через `OnApplicationPause`/`OnApplicationQuit`.

Никакого DI-фреймворка, никаких event-шин на ScriptableObject, никакого
общего слоя «менеджер менеджеров» — игра достаточно маленькая, чтобы
обычная инъекция через конструктор из одного composition root покрывала
всё без добавления фреймворка.

### Формулы экономики

- **Производство**: `baseProduction * productionGrowthPerLevel ^ (level - 1)`,
  суммируется по всем открытым машинам, умножается на множитель буста,
  когда буст активен.
- **Стоимость улучшения**: `baseUpgradeCost * upgradeCostGrowthPerLevel ^ (level - 1)`
  — *первое* улучшение после открытия стоит ровно `baseUpgradeCost`.
- **Офлайн-прогресс**: реально прошедшее время с последнего сохранения,
  ограниченное `maxOfflineSeconds`; множитель буста применяется только к
  той части этого окна, где буст действительно был активен — согласно
  «boost учитывает реально прошедшее время» из ТЗ.

### Путь Config -> Remote Config

Игровой код читает конфигурацию только через `IConfigProvider`.
`LocalConfigProvider` сегодня оборачивает ScriptableObject `EconomyConfig`;
`RemoteConfigProvider` (или композит, накладывающий remote-значения на
локальные как fallback) может реализовать тот же интерфейс позже без
изменения `Factory` или `GameBootstrap`.

## Использованные пакеты / SDK

- `com.unity.purchasing` **5.4.3** — Unity IAP v5, через актуальный API
  `StoreController` / `UnityIAPServices` (не устаревший `IStoreListener`).
  Проверено на встроенном Fake Store.
- `com.unity.inputsystem` — управляет всем игровым UI-инпутом.
- `com.unity.render-pipelines.universal` (URP) — из базового шаблона;
  собственный рендеринг не добавлялся.
- `com.unity.test-framework` — EditMode-юнит-тесты.
- Стандартный uGUI (`UnityEngine.UI`, legacy `Text`/`Image`/`Button`) для
  всего UI — без TextMeshPro, без внешних арт-ассетов.

## Известные ограничения

**Статус проверки сборки/устройства:** геймплей, UI и IAP проверялись
через Unity Editor Play Mode с активной платформой Android (симуляция
портретного телефона класса 1080x2340 с safe-area отступом), а также на
**настоящем Android-телефоне** через реальный APK, собранный из этого
репозитория.

## Затраченное время и дальнейшие шаги

Проект делался интерактивно с AI-агентом (см. `AI_USAGE.md`).

**Что осталось и что бы я сделал следующим шагом:**

1. Убрать `com.unity.ai.assistant` / `com.unity.ai.inference` (Sentis) —
   неиспользуемые остатки шаблона, на которые приходится почти всё 973
   предупреждения сборки и, вероятно, заметная часть размера APK (~73 МБ).
2. Поменять Android package name с дефолтного шаблонного перед любым
   реальным распространением.
3. Выбрать и подключить один бонусный пункт — Firebase Remote Config
   выглядит самым полезным, учитывая, что точка расширения
   `IConfigProvider` для этого уже готова.
4. Добавить пару Play Mode тестов на мобильный lifecycle
   (pause -> resume -> применение офлайн-дохода) — это усилило бы покрытие
   сверх чисто логических EditMode-тестов.
