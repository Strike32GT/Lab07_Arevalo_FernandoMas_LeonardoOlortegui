# 📚 Sistema de Gestión de Biblioteca

Aplicación de escritorio desarrollada para gestionar autores, libros, socios y préstamos de una biblioteca mediante una **arquitectura de capas**, separando la presentación, las reglas de negocio, el acceso a datos y las entidades del sistema.

El proyecto corresponde al laboratorio **Semana 07 – Arquitectura de Capas en el Proceso de Datos**, desarrollado para el curso **Desarrollo de Aplicaciones Empresariales Avanzadas**.

---

## 👥 Integrantes

* **Fernando Mas**
* **Leonardo Olortegui**

---

## 🎯 Objetivo del proyecto

Desarrollar un sistema de gestión de biblioteca que permita administrar libros y socios, registrar préstamos y devoluciones, controlar ejemplares disponibles y generar reportes de préstamos.

El sistema aplica una arquitectura de capas para mantener separadas las responsabilidades de cada parte de la aplicación y centralizar las reglas de negocio en una única capa.

---

## 🏗️ Arquitectura del proyecto

La solución está organizada en cuatro proyectos:

```text
Biblioteca
│
├── Biblioteca.Entidades
│   └── Clases que representan las entidades del sistema
│
├── Biblioteca.Datos
│   └── Acceso a SQL Server y operaciones CRUD
│
├── Biblioteca.Negocio
│   └── Reglas y validaciones del sistema
│
└── Biblioteca.WPF
    └── Interfaz gráfica de usuario
```

### Dependencias entre proyectos

```text
Biblioteca.Entidades
        ↑
        │
Biblioteca.Datos
        ↑
        │
Biblioteca.Negocio
        ↑
        │
Biblioteca.WPF
```

Las referencias se mantienen de la siguiente manera:

| Proyecto               | Referencias         |
| ---------------------- | ------------------- |
| `Biblioteca.Entidades` | Ninguna             |
| `Biblioteca.Datos`     | Entidades           |
| `Biblioteca.Negocio`   | Datos + Entidades   |
| `Biblioteca.WPF`       | Negocio + Entidades |

La capa `WPF` no accede directamente a `Biblioteca.Datos` ni utiliza tipos de `SqlClient`.

---

# 🗄️ Base de datos

La aplicación utiliza una base de datos denominada:

```text
BibliotecaDB
```

### Tablas principales

#### Autores

```text
AutorId
Nombre
Nacionalidad
Activo
```

#### Libros

```text
LibroId
Titulo
ISBN
AutorId
Ejemplares
Activo
```

#### Socios

```text
SocioId
DNI
Nombre
Email
Activo
```

#### Prestamos

```text
PrestamoId
SocioId
FechaPrestamo
FechaLimite
Estado
```

#### DetallePrestamo

```text
PrestamoId
LibroId
FechaDevolucion
```

`DetallePrestamo` utiliza una **clave primaria compuesta** y `FechaDevolucion` permite valores `NULL`.

Los campos `Activo` utilizan el tipo `BIT` y tienen como valor predeterminado `1`. Además, `ISBN` y `DNI` son valores únicos.

---

# 📦 Datos de prueba

La base de datos cuenta con datos iniciales para realizar las pruebas del sistema:

* 8 autores.
* 20 libros.
* 10 socios.
* 5 préstamos con sus respectivos detalles.
* Un socio con 3 libros pendientes para comprobar las restricciones de préstamos.

---

# 🧩 Capas del sistema

## 1. Biblioteca.Entidades

Contiene las clases que representan los datos utilizados por el sistema:

* `Autor`
* `Libro`
* `Socio`
* `Prestamo`
* `DetallePrestamo`

Estas clases contienen únicamente propiedades y no incluyen acceso a datos ni reglas de negocio.

---

## 2. Biblioteca.Datos

Esta capa se encarga de la comunicación con la base de datos.

Cada entidad cuenta con sus operaciones correspondientes de lectura y escritura.

Las consultas se ejecutan utilizando parámetros (`SqlParameter`) para evitar concatenaciones directas de valores.

La capa de Datos:

* Accede a SQL Server.
* Ejecuta consultas y operaciones de persistencia.
* Devuelve entidades o `List<T>`.
* No devuelve `SqlDataReader` ni `DataTable`.
* No contiene reglas de negocio.

La transacción utilizada al registrar préstamos garantiza que la operación sea atómica: se guardan la cabecera, los detalles y el descuento de ejemplares como una sola operación.

---

## 3. Biblioteca.Negocio

Esta capa concentra las reglas de negocio del sistema.

Se implementan las siguientes clases:

```text
LibroNegocio
SocioNegocio
PrestamoNegocio
```

Cuando una regla no se cumple, se genera una excepción propia, como:

```text
ReglaNegocioException
```

La interfaz WPF recibe únicamente los mensajes correspondientes a las reglas de negocio y no necesita capturar directamente `SqlException`.

---

## 4. Biblioteca.WPF

