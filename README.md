# Experiencia Interactiva sobre Grooming en Roblox

## Descripción del Proyecto

Este proyecto consiste en el desarrollo de una experiencia interactiva en primera persona que recrea situaciones de **grooming** dentro de la plataforma Roblox, desde la perspectiva del depredador.

La propuesta busca evidenciar la facilidad con la que operan estos individuos en entornos digitales y demostrar la vulnerabilidad de los menores en este tipo de plataformas.

Se trata de una experiencia narrativa basada en decisiones, diseñada con un enfoque educativo y preventivo dirigido principalmente a padres de familia.



## Enfoque del Proyecto

El proyecto adopta una perspectiva poco convencional al colocar al usuario en el rol del agresor, con el fin de:

* Exponer las estrategias utilizadas en el grooming digital
* Generar conciencia sobre los riesgos en plataformas interactivas
* Promover la prevención a través de la comprensión del problema

Forma parte de una propuesta multimedia que combina investigación, narrativa interactiva y diseño de experiencias.



## Tecnologías Utilizadas

* Unity 6 (versión 6000.4.1f1)
* C#
* Blender



## Mecánicas de Juego

La experiencia se desarrolla como una narrativa interactiva con exploración y toma de decisiones.

### Controles

| Acción          | Control         |
| --------------- | --------------- |
| Movimiento      | `W` `A` `S` `D` |
| Mirar           | Mouse           |
| Saltar          | `Espacio`       |
| Correr          | `Shift`         |
| Interactuar     | `E`             |
| Avanzar diálogo | Clic izquierdo  |
| Elegir opción   | Clic en botón   |



## Estructura de la Experiencia

La experiencia se divide en **4 días** con un total de **12 momentos de decisión**.

Cada decisión afecta el progreso mediante un sistema de puntos:

| Tipo de decisión | Color    | Puntos       |
| ---------------- | -------- | ------------ |
| Vulnerable       | 🟢 Verde | +2 Confianza |
| Neutra           | ⚪ Gris   | +1 Confianza |
| Extraña          | 🔴 Rojo  | +2 Riesgo    |



### Flujo General


Menú de inicio
→ Día 1 (momentos 1–3)
→ Día 2 (momentos 4–6)
→ Día 3 (momentos 7–9)
→ Día 4 (momentos 10–12)
→ Video final
→ Pantalla de retroalimentación
```

Cada día incluye interacciones con NPCs, decisiones y elementos de exploración.


## Mecánicas Específicas

### Interacción con NPCs

* El jugador debe interactuar con personajes para avanzar
* Cada interacción presenta 3 opciones de decisión
* Las decisiones afectan el resultado final

### Recolección de Monedas

* Ocurre entre momentos específicos (1→2 y 4→5)
* Se deben recolectar **3 monedas** para continuar
* Si no se completan, aparece un mensaje de bloqueo

### Transiciones entre Días

* Fundido a negro
* Aparición de fecha
* Cambio de escenario y NPC
* Reubicación del jugador

### Chat de Celular (Momentos 11 y 12)

* Interfaz estilo iMessage
* Respuestas del jugador y NPC en formato de chat
* Integrado sobre el entorno 3D


## Finales Posibles

| Final               | Condición          |
| ------------------- | ------------------ |
| Final 1 — Secuestro | Confianza mayor Riesgo |
| Final 2 — Policía   | Riesgo mayor Confianza |

El sistema permite diferentes rutas, aunque incluso decisiones ambiguas favorecen la confianza.



## Escenas del Proyecto

| Escena          | Descripción                    |
| --------------- | ------------------------------ |
| MenuInicio      | Pantalla inicial               |
| EscenaPrincipal | Desarrollo de la experiencia   |
| Final_1         | Cinemática + retroalimentación |
| Final_2         | Cinemática + retroalimentación |



## Scripts Principales

* GameManager.cs → Control general del sistema
* SistemaDialogo.cs → Diálogos principales
* DialogoCelular.cs → Sistema de chat
* TransicionDia.cs → Cambios entre días
* RecolectorMonedas.cs → Sistema de recolección
* Moneda.cs → Comportamiento de monedas
* UIRetroalimentacion.cs → Resultados finales
* MapaDecisiones.cs → Visualización del recorrido
* MenuInicio.cs → Menú principal


## Estado del Proyecto

En desarrollo — Prototipo 1


## Integrantes

* Stephano Pinto
* Paulina Sevilla



