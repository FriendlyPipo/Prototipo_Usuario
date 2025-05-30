# Pasos Detallados para Pruebas Unitarias y Cobertura de Código

Esta guía te llevará paso a paso a través del proceso de ejecutar pruebas, generar datos de cobertura, crear reportes HTML detallados y visualizar la cobertura directamente en VS Code.

**Asunciones Iniciales:**
* Tienes .NET SDK instalado.
* Tu proyecto de pruebas con xUnit y tus archivos de código fuente ya existen.
* Visual Studio Code está instalado y configurado con la extensión C#.
* El paquete NuGet `coverlet.collector` está añadido a tu proyecto de pruebas.


**Paso 1: Instalar Herramientas Adicionales**

1.  **Instalar ReportGenerator (Herramienta Global):**
    Si deseas reportes HTML detallados, instala `ReportGenerator` globalmente (si aún no lo has hecho):
    ```bash
    dotnet tool install -g dotnet-reportgenerator-globaltool
    ```

2.  **Instalar Extensión "Coverage Gutters" en VS Code:**
    1.  Abre VS Code.
    2.  Ve al panel de Extensiones (`Ctrl+Shift+X`).
    3.  Busca e instala "Coverage Gutters" de ryanluker.

**Paso 2: Configurar la Recolección de Cobertura (`coverlet.runsettings`)**

1.  **Crear/Verificar el archivo `coverlet.runsettings`:**
    Este archivo debe estar en la raíz de tu proyecto de pruebas o de la solución. Controla cómo Coverlet recolecta la cobertura.

2.  **Asegurar la siguiente configuración en `coverlet.runsettings`:**
    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <RunSettings>
      <DataCollectionRunSettings>
        <DataCollectors>
          <DataCollector friendlyName="XPlat Code Coverage">
            <Configuration>
              <Format>lcov</Format> <Include>[Users.Domain]*,[Users.Application]*,[Users.Core]*,[Users.Infrastructure]*</Include>
              <Exclude>[*Test*]*,[*Tests*]*,[*.Test]*,[xunit.*]*,[Moq*]*,[Microsoft.*]*</Exclude>
              <UseSourceLink>false</UseSourceLink> </Configuration>
          </DataCollector>
        </DataCollectors>
      </DataCollectionRunSettings>
    </RunSettings>
    ```
    * **Nota Clave:** Ajusta `<Include>` con los nombres de tus ensamblados. Si la cobertura de clases POCO (como `MongoUserDocument`) no aparece, prueba comentando `CompilerGeneratedAttribute` de la línea `<ExcludeByAttribute>`.

**Paso 3: Configurar "Coverage Gutters" en VS Code (si es necesario)**

1.  Abre tu archivo `settings.json` de VS Code (`Ctrl+Shift+P` -> "Preferences: Open User Settings (JSON)" o "Preferences: Open Workspace Settings (JSON)").
2.  Asegúrate de que `coverage.info` esté en la lista de archivos que busca la extensión (normalmente lo encuentra si usas formato LCOV y el archivo se genera con ese nombre o `lcov.info`):
    ```json
    "coverage-gutters.coverageFileNames": [
        "coverage.info", // Añade si tu archivo se llama así y no es lcov.info por defecto
        "lcov.info",
        // ...otros nombres por defecto
    ]
    ```
3.  Guarda `settings.json` y reinicia VS Code si hiciste cambios.

---
## Sección II: Ciclo de Pruebas y Cobertura (Pasos a Repetir)

Estos son los pasos que realizarás cada vez que quieras analizar la cobertura.

**Paso 4: Ejecutar Pruebas y Generar Datos de Cobertura**

1.  Abre una terminal en la raíz de tu proyecto de pruebas o de tu solución.
2.  Ejecuta el siguiente comando:
    ```bash
    dotnet test --settings coverlet.runsettings --collect:"XPlat Code Coverage"
    ```
3.  **Verificación:** Este comando generará un archivo `coverage.info` dentro de una subcarpeta en el directorio `TestResults` de tu proyecto de pruebas (ej. `TuProyectoDePruebas/TestResults/{GUID}/coverage.info`). Confirma que se crea.

**Paso 5: Generar Reporte HTML Detallado (Opcional, para análisis profundo)**

1.  Asegúrate de haber completado el Paso 4 y tener el archivo `coverage.info`.
2.  Desde una ubicación conveniente en tu terminal (ej. la raíz de tu solución), ejecuta:
    ```bash
    # Asume que el proyecto de pruebas está en 'Users.Test' relativo a tu ubicación actual
    # Ajusta las rutas según sea necesario
    dotnet tool run reportgenerator "-reports:TestResults/**/coverage.info" "-targetdir:Reports" "-reporttypes:Html"
    ```
    * `-reports:` Debe apuntar a tu archivo `coverage.info`. El `**` ayuda a encontrarlo.
    * `-targetdir:` Especifica la carpeta de salida para el reporte.
3.  **Ver el Reporte:** Abre el archivo `index.html` ubicado en la carpeta especificada en `-targetdir` (ej. `Reports/index.html`) en un navegador web. Aquí verás el porcentaje de cobertura total por proyecto/ensamblado.

**Paso 6: Visualizar Cobertura Directamente en VS Code**

1.  Asegúrate de haber completado el Paso 4 y tener el archivo `coverage.info` generado.
2.  Abre en VS Code un archivo de código fuente de tu proyecto de producción (ej. `User.cs`).
3.  Abre la Paleta de Comandos (`Ctrl+Shift+P` o `Cmd+Shift+P` en Mac).
4.  Escribe y selecciona una de las siguientes opciones de "Coverage Gutters":
    * `Coverage Gutters: Display Coverage` (para mostrar la cobertura actual)
    * `Coverage Gutters: Watch` (para que se actualice automáticamente al cambiar el archivo `coverage.info`)
5.  Deberías ver indicadores de cobertura (colores) en el margen izquierdo del editor.

---
## Sección III: Solución Rápida de Problemas Comunes

* **Coverage Gutters no muestra nada / Error "Could not find a Coverage file!":**
    * Verifica la generación y ruta de `coverage.info` (Paso 4.3).
    * Confirma la configuración de `coverage-gutters.coverageFileNames` en VS Code (Paso 3.2).
    * Revisa el panel "Salida" de VS Code (seleccionando "Coverage Gutters") para mensajes de error.
    * Confirma `<UseSourceLink>false</UseSourceLink>` en `coverlet.runsettings` (Paso 2.2).
* **Archivo/Clase aparece como "Uncovered" o 0% en reportes:**
    * **Filtro `<Include>` en `coverlet.runsettings`:** El ensamblado debe estar correctamente listado (Paso 2.2).
    * **`<ExcludeByAttribute>`:** Para POCOs, prueba comentando `CompilerGeneratedAttribute` (Paso 2.2).
    * **Falta de Pruebas:** Asegúrate de que haya pruebas que ejecuten ese código.
* **ReportGenerator reporta "found no matching files":**
    * La ruta en `-reports:` es incorrecta (Paso 5.2). Verifica tu ubicación en la terminal y la ruta al `coverage.info`.