Es la capa de presentación del sistema.

Desde esta aplicación se realizan las operaciones de:

* Mantenimiento de libros.
* Mantenimiento de socios.
* Registro de préstamos.
* Registro de devoluciones.
* Búsquedas.
* Reporte de préstamos.
* Visualización de multas.

La interfaz se comunica con la capa de Negocio y no accede directamente a la base de datos.

---

# 📚 Reglas de negocio

## 📖 Libros

La gestión de libros contempla:

* El título es obligatorio.
* El título no puede superar los 150 caracteres.
* El ISBN es obligatorio.
* El ISBN no puede superar los 20 caracteres.
* No se permiten ISBN repetidos.
* El autor debe existir.
* El autor debe encontrarse activo.
* Los ejemplares no pueden ser negativos.
* Un libro con préstamos pendientes no puede darse de baja.
* La eliminación se realiza de forma lógica mediante `Activo = 0`.

Estas reglas pertenecen a la capa de Negocio porque representan políticas propias del funcionamiento de la biblioteca.

---

## 👤 Socios

Para los socios se aplican las siguientes validaciones:

* El nombre es obligatorio.
* El nombre no puede superar los 100 caracteres.
* El DNI es obligatorio.
* El DNI debe contener exactamente 8 dígitos numéricos.
* No se permiten DNI repetidos.
* El correo electrónico es opcional.
* Si se registra un correo, debe tener un formato válido.
* El correo no puede superar los 100 caracteres.
* Un socio con préstamos pendientes no puede darse de baja.
* La eliminación es lógica mediante `Activo = 0`.

---

## 📕 Préstamos

Para registrar un préstamo:

* Debe existir un socio.
* Debe existir al menos un libro.
* No se puede repetir el mismo libro dentro del préstamo.
* Un socio no puede superar los 3 libros pendientes.
* No se pueden prestar libros dados de baja.
* No se pueden realizar préstamos a socios dados de baja.
* No se puede prestar un libro sin ejemplares disponibles.
* La fecha límite se calcula desde la fecha del préstamo.
* La cabecera, los detalles y el descuento de ejemplares se guardan dentro de una única transacción.

La fecha límite y las validaciones correspondientes son responsabilidad de `PrestamoNegocio`.

---

## 🔄 Devoluciones y multas

Al registrar una devolución:

1. Se registra la fecha de devolución.
2. Se devuelve el ejemplar al stock.
3. Se calcula la multa correspondiente.
4. Si ya no existen libros pendientes, el préstamo cambia a estado `Devuelto`.

La multa establecida por el sistema es:

```text
S/ 1.50 por cada día de retraso
```

La multa se calcula en la capa de Negocio y no se almacena directamente en la base de datos.

---

# 🖥️ Funcionalidades

## Gestión de libros

Permite:

* Registrar libros.
* Actualizar libros.
* Dar de baja libros.
* Buscar por título.
* Buscar por autor.
* Visualizar el nombre del autor.
* Controlar la cantidad de ejemplares.

## Gestión de socios

Permite:

* Registrar socios.
* Actualizar socios.
* Dar de baja socios.
* Buscar por nombre.
* Buscar por DNI.

## Gestión de préstamos

Permite:

* Seleccionar un socio.
* Agregar uno o varios libros.
* Validar disponibilidad.
* Validar límite de préstamos.
* Registrar el préstamo.
* Actualizar los ejemplares disponibles.

## Gestión de devoluciones

Permite:

* Registrar la devolución.
* Actualizar el stock.
* Calcular la multa.
* Actualizar el estado del préstamo.

## Reportes

Se implementa un reporte de préstamos por intervalo de fechas que muestra:

* Socio.
* Libros.
* Fecha límite.
* Estado.

El reporte obtiene la información mediante un `INNER JOIN` entre:

```text
Prestamos
DetallePrestamo
Libros
Socios
```

---

# 🔐 Eliminación lógica

El sistema no elimina físicamente los registros de libros ni socios.

En lugar de utilizar:

```sql
DELETE
```

se modifica el estado:

```text
Activo = 0
```

Esto permite conservar el historial de préstamos y mantener la integridad de las relaciones existentes.

De esta manera, un libro o socio dado de baja deja de estar disponible para nuevas operaciones, pero sus registros históricos permanecen almacenados.

---

# 🔄 Transacciones

El registro de un préstamo utiliza una transacción para garantizar la consistencia de los datos.

La operación comprende:

```text
Registrar préstamo
       ↓
Registrar detalles
       ↓
Descontar ejemplares
       ↓
Confirmar transacción
```

Si alguna operación falla:

```text
ROLLBACK
```

y no se guarda ninguna parte del préstamo.

Esto permite evitar situaciones donde, por ejemplo, el préstamo quede registrado pero los ejemplares no sean descontados correctamente.

---

# ⚡ Programación asíncrona

Para evitar que la interfaz se bloquee durante las operaciones de acceso a datos, las operaciones realizadas desde WPF utilizan `async/await`.

