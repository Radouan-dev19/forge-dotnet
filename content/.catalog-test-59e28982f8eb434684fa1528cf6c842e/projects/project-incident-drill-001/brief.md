# Exercice d'incident simulé

Un service simulé tombe en panne à une minute connue de lui seul, et votre code conduit l'incident
de bout en bout : détecter sur les signaux, appliquer une atténuation contre le service, constater
le rétablissement en relisant ses signaux, puis rendre compte en faits mesurés. La simulation est
déterministe — c'est ce qui rend l'exercice corrigible — mais votre code ne connaît ni la nature ni
l'instant de la panne : il ne voit que ce que les signaux montrent.

## Ce qui vous est fourni

Le squelette contient `SimulatedService` : construit avec un plan de panne `nature@minute` —
`deploiement` ou `charge` — il rend par `Signal(minute)` une mesure `minute:latenceMs:erreurs`, et
reçoit par `Apply(action, minute)` une tentative d'atténuation. L'action `retour-arriere` corrige
une panne de déploiement, `montee-en-charge` corrige une panne de charge ; l'effet devient visible
sur les signaux **deux minutes** après l'application, et une action inadaptée ne corrige rien.
**Ne modifiez pas ce service** : c'est lui qui joue la panne.

Un dépassement est une mesure dont les erreurs atteignent 20 ou dont la latence atteint 800 ms.

Vous pouvez ajouter jusqu'à trois fichiers à côté du rendu.

## Le contrat

```csharp
public static string DetectStart(string telemetry);
public static string RunDrill(string schedule, string action, int lastMinute);
public static string Postmortem(string telemetry, string action);
```

### `DetectStart`

`telemetry` est une suite de mesures `minute:latenceMs:erreurs` jointes par `;`. L'incident
commence à la première minute d'un dépassement **soutenu sur deux points consécutifs** — un pic
isolé n'est pas un incident. Rendez `minute=M;signal=S`, où `S` vaut `erreurs` si le premier point
en dépassement atteint le seuil d'erreurs, sinon `latence`. Sans incident, rendez `aucun`.

### `RunDrill`

Construisez le service avec `schedule`, puis déroulez les minutes de 0 à `lastMinute` incluse :
lisez `Signal`, et à la **première** minute en dépassement, appliquez `action` à cette minute —
c'est la détection d'intervention, sur un seul point, car on n'attend pas une confirmation pour
agir. Continuez à lire : le rétablissement est la première minute postérieure à la détection dont
la mesure est saine. Rendez `detecte=D;retabli=R`, ou `detecte=D;retabli=jamais` si aucune minute
saine n'arrive avant la fin, ou `aucun` si rien ne dépasse.

### `Postmortem`

Depuis la télémétrie complète d'un incident passé : `debut` est la première minute en dépassement,
la durée va du premier au dernier dépassement inclus — les accalmies intermédiaires comptent dans
la fenêtre d'impact —, le signal dominant est celui du premier dépassement, et le pic est le
maximum de **ce** signal sur la fenêtre. Rendez
`debut=D;duree=Xmin;signal=S;pic=P;action=A`, ou `aucun` sans dépassement.

## Ce qui est mesuré

Trois suites d'acceptation, une par jalon, exécutées dans le bac à sable. Les trois doivent être
vertes pour que le projet compte comme livrable vérifié — il satisfait alors l'exigence
**incident simulé** de la porte C.

## Ce qui n'est pas mesuré

La pression du réel : un incident vivant se conduit à plusieurs, sous interruption, avec des
signaux contradictoires. Le laboratoire d'exploitation s'en approche ; cette simulation entraîne le
geste de méthode, et le déclare.