No se utilizan:

```csharp
.Result
.Wait()
```

Las llamadas se esperan de manera asíncrona desde los eventos de la interfaz, permitiendo que la ventana continúe respondiendo mientras se realizan las operaciones.

---

# ⚙️ Configuración

La cadena de conexión se encuentra en el archivo:

```text
App.config
```

del proyecto:

```text
Biblioteca.WPF
```

Esto permite que `ConfigurationManager` pueda obtener la configuración de conexión durante la ejecución de la aplicación.

> **Importante:** configurar la cadena de conexión de acuerdo con la instancia local de SQL Server antes de ejecutar el proyecto.

---

# 🧠 ¿Por qué las reglas están en Negocio?

Las reglas del sistema se encuentran en la capa de Negocio porque representan decisiones propias de la biblioteca y no operaciones técnicas de almacenamiento.

Por ejemplo:

> Un socio no puede tener más de 3 libros pendientes.

Esta condición seguirá siendo válida independientemente de si los datos se almacenan en SQL Server, archivos u otro sistema.

Por esta razón, no corresponde colocar estas reglas en Datos ni repetirlas dentro de cada formulario de WPF.

La capa de Presentación únicamente recopila los datos, llama a Negocio y muestra el resultado o el mensaje de error correspondiente.

---

# 🧪 Pruebas y problemas encontrados

Durante las pruebas con la base de datos real se identificaron algunos problemas que no fueron detectados únicamente mediante la compilación:

* Era necesario abrir correctamente la conexión antes de iniciar una transacción.
* El parámetro correspondiente al identificador del libro no se estaba registrando correctamente.
* Algunos errores solo pudieron detectarse durante la ejecución contra la base de datos.

Esto permitió comprobar la importancia de realizar pruebas de ejecución y no limitarse únicamente a verificar que el proyecto compile correctamente.

---

# 📝 Observaciones

Durante el desarrollo se identificó que colocar la interfaz del repositorio en Negocio, tal como sugería el enunciado, generaba una referencia circular entre `Datos` y `Negocio`.

Por este motivo, la interfaz quedó ubicada en la capa de Datos, manteniendo el objetivo de facilitar las pruebas del repositorio sin depender directamente de la base de datos.

También se comprobó que, debido a que la capa de Datos trabaja de manera síncrona con el proveedor SQL, las pantallas deben ejecutar estas operaciones fuera del hilo de interfaz y esperarlas mediante `await` para evitar que la ventana se congele.

---

# 📌 Conclusiones

El desarrollo permitió comprobar que una arquitectura por capas no consiste únicamente en separar el código en proyectos, sino en definir correctamente la responsabilidad de cada capa.

La centralización de las reglas en Negocio permitió evitar validaciones repetidas en los formularios y mantener un comportamiento uniforme en las diferentes pantallas.

También se comprobó la importancia de la eliminación lógica para conservar el historial de préstamos y evitar problemas con las relaciones de la base de datos.

Finalmente, el uso de `async/await` permitió mantener la interfaz WPF responsiva durante las operaciones de consulta y persistencia de datos.

---

# 📁 Estructura general

```text
Biblioteca/
│
├── Biblioteca.Entidades/
│   ├── Autor.cs
│   ├── Libro.cs
│   ├── Socio.cs
│   ├── Prestamo.cs
│   └── DetallePrestamo.cs
│
├── Biblioteca.Datos/
│   ├── AutorDatos.cs
│   ├── LibroDatos.cs
│   ├── SocioDatos.cs
│   ├── PrestamoDatos.cs
│   └── ...
│
├── Biblioteca.Negocio/
│   ├── LibroNegocio.cs
│   ├── SocioNegocio.cs
│   ├── PrestamoNegocio.cs
│   └── ReglaNegocioException.cs
│
└── Biblioteca.WPF/
    ├── App.config
    ├── Views/
    ├── Windows/
    └── ...
```

> La estructura anterior representa la organización por capas del sistema. Los nombres concretos de archivos pueden variar según la implementación realizada en el repositorio.

---

# 🚀 Ejecución del proyecto

### 1. Clonar el repositorio

```bash
git clone https://github.com/Strike32GT/Lab07_Arevalo_FernandoMas_LeonardoOlortegui.git
```

### 2. Crear la base de datos

Crear la base de datos:

```text
BibliotecaDB
```

y ejecutar los scripts SQL incluidos en el proyecto.

### 3. Configurar la conexión

Modificar la cadena de conexión del archivo:

```text
Biblioteca.WPF/App.config
```

según la instancia de SQL Server utilizada.

### 4. Ejecutar la solución

Abrir la solución en Visual Studio y establecer:

```text
Biblioteca.WPF
```

como proyecto de inicio.

Finalmente, ejecutar la aplicación.

---

# 🛠️ Tecnologías utilizadas

* C#
* .NET
* WPF
* SQL Server
* ADO.NET
* `async/await`
* Arquitectura por capas
* Programación orientada a objetos

